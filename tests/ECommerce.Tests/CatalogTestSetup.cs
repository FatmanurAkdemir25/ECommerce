using ECommerce.Application.Authorization;
using ECommerce.Application.Catalog;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Tests;

public static class CatalogTestSetup
{
    // userId null ise istek anonim kabul edilir
    public static (ProductService Products, CategoryService Categories) Create(AppDbContext db, Guid? userId = null)
    {
        var cache = TestFactory.CreateCache();
        var permissions = new UserPermissionService(db, cache);
        var currentUser = new FakeCurrentUser(userId);
        var visibility = new CatalogVisibility(currentUser, permissions);
        var mapper = TestFactory.CreateMapper();

        var products = new ProductService(db, mapper, cache, currentUser, visibility,
            NullLogger<ProductService>.Instance);
        var categories = new CategoryService(db, mapper, cache, visibility,
            NullLogger<CategoryService>.Instance);
        return (products, categories);
    }
}