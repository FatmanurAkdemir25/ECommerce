using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using ECommerce.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Support;

public class FakeCurrentUser(Guid? userId = null) : ICurrentUserService
{
    public Guid? UserId { get; } = userId;
}

public static class TestFactory
{
    public static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static ICacheService CreateCache() => new MemoryCacheService(
        new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryCacheService>.Instance);

    public static async Task<User> AddUserAsync(AppDbContext db, bool active = true)
    {
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@test.com",
            FullName = "Test Kullanıcı",
            PasswordHash = "hash",
            IsActive = active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Role> AddRoleAsync(AppDbContext db, string name)
    {
        var role = new Role { Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    public static async Task<Permission> AddPermissionAsync(AppDbContext db, string name)
    {
        var permission = new Permission { Name = name };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();
        return permission;
    }

    public static async Task GrantToRoleAsync(AppDbContext db, Role role, Permission permission)
    {
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();
    }

    public static async Task AddToRoleAsync(AppDbContext db, User user, Role role)
    {
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
    }

    public static async Task SetOverrideAsync(AppDbContext db, User user, Permission permission, bool isGranted)
    {
        db.UserPermissions.Add(new UserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = isGranted
        });
        await db.SaveChangesAsync();
    }

    public static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(AuthorizationMappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    public static async Task<User> AddUserWithPermissionsAsync(AppDbContext db, params string[] permissionNames)
    {
        var user = await AddUserAsync(db);
        var role = await AddRoleAsync(db, $"Role-{Guid.NewGuid():N}");
        foreach (var name in permissionNames)
        {
            var permission = await AddPermissionAsync(db, name);
            await GrantToRoleAsync(db, role, permission);
        }
        await AddToRoleAsync(db, user, role);
        return user;
    }

    public static async Task<Category> AddCategoryAsync(AppDbContext db, string name, bool active = true)
    {
        var category = new Category { Name = name, IsActive = active };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public static async Task<Product> AddProductAsync(
        AppDbContext db, Category category, string name, decimal price = 10m, int stock = 0, bool active = true)
    {
        var product = new Product
        {
            CategoryId = category.Id,
            Name = name,
            Price = price,
            Stock = stock,
            IsActive = active
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}