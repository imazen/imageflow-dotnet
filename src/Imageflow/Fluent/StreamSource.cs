using Imageflow.Internal.Helpers;

using Microsoft.IO;

namespace Imageflow.Fluent;

/// <summary>
/// Represents a image source that is backed by a Stream.
/// </summary>
[Obsolete("Use BufferedStreamSource.UseStreamRemainderAndDispose() or BufferedStreamSource.BorrowStreamRemainder() instead")]
public class StreamSource(Stream underlying, bool disposeUnderlying) : IBytesSource
{
    private static readonly RecyclableMemoryStreamManager _mgr
        = new();
    private RecyclableMemoryStream? _copy;
    private Stream? _underlying = underlying;

    public void Dispose()
    {
        if (disposeUnderlying)
        {
            _underlying?.Dispose();
        }

        _underlying = null;
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
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        if (_copy != null)
        {
            return new ArraySegment<byte>(_copy.GetBuffer(), 0,
                (int)_copy.Length);
        }
        var length = _underlying.CanSeek ? _underlying.Length : 0;
        if (length >= int.MaxValue)
        {
            throw new OverflowException("Streams cannot exceed 2GB");
        }

        if (_underlying is MemoryStream underlyingMemoryStream &&
            underlyingMemoryStream.TryGetBufferSliceAllWrittenData(out var underlyingBuffer))
        {
            return underlyingBuffer;
        }

        if (_copy == null)
        {
            _copy = new RecyclableMemoryStream(_mgr, "StreamSource: IBytesSource", length);
            await _underlying.CopyToAsync(_copy, 81920, cancellationToken).ConfigureAwait(false);
        }

        return new ArraySegment<byte>(_copy.GetBuffer(), 0,
            (int)_copy.Length);
    }

    internal bool AsyncPreferred => _copy != null && _underlying is not MemoryStream && _underlying is not UnmanagedMemoryStream;

    public static implicit operator BytesSourceAdapter(StreamSource source)
    {
        return new BytesSourceAdapter(source);
    }
}
