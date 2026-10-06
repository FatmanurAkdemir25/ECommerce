using ECommerce.Application.Authorization;
using ECommerce.Tests.Support;
using Xunit;

namespace ECommerce.Tests;

public class UserPermissionServiceTests
{
    [Fact]
    public async Task Permissions_come_from_the_users_roles()
    {
        await using var db = TestFactory.CreateDb();
        var service = new UserPermissionService(db, TestFactory.CreateCache());
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);

        var result = await service.GetPermissionsAsync(user.Id, default);

        Assert.Contains("Products.Create", result);
    }

    [Fact]
    public async Task User_deny_beats_role_permission()
    {
        await using var db = TestFactory.CreateDb();
        var service = new UserPermissionService(db, TestFactory.CreateCache());
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);
        await TestFactory.SetOverrideAsync(db, user, permission, isGranted: false);

        var result = await service.GetPermissionsAsync(user.Id, default);

        Assert.DoesNotContain("Products.Create", result);
    }

    [Fact]
    public async Task Inactive_user_has_no_permissions()
    {
        await using var db = TestFactory.CreateDb();
        var service = new UserPermissionService(db, TestFactory.CreateCache());
        var user = await TestFactory.AddUserAsync(db, active: false);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);

        var result = await service.GetPermissionsAsync(user.Id, default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Second_call_is_served_from_cache_until_invalidated()
    {
        await using var db = TestFactory.CreateDb();
        var service = new UserPermissionService(db, TestFactory.CreateCache());
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var first = await TestFactory.AddPermissionAsync(db, "Products.Create");
        var second = await TestFactory.AddPermissionAsync(db, "Orders.Create");
        await TestFactory.GrantToRoleAsync(db, role, first);
        await TestFactory.AddToRoleAsync(db, user, role);

        await service.GetPermissionsAsync(user.Id, default); // cache dolar

        // Invalidation yapmadan DB'ye yeni izin ekle
        await TestFactory.GrantToRoleAsync(db, role, second);

        var cached = await service.GetPermissionsAsync(user.Id, default);
        Assert.DoesNotContain("Orders.Create", cached); // cache'ten geldi

        await service.InvalidateUserAsync(user.Id, default);

        var fresh = await service.GetPermissionsAsync(user.Id, default);
        Assert.Contains("Orders.Create", fresh); // DB'den yeniden hesaplandı
    }

    [Fact]
    public async Task InvalidateRoleUsers_clears_every_user_of_the_role_but_not_others()
    {
        await using var db = TestFactory.CreateDb();
        var service = new UserPermissionService(db, TestFactory.CreateCache());
        var userA = await TestFactory.AddUserAsync(db);
        var userB = await TestFactory.AddUserAsync(db);
        var outsider = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var otherRole = await TestFactory.AddRoleAsync(db, "Other");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.AddToRoleAsync(db, userA, role);
        await TestFactory.AddToRoleAsync(db, userB, role);
        await TestFactory.AddToRoleAsync(db, outsider, otherRole);

        // Üç kullanıcının cache'i de dolu (hepsinde izin yok)
        await service.GetPermissionsAsync(userA.Id, default);
        await service.GetPermissionsAsync(userB.Id, default);
        await service.GetPermissionsAsync(outsider.Id, default);

        // Her iki role de izin ekle ama invalidation'ı sadece "Support" için yap
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.GrantToRoleAsync(db, otherRole, permission);
        await service.InvalidateRoleUsersAsync(role.Id, default);

        Assert.Contains("Products.Create", await service.GetPermissionsAsync(userA.Id, default));
        Assert.Contains("Products.Create", await service.GetPermissionsAsync(userB.Id, default));
        Assert.DoesNotContain("Products.Create", await service.GetPermissionsAsync(outsider.Id, default));
    }
}