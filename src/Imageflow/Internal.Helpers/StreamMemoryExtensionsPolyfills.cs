using System.Buffers;
using System.Runtime.InteropServices;

namespace Imageflow.Internal.Helpers;

internal static class StreamMemoryExtensionsPolyfills
{
    internal static ValueTask WriteMemoryAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_1_OR_GREATER
        return stream.WriteAsync(buffer, cancellationToken);
#else
        if (cancellationToken.IsCancellationRequested)
        {
            return new(Task.FromCanceled(cancellationToken));
        }

        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
        {
            return new(stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken));
        }

        // Local function, same idea as above
        static async Task WriteAsyncFallback(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            byte[] rent = ArrayPool<byte>.Shared.Rent(buffer.Length);

            try
            {
                buffer.Span.CopyTo(rent);

                await stream.WriteAsync(rent, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rent);
            }
        }

        return new(WriteAsyncFallback(stream, buffer, cancellationToken));
#endif
    }


    internal static void WriteMemory(this Stream stream, ReadOnlyMemory<byte> buffer)
    {
#if NETSTANDARD2_1_OR_GREATER
        stream.Write(buffer.Span);
#else

        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
        {
            stream.Write(segment.Array!, segment.Offset, segment.Count);
            return;
        }

        byte[] rent = ArrayPool<byte>.Shared.Rent(buffer.Length);

        try
        {
            buffer.Span.CopyTo(rent);
            stream.Write(rent, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
#endif
    }
    
    internal static void WriteSpan(this Stream stream, ReadOnlySpan<byte> buffer)
    {
#if NETSTANDARD2_1_OR_GREATER
        stream.Write(buffer);
#else
        var rent = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(rent);
            stream.Write(rent, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
#endif
    }
}