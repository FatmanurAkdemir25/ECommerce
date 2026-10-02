using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seed;
//veritabanına başlangıç verilerini ekler/günceller
public static class DbSeeder
{
    private static readonly Dictionary<string, string> RoleDefinitions = new()
    {
        [RoleNames.Admin] = "Sistem yöneticisi",
        [RoleNames.Sales] = "Satış personeli",
        [RoleNames.Customer] = "Müşteri"
    };

    private static readonly Dictionary<string, string[]> DefaultRolePermissions = new()
    {
        [RoleNames.Admin] = Permissions.GetAll().ToArray(),
        [RoleNames.Sales] =
        [
            Permissions.Products.Create, Permissions.Products.Update,
            Permissions.Categories.Create, Permissions.Categories.Update,
            Permissions.Customers.ViewAll,
            Permissions.Orders.ViewAll, Permissions.Orders.UpdateStatus, Permissions.Orders.Cancel,
            Permissions.Payments.UpdateStatus
        ],
        [RoleNames.Customer] =
        [
            Permissions.Carts.Manage, Permissions.Orders.Create,
            Permissions.Orders.Cancel // sadece kendi siparişi
        ]
    };

    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        //Roller
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, ct);
        var newRoles = new HashSet<string>();
        foreach (var (name, description) in RoleDefinitions)
        {
            if (roles.ContainsKey(name)) continue;
            var role = new Role { Name = name, Description = description };
            db.Roles.Add(role);
            roles[name] = role;
            newRoles.Add(name);
        }

        //Permission'lar (koddaki sabitlerden)
        var permissions = await db.Permissions.ToDictionaryAsync(p => p.Name, ct);
        var newPermissions = new HashSet<string>();
        foreach (var name in Permissions.GetAll())
        {
            if (permissions.ContainsKey(name)) continue;
            var permission = new Permission { Name = name };
            db.Permissions.Add(permission);
            permissions[name] = permission;
            newPermissions.Add(name);
        }

        //Varsayılan rol-permission atamaları.
        // Sadece yeni rol veya yeni permission için eklenir; sonradan elle kaldırılan izin geri gelmez.
        var existing = (await db.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId }).ToListAsync(ct))
            .Select(x => (x.RoleId, x.PermissionId)).ToHashSet();

        foreach (var (roleName, permissionNames) in DefaultRolePermissions)
        {
            foreach (var permissionName in permissionNames)
            {
                if (!newRoles.Contains(roleName) && !newPermissions.Contains(permissionName)) continue;
                var role = roles[roleName];
                var permission = permissions[permissionName];
                if (existing.Contains((role.Id, permission.Id))) continue;
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }

        await db.SaveChangesAsync(ct);

        //Varsayılan admin (bilgiler konfigürasyondan gelir, kodda yok)
        var email = config["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password tanımlı değil, admin kullanıcısı oluşturulmadı.");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return;

        var admin = new User { Email = email };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = roles[RoleNames.Admin].Id });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Varsayılan admin kullanıcısı oluşturuldu: {Email}", email);
    }
}