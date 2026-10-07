namespace ECommerce.Application.Common;

public static class CacheKeys
{
    public static string UserPermissions(Guid userId) => $"permissions:{userId}";
    public const string AllCategories = "categories:all";
    public static string Product(Guid productId) => $"products:{productId}";
}