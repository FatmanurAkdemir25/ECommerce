using ECommerce.API.Authorization;
using ECommerce.Application.Authorization;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/roles")]
[Produces("application/json")]
[HasPermission(Permissions.Roles.Manage)]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RoleDto>>> List([FromQuery] RoleListQuery query, CancellationToken ct) =>
        Ok(await roleService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await roleService.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var role = await roleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = role.Id }, role);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, UpdateRoleRequest request, CancellationToken ct) =>
        Ok(await roleService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await roleService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> AddPermission(Guid id, Guid permissionId, CancellationToken ct)
    {
        await roleService.AddPermissionAsync(id, permissionId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken ct)
    {
        await roleService.RemovePermissionAsync(id, permissionId, ct);
        return NoContent();
    }
}