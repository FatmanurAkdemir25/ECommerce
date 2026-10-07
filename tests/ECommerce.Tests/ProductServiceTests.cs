using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Enums;
using ECommerce.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task Creating_product_with_initial_stock_writes_receipt_and_transaction()
    {
        await using var db = TestFactory.CreateDb();
        var admin = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var (products, _) = CatalogTestSetup.Create(db, admin.Id);

        var created = await products.CreateAsync(new CreateProductRequest
        {
            CategoryId = category.Id,
            Name = "Kulaklık",
            Price = 100m,
            Stock = 10
        }, default);

        var receipt = await db.Receipts.SingleAsync();
        var line = await db.ProductTransactions.SingleAsync();

        Assert.Equal(10, created.Stock);
        Assert.Equal(ReceiptSource.InitialStock, receipt.Source);
        Assert.Equal(admin.Id, receipt.PerformedByUserId);
        Assert.Equal(receipt.Id, line.ReceiptId);
        Assert.Equal(ProductTransactionType.In, line.Type);
        Assert.Equal(10, line.Quantity);
        Assert.Equal(10, line.StockAfter);
    }

    [Fact]
    public async Task Creating_product_without_stock_writes_no_ledger_rows()
    {
        await using var db = TestFactory.CreateDb();
        var admin = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var (products, _) = CatalogTestSetup.Create(db, admin.Id);

        await products.CreateAsync(new CreateProductRequest
        {
            CategoryId = category.Id,
            Name = "Kulaklık",
            Price = 100m,
            Stock = 0
        }, default);

        Assert.Empty(await db.Receipts.ToListAsync());
        Assert.Empty(await db.ProductTransactions.ToListAsync());
    }

    [Fact]
    public async Task Updating_product_keeps_stock_and_refreshes_cache()
    {
        await using var db = TestFactory.CreateDb();
        var admin = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var product = await TestFactory.AddProductAsync(db, category, "Eski", stock: 10);
        var (products, _) = CatalogTestSetup.Create(db, admin.Id);

        await products.GetAsync(product.Id, default); // cache dolar

        await products.UpdateAsync(product.Id, new UpdateProductRequest
        {
            CategoryId = category.Id,
            Name = "Yeni",
            Price = 20m,
            IsActive = true
        }, default);

        var updated = await products.GetAsync(product.Id, default);
        Assert.Equal("Yeni", updated.Name);
        Assert.Equal(10, updated.Stock);
    }

    [Fact]
    public async Task Product_with_stock_history_cannot_be_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var admin = await TestFactory.AddUserAsync(db);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var (products, _) = CatalogTestSetup.Create(db, admin.Id);
        var created = await products.CreateAsync(new CreateProductRequest
        {
            CategoryId = category.Id,
            Name = "Kulaklık",
            Price = 100m,
            Stock = 5
        }, default);

        await Assert.ThrowsAsync<ConflictException>(() => products.DeleteAsync(created.Id, default));
    }

    [Fact]
    public async Task Product_without_history_can_be_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var product = await TestFactory.AddProductAsync(db, category, "Kulaklık");
        var (products, _) = CatalogTestSetup.Create(db);

        await products.DeleteAsync(product.Id, default);

        Assert.False(await db.Products.AnyAsync(p => p.Id == product.Id));
    }

    [Fact]
    public async Task List_filters_sorts_and_pages()
    {
        await using var db = TestFactory.CreateDb();
        var phones = await TestFactory.AddCategoryAsync(db, "Telefon");
        var laptops = await TestFactory.AddCategoryAsync(db, "Laptop");
        await TestFactory.AddProductAsync(db, phones, "Ucuz Telefon", 100m);
        await TestFactory.AddProductAsync(db, phones, "Orta Telefon", 200m);
        await TestFactory.AddProductAsync(db, phones, "Pahalı Telefon", 300m);
        await TestFactory.AddProductAsync(db, laptops, "Laptop", 900m);
        var (products, _) = CatalogTestSetup.Create(db);

        var byCategory = await products.ListAsync(new ProductListQuery { CategoryId = phones.Id }, default);
        Assert.Equal(3, byCategory.TotalCount);

        var byPrice = await products.ListAsync(new ProductListQuery { MinPrice = 150m, MaxPrice = 350m }, default);
        Assert.Equal(2, byPrice.TotalCount);

        var bySearch = await products.ListAsync(new ProductListQuery { Search = "Orta" }, default);
        Assert.Single(bySearch.Items);

        var sorted = await products.ListAsync(
            new ProductListQuery { SortBy = "price", SortDir = "desc", Page = 1, PageSize = 2 }, default);
        Assert.Equal(4, sorted.TotalCount);
        Assert.Equal(new[] { "Laptop", "Pahalı Telefon" }, sorted.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Inactive_products_are_hidden_from_anonymous_but_visible_to_staff()
    {
        await using var db = TestFactory.CreateDb();
        var staff = await TestFactory.AddUserWithPermissionsAsync(db, Permissions.Products.Update);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        var hidden = await TestFactory.AddProductAsync(db, category, "Pasif Ürün", active: false);
        await TestFactory.AddProductAsync(db, category, "Aktif Ürün");

        var (anonymous, _) = CatalogTestSetup.Create(db);
        var (staffService, _) = CatalogTestSetup.Create(db, staff.Id);

        Assert.Single((await anonymous.ListAsync(new ProductListQuery(), default)).Items);
        await Assert.ThrowsAsync<NotFoundException>(() => anonymous.GetAsync(hidden.Id, default));

        Assert.Equal(2, (await staffService.ListAsync(new ProductListQuery(), default)).TotalCount);
        Assert.Equal(hidden.Id, (await staffService.GetAsync(hidden.Id, default)).Id);
    }
}