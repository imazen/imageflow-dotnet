using Imageflow.Bindings;
using Imageflow.Internal.Helpers;
using Microsoft.Win32.SafeHandles;

namespace Imageflow.Fluent;

public sealed class FileDestination : IOutputDestination, IAsyncOutputSink, IOutputSink
{
    public FileDestination(string path) : this(path, useSfh: true, useHardFlush: false, shareRead: true) { }
    private FileDestination(string path, bool useSfh, bool useHardFlush, bool shareRead)
    {
        _path = path;
#if NET6_0_OR_GREATER
        _useSfh = useSfh;
#else
        _useSfh = false;
#endif
        _useHardFlush = useHardFlush;
        _shareRead = shareRead;
    }
    public static FileDestination ToPath(string path) => new(path);
    internal static FileDestination ToPath(string path, bool useSfh = true, bool useHardFlush = false, bool shareRead = true) => new(path, useSfh, useHardFlush, shareRead);
    private readonly string _path;
    public string Path => _path;

    // We can benchmark our implementation vs File.WriteAllBytes(Async)
    private readonly bool _useSfh;
    // Force write, bypassing OS cache, overkill.
    private readonly bool _useHardFlush;
    private readonly bool _shareRead;
    private FileStream? _stream;
    private SafeFileHandle? _handle;

    private int _writeCallCount;
    private long? _position;
    private long? _pendingPosition;
    private bool? _asynchronous;
    private long _requestedCapacityHint;

    private bool _finishCalled;


    public void Finished()
    {
        if (_useHardFlush)
        {
#if NET6_0_OR_GREATER
            if (_handle != null) RandomAccess.FlushToDisk(_handle);
#endif
            _stream?.Flush(_useHardFlush);
        }
        _stream?.Dispose();
        _stream = null;
        _handle?.Dispose();
        _handle = null;
        _finishCalled = true;
    }

    public async ValueTask FinishedAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        if (_useHardFlush && _stream != null)
        {
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        if (_stream != null)
        {
#if NETSTANDARD2_1
            await _stream.DisposeAsync().ConfigureAwait(false);
#else
            _stream.Dispose();
#endif
            _stream = null;
        }
        if (_handle != null)
        {
            _handle.Dispose();
            _handle = null;
        }
        _finishCalled = true;
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_writeCallCount > 1) throw new InvalidOperationException("FileDestination not tested with multiple writes yet, even if implementation is desgined accordingly.");
        _disposed = true;
        _position = null;
        _pendingPosition = null;
        _asynchronous = null;
        _stream?.Dispose();
        _stream = null;
        _handle?.Dispose();
        _handle = null;
    }


    private FileStream CreateOpen(long preallocationSize, bool asynchronous)
    {
        _asynchronous = asynchronous;
        _requestedCapacityHint = preallocationSize;
        var fileOptions = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        if (FileSource.IsWindows) fileOptions |= FileOptions.SequentialScan;
#if NET6_0_OR_GREATER
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = _shareRead ? FileShare.Read : FileShare.None, // Unsure if none is more performant?
            BufferSize = 0, //Buffering not useful, we're writing it all at once.
            PreallocationSize = preallocationSize,
            Options = fileOptions,

        };
        return File.Open(Path, options);
#else
        return new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.Read, 1, fileOptions);
#endif
    }

#if NET6_0_OR_GREATER
    private SafeFileHandle OpenHandle(long preallocationSize, bool asynchronous)
    {

        _asynchronous = asynchronous;
        _requestedCapacityHint = preallocationSize;
        var fileOptions = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        if (FileSource.IsWindows) fileOptions |= FileOptions.SequentialScan;
        return File.OpenHandle(Path, FileMode.Create, FileAccess.Write, _shareRead ? FileShare.Read : FileShare.None, fileOptions, preallocationSize);
        throw new NotSupportedException("OpenHandle not supported on .net standard 2.0");
    }
#endif

    private void Prepare(int bytes, bool asynchronous)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        if (_finishCalled) throw new InvalidOperationException("FileDestination.Finished already called, no more writes allowed.");
        if (!_useSfh)
        {
            _stream ??= CreateOpen(bytes, asynchronous);
        }
        else
        {
#if NET6_0_OR_GREATER
            _handle ??= OpenHandle(bytes, asynchronous);
#else
            throw new NotSupportedException("RandomAccess not supported on this .NET version");
#endif
        }
        _position ??= 0;
    }

    public Task RequestCapacityAsync(int bytes)
    {
        Prepare(bytes, asynchronous: true);
        return Task.CompletedTask;
    }
    public ValueTask FastRequestCapacityAsync(int bytes)
    {
        Prepare(bytes, asynchronous: true);
        return default;
    }
    public void RequestCapacity(int bytes)
    {
        Prepare(bytes, asynchronous: false);
    }


    public async Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken)
    {
        if (bytes.Array == null)
        {
            throw new ImageflowAssertionFailed("StreamDestination.WriteAsync called with null array");
        }
        await FastWriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
    }


    public async ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        Prepare(data.Length, asynchronous: true);
        _writeCallCount++;
        _position ??= 0;
        if (_pendingPosition.HasValue) throw new InvalidOperationException("Write already pending; cannot issue overlapping write calls");
        _pendingPosition = _position + data.Length;
        if (!_useSfh)
        {
            await _stream!.WriteMemoryAsync(data, cancellationToken).ConfigureAwait(false);
        }
#if NET6_0_OR_GREATER
        else
        {
            await RandomAccess.WriteAsync(_handle!, data, _position.Value, cancellationToken).ConfigureAwait(false);
        }
#endif
        if (_pendingPosition != _position + data.Length) throw new InvalidOperationException("Write already pending; cannot issue overlapping write calls");
        _position = _pendingPosition;
        _pendingPosition = null;
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        Prepare(data.Length, asynchronous: false);
        _writeCallCount++;
        _position ??= 0;
        if (_pendingPosition.HasValue) throw new InvalidOperationException("Write already pending; cannot issue overlapping write calls");
        _pendingPosition = _position + data.Length;
        if (!_useSfh)
        {
            _stream!.WriteSpan(data);
        }
#if NET6_0_OR_GREATER
        else
        {
            RandomAccess.Write(_handle!, data, _position!.Value);
        }
#endif
        if (_pendingPosition != _position + data.Length) throw new InvalidOperationException("Write already pending; cannot issue overlapping write calls");
        _position = _pendingPosition;
        _pendingPosition = null;
    }

    public Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        if (_finishCalled) throw new InvalidOperationException("FileDestination.Finished already called, no more writes allowed.");
        if (_stream != null)
        {
            return _stream.FlushAsync(cancellationToken);
        }
        if (_handle != null)
        {
            if (_useHardFlush)
            {
#if NET6_0_OR_GREATER
                RandomAccess.FlushToDisk(_handle);
#else
                throw new NotSupportedException("SafeFileHandle not supported on this .NET version");
#endif
            }
        }
        return Task.CompletedTask;
    }

}
