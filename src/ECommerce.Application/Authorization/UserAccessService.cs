using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Authorization;

public class UserAccessService(
    IAppDbContext db,
    IUserPermissionService permissionService,
    ICurrentUserService currentUser,
    ILogger<UserAccessService> logger) : IUserAccessService
{
    public async Task<UserAccessDto> GetAccessAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email, u.FullName })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Kullanıcı", userId);

        var roles = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .OrderBy(n => n)
            .ToListAsync(ct);

        var overrides = await db.UserPermissions.AsNoTracking()
            .Where(up => up.UserId == userId)
            .OrderBy(up => up.Permission.Name)
            .Select(up => new UserPermissionOverrideDto
            {
                PermissionId = up.PermissionId,
                PermissionName = up.Permission.Name,
                IsGranted = up.IsGranted
            })
            .ToListAsync(ct);

        var effective = await permissionService.GetPermissionsAsync(userId, ct);

        return new UserAccessDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Overrides = overrides,
            EffectivePermissions = effective.OrderBy(p => p).ToList()
        };
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        EnsureNotSelf(userId);
        await EnsureUserAndRoleExistAsync(userId, roleId, ct);

        var exists = await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        if (!exists)
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await db.SaveChangesAsync(ct);
        }

        await permissionService.InvalidateUserAsync(userId, ct);
        logger.LogInformation("Kullanıcıya rol atandı. UserId: {UserId}, RoleId: {RoleId}, ActorId: {ActorId}",
            userId, roleId, currentUser.UserId);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        EnsureNotSelf(userId);
        await EnsureUserAndRoleExistAsync(userId, roleId, ct);

        var link = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        if (link is not null)
        {
            db.UserRoles.Remove(link);
            await db.SaveChangesAsync(ct);
        }

        await permissionService.InvalidateUserAsync(userId, ct);
        logger.LogInformation("Kullanıcıdan rol kaldırıldı. UserId: {UserId}, RoleId: {RoleId}, ActorId: {ActorId}",
            userId, roleId, currentUser.UserId);
    }

    public async Task SetPermissionAsync(Guid userId, Guid permissionId, bool isGranted, CancellationToken ct)
    {
        EnsureNotSelf(userId);
        await EnsureUserAndPermissionExistAsync(userId, permissionId, ct);

        var entry = await db.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, ct);

        if (entry is null)
        {
            db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted
            });
        }
        else
        {
            entry.IsGranted = isGranted;
        }

        await db.SaveChangesAsync(ct);
        await permissionService.InvalidateUserAsync(userId, ct);

        logger.LogInformation(
            "Kullanıcıya özel izin ayarlandı. UserId: {UserId}, PermissionId: {PermissionId}, IsGranted: {IsGranted}, ActorId: {ActorId}",
            userId, permissionId, isGranted, currentUser.UserId);
    }

    public async Task RemovePermissionAsync(Guid userId, Guid permissionId, CancellationToken ct)
    {
        EnsureNotSelf(userId);
        await EnsureUserAndPermissionExistAsync(userId, permissionId, ct);

        var entry = await db.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, ct);

        if (entry is not null)
        {
            db.UserPermissions.Remove(entry);
            await db.SaveChangesAsync(ct);
        }

        await permissionService.InvalidateUserAsync(userId, ct);
        logger.LogInformation(
            "Kullanıcıya özel izin kaldırıldı. UserId: {UserId}, PermissionId: {PermissionId}, ActorId: {ActorId}",
            userId, permissionId, currentUser.UserId);
    }

    private void EnsureNotSelf(Guid userId)
    {
        if (currentUser.UserId == userId)
        {
            throw new ForbiddenException("Kendi hesabınızın rollerini veya izinlerini değiştiremezsiniz.");
        }
    }

    private async Task EnsureUserAndRoleExistAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) throw new NotFoundException("Kullanıcı", userId);
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct)) throw new NotFoundException("Rol", roleId);
    }

    private async Task EnsureUserAndPermissionExistAsync(Guid userId, Guid permissionId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) throw new NotFoundException("Kullanıcı", userId);
        if (!await db.Permissions.AnyAsync(p => p.Id == permissionId, ct)) throw new NotFoundException("İzin", permissionId);
    }
}