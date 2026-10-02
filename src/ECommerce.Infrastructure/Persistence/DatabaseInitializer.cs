using ECommerce.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;
//uygulama başlarken veritabanını hazırlar
public class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 

        await db.Database.MigrateAsync(ct); //migretion ları uygular
        await DbSeeder.SeedAsync(db, config, logger, ct); //seed işlemini başlatır
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}