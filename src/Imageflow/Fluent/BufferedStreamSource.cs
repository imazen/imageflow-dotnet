using Imageflow.Internal.Helpers;

using Microsoft.IO;

namespace Imageflow.Fluent;

public sealed class BufferedStreamSource : IAsyncMemorySource, IMemorySource
{
    private BufferedStreamSource(Stream stream, bool disposeUnderlying, bool seekToStart)
    {
        if (stream.Position != 0 && !stream.CanSeek && seekToStart)
        {
            throw new ArgumentException("Stream must be seekable if seekToStart is true");
        }
        var length = stream.CanSeek ? stream.Length : 0;
        if (length >= int.MaxValue)
        {
            throw new OverflowException("Streams cannot exceed 2GB");
        }

        _underlying = stream;
        _disposeUnderlying = disposeUnderlying;
        _seekToStart = seekToStart;
    }

    private readonly bool _seekToStart;
    private Stream? _underlying;
    private readonly bool _disposeUnderlying;

    private static readonly RecyclableMemoryStreamManager Mgr
        = new();

    private RecyclableMemoryStream? _copy;

    public void Dispose()
    {
        if (_disposeUnderlying)
        {
            _underlying?.Dispose();
            _underlying = null;
        }

        _copy?.Dispose();
        _copy = null;

    }

    private bool TryGetWrittenMemory(
        out ReadOnlyMemory<byte> memory)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        var memStream = _underlying as MemoryStream ?? _copy;
        if (memStream != null)
        {
            // If it's a recyclable memory stream, it will cache the buffer no matter how many times we call it.
            if (_seekToStart ? memStream.TryGetBufferSliceAllWrittenData(out var segment)
                    : memStream.TryGetBufferSliceAfterPosition(out segment))
            {
                memory = segment;
                return true;
            }
            throw new OverflowException("Streams cannot exceed 2GB");
        }
        memory = default;
        return false;
    }

    /// <summary>
    /// Note that bytes will not be valid after BufferedStreamSource is disposed
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="OverflowException"></exception>
    public async ValueTask<ReadOnlyMemory<byte>> BorrowReadOnlyMemoryAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        if (TryGetWrittenMemory(out var segment))
        {
            return segment;
        }
        if (_seekToStart)
        {
            _underlying.Seek(0, SeekOrigin.Begin);
        }
        _copy = new RecyclableMemoryStream(Mgr, "BufferedStreamSource: IMemorySource", _underlying.CanSeek ? _underlying.Length : 0);
        await _underlying.CopyToAsync(_copy, 81920, cancellationToken).ConfigureAwait(false);
        _copy.Seek(0, SeekOrigin.Begin);
        if (!TryGetWrittenMemory(out segment))
        {
            throw new InvalidOperationException("Could not get RecyclableMemoryStream buffer; please report this bug to support@imazen.io");
        }
        return segment;
    }

    public ReadOnlyMemory<byte> BorrowReadOnlyMemory()
    {
        ObjectDisposedHelper.ThrowIf(_underlying == null, this);
        if (TryGetWrittenMemory(out var segment))
        {
            return segment;
        }
        if (_seekToStart)
        {
            _underlying.Seek(0, SeekOrigin.Begin);
        }

        _copy = new RecyclableMemoryStream(Mgr, "BufferedStreamSource: IMemorySource", _underlying.CanSeek ? _underlying.Length : 0);
        _underlying.CopyTo(_copy);
        _copy.Seek(0, SeekOrigin.Begin);
        if (!TryGetWrittenMemory(out segment))
        {
            throw new InvalidOperationException("Could not get RecyclableMemoryStream buffer; please report this bug to support@imazen.io");
        }
        return segment;
    }

    public bool AsyncPreferred => _underlying is not MemoryStream && _underlying is not UnmanagedMemoryStream;

    /// <summary>
    /// Seeks to the beginning of the stream before reading.
    /// You swear not to close, dispose, or reuse the stream or its underlying memory/stream until after this wrapper and the job are disposed.
    /// <strong>You remain responsible for disposing and cleaning up the stream after the job is disposed.</strong>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IAsyncMemorySource BorrowEntireStream(Stream stream)
    {
        return new BufferedStreamSource(stream, false, true);
    }
    /// <summary>
    /// <strong>You remain responsible for disposing and cleaning up the stream after the job is disposed.</strong>
    /// Only reads from the current position to the end of the image file.
    /// You swear not to close, dispose, or reuse the stream or its underlying memory/stream until after this wrapper and the job are disposed.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IAsyncMemorySource BorrowStreamRemainder(Stream stream)
    {
        return new BufferedStreamSource(stream, false, false);
    }

    /// <summary>
    /// The stream will be closed and disposed with the BufferedStreamSource. You must not close, dispose, or reuse the stream or its underlying streams/buffers until after the job and the owning objects are disposed.
    /// <remarks>You must not close, dispose, or reuse the stream or its underlying streams/buffers until after the job and the owning objects are disposed. <br/>
    ///
    /// The BufferedStreamSource will still need to be disposed after the job, either with a using declaration or by transferring ownership of it to the job (which should be in a using declaration).</remarks>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IAsyncMemorySource UseEntireStreamAndDisposeWithSource(Stream stream)
    {
        return new BufferedStreamSource(stream, true, true);
    }

    /// <summary>
    /// The stream will be closed and disposed with the BufferedStreamSource.
    /// <remarks>You must not close, dispose, or reuse the stream or its underlying streams/buffers until after the job and the owning objects are disposed.
    /// The BufferedStreamSource will still need to be disposed after the job, either with a using declaration or by transferring ownership of it to the job (which should be in a using declaration).</remarks>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static IAsyncMemorySource UseStreamRemainderAndDisposeWithSource(Stream stream)
    {
        return new BufferedStreamSource(stream, true, false);
    }
}
