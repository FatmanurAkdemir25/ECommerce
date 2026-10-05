using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessTokenResult CreateAccessToken(User user);

    // Kriptografik olarak güvenli rastgele ham token (istemciye gider)
    string CreateRefreshTokenValue();

    // Veritabanında saklanan hâli (SHA-256)
    string HashToken(string token);
}