using ECommerce.Application.Abstractions;
using ECommerce.Application.Authorization;

namespace ECommerce.Application.Catalog;

// Pasif kategori/ürünleri sadece ilgili yazma iznine sahip personel görebilir.
// Token geçerliyse kullanıcı, herkese açık endpoint'lerde de tanınır.
public class CatalogVisibility(ICurrentUserService currentUser, IUserPermissionService permissionService)
{
    public async Task<bool> CanSeeInactiveAsync(string permission, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return false;

        var permissions = await permissionService.GetPermissionsAsync(userId, ct);
        return permissions.Contains(permission);
    }
}