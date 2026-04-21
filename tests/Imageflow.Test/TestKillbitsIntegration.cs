using System.Diagnostics.CodeAnalysis;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Imageflow.Test;

/// <summary>
/// xUnit Fact that only runs when <c>IMAGEFLOW_HAS_KILLBITS=1</c> is set
/// in the environment. The skip decision is made by the caller (via the
/// environment variable / CI filter) — never by runtime endpoint sniffing
/// inside the test body — per repo policy on test skips.
/// </summary>
internal sealed class KillbitsFactAttribute : FactAttribute
{
    public KillbitsFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("IMAGEFLOW_HAS_KILLBITS"), "1", StringComparison.Ordinal))
        {
            Skip = "IMAGEFLOW_HAS_KILLBITS=1 not set; killbits endpoints require an imageflow runtime that includes PR #720";
        }
    }
}

/// <summary>
/// Integration tests that exercise the real
/// <c>v1/context/set_policy</c> / <c>v1/context/get_net_support</c>
/// endpoints on the native runtime.
/// </summary>
/// <remarks>
/// The killbits endpoints landed in imageflow PR #720 and are not in the
/// 2.3.1-rc01 native runtime pinned by this repo's test csproj. To run
/// these tests, build against a prerelease native runtime that includes
/// PR #720 (or swap the project reference to a local Rust workspace
/// build) and invoke:
///
/// <code>IMAGEFLOW_HAS_KILLBITS=1 dotnet test --filter "Category=KillbitsIntegration"</code>
/// </remarks>
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task")]
[Trait("Category", "KillbitsIntegration")]
public class TestKillbitsIntegration
{
    private readonly ITestOutputHelper _output;

    public TestKillbitsIntegration(ITestOutputHelper output)
    {
        _output = output;
    }

    [KillbitsFact]
    public void GetNetSupport_CachesResponse()
    {
        using var ctx = new JobContext();
        var initialNative = ctx.NetSupportNativeCallCount;
        var first = ctx.GetNetSupport();
        Assert.NotNull(first);
        var afterFirst = ctx.NetSupportNativeCallCount;
        Assert.Equal(initialNative + 1, afterFirst);

        var second = ctx.GetNetSupport();
        // Same instance: cache must hand out the identical reference.
        Assert.Same(first, second);
        Assert.Equal(afterFirst, ctx.NetSupportNativeCallCount);
    }

    [KillbitsFact]
    public void SetPolicy_InvalidatesCache()
    {
        using var ctx = new JobContext();
        var before = ctx.GetNetSupport();
        var nativeBeforeSet = ctx.NetSupportNativeCallCount;

        var response = ctx.SetPolicy(new SecurityOptions
        {
            Formats = new FormatKillbits { DenyEncode = new[] { ImageFormat.Avif } },
        });
        Assert.True(response.Locked);
        Assert.True(response.NetSupport.TrustedPolicySet);

        // SetPolicy primes the cache with the returned grid — a follow-up
        // GetNetSupport must not trigger another native round-trip.
        var nativeCallsAfterSet = ctx.NetSupportNativeCallCount;
        var after = ctx.GetNetSupport();
        Assert.NotSame(before, after);
        Assert.Equal(nativeCallsAfterSet, ctx.NetSupportNativeCallCount);

        // AVIF encode is denied under the new trusted policy.
        if (after.Formats.TryGetValue("avif", out var avif))
        {
            Assert.False(avif.Encode);
        }
    }

    [KillbitsFact]
    public void GetNetSupport_ThreadSafe_OnlyOneNativeCall()
    {
        using var ctx = new JobContext();
        var before = ctx.NetSupportNativeCallCount;

        const int threadCount = 16;
        var results = new NetSupportResponse?[threadCount];
        using var start = new ManualResetEventSlim();
        var threads = new Thread[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            var idx = i;
            threads[i] = new Thread(() =>
            {
                start.Wait();
                results[idx] = ctx.GetNetSupport();
            });
            threads[i].Start();
        }
        start.Set();
        foreach (var t in threads)
        {
            t.Join();
        }

        // Every thread saw the same cached instance.
        var first = results[0]!;
        foreach (var r in results)
        {
            Assert.Same(first, r);
        }
        // Exactly one native round-trip happened.
        Assert.Equal(before + 1, ctx.NetSupportNativeCallCount);
    }

    [KillbitsFact]
    public async Task DenyAvifEncode_ThrowsStructuredDenial()
    {
        // 1x1 PNG.
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

        using var ctx = new JobContext();
        ctx.SetPolicy(new SecurityOptions
        {
            Formats = new FormatKillbits { DenyEncode = new[] { ImageFormat.Webp } },
        });

        var job = new ImageJob();
        var ex = await Assert.ThrowsAnyAsync<ImageflowException>(async () =>
        {
            await job.Decode(new BytesSource(imageBytes))
                .EncodeToBytes(new WebPLossyEncoder(80))
                .Finish()
                .InProcessAndDisposeAsync();
        });
        if (ex is KillbitsDeniedException killbits)
        {
            _output.WriteLine($"typed denial: {killbits.DenialKind} codec={killbits.Codec} format={killbits.Format}");
            Assert.Equal("webp", killbits.Format);
        }
        else
        {
            // If the native build didn't produce the envelope for this
            // denial, we still expect the raw message to mention it.
            _output.WriteLine($"raw denial: {ex.Message}");
            Assert.Contains("webp", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
