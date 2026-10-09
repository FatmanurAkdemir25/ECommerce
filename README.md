# ECommerce API

Katmanlı mimariyle yazılmış .NET 10 e-ticaret yönetim API'si: JWT kimlik doğrulama, izin tabanlı yetkilendirme, ürün/stok defteri, sepet, sipariş ve ödeme durumu takibi.

## Teknolojiler
.NET 10, ASP.NET Core Web API, EF Core + SQL Server 2022, FluentValidation, AutoMapper, Serilog, Swagger (JWT destekli), xUnit.

## Mimari
Domain Entity, enum, sabitler (izinler, roller), iş kuralları (OrderStatusRules)
Application Servisler, DTO’lar, validator’lar, soyutlamalar (IAppDbContext, ICacheService)
Infrastructure EF Core, migration, seed, JWT/şifre/cache uygulamaları
API Controller’lar, middleware’ler (hata, correlation id, rate limit), Swagger

Application katmanı veritabanına `IAppDbContext` soyutlaması üzerinden erişir.

## Çalıştırma

### Docker ile (önerilen)
```powershell
Copy-Item .env.example .env      # değerleri doldur (SA_PASSWORD, SEED_ADMIN_*, JWT_KEY)
docker compose up --build
```
Migration ve seed uygulama açılırken otomatik çalışır. Swagger: `http://localhost:<API_PORTU>/swagger` (port `docker-compose.yml`'de).

### Yerel geliştirme
SQL Server'ı docker ile (host portu 14333) çalıştırıp bağlantı bilgilerini user-secrets'a yaz:
```powershell
cd src/ECommerce.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,14333;Database=ECommerceDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True"
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "<GÜÇLÜ_ŞİFRE>"
dotnet user-secrets set "Jwt:Key" "<EN_AZ_32_KARAKTERLİK_GİZLİ_ANAHTAR>"
dotnet run
```

### Testler
```powershell
dotnet test
```

## Yapılandırma
| Anahtar | Açıklama |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server bağlantısı |
| `Jwt:Key / Issuer / Audience` | Token imzası. `AccessTokenMinutes` (15), `RefreshTokenDays` (7) |
| `SeedAdmin:Email / Password` | İlk açılışta oluşturulan admin |
| `RateLimiting:Enabled` | İstek sınırlamayı aç/kapat |
| `RateLimiting:Global` | `PermitLimit` / `WindowSeconds` (varsayılan 100 / 60) |
| `RateLimiting:Auth` | register/login/refresh limiti (varsayılan 5 / 60) |

Ortam değişkenleriyle ezmek için `__` kullan: `RateLimiting__Auth__PermitLimit=10`.

## Kimlik doğrulama ve yetkilendirme
- Access token (15 dk) + refresh token (7 gün, hash'i saklanır, her yenilemede döner, yeniden kullanım tespit edilirse tüm oturumlar kapatılır).
- İzinler token'da değil veritabanındadır. Etkin izin = kullanıcıya özel *yasak* > kullanıcıya özel *izin* > rol izinleri. Sonuç 10 dk cache'lenir, rol/izin değişince temizlenir.
- Yetkisiz istek 401, izinsiz istek 403 (ProblemDetails).

### Rol ve izin tablosu
| İzin | Admin | Sales | Customer |
|---|:-:|:-:|:-:|
| Categories.Create / Update | ✓ | ✓ | |
| Categories.Delete | ✓ | | |
| Products.Create / Update / AdjustStock | ✓ | ✓ | |
| Products.Delete | ✓ | | |
| Users.ViewAll | ✓ | ✓ | |
| Users.Manage | ✓ | | |
| Carts.Manage | ✓ | | ✓ |
| Orders.Create | ✓ | | ✓ |
| Orders.Cancel | ✓ | ✓ | ✓ (sadece kendi siparişi) |
| Orders.ViewAll / Orders.UpdateStatus | ✓ | ✓ | |
| Payments.UpdateStatus | ✓ | ✓ | |
| Roles.Manage | ✓ | | |
| Permissions.Manage | ✓ | | |

## İş kuralları
- **Stok:** Yalnızca `IStockService` değiştirir. Her hareket bir fiş (`receipts`) ve satır (`product_transactions`) üretir. Atomik `UPDATE ... WHERE stock + delta >= 0` ile eşzamanlı isteklerde stok negatife düşmez. Veritabanında `CHECK (stock >= 0)` vardır.
- **Sepet:** Stok rezervasyonu yapmaz. Satır başına en fazla 100 adet, en fazla 50 ürün.
- **Sipariş:** Tek transaction'da sepet boşaltılır, sipariş ve ödeme oluşur, stok düşer. Fiyat sipariş anında kopyalanır.
- **Durum akışı:** Pending → Confirmed → Shipped → Delivered. Pending/Confirmed → Cancelled (iptalde stok iade edilir). Geçersiz geçiş 409. Onay için ödeme `Paid` olmalıdır.
- **Ödeme:** Gerçek ödeme yoktur, yalnızca durum takibi (Pending → Paid / Cancelled).

## API özeti
Auth (`register`, `login`, `refresh`, `logout`), Roles, Permissions, kullanıcı izinleri, Categories, Products (+ stok düzeltme ve geçmiş), Profile ve Addresses (`api/users/me/...`), Users, Cart (`api/cart`), Orders (`api/orders`). Tüm uçlar Swagger'da listelenir.

## Bilinen sınırlar
- Ürün stok miktarı herkese açık görünür (sepet arayüzü için).
- Sepette miktar artırma eşzamanlı isteklerde "son yazan kazanır".
- Rate limit sayaçları bellektedir, reverse proxy arkasında `ForwardedHeaders` ayarı gerekir, hesap bazlı kilitleme yoktur.
- Parola/e-posta değiştirme ve hesap pasifleştirme uçları yoktur (eklenirse izin cache'i temizlenmelidir).
