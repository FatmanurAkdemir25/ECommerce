using System.Text;
using ECommerce.Application.Abstractions;
using ECommerce.Infrastructure.Auth;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection bulunamadı.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHostedService<DatabaseInitializer>();

        // Redis'e geçerken sadece bu iki satır değişecek
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => Encoding.UTF8.GetByteCount(s.Key) >= 32,
                "Jwt:Key en az 32 karakter olmalı (user-secrets veya ortam değişkeni ile verin).")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
                "Jwt:Issuer ve Jwt:Audience zorunludur.")
            .Validate(s => s.AccessTokenMinutes > 0 && s.RefreshTokenDays > 0,
                "Jwt süreleri sıfırdan büyük olmalı.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}