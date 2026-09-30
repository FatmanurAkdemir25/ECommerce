using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Permission : BaseEntity
{
    // Örn: Orders.Create
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}