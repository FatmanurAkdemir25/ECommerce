using ECommerce.Application.Authorization;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Tests;

public class UserAccessServiceTests
{
    private static (UserAccessService Access, UserPermissionService Permissions) Create(
        AppDbContext db, Guid? actorId = null)
    {
        var permissions = new UserPermissionService(db, TestFactory.CreateCache());
        var access = new UserAccessService(
            db, permissions, new FakeCurrentUser(actorId), NullLogger<UserAccessService>.Instance);
        return (access, permissions);
    }

    [Fact]
    public async Task Assigning_role_updates_user_permissions_without_relogin()
    {
        await using var db = TestFactory.CreateDb();
        var (access, permissions) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);

        Assert.Empty(await permissions.GetPermissionsAsync(user.Id, default)); // cache dolar

        await access.AssignRoleAsync(user.Id, role.Id, default);

        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));
    }

    [Fact]
    public async Task Removing_role_updates_user_permissions()
    {
        await using var db = TestFactory.CreateDb();
        var (access, permissions) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);

        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));

        await access.RemoveRoleAsync(user.Id, role.Id, default);

        Assert.Empty(await permissions.GetPermissionsAsync(user.Id, default));
    }

    [Fact]
    public async Task User_deny_takes_effect_immediately_and_can_be_removed()
    {
        await using var db = TestFactory.CreateDb();
        var (access, permissions) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Products.Create");
        await TestFactory.GrantToRoleAsync(db, role, permission);
        await TestFactory.AddToRoleAsync(db, user, role);

        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));

        await access.SetPermissionAsync(user.Id, permission.Id, isGranted: false, default);
        Assert.DoesNotContain("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));

        await access.RemovePermissionAsync(user.Id, permission.Id, default);
        Assert.Contains("Products.Create", await permissions.GetPermissionsAsync(user.Id, default));
    }

    [Fact]
    public async Task User_allow_grants_permission_immediately()
    {
        await using var db = TestFactory.CreateDb();
        var (access, permissions) = Create(db);
        var user = await TestFactory.AddUserAsync(db);
        var permission = await TestFactory.AddPermissionAsync(db, "Reports.View");

        Assert.Empty(await permissions.GetPermissionsAsync(user.Id, default));

        await access.SetPermissionAsync(user.Id, permission.Id, isGranted: true, default);

        Assert.Contains("Reports.View", await permissions.GetPermissionsAsync(user.Id, default));
    }

    [Fact]
    public async Task Changing_own_access_is_forbidden()
    {
        await using var db = TestFactory.CreateDb();
        var actor = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        var permission = await TestFactory.AddPermissionAsync(db, "Reports.View");
        var (access, _) = Create(db, actorId: actor.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => access.AssignRoleAsync(actor.Id, role.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(
            () => access.SetPermissionAsync(actor.Id, permission.Id, false, default));
    }
}