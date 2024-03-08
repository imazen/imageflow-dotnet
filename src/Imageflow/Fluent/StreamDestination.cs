using Imageflow.Bindings;
using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent;

public class StreamDestination(Stream underlying, bool disposeUnderlying) : IOutputDestination
{
    public void Dispose()
    {
        if (disposeUnderlying)
        {
            underlying?.Dispose();
        }
    }

    public Task RequestCapacityAsync(int bytes)
    {
        if (underlying is { CanSeek: true, CanWrite: true })
        {
            underlying.SetLength(bytes);
        }

        return Task.CompletedTask;
    }

    public Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
    {
        if (bytes.Array == null)
        {
            throw new ImageflowAssertionFailed("StreamDestination.WriteAsync called with null array");
        }

        return underlying.WriteAsync(bytes.Array, bytes.Offset, bytes.Count, cancellationToken);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        return underlying.WriteMemoryAsync(bytes, cancellationToken);
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        underlying.WriteSpan(bytes);
    }

    public Task FlushAsync(CancellationToken cancellationToken)
    {
        if (underlying is { CanSeek: true, CanWrite: true }
            && underlying.Position < underlying.Length)
        {
            underlying.SetLength(underlying.Position);
        }

        return underlying.FlushAsync(cancellationToken);
    }
}
