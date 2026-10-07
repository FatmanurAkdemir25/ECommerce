using ECommerce.Application.Accounts;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests;

public class AddressServiceTests
{
    private static CreateAddressRequest NewAddress(bool isDefault = false, string city = "İstanbul") => new()
    {
        Title = "Ev",
        City = city,
        District = "Kadıköy",
        AddressLine = "Test Mah. Test Sok. 1",
        IsDefault = isDefault
    };

    private static UpdateAddressRequest UpdateRequest() => new()
    {
        Title = "İş",
        City = "Ankara",
        AddressLine = "Yeni Mah. Yeni Sok. 2"
    };

    [Fact]
    public async Task First_address_becomes_default_automatically()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        var created = await addresses.CreateAsync(NewAddress(isDefault: false), default);

        Assert.True(created.IsDefault);
    }

    [Fact]
    public async Task Creating_a_default_address_moves_the_default_flag()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        await addresses.CreateAsync(NewAddress(), default);
        var second = await addresses.CreateAsync(NewAddress(isDefault: true, city: "Ankara"), default);

        var list = await addresses.ListMineAsync(default);
        Assert.Equal(2, list.Count);
        Assert.Equal(second.Id, Assert.Single(list, a => a.IsDefault).Id);
    }

    [Fact]
    public async Task Creating_a_non_default_address_keeps_the_existing_default()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        var first = await addresses.CreateAsync(NewAddress(), default);
        await addresses.CreateAsync(NewAddress(city: "Ankara"), default);

        var list = await addresses.ListMineAsync(default);
        Assert.Equal(first.Id, Assert.Single(list, a => a.IsDefault).Id);
    }

    [Fact]
    public async Task Set_default_leaves_exactly_one_default()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        await addresses.CreateAsync(NewAddress(), default);
        await addresses.CreateAsync(NewAddress(city: "Ankara"), default);
        var third = await addresses.CreateAsync(NewAddress(city: "İzmir"), default);

        await addresses.SetDefaultAsync(third.Id, default);

        var list = await addresses.ListMineAsync(default);
        Assert.Equal(third.Id, Assert.Single(list, a => a.IsDefault).Id);
    }

    [Fact]
    public async Task Another_users_address_is_not_found_for_every_operation()
    {
        await using var db = TestFactory.CreateDb();
        var owner = await TestFactory.AddUserAsync(db);
        var intruder = await TestFactory.AddUserAsync(db);
        var address = await TestFactory.AddAddressAsync(db, owner, isDefault: true);
        var (addresses, _, _) = AccountTestSetup.Create(db, intruder.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => addresses.GetMineAsync(address.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => addresses.UpdateAsync(address.Id, UpdateRequest(), default));
        await Assert.ThrowsAsync<NotFoundException>(() => addresses.SetDefaultAsync(address.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => addresses.DeleteAsync(address.Id, default));

        Assert.Empty(await addresses.ListMineAsync(default));
        Assert.True(await db.Addresses.AnyAsync(a => a.Id == address.Id)); // sahibinin adresi duruyor
    }

    [Fact]
    public async Task Deleting_the_default_address_promotes_another_one()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        var first = await addresses.CreateAsync(NewAddress(), default); // varsayılan
        await addresses.CreateAsync(NewAddress(city: "Ankara"), default);
        await addresses.CreateAsync(NewAddress(city: "İzmir"), default);

        await addresses.DeleteAsync(first.Id, default);

        var list = await addresses.ListMineAsync(default);
        Assert.Equal(2, list.Count);
        Assert.Single(list, a => a.IsDefault);
    }

    [Fact]
    public async Task Address_used_by_an_order_cannot_be_updated_or_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var address = await TestFactory.AddAddressAsync(db, user, isDefault: true);
        await TestFactory.AddOrderAsync(db, user, address);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        await Assert.ThrowsAsync<ConflictException>(() => addresses.UpdateAsync(address.Id, UpdateRequest(), default));
        await Assert.ThrowsAsync<ConflictException>(() => addresses.DeleteAsync(address.Id, default));
    }

    [Fact]
    public async Task Address_limit_is_enforced()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var (addresses, _, _) = AccountTestSetup.Create(db, user.Id);

        for (var i = 0; i < 20; i++)
        {
            await addresses.CreateAsync(NewAddress(city: $"Şehir {i}"), default);
        }

        await Assert.ThrowsAsync<ConflictException>(() => addresses.CreateAsync(NewAddress(), default));
    }

    [Fact]
    public async Task Staff_can_list_a_users_addresses_and_unknown_user_is_not_found()
    {
        await using var db = TestFactory.CreateDb();
        var target = await TestFactory.AddUserAsync(db);
        var staff = await TestFactory.AddUserAsync(db);
        await TestFactory.AddAddressAsync(db, target, isDefault: true);
        await TestFactory.AddAddressAsync(db, target, city: "Ankara");
        var (addresses, _, _) = AccountTestSetup.Create(db, staff.Id);

        Assert.Equal(2, (await addresses.ListForUserAsync(target.Id, default)).Count);
        await Assert.ThrowsAsync<NotFoundException>(() => addresses.ListForUserAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Inactive_or_anonymous_users_are_unauthorized()
    {
        await using var db = TestFactory.CreateDb();
        var inactive = await TestFactory.AddUserAsync(db, active: false);

        var (inactiveService, _, _) = AccountTestSetup.Create(db, inactive.Id);
        var (anonymousService, _, _) = AccountTestSetup.Create(db);

        await Assert.ThrowsAsync<UnauthorizedException>(() => inactiveService.ListMineAsync(default));
        await Assert.ThrowsAsync<UnauthorizedException>(() => anonymousService.ListMineAsync(default));
    }
}