using System.Buffers;
using System.Runtime.InteropServices;

namespace Imageflow.Fluent;

public static class IOutputDestinationExtensions
{
    internal static void AdaptiveWriteAll(this IOutputDestination dest, ReadOnlyMemory<byte> data)
    {
        if (dest is IOutputSink syncSink)
        {
            syncSink.RequestCapacity(data.Length);
            syncSink.Write(data.Span);
            syncSink.Flush();
        }
        else
        {
            dest.RequestCapacityAsync(data.Length).Wait();
            dest.AdaptedWrite(data.Span);
            dest.FlushAsync(default).Wait();
        }
    }

    internal static async ValueTask AdaptiveWriteAllAsync(this IOutputDestination dest, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (dest is IAsyncOutputSink sink)
        {
            await sink.FastRequestCapacityAsync(data.Length);
            await sink.FastWriteAsync(data, cancellationToken);
            await sink.FastFlushAsync(cancellationToken);
            return;
        }
        await dest.RequestCapacityAsync(data.Length);
        await dest.AdaptedWriteAsync(data, cancellationToken);
        await dest.FlushAsync(cancellationToken);
    }

    internal static async ValueTask AdaptedWriteAsync(this IOutputDestination dest, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment))
        {
            await dest.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            return;
        }

        var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920, data.Length));
        try
        {
            for (int i = 0; i < data.Length; i += rent.Length)
            {
                int len = Math.Min(rent.Length, data.Length - i);
                data.Span.Slice(i, len).CopyTo(rent);
                await dest.WriteAsync(new ArraySegment<byte>(rent, 0, len), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }

    }
    internal static void AdaptedWrite(this IOutputDestination dest, ReadOnlySpan<byte> data)
    {

        var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920, data.Length));
        try
        {
            for (int i = 0; i < data.Length; i += rent.Length)
            {
                int len = Math.Min(rent.Length, data.Length - i);
                data.Slice(i, len).CopyTo(rent);
                dest.WriteAsync(new ArraySegment<byte>(rent, 0, len), default).Wait();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    // internal static IAsyncOutputSink ToAsyncOutputSink(this IOutputDestination dest, bool disposeUnderlying = true)
    // {
    //     if (dest is IAsyncOutputSink sink) return sink;
    //     return new OutputDestinationToSinkAdapter(dest, disposeUnderlying);
    // }
    [Obsolete("Users should not write to IOutputDestination directly; this is only for Imageflow internal use.")]
    public static async Task CopyFromStreamAsync(this IOutputDestination dest, Stream stream,
        CancellationToken cancellationToken)
    => await dest.CopyFromStreamAsyncInternal(stream, cancellationToken);

    internal static async Task CopyFromStreamAsyncInternal(this IOutputDestination dest, Stream stream,
        CancellationToken cancellationToken)
    {
        if (stream is { CanRead: true, CanSeek: true })
        {
            await dest.RequestCapacityAsync((int)stream.Length);
        }

        const int bufferSize = 81920;
        var buffer = new byte[bufferSize];

        int bytesRead;
        while ((bytesRead =
                   await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await dest.WriteAsync(new ArraySegment<byte>(buffer, 0, bytesRead), cancellationToken)
                .ConfigureAwait(false);
        }

        await dest.FlushAsync(cancellationToken);
    }
}
