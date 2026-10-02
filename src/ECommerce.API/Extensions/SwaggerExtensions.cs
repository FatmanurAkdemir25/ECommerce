using Microsoft.OpenApi;

namespace ECommerce.API.Extensions;

public static class SwaggerExtensions //Swagger ekranında Authorize butonu çıkmasını ve JWT token girerek korumalı endpoint'leri test edebilmeni sağlıyo
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services) //extension method. normalde böyle bir method yok kendimiz ekledik.
    {
        services.AddEndpointsApiExplorer(); //API endpoint'lerinin Swagger tarafından keşfedilmesine yardımcı olur.
        services.AddSwaggerGen(options => //swagger/OpenAPI dokümantasyonu oluşturmak için kullanılıyor.
        {
            options.SwaggerDoc("v1", new OpenApiInfo //swagger dökümanının bilgileri
            {
                Title = "E-Ticaret Yönetim Sistemi API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme //burada swagger a diyoruz ki benim API'm JWT Bearer authentication kullanıyo
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token'ı yapıştır (başına 'Bearer ' yazma)."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement //swagger a API endpoint'lerinde Bearer authentication kullanılabilir diyoruz
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }
}