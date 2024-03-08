using Imageflow.Bindings;
using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent;

public class BytesDestination : IOutputDestination, IOutputSink, IAsyncOutputSink
{
    private MemoryStream? _m;
    public void Dispose()
    {

    }

    public Task RequestCapacityAsync(int bytes)
    {
        RequestCapacity(bytes);
        return Task.CompletedTask;
    }

    public Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
    {
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
        _m ??= new MemoryStream(bytes);
        if (_m.Capacity < bytes)
        {
            _m.Capacity = bytes;
        }
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.Write called before RequestCapacity");
        }

        _m.WriteSpan(data);
    }
    public ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_m == null)
        {
            throw new ImageflowAssertionFailed("BytesDestination.FastWriteAsync called before RequestCapacityAsync");
        }

        return _m.WriteMemoryAsync(data, cancellationToken);
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
    public ValueTask FastFlushAsync(CancellationToken cancellationToken)
    {
        Flush();
        return new ValueTask();
    }
}
