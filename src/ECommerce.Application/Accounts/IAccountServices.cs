using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Accounts;

public interface IProfileService
{
    Task<ProfileDto> GetMyProfileAsync(CancellationToken ct);
    Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken ct);
}

// Personelin kullanıcıları görmesi (salt okunur)
public interface IUserDirectoryService
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken ct);
    Task<UserSummaryDto> GetAsync(Guid userId, CancellationToken ct);
}

public interface IAddressService
{
    // Token'daki kullanıcının kendi adresleri
    Task<IReadOnlyList<AddressDto>> ListMineAsync(CancellationToken ct);
    Task<AddressDto> GetMineAsync(Guid id, CancellationToken ct);
    Task<AddressDto> CreateAsync(CreateAddressRequest request, CancellationToken ct);
    Task<AddressDto> UpdateAsync(Guid id, UpdateAddressRequest request, CancellationToken ct);
    Task SetDefaultAsync(Guid id, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    // Personel: başka bir kullanıcının adresleri (salt okunur)
    Task<IReadOnlyList<AddressDto>> ListForUserAsync(Guid userId, CancellationToken ct);
}