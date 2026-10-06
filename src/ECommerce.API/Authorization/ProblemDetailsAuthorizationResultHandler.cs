using ECommerce.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ECommerce.API.Authorization;

// 401 ve 403 yanıtlarını standart ProblemDetails formatına çevirir
public class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await ProblemResponseWriter.WriteAsync(context, StatusCodes.Status401Unauthorized,
                "Kimlik doğrulama gerekli", "Geçerli bir access token göndermelisiniz.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await ProblemResponseWriter.WriteAsync(context, StatusCodes.Status403Forbidden,
                "Erişim reddedildi", "Bu işlem için yetkiniz yok.");
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}