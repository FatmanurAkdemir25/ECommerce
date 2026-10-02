using ECommerce.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;
//Uygulama açılırken veritabanını hazırla ve gerekli başlangıç verilerini (seed) ekle
public class DatabaseInitializer(
    IServiceScopeFactory scopeFactory, //Dependency Injection'dan yeni bir scope oluşturmak için
    IConfiguration config,//.env gibi configuration kaynaklarından ayar okumak için kullanılıyo
    ILogger<DatabaseInitializer> logger) : IHostedService//Migration ve seed sırasında log yazmak için
{
    public async Task StartAsync(CancellationToken ct)//uygulama başlarken çalışır. CancellationToken ct işlemin iptal edilmesi durumunda kullanılan token
    {
        using var scope = scopeFactory.CreateScope();//scope oluştur
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();//AppDbContext i iste yoksa hata ver

        await db.Database.MigrateAsync(ct); //migrationları vt ye uygular
        await DbSeeder.SeedAsync(db, config, logger, ct);//burada seed veritabanına başlangıç/gerekli verileri otomatik ekler
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;//uygulamayı kapatırken özel bir işlem yapmıyoruz
}