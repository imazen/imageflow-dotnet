using Imageflow.Bindings;
using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent;

public class BytesDestination : IOutputDestination, IOutputSink, IAsyncOutputSink
{
    private MemoryStream? _m;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _m?.Dispose();
        _m = null;
    }

    // CA1513 wants ObjectDisposedException.ThrowIf, but that's net7.0+ only
#pragma warning disable CA1513
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BytesDestination));
    }
#pragma warning restore CA1513

    public Task RequestCapacityAsync(int bytes)
    {
        RequestCapacity(bytes);
        return Task.CompletedTask;
    }

    public Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.WriteAsync called before RequestCapacityAsync");
        }

        if (bytes.Array == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.WriteAsync called with null array");
        }

        return _m.WriteAsync(bytes.Array, bytes.Offset, bytes.Count, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.FlushAsync called before RequestCapacityAsync");
        }

        return _m.FlushAsync(cancellationToken);
    }

    public ArraySegment<byte> GetBytes()
    {
        ThrowIfDisposed();
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.GetBytes called before RequestCapacityAsync");
        }

        if (!_m.TryGetBuffer(out var bytes))
        {
            throw new ImageflowAssertionFailed("MemoryStream TryGetBuffer should not fail here");
        }
        return bytes;
    }

    public void RequestCapacity(int bytes)
    {
        ThrowIfDisposed();
        _m ??= new MemoryStream(bytes);
        if (_m.Capacity < bytes)
        {
            _m.Capacity = bytes;
        }
    }

    public void SetHints(OutputSinkHints hints)
    {
        RequestCapacity((int)Math.Min(hints.ExpectedSize ?? 0, int.MaxValue));
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.Write called before RequestCapacity");
        }

        _m.WriteSpan(data);
    }
    public bool PreferSynchronousWrites => true;
    public void Write(ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.Write called before RequestCapacity");
        }

        _m.WriteMemory(data);
    }

    public ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.FastWriteAsync called before RequestCapacityAsync");
        }

        return _m.WriteMemoryAsync(data, cancellationToken);
    }
    public void Finished()
    {
        _m?.Flush(); // Redundant for MemoryStream.
    }
    public void Flush()
    {
        _m?.Flush(); // Redundant for MemoryStream.
    }

    public ValueTask FastRequestCapacityAsync(int bytes)
    {
        RequestCapacity(bytes);
        return new ValueTask();
    }

    public ValueTask FinishedAsync(CancellationToken cancellationToken)
    {
        Finished();
        return new ValueTask();
    }

    public ValueTask FastFlushAsync(CancellationToken cancellationToken)
    {
        Flush();
        return new ValueTask();
    }
}
