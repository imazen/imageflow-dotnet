using Imageflow.Internal.Helpers;
using Microsoft.IO;

namespace Imageflow.Fluent;

/// <summary>
/// Represents a image source that is backed by a Stream. 
/// </summary>
[Obsolete("Use BufferedStreamSource.UseStreamRemainderAndDispose() or BufferedStreamSource.BorrowStreamRemainder() instead")]
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