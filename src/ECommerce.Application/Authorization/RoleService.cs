using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Authorization;

public class RoleService(
    IAppDbContext db,
    IMapper mapper,
    IUserPermissionService permissionService,
    ILogger<RoleService> logger) : IRoleService
{
    public async Task<PagedResult<RoleDto>> ListAsync(RoleListQuery query, CancellationToken ct)
    {
        var roles = db.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            roles = roles.Where(r => r.Name.Contains(search));
        }

        var total = await roles.CountAsync(ct);

        roles = query.IsDescending ? roles.OrderByDescending(r => r.Name) : roles.OrderBy(r => r.Name);
        var items = await roles.Skip(query.Skip).Take(query.PageSize)
            .ProjectTo<RoleDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return PagedResult<RoleDto>.Create(items, total, query);
    }

    public async Task<RoleDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking()
            .Where(r => r.Id == id)
            .ProjectTo<RoleDetailDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return role ?? throw new NotFoundException("Rol", id);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(r => r.Name == name, ct))
        {
            throw new ConflictException($"'{name}' adında bir rol zaten var.");
        }

        var role = new Role { Name = name, Description = CleanText(request.Description) };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Rol oluşturuldu. RoleId: {RoleId}", role.Id);
        return mapper.Map<RoleDto>(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Rol", id);

        var name = request.Name.Trim();

        if (RoleNames.IsSystem(role.Name) && !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Sistem rollerinin adı değiştirilemez.");
        }

        if (await db.Roles.AnyAsync(r => r.Id != id && r.Name == name, ct))
        {
            throw new ConflictException($"'{name}' adında bir rol zaten var.");
        }

        role.Name = name;
        role.Description = CleanText(request.Description);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Rol güncellendi. RoleId: {RoleId}", role.Id);
        return mapper.Map<RoleDto>(role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Rol", id);

        if (RoleNames.IsSystem(role.Name))
        {
            throw new ConflictException("Sistem rolleri silinemez.");
        }

        var userCount = await db.UserRoles.CountAsync(ur => ur.RoleId == id, ct);
        if (userCount > 0)
        {
            throw new ConflictException($"Bu role atanmış {userCount} kullanıcı var. Önce kullanıcılardan kaldırın.");
        }

        // Rolün kullanıcısı olmadığı için temizlenecek cache yok
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Rol silindi. RoleId: {RoleId}", id);
    }

    public async Task AddPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        await EnsureEditableRoleAndPermissionExistAsync(roleId, permissionId, ct);

        var exists = await db.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);
        if (exists) return; // idempotent

        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await db.SaveChangesAsync(ct);

        // Role sahip TÜM kullanıcıların cache'i temizlenir
        await permissionService.InvalidateRoleUsersAsync(roleId, ct);
        logger.LogInformation("Role izin eklendi. RoleId: {RoleId}, PermissionId: {PermissionId}", roleId, permissionId);
    }

    public async Task RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        await EnsureEditableRoleAndPermissionExistAsync(roleId, permissionId, ct);

        var link = await db.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);
        if (link is null) return; // idempotent

        db.RolePermissions.Remove(link);
        await db.SaveChangesAsync(ct);

        await permissionService.InvalidateRoleUsersAsync(roleId, ct);
        logger.LogInformation("Rolden izin kaldırıldı. RoleId: {RoleId}, PermissionId: {PermissionId}", roleId, permissionId);
    }

    private async Task EnsureEditableRoleAndPermissionExistAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException("Rol", roleId);

        if (string.Equals(role.Name, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Admin rolünün izinleri değiştirilemez. Admin her zaman tüm izinlere sahiptir.");
        }

        if (!await db.Permissions.AnyAsync(p => p.Id == permissionId, ct))
        {
            throw new NotFoundException("İzin", permissionId);
        }
    }

    private static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}