using System.Buffers;
using Imageflow.Internal.Helpers;
using Microsoft.IO;

namespace Imageflow.Fluent
{
    [Obsolete("Use IMemorySource instead")]
    public interface IBytesSource: IDisposable
    {
        /// <summary>
        /// Return a reference to a byte array that (until the implementor is disposed) will (a) remain immutable, and (b) can be GC pinned.
        /// </summary>
        /// <returns></returns>
        Task<ArraySegment<byte>> GetBytesAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents a source backed by an ArraySegment or byte[] array. Use MemorySource for ReadOnlyMemory<byte> backed memory instead.
    /// </summary>
    [Obsolete("Use MemorySource instead")]
    public readonly struct BytesSource : IBytesSource
    {
        public BytesSource(byte[] bytes)
        {
            _bytes = new ArraySegment<byte>(bytes, 0, bytes.Length);
        }
        public BytesSource(byte[] bytes, int offset, int length)
        {
            _bytes = new ArraySegment<byte>(bytes, offset, length);
        }
        public BytesSource(ArraySegment<byte> bytes)
        {
            _bytes = bytes;
        }
        
        private readonly ArraySegment<byte> _bytes;

        public void Dispose()
        {
        }

        public Task<ArraySegment<byte>> GetBytesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_bytes);
        }
        
        public static implicit operator MemorySource(BytesSource source)
        {
            return new MemorySource(source._bytes);
        }
    }
    /// <summary>
    /// Represents a image source that is backed by a Stream. 
    /// </summary>
    [Obsolete("Use BufferedStreamSource instead")]
    public class StreamSource(Stream underlying, bool disposeUnderlying) : IBytesSource
    {
        private static readonly RecyclableMemoryStreamManager Mgr
            = new();
        private RecyclableMemoryStream? _copy;

        public void Dispose()
        {
            if (disposeUnderlying)
            {
                underlying?.Dispose();
            }
            _copy?.Dispose();
        }

        /// <summary>
        /// Note that bytes will not be valid after StreamSource is disposed
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"></exception>
        public async Task<ArraySegment<byte>> GetBytesAsync(CancellationToken cancellationToken)
        {
            if (_copy != null)
            {
                return new ArraySegment<byte>(_copy.GetBuffer(), 0,
                    (int) _copy.Length);
            }
            var length = underlying.CanSeek ? underlying.Length : 0;
            if (length >= int.MaxValue) throw new OverflowException("Streams cannot exceed 2GB");
            
            if (underlying is MemoryStream underlyingMemoryStream &&
                underlyingMemoryStream.TryGetBufferSliceAllWrittenData(out var underlyingBuffer))
            {
                return underlyingBuffer;
            }

            if (_copy == null)
            {
                _copy = new RecyclableMemoryStream(Mgr,"StreamSource: IBytesSource", length);
                await underlying.CopyToAsync(_copy,81920, cancellationToken);
            }
            
            return new ArraySegment<byte>(_copy.GetBuffer(), 0,
                (int) _copy.Length);
        }

        internal bool AsyncPreferred => _copy != null && underlying is not MemoryStream && underlying is not UnmanagedMemoryStream;

        public static implicit operator BytesSourceAdapter(StreamSource source)
        {
            return new BytesSourceAdapter(source);
        }
    }
    
    public static class BytesSourceExtensions
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public static IAsyncMemorySource ToMemorySource(this IBytesSource source)
#pragma warning restore CS0618 // Type or member is obsolete
        {
           return new BytesSourceAdapter(source);
        }
    }

}

