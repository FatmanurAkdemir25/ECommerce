using System.Collections.Frozen;

namespace ECommerce.Application.Authorization;

public record UserPermissionOverride(string Permission, bool IsGranted);

public static class EffectivePermissionCalculator
{
    // Sıra: 1) özel deny reddeder, 2) özel allow verir, 3) aksi halde rollerden gelenler.
    // Rol izinleriyle başlayıp özel kayıtları üstüne uygulamak bu sıralamayla aynı sonucu verir.
    public static FrozenSet<string> Calculate(
        IEnumerable<string> rolePermissions,
        IEnumerable<UserPermissionOverride> overrides)
    {
        var effective = new HashSet<string>(rolePermissions, StringComparer.Ordinal);

        foreach (var item in overrides)
        {
            if (item.IsGranted) effective.Add(item.Permission);
            else effective.Remove(item.Permission);
        }

        return effective.ToFrozenSet(StringComparer.Ordinal);
    }
}