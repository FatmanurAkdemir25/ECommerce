using System.Text.Json;
using ECommerce.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Middleware;
//merkezi hata yönrtim sistemi
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // İstemci bağlantıyı kesti, hata değil
            logger.LogInformation("İstek istemci tarafından iptal edildi.");
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(ex, "Yanıt başladıktan sonra hata oluştu.");
            return;
        }

        ProblemDetails problem;

        switch (ex)
        {
            case ValidationException validationException:
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Doğrulama hatası",
                    Detail = "Bir veya daha fazla alan geçersiz."
                };
                logger.LogWarning("Doğrulama hatası: {@Errors}", errors);
                break;

            case UnauthorizedException:
                problem = Create(StatusCodes.Status401Unauthorized, "Kimlik doğrulama başarısız", ex.Message);
                logger.LogWarning("Kimlik doğrulama hatası: {Message}", ex.Message);
                break;

            case ForbiddenException:
                problem = Create(StatusCodes.Status403Forbidden, "Erişim reddedildi", ex.Message);
                logger.LogWarning("Erişim reddedildi: {Message}", ex.Message);
                break;

            case NotFoundException:
                problem = Create(StatusCodes.Status404NotFound, "Kayıt bulunamadı", ex.Message);
                logger.LogWarning("Kayıt bulunamadı: {Message}", ex.Message);
                break;

            case ConflictException:
                problem = Create(StatusCodes.Status409Conflict, "İş kuralı ihlali", ex.Message);
                logger.LogWarning("İş kuralı ihlali: {Message}", ex.Message);
                break;

            case DbUpdateConcurrencyException:
                problem = Create(StatusCodes.Status409Conflict, "Eşzamanlılık çakışması",
                    "Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin.");
                logger.LogWarning(ex, "Eşzamanlılık çakışması");
                break;

            default:
                logger.LogError(ex, "Beklenmeyen hata");
                problem = Create(StatusCodes.Status500InternalServerError, "Sunucu hatası",
                    environment.IsDevelopment()
                        ? ex.Message
                        : "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                break;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // Çalışma zamanı tipini veriyoruz ki ValidationProblemDetails.errors kaybolmasın
        await context.Response.WriteAsJsonAsync(
            problem, problem.GetType(), (JsonSerializerOptions?)null, "application/problem+json");
    }

    private static ProblemDetails Create(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}