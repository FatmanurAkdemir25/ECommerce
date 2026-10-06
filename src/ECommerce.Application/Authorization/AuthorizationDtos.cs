using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Authorization;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<string> Permissions { get; set; } = [];
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreatePermissionRequest
{
    // Kaynak.Eylem formatı, örn. Reports.View
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SetUserPermissionRequest
{
    // true = izin ver, false = engelle (rol iznini geçersiz kılar)
    public bool IsGranted { get; set; }
}

// SortBy şimdilik sadece "name" destekler
public class RoleListQuery : PagedQuery
{
    public string? Search { get; set; }
}

public class PermissionListQuery : PagedQuery
{
    public string? Search { get; set; }
}

public class UserPermissionOverrideDto
{
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}

public class UserAccessDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<UserPermissionOverrideDto> Overrides { get; set; } = [];
    public List<string> EffectivePermissions { get; set; } = [];
}