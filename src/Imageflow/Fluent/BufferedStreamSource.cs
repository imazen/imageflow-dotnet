using Imageflow.Internal.Helpers;
using Microsoft.IO;

namespace Imageflow.Fluent;

public sealed class BufferedStreamSource : IAsyncMemorySource, IMemorySource
{
    private BufferedStreamSource(Stream stream, bool disposeUnderlying, bool seekToStart = true)
    {
        if (stream.Position != 0 && !stream.CanSeek && seekToStart)
        {
            throw new ArgumentException("Stream must be seekable if seekToStart is true");
        }
        var length = stream.CanSeek ? stream.Length : 0;
        if (length >= int.MaxValue) throw new OverflowException("Streams cannot exceed 2GB");
        
        _underlying = stream;
        _disposeUnderlying = disposeUnderlying;
        _seekToStart = seekToStart;
    }
    
    private readonly bool _seekToStart;
    private readonly Stream _underlying;
    private readonly bool _disposeUnderlying;

    private static readonly RecyclableMemoryStreamManager Mgr
        = new();

    private RecyclableMemoryStream? _copy;

    public void Dispose()
    {
        if (_disposeUnderlying)
        {
            _underlying?.Dispose();
        }

        _copy?.Dispose();
    }
    
    private bool TryGetWrittenMemory(
        out ReadOnlyMemory<byte> memory)
    {
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
        if (TryGetWrittenMemory(out var segment))
        {
            return segment;
        }
        if (_seekToStart)
        {
            _underlying.Seek(0, SeekOrigin.Begin);
        }
        _copy = new RecyclableMemoryStream(Mgr, "BufferedStreamSource: IMemorySource", _underlying.CanSeek ? _underlying.Length : 0);
        await _underlying.CopyToAsync(_copy, 81920, cancellationToken);
        _copy.Seek(0, SeekOrigin.Begin);
        if (!TryGetWrittenMemory(out segment))
        {
            throw new InvalidOperationException("Could not get RecyclableMemoryStream buffer; please report this bug to support@imazen.io");
        }        
        return segment;
    }
    
    public ReadOnlyMemory<byte> BorrowReadOnlyMemory()
    {
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


    public static IAsyncMemorySource BorrowEntireStream(Stream stream)
    {
        return new BufferedStreamSource(stream, false, true);
    }
    public static IAsyncMemorySource BorrowStreamRemainder(Stream stream)
    {
        return new BufferedStreamSource(stream, false, false);
    }
    public static IAsyncMemorySource UseEntireStreamAndDispose(Stream stream)
    {
        return new BufferedStreamSource(stream, true, true);
    }
    public static IAsyncMemorySource UseStreamRemainderAndDispose(Stream stream)
    {
        return new BufferedStreamSource(stream, true, false);
    }
}