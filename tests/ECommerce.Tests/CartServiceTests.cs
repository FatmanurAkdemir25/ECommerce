using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests;

public class CartServiceTests
{
    private static AddCartItemRequest Add(Guid productId, int quantity) =>
        new() { ProductId = productId, Quantity = quantity };

    private static async Task<(User User, Product Product, CartService Cart)> ArrangeAsync(
        AppDbContext db, int stock = 10, decimal price = 100m)
    {
        var user = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, $"Kategori-{Guid.NewGuid():N}");
        var product = await TestFactory.AddProductAsync(db, category, "Kulaklık", price, stock);
        return (user, product, CartTestSetup.Create(db, user.Id));
    }

    [Fact]
    public async Task Cart_is_created_lazily_on_first_add_and_get_never_writes()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db);

        var empty = await cart.GetAsync(default);
        Assert.Empty(empty.Items);
        Assert.Equal(0m, empty.TotalAmount);
        Assert.Empty(await db.Carts.ToListAsync()); // GET sepet oluşturmaz

        var result = await cart.AddItemAsync(Add(product.Id, 2), default);

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(200m, item.LineTotal);
        Assert.Equal(200m, result.TotalAmount);
        Assert.Equal(2, result.TotalQuantity);
        Assert.Single(await db.Carts.ToListAsync());
    }

    [Fact]
    public async Task Adding_the_same_product_again_increases_the_quantity()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db);

        await cart.AddItemAsync(Add(product.Id, 2), default);
        var result = await cart.AddItemAsync(Add(product.Id, 3), default);

        Assert.Equal(5, Assert.Single(result.Items).Quantity);
        Assert.Single(await db.CartItems.ToListAsync()); // tek satır
    }

    [Fact]
    public async Task Inactive_product_cannot_be_added()
    {
        await using var db = TestFactory.CreateDb();
        var (_, _, cart) = await ArrangeAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Pasifler");
        var inactive = await TestFactory.AddProductAsync(db, category, "Pasif", stock: 10, active: false);

        await Assert.ThrowsAsync<ConflictException>(() => cart.AddItemAsync(Add(inactive.Id, 1), default));
        Assert.Empty(await db.CartItems.ToListAsync());
    }

    [Fact]
    public async Task Unknown_product_is_not_found()
    {
        await using var db = TestFactory.CreateDb();
        var (_, _, cart) = await ArrangeAsync(db);

        await Assert.ThrowsAsync<NotFoundException>(() => cart.AddItemAsync(Add(Guid.NewGuid(), 1), default));
    }

    [Fact]
    public async Task Stock_check_includes_the_quantity_already_in_the_cart()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db, stock: 5);

        await cart.AddItemAsync(Add(product.Id, 3), default);

        await Assert.ThrowsAsync<ConflictException>(() => cart.AddItemAsync(Add(product.Id, 3), default));
        Assert.Equal(3, (await cart.GetAsync(default)).Items.Single().Quantity); // değişmedi
    }

    [Fact]
    public async Task Set_quantity_replaces_the_quantity_and_respects_stock()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db, stock: 5);
        var category = await TestFactory.AddCategoryAsync(db, "Diğer");
        var other = await TestFactory.AddProductAsync(db, category, "Diğer Ürün", stock: 5);

        await cart.AddItemAsync(Add(product.Id, 1), default);

        var updated = await cart.SetQuantityAsync(product.Id, new SetCartItemQuantityRequest { Quantity = 4 }, default);
        Assert.Equal(4, updated.Items.Single().Quantity);

        await Assert.ThrowsAsync<ConflictException>(
            () => cart.SetQuantityAsync(product.Id, new SetCartItemQuantityRequest { Quantity = 6 }, default));

        // Sepette olmayan ürün
        await Assert.ThrowsAsync<NotFoundException>(
            () => cart.SetQuantityAsync(other.Id, new SetCartItemQuantityRequest { Quantity = 1 }, default));
    }

    [Fact]
    public async Task Remove_and_clear_are_idempotent()
    {
        await using var db = TestFactory.CreateDb();
        var (_, first, cart) = await ArrangeAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Diğer");
        var second = await TestFactory.AddProductAsync(db, category, "İkinci", stock: 10);

        await cart.AddItemAsync(Add(first.Id, 1), default);
        await cart.AddItemAsync(Add(second.Id, 1), default);

        await cart.RemoveItemAsync(first.Id, default);
        await cart.RemoveItemAsync(first.Id, default); // ikinci çağrı hata vermez
        Assert.Equal(second.Id, Assert.Single((await cart.GetAsync(default)).Items).ProductId);

        await cart.ClearAsync(default);
        await cart.ClearAsync(default);
        Assert.Empty((await cart.GetAsync(default)).Items);
    }

    [Fact]
    public async Task Items_that_became_unavailable_are_flagged_not_removed()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db, stock: 10);
        await cart.AddItemAsync(Add(product.Id, 3), default);

        product.Stock = 2;
        await db.SaveChangesAsync();
        var lowStock = await cart.GetAsync(default);
        Assert.True(lowStock.HasIssues);
        Assert.Contains("Stok yetersiz", lowStock.Items.Single().Issue);

        product.IsActive = false;
        await db.SaveChangesAsync();
        var inactive = await cart.GetAsync(default);
        Assert.Contains("satışta değil", inactive.Items.Single().Issue);
        Assert.False(inactive.Items.Single().IsAvailable);
    }

    [Fact]
    public async Task Each_user_only_sees_their_own_cart()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cartA) = await ArrangeAsync(db);
        var userB = await TestFactory.AddUserAsync(db);
        var cartB = CartTestSetup.Create(db, userB.Id);

        await cartA.AddItemAsync(Add(product.Id, 2), default);

        Assert.Empty((await cartB.GetAsync(default)).Items);

        await cartB.AddItemAsync(Add(product.Id, 1), default);
        Assert.Equal(2, (await cartA.GetAsync(default)).Items.Single().Quantity);
        Assert.Equal(1, (await cartB.GetAsync(default)).Items.Single().Quantity);
        Assert.Equal(2, await db.Carts.CountAsync());
    }

    [Fact]
    public async Task Distinct_item_limit_is_enforced()
    {
        await using var db = TestFactory.CreateDb();
        var user = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Çok");
        var cart = CartTestSetup.Create(db, user.Id);

        for (var i = 0; i < CartLimits.MaxDistinctItems; i++)
        {
            var product = await TestFactory.AddProductAsync(db, category, $"Ürün {i}", stock: 5);
            await cart.AddItemAsync(Add(product.Id, 1), default);
        }

        var extra = await TestFactory.AddProductAsync(db, category, "Fazla", stock: 5);
        await Assert.ThrowsAsync<ConflictException>(() => cart.AddItemAsync(Add(extra.Id, 1), default));
    }

    [Fact]
    public async Task Per_item_quantity_limit_is_enforced()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, cart) = await ArrangeAsync(db, stock: 1000);

        await cart.AddItemAsync(Add(product.Id, CartLimits.MaxQuantityPerItem), default);

        await Assert.ThrowsAsync<ConflictException>(() => cart.AddItemAsync(Add(product.Id, 1), default));
    }

    [Fact]
    public async Task Inactive_or_anonymous_users_are_unauthorized()
    {
        await using var db = TestFactory.CreateDb();
        var (_, product, _) = await ArrangeAsync(db);
        var inactive = await TestFactory.AddUserAsync(db, active: false);

        var inactiveCart = CartTestSetup.Create(db, inactive.Id);
        var anonymousCart = CartTestSetup.Create(db);

        await Assert.ThrowsAsync<UnauthorizedException>(() => inactiveCart.GetAsync(default));
        await Assert.ThrowsAsync<UnauthorizedException>(() => anonymousCart.AddItemAsync(Add(product.Id, 1), default));
    }
}