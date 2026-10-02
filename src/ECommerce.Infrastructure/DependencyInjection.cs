using ECommerce.Application.Abstractions;
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

        services.AddHostedService<DatabaseInitializer>();
        // Redis'e geçerken sadece bu iki satır değişecek
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }
}