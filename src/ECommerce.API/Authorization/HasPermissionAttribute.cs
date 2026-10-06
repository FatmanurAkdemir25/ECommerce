using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Authorization;

// Kullanım: [HasPermission(Permissions.Products.Create)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission) : base(PolicyPrefix + permission)
    {
    }
}