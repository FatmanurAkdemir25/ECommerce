using System.Collections.Frozen;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Authorization;

public class UserPermissionService(IAppDbContext db, ICacheService cache) : IUserPermissionService
{
    // Asıl temizleme invalidation ile olur, süre sadece güvenlik ağıdır
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct) =>
        cache.GetOrCreateAsync<IReadOnlySet<string>>(
            CacheKeys.UserPermissions(userId),
            c => LoadAsync(userId, c),
            CacheDuration,
            ct);

    public Task InvalidateUserAsync(Guid userId, CancellationToken ct) =>
        cache.RemoveAsync(CacheKeys.UserPermissions(userId), ct);

    public async Task InvalidateRoleUsersAsync(Guid roleId, CancellationToken ct)
    {
        var userIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            await cache.RemoveAsync(CacheKeys.UserPermissions(userId), ct);
        }
    }

    private async Task<IReadOnlySet<string>> LoadAsync(Guid userId, CancellationToken ct)
    {
        // Pasif veya silinmiş kullanıcının hiçbir izni yoktur
        var isActive = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive, ct);
        if (!isActive) return FrozenSet<string>.Empty;

        var rolePermissions = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);

        var overrides = await db.UserPermissions.AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => new UserPermissionOverride(up.Permission.Name, up.IsGranted))
            .ToListAsync(ct);

        return EffectivePermissionCalculator.Calculate(rolePermissions, overrides);
    }
}