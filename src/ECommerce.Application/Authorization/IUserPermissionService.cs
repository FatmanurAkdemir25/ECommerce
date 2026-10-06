namespace ECommerce.Application.Authorization;

public interface IUserPermissionService
{
    // Önce cache'e bakar, yoksa DB'den hesaplayıp cache'ler
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct);

    Task InvalidateUserAsync(Guid userId, CancellationToken ct);

    // Role sahip tüm kullanıcıların cache'ini temizler
    Task InvalidateRoleUsersAsync(Guid roleId, CancellationToken ct);
}