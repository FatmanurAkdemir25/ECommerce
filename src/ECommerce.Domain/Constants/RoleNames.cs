using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Constants;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Sales = "Sales";
    public const string Customer = "Customer";

    private static readonly string[] SystemRoles = [Admin, Sales, Customer];

    public static bool IsSystem(string name) =>
        SystemRoles.Contains(name, StringComparer.OrdinalIgnoreCase);
}