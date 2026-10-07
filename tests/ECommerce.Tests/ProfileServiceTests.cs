using ECommerce.Application.Accounts;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Tests.Support;
using Xunit;

namespace ECommerce.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task Update_changes_only_the_current_users_profile()
    {
        await using var db = TestFactory.CreateDb();
        var me = await TestFactory.AddUserAsync(db);
        var other = await TestFactory.AddUserAsync(db);
        var (_, profiles, _) = AccountTestSetup.Create(db, me.Id);

        var updated = await profiles.UpdateMyProfileAsync(
            new UpdateProfileRequest { FullName = "  Yeni Ad  ", Phone = "0555 111 22 33" }, default);

        Assert.Equal("Yeni Ad", updated.FullName);
        Assert.Equal("0555 111 22 33", updated.Phone);
        Assert.Equal(me.Email, updated.Email); // e-posta değişmez
        Assert.Equal("Test Kullanıcı", (await db.Users.FindAsync(other.Id))!.FullName);
    }

    [Fact]
    public async Task Blank_phone_is_stored_as_null()
    {
        await using var db = TestFactory.CreateDb();
        var me = await TestFactory.AddUserAsync(db);
        var (_, profiles, _) = AccountTestSetup.Create(db, me.Id);

        var updated = await profiles.UpdateMyProfileAsync(
            new UpdateProfileRequest { FullName = "Ad", Phone = "   " }, default);

        Assert.Null(updated.Phone);
    }

    [Fact]
    public async Task Inactive_or_anonymous_users_are_unauthorized()
    {
        await using var db = TestFactory.CreateDb();
        var inactive = await TestFactory.AddUserAsync(db, active: false);

        var (_, inactiveProfiles, _) = AccountTestSetup.Create(db, inactive.Id);
        var (_, anonymousProfiles, _) = AccountTestSetup.Create(db);

        await Assert.ThrowsAsync<UnauthorizedException>(() => inactiveProfiles.GetMyProfileAsync(default));
        await Assert.ThrowsAsync<UnauthorizedException>(() => anonymousProfiles.GetMyProfileAsync(default));
    }
}