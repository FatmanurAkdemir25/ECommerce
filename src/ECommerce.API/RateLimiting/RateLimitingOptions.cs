namespace ECommerce.API.RateLimiting;

public class FixedPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

public sealed class SlidingPolicyOptions : FixedPolicyOptions
{
    public int SegmentsPerWindow { get; set; } = 6;
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public FixedPolicyOptions Global { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };
    public SlidingPolicyOptions Auth { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60, SegmentsPerWindow = 6 };

    public void EnsureValid()
    {
        var errors = new List<string>();

        void Check(string name, FixedPolicyOptions p)
        {
            if (p.PermitLimit < 1) errors.Add($"{name}.PermitLimit en az 1 olmalı.");
            if (p.WindowSeconds < 1) errors.Add($"{name}.WindowSeconds en az 1 olmalı.");
        }

        Check(nameof(Global), Global);
        Check(nameof(Auth), Auth);
        if (Auth.SegmentsPerWindow < 1) errors.Add("Auth.SegmentsPerWindow en az 1 olmalı.");

        if (errors.Count > 0)
            throw new InvalidOperationException("RateLimiting ayarları geçersiz: " + string.Join(" ", errors));
    }
}