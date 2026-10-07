using ECommerce.Application.Accounts;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Tests;

public static class AccountTestSetup
{
    // userId null ise istek anonim kabul edilir
    public static (AddressService Addresses, ProfileService Profiles, UserDirectoryService Directory) Create(
        AppDbContext db, Guid? userId = null)
    {
        var mapper = TestFactory.CreateMapper();
        var currentUser = new FakeCurrentUser(userId);

        return (
            new AddressService(db, mapper, currentUser, NullLogger<AddressService>.Instance),
            new ProfileService(db, mapper, currentUser, NullLogger<ProfileService>.Instance),
            new UserDirectoryService(db, mapper));
    }
}