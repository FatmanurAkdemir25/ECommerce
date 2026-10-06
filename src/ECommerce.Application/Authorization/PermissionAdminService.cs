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

public class PermissionAdminService(
    IAppDbContext db,
    IMapper mapper,
    IUserPermissionService permissionService,
    ILogger<PermissionAdminService> logger) : IPermissionAdminService
{
    public async Task<PagedResult<PermissionDto>> ListAsync(PermissionListQuery query, CancellationToken ct)
    {
        var permissions = db.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            permissions = permissions.Where(p => p.Name.Contains(search));
        }

        var total = await permissions.CountAsync(ct);

        permissions = query.IsDescending
            ? permissions.OrderByDescending(p => p.Name)
            : permissions.OrderBy(p => p.Name);

        var items = await permissions.Skip(query.Skip).Take(query.PageSize)
            .ProjectTo<PermissionDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return PagedResult<PermissionDto>.Create(items, total, query);
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Permissions.AnyAsync(p => p.Name == name, ct))
        {
            throw new ConflictException($"'{name}' adında bir izin zaten var.");
        }

        var permission = new Permission
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        db.Permissions.Add(permission);

        // Admin her zaman tüm izinlere sahip olur
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Admin, ct);
        if (adminRole is not null)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permission.Id });
        }

        await db.SaveChangesAsync(ct);

        if (adminRole is not null)
        {
            await permissionService.InvalidateRoleUsersAsync(adminRole.Id, ct);
        }

        logger.LogInformation("İzin oluşturuldu. PermissionId: {PermissionId}", permission.Id);
        return mapper.Map<PermissionDto>(permission);
    }
}