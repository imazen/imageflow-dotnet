using Imageflow.Bindings;
using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent;

public class StreamDestination(Stream underlying, bool disposeUnderlying) : IOutputDestination
{
    private Stream? _underlying = underlying;

    public void Dispose()
    {
        if (disposeUnderlying)
        {
            _underlying?.Dispose();
        }
        _underlying = null!;
    }

    public Task RequestCapacityAsync(int bytes)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        if (_underlying is { CanSeek: true, CanWrite: true })
        {
            _underlying.SetLength(bytes);
        }

        return Task.CompletedTask;
    }

    public Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
    {
        if (bytes.Array == null)
        {
            throw new ImageflowAssertionFailed("StreamDestination.WriteAsync called with null array");
        }
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        return _underlying.WriteAsync(bytes.Array, bytes.Offset, bytes.Count, cancellationToken);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        return _underlying.WriteMemoryAsync(bytes, cancellationToken);
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        _underlying.WriteSpan(bytes);
    }

    public Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        // Truncate the stream if it's seekable and we haven't written to the end
        if (_underlying is { CanSeek: true, CanWrite: true }
            && _underlying.Position < _underlying.Length)
        {
            _underlying.SetLength(_underlying.Position);
        }

        return _underlying.FlushAsync(cancellationToken);
    }
}
