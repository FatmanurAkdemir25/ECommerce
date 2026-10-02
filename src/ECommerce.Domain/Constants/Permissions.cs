using System.Reflection;

namespace ECommerce.Domain.Constants;

public static class Permissions
{
    public static class Categories
    {
        public const string Create = "Categories.Create";
        public const string Update = "Categories.Update";
        public const string Delete = "Categories.Delete";
    }

    public static class Products
    {
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }

    public static class Customers
    {
        public const string ViewAll = "Customers.ViewAll";
    }

    public static class Carts
    {
        public const string Manage = "Carts.Manage";
    }

    public static class Orders
    {
        public const string Create = "Orders.Create";
        public const string ViewAll = "Orders.ViewAll";
        public const string UpdateStatus = "Orders.UpdateStatus";
        public const string Cancel = "Orders.Cancel";
    }

    public static class Payments
    {
        public const string UpdateStatus = "Payments.UpdateStatus";
    }

    public static class Roles
    {
        public const string Manage = "Roles.Manage";
    }

    // C# bir sınıfın içinde aynı adlı iç sınıfa izin vermediği için "PermissionAdmin"
    public static class PermissionAdmin
    {
        public const string Manage = "Permissions.Manage";
    }

    public static class Users
    {
        public const string Manage = "Users.Manage";
    }

    // Seeder bunu kullanır: yeni sabit eklemek yeterli, seed otomatik güncellenir
    //Permissions sınıfının içindeki bütün sınıfları bul - onların içindeki bütün public static alanları bul - sadece const string olanları seç - değerlerini al - liste olarak döndür.
    public static IReadOnlyList<string> GetAll() => typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}