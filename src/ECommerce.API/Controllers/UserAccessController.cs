using ECommerce.API.Authorization;
using ECommerce.Application.Authorization;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}")]
[Produces("application/json")]
[HasPermission(Permissions.Users.Manage)]
public class UserAccessController(IUserAccessService accessService) : ControllerBase
{
    // Kullanıcının rolleri, özel izinleri ve etkin izin listesi
    [HttpGet("access")]
    public async Task<ActionResult<UserAccessDto>> GetAccess(Guid userId, CancellationToken ct) =>
        Ok(await accessService.GetAccessAsync(userId, ct));

    [HttpPut("roles/{roleId:guid}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        await accessService.AssignRoleAsync(userId, roleId, ct);
        return NoContent();
    }

    [HttpDelete("roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        await accessService.RemoveRoleAsync(userId, roleId, ct);
        return NoContent();
    }

    // isGranted: true = izin ver, false = engelle (rol iznini geçersiz kılar)
    [HttpPut("permissions/{permissionId:guid}")]
    public async Task<IActionResult> SetPermission(
        Guid userId, Guid permissionId, SetUserPermissionRequest request, CancellationToken ct)
    {
        await accessService.SetPermissionAsync(userId, permissionId, request.IsGranted, ct);
        return NoContent();
    }

    // Özel kaydı siler, kullanıcı tekrar sadece rollerinin izinlerine tabi olur
    [HttpDelete("permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid userId, Guid permissionId, CancellationToken ct)
    {
        await accessService.RemovePermissionAsync(userId, permissionId, ct);
        return NoContent();
    }
}