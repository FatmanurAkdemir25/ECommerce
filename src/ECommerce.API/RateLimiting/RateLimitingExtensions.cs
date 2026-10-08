using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.RateLimiting;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                       ?? new RateLimitingOptions();
        settings.EnsureValid();                     // hatalı ayarda uygulama açılmaz
        services.AddSingleton(settings);

        if (!settings.Enabled) return services;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Her istek için genel limit (Swagger hariç)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/swagger"))
                    return RateLimitPartition.GetNoLimiter("swagger");

                return RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitionKey.For(ctx),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Global.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Global.WindowSeconds),
                        QueueLimit = 0
                    });
            });

            // register / login / refresh için daha sıkı politika
            options.AddPolicy(AuthPolicy, ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    RateLimitPartitionKey.ForIp(ctx),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Auth.WindowSeconds),
                        SegmentsPerWindow = settings.Auth.SegmentsPerWindow,
                        QueueLimit = 0
                    }));

            options.OnRejected = WriteRejectionAsync;
        });

        return services;
    }

    public static WebApplication UseApiRateLimiting(this WebApplication app)
    {
        if (app.Services.GetRequiredService<RateLimitingOptions>().Enabled)
            app.UseRateLimiter();
        return app;
    }

    // Test edilebilsin diye public
    public static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var http = context.HttpContext;
        int? retryAfterSeconds = null;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            http.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
        }

        http.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("RateLimiting")
            .LogWarning("Rate limit aşıldı: {Method} {Path} partition={Partition}",
                http.Request.Method, http.Request.Path, RateLimitPartitionKey.For(http));

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await http.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Çok fazla istek",
            Type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            Detail = retryAfterSeconds is null
                ? "Çok fazla istek gönderdiniz. Lütfen biraz sonra tekrar deneyin."
                : $"Çok fazla istek gönderdiniz. {retryAfterSeconds} saniye sonra tekrar deneyin."
        }, options: null, contentType: "application/problem+json", cancellationToken: ct);
    }
}