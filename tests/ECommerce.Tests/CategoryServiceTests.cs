using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Constants;
using ECommerce.Tests.Support;
using Xunit;

namespace ECommerce.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task Creating_a_category_refreshes_the_cached_list()
    {
        await using var db = TestFactory.CreateDb();
        var (_, categories) = CatalogTestSetup.Create(db);

        await categories.ListAsync(new CategoryListQuery(), default); // cache dolar
        await categories.CreateAsync(new CreateCategoryRequest { Name = "Yeni" }, default);

        var list = await categories.ListAsync(new CategoryListQuery(), default);
        Assert.Contains(list.Items, c => c.Name == "Yeni");
    }

    [Fact]
    public async Task Duplicate_category_name_conflicts()
    {
        await using var db = TestFactory.CreateDb();
        var (_, categories) = CatalogTestSetup.Create(db);
        await TestFactory.AddCategoryAsync(db, "Elektronik");

        await Assert.ThrowsAsync<ConflictException>(
            () => categories.CreateAsync(new CreateCategoryRequest { Name = "Elektronik" }, default));
    }

    [Fact]
    public async Task Category_with_products_cannot_be_deleted()
    {
        await using var db = TestFactory.CreateDb();
        var (_, categories) = CatalogTestSetup.Create(db);
        var category = await TestFactory.AddCategoryAsync(db, "Elektronik");
        await TestFactory.AddProductAsync(db, category, "Kulaklık");

        await Assert.ThrowsAsync<ConflictException>(() => categories.DeleteAsync(category.Id, default));
    }

    [Fact]
    public async Task Renaming_a_category_refreshes_cached_product_details()
    {
        await using var db = TestFactory.CreateDb();
        var (products, categories) = CatalogTestSetup.Create(db);
        var category = await TestFactory.AddCategoryAsync(db, "Eski Ad");
        var product = await TestFactory.AddProductAsync(db, category, "Ürün");

        Assert.Equal("Eski Ad", (await products.GetAsync(product.Id, default)).CategoryName); // cache dolar

        await categories.UpdateAsync(category.Id,
            new UpdateCategoryRequest { Name = "Yeni Ad", IsActive = true }, default);

        Assert.Equal("Yeni Ad", (await products.GetAsync(product.Id, default)).CategoryName);
    }

    [Fact]
    public async Task Inactive_categories_are_hidden_from_anonymous_but_visible_to_staff()
    {
        await using var db = TestFactory.CreateDb();
        var staff = await TestFactory.AddUserWithPermissionsAsync(db, Permissions.Categories.Update);
        var hidden = await TestFactory.AddCategoryAsync(db, "Pasif", active: false);
        await TestFactory.AddCategoryAsync(db, "Aktif");

        var (_, anonymous) = CatalogTestSetup.Create(db);
        var (_, staffService) = CatalogTestSetup.Create(db, staff.Id);

        Assert.Single((await anonymous.ListAsync(new CategoryListQuery(), default)).Items);
        await Assert.ThrowsAsync<NotFoundException>(() => anonymous.GetAsync(hidden.Id, default));

        Assert.Equal(2, (await staffService.ListAsync(new CategoryListQuery(), default)).TotalCount);
        Assert.Equal(hidden.Id, (await staffService.GetAsync(hidden.Id, default)).Id);
    }
}