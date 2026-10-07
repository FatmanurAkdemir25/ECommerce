using ECommerce.Application.Accounts;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Tests.Support;
using Xunit;

namespace ECommerce.Tests;

public class UserDirectoryServiceTests
{
    [Fact]
    public async Task List_searches_filters_sorts_and_pages()
    {
        await using var db = TestFactory.CreateDb();
        await TestFactory.AddUserAsync(db, email: "ali@test.com", fullName: "Ali Veli");
        await TestFactory.AddUserAsync(db, email: "ayse@test.com", fullName: "Ayşe Yılmaz");
        await TestFactory.AddUserAsync(db, active: false, email: "mehmet@test.com", fullName: "Mehmet Kaya");
        var (_, _, directory) = AccountTestSetup.Create(db);

        Assert.Equal(3, (await directory.ListAsync(new UserListQuery(), default)).TotalCount);
        Assert.Single((await directory.ListAsync(new UserListQuery { Search = "ayse" }, default)).Items); // e-posta
        Assert.Single((await directory.ListAsync(new UserListQuery { Search = "Kaya" }, default)).Items);  // ad soyad
        Assert.Single((await directory.ListAsync(new UserListQuery { IsActive = false }, default)).Items);

        var page = await directory.ListAsync(new UserListQuery { SortBy = "email", Page = 2, PageSize = 2 }, default);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("mehmet@test.com", Assert.Single(page.Items).Email);
    }

    [Fact]
    public async Task Get_includes_roles_and_unknown_user_is_not_found()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var role = await TestFactory.AddRoleAsync(db, "Support");
        await TestFactory.AddToRoleAsync(db, user, role);
        var (_, _, directory) = AccountTestSetup.Create(db);

        var dto = await directory.GetAsync(user.Id, default);

        Assert.Equal(new[] { "Support" }, dto.Roles);
        await Assert.ThrowsAsync<NotFoundException>(() => directory.GetAsync(Guid.NewGuid(), default));
    }
}