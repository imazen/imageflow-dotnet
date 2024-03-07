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
    /// Represents a source backed by an ArraySegment or byte[] array. Use MemorySource for ReadOnlyMemory&lt;byte> backed memory instead.
    /// </summary>
    [Obsolete("Use MemorySource.Borrow(bytes, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed) instead")]
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

