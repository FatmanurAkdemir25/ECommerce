using System.Security.Claims;
using ECommerce.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Authorization;

public class PermissionAuthorizationHandler(
    IUserPermissionService permissionService,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("sub"), out var userId))
        {
            return;
        }

        var ct = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        var permissions = await permissionService.GetPermissionsAsync(userId, ct);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        logger.LogWarning("İzin reddedildi. UserId: {UserId}, Permission: {Permission}",
            userId, requirement.Permission);
    }
}