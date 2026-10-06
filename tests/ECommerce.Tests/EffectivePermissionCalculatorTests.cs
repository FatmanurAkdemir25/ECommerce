using ECommerce.Application.Authorization;
using Xunit;

namespace ECommerce.Tests;

public class EffectivePermissionCalculatorTests
{
    [Fact]
    public void Role_permissions_are_effective_when_there_is_no_override()
    {
        var result = EffectivePermissionCalculator.Calculate(["Products.Create", "Orders.Create"], []);

        Assert.Equal(2, result.Count);
        Assert.Contains("Products.Create", result);
        Assert.Contains("Orders.Create", result);
    }

    [Fact]
    public void User_deny_overrides_role_permission()
    {
        var result = EffectivePermissionCalculator.Calculate(
            ["Products.Create"],
            [new UserPermissionOverride("Products.Create", false)]);

        Assert.DoesNotContain("Products.Create", result);
    }

    [Fact]
    public void User_allow_grants_permission_missing_from_roles()
    {
        var result = EffectivePermissionCalculator.Calculate(
            [],
            [new UserPermissionOverride("Reports.View", true)]);

        Assert.Contains("Reports.View", result);
    }

    [Fact]
    public void User_allow_for_permission_already_in_role_does_not_duplicate()
    {
        var result = EffectivePermissionCalculator.Calculate(
            ["Products.Create"],
            [new UserPermissionOverride("Products.Create", true)]);

        Assert.Single(result);
    }

    [Fact]
    public void Deny_only_affects_the_denied_permission()
    {
        var result = EffectivePermissionCalculator.Calculate(
            ["Products.Create", "Products.Update"],
            [new UserPermissionOverride("Products.Create", false)]);

        Assert.Contains("Products.Update", result);
        Assert.DoesNotContain("Products.Create", result);
    }

    [Fact]
    public void No_roles_and_no_overrides_gives_empty_set()
    {
        Assert.Empty(EffectivePermissionCalculator.Calculate([], []));
    }
}