using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Imageflow.Internal.Helpers;

using Xunit;
using Xunit.Abstractions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Imageflow.Test;

/// <summary>
/// Regression tests for memory safety fixes (Issues 1-7).
/// These tests assert CORRECT behavior after fixes have been applied.
/// If any test fails, a bug has been reintroduced.
/// </summary>
public class TestMemorySafety
{
    private readonly ITestOutputHelper _output;

    public TestMemorySafety(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");

    // ═══════════════════════════════════════════════════════════
    // GCHandle baselines: document runtime behavior
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Baseline_GCHandle_SameCopy_DoubleFree_Throws_InvalidOperationException()
    {
        var data = new byte[16];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        handle.Free();
        Assert.Throws<InvalidOperationException>(() => handle.Free());
    }

    [Fact]
    public void Baseline_GCHandle_DifferentCopy_DoubleFree_DoesNotThrow()
    {
        var data = new byte[16];
        var h1 = GCHandle.Alloc(data, GCHandleType.Pinned);
        var h2 = h1;
        h1.Free();
        var ex = Record.Exception(() => h2.Free());
        Assert.Null(ex);
    }

    [Fact]
    public void Baseline_MemoryHandle_SameCopy_DoubleFree_IsNoop()
    {
        var data = new ReadOnlyMemory<byte>(new byte[16]);
        var pinned = data.Pin();
        pinned.Dispose();
        var ex = Record.Exception(() => pinned.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Baseline_MemoryHandle_ListForeach_DoubleFree_SilentlyProceeds()
    {
        var data = new ReadOnlyMemory<byte>(new byte[16]);
        var pinned = data.Pin();
        var list = new List<MemoryHandle> { pinned, pinned };

        Exception? caught = null;
        foreach (var active in list)
        {
            var ex = Record.Exception(() => active.Dispose());
            if (ex != null) caught = ex;
        }
        Assert.Null(caught);
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 1 FIXED: AddInputBytesPinned pins exactly once
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Issue1_Fixed_PinnedMemoryCount_Is_1_After_SingleCall()
    {
        using var ctx = new JobContext();
        ctx.AddInputBytesPinned(0, new ReadOnlyMemory<byte>(TinyPng),
            MemoryLifetimePromise.MemoryIsOwnedByRuntime);

        var field = typeof(JobContext).GetField("_pinnedMemory",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var pinnedMemory = (System.Collections.IList)field.GetValue(ctx)!;

        Assert.Equal(1, pinnedMemory.Count);
    }

    [Fact]
    public void Issue1_Fixed_CountingMemoryManager_Shows_Balanced_Pin_Unpin()
    {
        using var manager = new CountingMemoryManager((byte[])TinyPng.Clone());
        var ctx = new JobContext();
        ctx.AddInputBytesPinned(0, manager.Memory,
            MemoryLifetimePromise.MemoryIsOwnedByRuntime);

        Assert.Equal(1, manager.PinCount);
        _output.WriteLine($"Pin called {manager.PinCount}x, Unpin called {manager.UnpinCount}x before dispose");

        ctx.Dispose();
        _output.WriteLine($"Pin called {manager.PinCount}x, Unpin called {manager.UnpinCount}x after dispose");

        // Pin and Unpin should be balanced: 1 pin, 1 unpin
        Assert.Equal(1, manager.PinCount);
        Assert.Equal(1, manager.UnpinCount);
    }

    [Fact]
    public async Task Issue1_Production_CodePath_Via_ImageJob()
    {
        using var job = new ImageJob();
        var result = await job.Decode(TinyPng)
            .ConstrainWithin(1, 1)
            .EncodeToBytes(new GifEncoder())
            .Finish()
            .InProcessAsync();

        Assert.NotNull(result.First);
        Assert.True(result.First!.TryGetBytes().HasValue);
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 2 FIXED: MemorySource.TakeOwnership works correctly
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Issue2_Fixed_TakeOwnership_Succeeds()
    {
        var owner = new TrackingMemoryOwner(new byte[16]);

        var source = MemorySource.TakeOwnership(owner, MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource);
        Assert.NotNull(source);

        // The IMemoryOwner should be disposed when the MemorySource is disposed
        Assert.False(owner.IsDisposed);
        source.Dispose();
        Assert.True(owner.IsDisposed);
    }

    [Fact]
    public void Issue2_Fixed_TakeOwnership_Returns_Correct_Memory()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var owner = new TrackingMemoryOwner(data);

        using var source = MemorySource.TakeOwnership(owner, MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource);
        var memory = ((IMemorySource)source).BorrowReadOnlyMemory();

        Assert.Equal(4, memory.Length);
        Assert.Equal(1, memory.Span[0]);
        Assert.Equal(4, memory.Span[3]);
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 3 FIXED: FinishWithTimeout cancellation works
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Issue3_Fixed_FinishWithTimeout_1ms_Does_Cancel()
    {
        using var job = new ImageJob();
        var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .FinishWithTimeout(1);

        // Give the CTS timer enough time to fire even on slow CI runners
        await Task.Delay(500);

        await Assert.ThrowsAsync<OperationCanceledException>(() => builder.InProcessAsync());
    }

    [Fact]
    public async Task Issue3_Fixed_FinishWithTimeout_Token_Fires()
    {
        using var job = new ImageJob();
        var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .FinishWithTimeout(1);

        var tokenField = typeof(FinishJobBuilder).GetField("_token",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var token = (CancellationToken)tokenField.GetValue(builder)!;

        int callbackFired = 0;
        token.Register(() => Interlocked.Increment(ref callbackFired));

        await Task.Delay(500);

        Assert.Equal(1, callbackFired);
        _output.WriteLine("FinishWithTimeout callback fired — CTS stays alive.");
    }

    [Fact]
    public async Task Issue3_SetCancellationTimeout_Also_Cancels()
    {
        using var job = new ImageJob();
        var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .Finish()
            .SetCancellationTimeout(1);

        await Task.Delay(500);

        await Assert.ThrowsAsync<OperationCanceledException>(() => builder.InProcessAsync());
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 4 FIXED: Previous CTS disposed on replacement,
    // FinishJobBuilder implements IDisposable
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Issue4_Fixed_SetCancellationTimeout_Disposes_Previous_CTS()
    {
        using var job = new ImageJob();
        using var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .Finish();

        builder.SetCancellationTimeout(10000);
        var field = typeof(FinishJobBuilder).GetField("_tokenSource",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var first = (CancellationTokenSource)field.GetValue(builder)!;

        builder.SetCancellationTimeout(20000);
        var second = (CancellationTokenSource)field.GetValue(builder)!;

        Assert.NotSame(first, second);

        // The old CTS should be disposed: Cancel() throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => first.Cancel());
        _output.WriteLine("first.Cancel() threw ObjectDisposedException — properly disposed.");
    }

    [Fact]
    public void Issue4_Fixed_FinishJobBuilder_Is_IDisposable()
    {
        Assert.Contains(typeof(IDisposable), typeof(FinishJobBuilder).GetInterfaces());
    }

    [Fact]
    public void Issue4_Fixed_Dispose_Cleans_Up_Final_CTS()
    {
        using var job = new ImageJob();
        var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .Finish();

        builder.SetCancellationTimeout(10000);
        var field = typeof(FinishJobBuilder).GetField("_tokenSource",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cts = (CancellationTokenSource)field.GetValue(builder)!;

        builder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cts.Cancel());
        _output.WriteLine("Final CTS disposed on builder.Dispose().");
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 5 FIXED: ImageflowUnmanagedReadStream calls
    // base.Dispose — stream is properly closed after Dispose
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Issue5_Fixed_CanRead_False_After_Dispose()
    {
        using var ctx = new JobContext();
        ctx.AddInputBytesPinned(0, TinyPng);
        ctx.AddOutputBuffer(1);
        ctx.ExecuteImageResizer4CommandStringInternal(0, 1, "w=1&h=1&format=png");

        var stream = ctx.GetOutputBuffer(1);
        Assert.True(stream.CanRead);

        stream.Dispose();

        Assert.False(stream.CanRead);
    }

    [Fact]
    public void Issue5_Fixed_Read_Throws_After_Dispose()
    {
        using var ctx = new JobContext();
        ctx.AddInputBytesPinned(0, TinyPng);
        ctx.AddOutputBuffer(1);
        ctx.ExecuteImageResizer4CommandStringInternal(0, 1, "w=1&h=1&format=png");

        var stream = ctx.GetOutputBuffer(1);
        stream.Dispose();

        var buf = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => stream.Read(buf, 0, buf.Length));
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 6: SerializeNode now disposes Utf8JsonWriter
    // (verified by code inspection; ArrayPool leak not
    // directly observable from managed code)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Issue6_SerializeNode_Produces_Valid_JSON()
    {
        var node = new System.Text.Json.Nodes.JsonObject { ["key"] = "val" };
        var bytes = JobContext.SerializeNode(node);
        var json = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("key", json);
        Assert.Contains("val", json);
        _output.WriteLine($"SerializeNode produced: {json}");
    }

    // ═══════════════════════════════════════════════════════════
    // Issue 7 FIXED: BytesDestination.Dispose disposes the
    // MemoryStream and rejects further operations
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Issue7_Fixed_BytesDestination_Throws_After_Dispose()
    {
        var dest = new BytesDestination();
        await dest.RequestCapacityAsync(1024);
        await dest.WriteAsync(new ArraySegment<byte>([1, 2, 3]), CancellationToken.None);
        await dest.FlushAsync(CancellationToken.None);

        var bytes = dest.GetBytes();
        Assert.Equal(3, bytes.Count);

        dest.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dest.GetBytes());
        Assert.Throws<ObjectDisposedException>(() => dest.RequestCapacity(1));
    }

    [Fact]
    public void Issue7_Fixed_BytesDestination_Double_Dispose_Is_Safe()
    {
        var dest = new BytesDestination();
        dest.RequestCapacity(16);
        dest.Dispose();
        var ex = Record.Exception(() => dest.Dispose());
        Assert.Null(ex);
    }

    // ═══════════════════════════════════════════════════════════
    // Long-running leak detection tests (local only, skipped in CI)
    // Run locally with: dotnet test --filter "Category=LeakTest"
    // ═══════════════════════════════════════════════════════════

    private static long GetManagedMemory()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue1_PinnedMemory_NoGrowth()
    {
        // Exercise AddInputBytesPinned + Dispose thousands of times.
        // If pinned handles leak, managed memory grows without bound.
        const int warmup = 50;
        const int iterations = 2000;

        for (int i = 0; i < warmup; i++)
        {
            using var ctx = new JobContext();
            ctx.AddInputBytesPinned(0, TinyPng);
        }

        var baseline = GetManagedMemory();

        for (int i = 0; i < iterations; i++)
        {
            using var ctx = new JobContext();
            ctx.AddInputBytesPinned(0, TinyPng);
        }

        var after = GetManagedMemory();
        var growth = after - baseline;
        _output.WriteLine($"Baseline: {baseline:N0} bytes, After {iterations} iterations: {after:N0} bytes, Growth: {growth:N0} bytes");

        // Allow up to 5MB of noise — CI runners have high GC variance from parallel TFM testing.
        // A real leak of 2000 pinned handles would show tens of MB of growth.
        Assert.True(growth < 5_000_000,
            $"Managed memory grew by {growth:N0} bytes over {iterations} iterations — possible pinned memory leak");
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue1_MemoryManager_Balanced_Over_Many_Iterations()
    {
        // Verify Pin/Unpin stay balanced across many iterations
        const int iterations = 1000;
        using var manager = new CountingMemoryManager((byte[])TinyPng.Clone());

        for (int i = 0; i < iterations; i++)
        {
            var ctx = new JobContext();
            ctx.AddInputBytesPinned(0, manager.Memory,
                MemoryLifetimePromise.MemoryIsOwnedByRuntime);
            ctx.Dispose();
        }

        _output.WriteLine($"After {iterations} iterations: Pin={manager.PinCount}, Unpin={manager.UnpinCount}");
        Assert.Equal(manager.PinCount, manager.UnpinCount);
        Assert.Equal(iterations, manager.PinCount);
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue2_TakeOwnership_AllDisposed()
    {
        // Create many MemorySource via TakeOwnership, verify every owner is disposed
        const int iterations = 1000;
        var owners = new TrackingMemoryOwner[iterations];

        for (int i = 0; i < iterations; i++)
        {
            owners[i] = new TrackingMemoryOwner(new byte[1024]);
            var source = MemorySource.TakeOwnership(owners[i],
                MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource);
            source.Dispose();
        }

        int leakedCount = owners.Count(o => !o.IsDisposed);
        _output.WriteLine($"Disposed: {iterations - leakedCount}/{iterations}");
        Assert.Equal(0, leakedCount);
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue2_TakeOwnership_NoMemoryGrowth()
    {
        const int warmup = 50;
        const int iterations = 2000;

        for (int i = 0; i < warmup; i++)
        {
            var owner = new TrackingMemoryOwner(new byte[4096]);
            var source = MemorySource.TakeOwnership(owner,
                MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource);
            source.Dispose();
        }

        var baseline = GetManagedMemory();

        for (int i = 0; i < iterations; i++)
        {
            var owner = new TrackingMemoryOwner(new byte[4096]);
            var source = MemorySource.TakeOwnership(owner,
                MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource);
            source.Dispose();
        }

        var after = GetManagedMemory();
        var growth = after - baseline;
        _output.WriteLine($"Baseline: {baseline:N0}, After: {after:N0}, Growth: {growth:N0}");
        Assert.True(growth < 5_000_000,
            $"Managed memory grew by {growth:N0} bytes — possible IMemoryOwner leak");
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue4_CTS_Replacement_NoGrowth()
    {
        // Replace CTS many times on the same builder — old ones must be disposed
        const int warmup = 50;
        const int iterations = 5000;

        using var job = new ImageJob();
        using var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .Finish();

        for (int i = 0; i < warmup; i++)
            builder.SetCancellationTimeout(600_000);

        var baseline = GetManagedMemory();

        for (int i = 0; i < iterations; i++)
            builder.SetCancellationTimeout(600_000);

        var after = GetManagedMemory();
        var growth = after - baseline;
        _output.WriteLine($"Baseline: {baseline:N0}, After {iterations} replacements: {after:N0}, Growth: {growth:N0}");
        Assert.True(growth < 5_000_000,
            $"Managed memory grew by {growth:N0} bytes over {iterations} CTS replacements — possible CTS leak");
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue4_CTS_WeakRefs_AllCollected()
    {
        // Track every replaced CTS with a WeakReference — after GC, all should be collected
        const int iterations = 500;
        var weakRefs = new WeakReference[iterations];

        using var job = new ImageJob();
        using var builder = job.Decode(TinyPng)
            .EncodeToBytes(new GifEncoder())
            .Finish();

        var field = typeof(FinishJobBuilder).GetField("_tokenSource",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        for (int i = 0; i < iterations; i++)
        {
            builder.SetCancellationTimeout(600_000);
            var cts = field.GetValue(builder)!;
            weakRefs[i] = new WeakReference(cts);
        }

        // The last CTS is still alive in the builder — all previous ones should be collectible
        GetManagedMemory();

        int alive = weakRefs.Take(iterations - 1).Count(w => w.IsAlive);
        _output.WriteLine($"Old CTS still alive: {alive}/{iterations - 1}");

        // Allow some to survive (GC is non-deterministic) but not most
        Assert.True(alive < iterations / 4,
            $"{alive}/{iterations - 1} old CTS instances still alive after forced GC — possible leak");
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue7_BytesDestination_NoGrowth()
    {
        const int warmup = 50;
        const int iterations = 2000;

        for (int i = 0; i < warmup; i++)
        {
            var dest = new BytesDestination();
            dest.RequestCapacity(8192);
            dest.Write(new ReadOnlySpan<byte>(new byte[4096]));
            dest.Dispose();
        }

        var baseline = GetManagedMemory();

        for (int i = 0; i < iterations; i++)
        {
            var dest = new BytesDestination();
            dest.RequestCapacity(8192);
            dest.Write(new ReadOnlySpan<byte>(new byte[4096]));
            dest.Dispose();
        }

        var after = GetManagedMemory();
        var growth = after - baseline;
        _output.WriteLine($"Baseline: {baseline:N0}, After: {after:N0}, Growth: {growth:N0}");
        Assert.True(growth < 5_000_000,
            $"Managed memory grew by {growth:N0} bytes — possible MemoryStream leak in BytesDestination");
    }

    private static IEncoderPreset[] AllEncoders =>
    [
        new GifEncoder(),
        new LibPngEncoder(),
        new PngQuantEncoder(),
        new LodePngEncoder(),
        new LibJpegTurboEncoder(),
        new MozJpegEncoder(80),
        new WebPLossyEncoder(80),
        new WebPLosslessEncoder(),
    ];

    [Fact]
    [Trait("Category", "LeakTest")]
    public async Task LeakTest_FullPipeline_AllCodecs_NoMonotonicGrowth()
    {
        // End-to-end: decode → resize → encode for every codec, many rounds.
        // Sample memory at intervals to detect monotonic growth.
        var encoders = AllEncoders;
        const int warmupRounds = 5;
        const int measureRounds = 20;
        const int jobsPerRound = 10; // 10 rounds × 8 codecs × 10 jobs = 800 per sample

        // Warmup: run each codec enough times to stabilize JIT, caches, etc.
        for (int w = 0; w < warmupRounds; w++)
        {
            foreach (var encoder in encoders)
            {
                for (int j = 0; j < jobsPerRound; j++)
                {
                    using var job = new ImageJob();
                    await job.Decode(TinyPng)
                        .ConstrainWithin(1, 1)
                        .EncodeToBytes(encoder)
                        .Finish()
                        .InProcessAsync();
                }
            }
        }

        // Measure: take memory samples after each round
        var samples = new long[measureRounds];
        for (int round = 0; round < measureRounds; round++)
        {
            foreach (var encoder in encoders)
            {
                for (int j = 0; j < jobsPerRound; j++)
                {
                    using var job = new ImageJob();
                    await job.Decode(TinyPng)
                        .ConstrainWithin(1, 1)
                        .EncodeToBytes(encoder)
                        .Finish()
                        .InProcessAsync();
                }
            }
            samples[round] = GetManagedMemory();
        }

        // Log all samples
        _output.WriteLine($"Codecs tested: {string.Join(", ", encoders.Select(e => e.GetType().Name))}");
        _output.WriteLine($"Jobs per sample point: {encoders.Length * jobsPerRound}");
        for (int i = 0; i < samples.Length; i++)
        {
            var delta = i > 0 ? samples[i] - samples[i - 1] : 0;
            _output.WriteLine($"  Round {i + 1,2}: {samples[i],12:N0} bytes (Δ {delta:+#,0;-#,0;0})");
        }

        // Check 1: total growth from first to last sample should be small
        var totalGrowth = samples[^1] - samples[0];
        _output.WriteLine($"Total growth (first→last): {totalGrowth:N0} bytes");
        Assert.True(totalGrowth < 2_000_000,
            $"Memory grew by {totalGrowth:N0} bytes over {measureRounds} rounds — possible leak");

        // Check 2: detect monotonic growth — count how many consecutive samples increased
        int consecutiveIncreases = 0;
        int maxConsecutiveIncreases = 0;
        for (int i = 1; i < samples.Length; i++)
        {
            if (samples[i] > samples[i - 1])
            {
                consecutiveIncreases++;
                maxConsecutiveIncreases = Math.Max(maxConsecutiveIncreases, consecutiveIncreases);
            }
            else
            {
                consecutiveIncreases = 0;
            }
        }

        _output.WriteLine($"Max consecutive increases: {maxConsecutiveIncreases}");
        // If memory increased monotonically for 10+ consecutive rounds, that's suspicious
        Assert.True(maxConsecutiveIncreases < 10,
            $"Memory increased {maxConsecutiveIncreases} consecutive rounds — likely monotonic leak");
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public async Task LeakTest_FullPipeline_PerCodec_NoGrowth()
    {
        // Test each codec individually for leaks.
        // Global warmup across ALL codecs first to stabilize JIT, native allocators,
        // and GC state — prevents first-codec false positives from prior test residue.
        var encoders = AllEncoders;
        const int globalWarmup = 5;
        const int perCodecWarmup = 20;
        const int iterations = 100;

        // Global warmup: exercise every codec so JIT and native libs are fully initialized
        for (int w = 0; w < globalWarmup; w++)
        {
            foreach (var encoder in encoders)
            {
                using var job = new ImageJob();
                await job.Decode(TinyPng)
                    .ConstrainWithin(1, 1)
                    .EncodeToBytes(encoder)
                    .Finish()
                    .InProcessAsync();
            }
        }
        GetManagedMemory(); // settle GC after global warmup

        foreach (var encoder in encoders)
        {
            var name = encoder.GetType().Name;

            for (int i = 0; i < perCodecWarmup; i++)
            {
                using var job = new ImageJob();
                await job.Decode(TinyPng)
                    .ConstrainWithin(1, 1)
                    .EncodeToBytes(encoder)
                    .Finish()
                    .InProcessAsync();
            }

            var baseline = GetManagedMemory();

            for (int i = 0; i < iterations; i++)
            {
                using var job = new ImageJob();
                await job.Decode(TinyPng)
                    .ConstrainWithin(1, 1)
                    .EncodeToBytes(encoder)
                    .Finish()
                    .InProcessAsync();
            }

            var after = GetManagedMemory();
            var growth = after - baseline;
            _output.WriteLine($"{name,-25} Baseline: {baseline,12:N0}  After: {after,12:N0}  Growth: {growth,8:N0}");

            // 5MB threshold: generous enough to absorb GC noise on CI runners (parallel TFMs,
            // shared memory pressure). A real per-codec leak over 100 iterations would far exceed this.
            // The all-codecs monotonic test is the primary leak guard.
            Assert.True(growth < 5_000_000,
                $"{name}: memory grew by {growth:N0} bytes over {iterations} iterations — possible leak");
        }
    }

    [Fact]
    [Trait("Category", "LeakTest")]
    public void LeakTest_Issue6_SerializeNode_NoGrowth()
    {
        const int warmup = 100;
        const int iterations = 5000;

        for (int i = 0; i < warmup; i++)
        {
            var node = new System.Text.Json.Nodes.JsonObject { ["i"] = i };
            JobContext.SerializeNode(node);
        }

        var baseline = GetManagedMemory();

        for (int i = 0; i < iterations; i++)
        {
            var node = new System.Text.Json.Nodes.JsonObject { ["i"] = i };
            JobContext.SerializeNode(node);
        }

        var after = GetManagedMemory();
        var growth = after - baseline;
        _output.WriteLine($"Baseline: {baseline:N0}, After {iterations} serializations: {after:N0}, Growth: {growth:N0}");
        Assert.True(growth < 5_000_000,
            $"Managed memory grew by {growth:N0} bytes — possible Utf8JsonWriter buffer leak");
    }

    // ═══════════════════════════════════════════════════════════
    // Helper types
    // ═══════════════════════════════════════════════════════════

    private sealed class TrackingMemoryOwner : IMemoryOwner<byte>
    {
        private readonly byte[] _data;
        public bool IsDisposed { get; private set; }
        public TrackingMemoryOwner(byte[] data) => _data = data;
        public Memory<byte> Memory => _data;
        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// A MemoryManager that counts Pin/Unpin calls, letting us observe
    /// exactly how many times AddInputBytesPinned pins the data.
    /// </summary>
    private sealed class CountingMemoryManager : MemoryManager<byte>
    {
        private readonly byte[] _data;
        private GCHandle _gcHandle;
        public int PinCount;
        public int UnpinCount;

        public CountingMemoryManager(byte[] data)
        {
            _data = data;
            _gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        }

        public override Span<byte> GetSpan() => _data;

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            Interlocked.Increment(ref PinCount);
            unsafe
            {
                return new MemoryHandle((byte*)_gcHandle.AddrOfPinnedObject() + elementIndex, default, this);
            }
        }

        public override void Unpin()
        {
            Interlocked.Increment(ref UnpinCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (_gcHandle.IsAllocated) _gcHandle.Free();
        }
    }
}
