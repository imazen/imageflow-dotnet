using System.Buffers;
using System.Runtime.InteropServices;
using Imageflow.Bindings;
using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent
{
    public interface IOutputDestination : IDisposable
    {
        Task RequestCapacityAsync(int bytes);
        Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }



    // ReSharper disable once InconsistentNaming
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
        
            var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920,data.Length));
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

            var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920,data.Length));
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
        
        public static async Task CopyFromStreamAsync(this IOutputDestination dest, Stream stream,
            CancellationToken cancellationToken)
        {
            if (stream is { CanRead: true, CanSeek: true })
            {
                await dest.RequestCapacityAsync((int) stream.Length);
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
            if (_m == null) throw new ImageflowAssertionFailed("BytesDestination.WriteAsync called before RequestCapacityAsync");
            if (bytes.Array == null) throw new ImageflowAssertionFailed("BytesDestination.WriteAsync called with null array");
            return _m.WriteAsync(bytes.Array, bytes.Offset, bytes.Count, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_m == null) throw new ImageflowAssertionFailed("BytesDestination.FlushAsync called before RequestCapacityAsync");
            return _m.FlushAsync(cancellationToken);
        }
        
        public ArraySegment<byte> GetBytes()
        {
            if (_m == null) throw new ImageflowAssertionFailed("BytesDestination.GetBytes called before RequestCapacityAsync");
            if (!_m.TryGetBuffer(out var bytes))
            {
                throw new ImageflowAssertionFailed("MemoryStream TryGetBuffer should not fail here");
            }
            return bytes;
        }

        public void RequestCapacity(int bytes)
        {
            _m ??= new MemoryStream(bytes);
            if (_m.Capacity < bytes) _m.Capacity = bytes;
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (_m == null) throw new ImageflowAssertionFailed("BytesDestination.Write called before RequestCapacity");
            _m.WriteSpan(data);
        }
        public ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            if (_m == null) throw new ImageflowAssertionFailed("BytesDestination.FastWriteAsync called before RequestCapacityAsync");
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

    public class StreamDestination(Stream underlying, bool disposeUnderlying) : IOutputDestination
    {
        public void Dispose()
        {
            if (disposeUnderlying) underlying?.Dispose();
        }

        public Task RequestCapacityAsync(int bytes)
        {
            if (underlying is { CanSeek: true, CanWrite: true }) underlying.SetLength(bytes);
            return Task.CompletedTask;
        }

        public Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
        {
            if (bytes.Array == null) throw new ImageflowAssertionFailed("StreamDestination.WriteAsync called with null array");
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
                underlying.SetLength(underlying.Position);
            return underlying.FlushAsync(cancellationToken);
        }
    }
}