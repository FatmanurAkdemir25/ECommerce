using ECommerce.API.Extensions;
using ECommerce.API.Filters;
using ECommerce.API.Middleware;
using ECommerce.Application;
using ECommerce.Infrastructure;
using Microsoft.Extensions.Hosting;
using Serilog;

// Uygulama ayağa kalkarken oluşan hataları da yakalamak için geçici logger
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddJwtAuthentication(); 

    var app = builder.Build();

    // Sıra önemli: correlation ID → request logging → hata yakalama
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException: 'dotnet ef' komutları uygulamayı bilerek durdurur, hata değil
    Log.Fatal(ex, "Uygulama beklenmedik şekilde sonlandı.");
}
finally
{
    Log.CloseAndFlush();
}