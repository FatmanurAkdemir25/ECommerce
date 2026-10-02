using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class DependencyInjection //Application katmanındaki FluentValidation ve AutoMapper yapılandırmasını tek bir yerde topluyo
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Bu assembly'deki tüm validator'lar otomatik bulunur
        services.AddValidatorsFromAssembly(assembly);

        // Bu assembly'deki tüm AutoMapper Profile'ları otomatik bulunur
        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = configuration["AutoMapper:LicenseKey"];
        }, assembly);

        return services;
    }
}