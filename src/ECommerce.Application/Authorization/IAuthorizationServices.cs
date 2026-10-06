using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Authorization;

public interface IRoleService
{
    Task<PagedResult<RoleDto>> ListAsync(RoleListQuery query, CancellationToken ct);
    Task<RoleDetailDto> GetAsync(Guid id, CancellationToken ct);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct);
    Task RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct);
}

public interface IPermissionAdminService
{
    Task<PagedResult<PermissionDto>> ListAsync(PermissionListQuery query, CancellationToken ct);
    Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken ct);
}

public interface IUserAccessService
{
    Task<UserAccessDto> GetAccessAsync(Guid userId, CancellationToken ct);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task SetPermissionAsync(Guid userId, Guid permissionId, bool isGranted, CancellationToken ct);
    Task RemovePermissionAsync(Guid userId, Guid permissionId, CancellationToken ct);
}