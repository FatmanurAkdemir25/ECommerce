using ECommerce.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

// Giriş yapmış kullanıcının KENDİ profili ve adresleri. Kullanıcı Id'si token'dan gelir.
[ApiController]
[Route("api/users/me")]
[Produces("application/json")]
[Authorize]
public class ProfileController(IProfileService profileService, IAddressService addressService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken ct) =>
        Ok(await profileService.GetMyProfileAsync(ct));

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await profileService.UpdateMyProfileAsync(request, ct));

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> ListAddresses(CancellationToken ct) =>
        Ok(await addressService.ListMineAsync(ct));

    [HttpGet("addresses/{id:guid}")]
    public async Task<ActionResult<AddressDto>> GetAddress(Guid id, CancellationToken ct) =>
        Ok(await addressService.GetMineAsync(id, ct));

    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> CreateAddress(CreateAddressRequest request, CancellationToken ct)
    {
        var address = await addressService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, address);
    }

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        Guid id, UpdateAddressRequest request, CancellationToken ct) =>
        Ok(await addressService.UpdateAsync(id, request, ct));

    [HttpPut("addresses/{id:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken ct)
    {
        await addressService.SetDefaultAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        await addressService.DeleteAsync(id, ct);
        return NoContent();
    }
}