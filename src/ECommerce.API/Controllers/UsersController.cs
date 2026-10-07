using ECommerce.API.Authorization;
using ECommerce.Application.Accounts;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

// Personelin kullanıcıları ve adreslerini GÖRMESİ (salt okunur). Admin ve Sales'te Users.ViewAll var.
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[HasPermission(Permissions.Users.ViewAll)]
public class UsersController(IUserDirectoryService directory, IAddressService addressService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> List(
        [FromQuery] UserListQuery query, CancellationToken ct) =>
        Ok(await directory.ListAsync(query, ct));

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid userId, CancellationToken ct) =>
        Ok(await directory.GetAsync(userId, ct));

    [HttpGet("{userId:guid}/addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> ListAddresses(Guid userId, CancellationToken ct) =>
        Ok(await addressService.ListForUserAsync(userId, ct));
}