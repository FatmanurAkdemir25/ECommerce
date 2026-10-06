using ECommerce.API.Authorization;
using ECommerce.Application.Authorization;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Produces("application/json")]
[HasPermission(Permissions.PermissionAdmin.Manage)]
public class PermissionsController(IPermissionAdminService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PermissionDto>>> List(
        [FromQuery] PermissionListQuery query, CancellationToken ct) =>
        Ok(await permissionService.ListAsync(query, ct));

    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create(CreatePermissionRequest request, CancellationToken ct)
    {
        var permission = await permissionService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, permission);
    }
}