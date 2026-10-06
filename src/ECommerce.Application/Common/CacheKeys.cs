namespace ECommerce.Application.Common;

public static class CacheKeys
{
    public static string UserPermissions(Guid userId) => $"permissions:{userId}";
}