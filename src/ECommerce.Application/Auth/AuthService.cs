using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
        }

        var customerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Customer, ct)
            ?? throw new InvalidOperationException("Customer rolü bulunamadı. Seed verisi yüklenmemiş olabilir.");

        var now = Now();
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            CreatedAt = now
        };

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id });
        var (response, _) = CreateSession(user, now);

        // Kullanıcı, rol ataması ve refresh token tek SaveChanges içinde, yani tek transaction'da yazılır
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Yeni kullanıcı kaydedildi. UserId: {UserId}", user.Id);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Hangi alanın yanlış olduğunu belli etmiyoruz (kullanıcı keşfini önlemek için)
        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Başarısız giriş denemesi.");
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        var (response, _) = CreateSession(user, Now());
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Kullanıcı giriş yaptı. UserId: {UserId}", user.Id);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hash, ct);

        if (stored is null)
        {
            throw new UnauthorizedException("Geçersiz refresh token.");
        }

        var now = Now();
        DateTime? revokedAt = now;

        if (stored.RevokedAt is not null)
        {
            // İptal edilmiş token tekrar kullanıldı: olası çalınma. Kullanıcının tüm aktif oturumlarını kapat.
            logger.LogWarning("İptal edilmiş refresh token tekrar kullanıldı, tüm oturumlar kapatılıyor. UserId: {UserId}",
                stored.UserId);

            await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt), ct);

            throw new UnauthorizedException("Refresh token iptal edilmiş.");
        }

        if (stored.ExpiresAt <= now)
        {
            throw new UnauthorizedException("Refresh token süresi dolmuş.");
        }

        if (!stored.User.IsActive)
        {
            throw new UnauthorizedException("Hesap pasif.");
        }

        var (response, newToken) = CreateSession(stored.User, now);

        // Eski token'ı tek bir atomik UPDATE ile iptal et. Aynı token aynı anda iki kez gelirse
        // sadece biri kazanır, diğerinin etkilenen satır sayısı 0 olur.
        var replacedBy = newToken.Token;
        var revoked = await db.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, revokedAt)
                .SetProperty(t => t.ReplacedByToken, replacedBy), ct);

        if (revoked == 0)
        {
            throw new UnauthorizedException("Refresh token zaten kullanılmış.");
        }

        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        DateTime? revokedAt = Now();

        // Tekrarlanabilir (idempotent): token yoksa veya zaten iptal edilmişse de başarılı sayılır
        await db.RefreshTokens
            .Where(t => t.Token == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt), ct);
    }

    // Access token üretir, yeni refresh token'ı (hash'li hâliyle) context'e ekler
    private (AuthResponse Response, RefreshToken Entity) CreateSession(User user, DateTime now)
    {
        var access = tokenService.CreateAccessToken(user);
        var rawRefresh = tokenService.CreateRefreshTokenValue();

        var entity = new RefreshToken
        {
            UserId = user.Id,
            Token = tokenService.HashToken(rawRefresh),
            CreatedAt = now,
            ExpiresAt = now.Add(tokenService.RefreshTokenLifetime)
        };
        db.RefreshTokens.Add(entity);

        var response = new AuthResponse
        {
            AccessToken = access.Token,
            AccessTokenExpiresAt = access.ExpiresAt,
            RefreshToken = rawRefresh,
            RefreshTokenExpiresAt = entity.ExpiresAt
        };

        return (response, entity);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}