using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using ECommerce.API.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace ECommerce.Tests.RateLimiting;

public class RateLimitingTests
{
    // ---- Partition key ----
    [Fact]
    public void Authenticated_user_is_partitioned_by_sub()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u-1")], "test"));
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        Assert.Equal("user:u-1", RateLimitPartitionKey.For(ctx));
    }

    [Fact]
    public void Anonymous_is_partitioned_by_ip()
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.2");
        Assert.Equal("ip:10.0.0.2", RateLimitPartitionKey.For(ctx));
    }

    [Fact]
    public void Missing_ip_falls_back_to_unknown()
        => Assert.Equal("ip:unknown", RateLimitPartitionKey.ForIp(new DefaultHttpContext()));

    [Fact]
    public void Auth_policy_always_uses_ip_even_when_authenticated()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u-1")], "test"));
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.3");
        Assert.Equal("ip:10.0.0.3", RateLimitPartitionKey.ForIp(ctx));
    }

    // ---- Options ----
    [Fact]
    public void Default_options_are_valid() => new RateLimitingOptions().EnsureValid();

    [Theory]
    [InlineData(0, 60, 6)]
    [InlineData(5, 0, 6)]
    [InlineData(5, 60, 0)]
    public void Invalid_auth_options_throw(int permit, int window, int segments)
    {
        var o = new RateLimitingOptions
        {
            Auth = new SlidingPolicyOptions { PermitLimit = permit, WindowSeconds = window, SegmentsPerWindow = segments }
        };
        Assert.Throws<InvalidOperationException>(o.EnsureValid);
    }

    [Fact]
    public void Invalid_global_options_throw()
    {
        var o = new RateLimitingOptions { Global = new FixedPolicyOptions { PermitLimit = 0, WindowSeconds = 60 } };
        Assert.Throws<InvalidOperationException>(o.EnsureValid);
    }

    // ---- 429 yanıtı ----
    [Fact]
    public async Task Rejection_writes_429_with_retry_after_and_problem_details()
    {
        var http = NewHttpContext();
        await RateLimitingExtensions.WriteRejectionAsync(
            new OnRejectedContext { HttpContext = http, Lease = new FakeLease(TimeSpan.FromSeconds(30.5)) }, default);

        Assert.Equal(429, http.Response.StatusCode);
        Assert.Equal("31", http.Response.Headers.RetryAfter.ToString());
        Assert.StartsWith("application/problem+json", http.Response.ContentType);

        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Rejection_without_retry_metadata_has_no_header()
    {
        var http = NewHttpContext();
        await RateLimitingExtensions.WriteRejectionAsync(
            new OnRejectedContext { HttpContext = http, Lease = new FakeLease(null) }, default);

        Assert.Equal(429, http.Response.StatusCode);
        Assert.False(http.Response.Headers.ContainsKey("Retry-After"));
    }

    private static DefaultHttpContext NewHttpContext() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() }
    };

    private sealed class FakeLease(TimeSpan? retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames
            => retryAfter is null ? [] : [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (retryAfter is not null && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter.Value;
                return true;
            }
            metadata = null;
            return false;
        }
    }
}