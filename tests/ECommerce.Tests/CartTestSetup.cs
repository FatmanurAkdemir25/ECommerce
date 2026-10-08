using ECommerce.Application.Shopping;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Tests;

public static class CartTestSetup
{
    // userId null ise istek anonim kabul edilir
    public static CartService Create(AppDbContext db, Guid? userId = null) => new(
        db,
        TestFactory.CreateMapper(),
        new FakeCurrentUser(userId),
        TimeProvider.System,
        NullLogger<CartService>.Instance);
}