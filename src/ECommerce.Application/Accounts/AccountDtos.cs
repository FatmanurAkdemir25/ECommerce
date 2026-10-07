using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Accounts;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

// E-posta değiştirilemez
public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = [];
}

// SortBy: name | email | date
public class UserListQuery : PagedQuery
{
    // E-posta veya ad soyad içinde arar
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class AddressDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class CreateAddressRequest
{
    public string? Title { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;

    // İlk adres her zaman varsayılan olur
    public bool IsDefault { get; set; }
}

// Varsayılan adres değişimi ayrı endpoint'le yapılır
public class UpdateAddressRequest
{
    public string? Title { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
}