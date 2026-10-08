using System.Security.Claims;

namespace ECommerce.API.RateLimiting;

public static class RateLimitPartitionKey
{
    // Giriş yapmışsa kullanıcı, yapmamışsa IP
    public static string For(HttpContext ctx)
    {
        var sub = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ctx.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(sub)
            ? $"user:{sub}"
            : ForIp(ctx);
    }

    public static string ForIp(HttpContext ctx)
        => $"ip:{ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}