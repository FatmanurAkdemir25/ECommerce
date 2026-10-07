using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Accounts;

// "me" uç noktaları sadece [Authorize] ile korunur. Access token 15 dk geçerli olduğu için
// hesabın o an hâlâ aktif olduğu burada ayrıca doğrulanır.
internal static class UserGuard
{
    public static async Task<Guid> RequireActiveUserAsync(
        ICurrentUserService currentUser, IAppDbContext db, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Kimlik doğrulanamadı.");

        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive, ct))
        {
            throw new UnauthorizedException("Hesap bulunamadı veya pasif.");
        }

        return userId;
    }
}