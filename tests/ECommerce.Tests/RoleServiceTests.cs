using ECommerce.Application.Authorization;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Tests;

public class RoleServiceTests
{
    private static (RoleService Roles, UserPermissionService Permissions) Create(AppDbContext db)
    {
        var permissions = new UserPermissionService(db, TestFactory.CreateCache());
        // IMapper bu testlerde kullanılan metotlarda gerekmiyor
        var roles = new RoleService(db, null!, permissions, NullLogger<RoleService>.Instance);
        return (roles, permissions);
    }

    [Fact]
    public async Task Adding_permission_to_role_updates_all_its_users_without_relogin()
    {
        await using var db = TestFactory.CreateDb();
        var (roles, permissions) = Create(db);
        var userA = await TestFactory.AddUserAsync(db);
        var userB = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.AddToRoleAsync(db, userA, role);
        await TestFactory.AddToRoleAsync(db, userB, role);

        Assert.Empty(await permissions.GetPermissionsAsync(userA.Id, default)); // cache dolar
        Assert.Empty(await permissions.GetPermissionsAsync(userB.Id, default));

        await roles.AddPermissionAsync(role.Id, permission.Id, default);

        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(userA.Id, default));
        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(userB.Id, default));
    }

    [Fact]
    public async Task Removing_permission_from_role_updates_its_users()
    {
        await using var db = TestFactory.CreateDb();
        var (roles, permissions) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);

        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));

        await roles.RemovePermissionAsync(role.Id, permission.Id, default);

        Assert.DoesNotContain("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));
    }

    [Fact]
    public async Task Admin_role_permissions_cannot_be_changed()
    {
        await using var db = TestFactory.CreateDb();
        var (roles, _) = Create(db);
        var admin = await TestFactory.AddRoleAsync(db, RoleNames.Admin);
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");

        await Assert.ThrowsAsync<ConflictException>(
            () => roles.AddPermissionAsync(admin.Id, permission.Id, default));
        await Assert.ThrowsAsync<ConflictException>(
            () => roles.RemovePermissionAsync(admin.Id, permission.Id, default));
    }

    [Fact]
    public async Task System_roles_cannot_be_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var (roles, _) = Create(db);
        var sales = await TestFactory.AddRoleAsync(db, RoleNames.Sales);

        await Assert.ThrowsAsync<ConflictException>(() => roles.DeleteAsync(sales.Id, default));
    }

    [Fact]
    public async Task Role_with_users_cannot_be_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var (roles, _) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        await TestFactory.AddToRoleAsync(db, user, role);

        await Assert.ThrowsAsync<ConflictException>(() => roles.DeleteAsync(role.Id, default));
    }
}