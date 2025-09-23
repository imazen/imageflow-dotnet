using Imageflow.Bindings;
using Imageflow.Internal.Helpers;
using Microsoft.Win32.SafeHandles;

namespace Imageflow.Fluent;

public sealed record FileDestinationOptions(
    bool Atomic,
    bool ShareReadAccess,
    bool PreferRandomAccessApi
    );
// Findings: SequentialScan and Preallocation do not improve performance for single writes. WriteThrough +Flush(false) is faster than no write through but Flush(true)\
// sharing read access helps a little.
// Buffer size is irrelevant, 0/1 is fine.
// THis is all tilting at windmills, FileStream uses adaptive strategies and RandomAccess itself.

public sealed class FileDestination : IOutputDestination, IAsyncOutputSink, IOutputSink
{
    private static readonly FileDestinationOptions DefaultOptions = new(Atomic: false, ShareReadAccess: true, PreferRandomAccessApi: true);

    public FileDestination(string path) : this(path, DefaultOptions) { }
    private FileDestination(string path, FileDestinationOptions options)
    {
        _path = path;
#if NET6_0_OR_GREATER
        _useFileStream = !options.PreferRandomAccessApi;
#else
        _useFileStream = true;
#endif
        _options = options;
    }
    public static FileDestination ToPath(string path) => new(path);
    internal static FileDestination ToPath(string path, FileDestinationOptions options) => new(path, options);
    private readonly string _path;
    public string Path => _path;

    private readonly FileDestinationOptions _options;
    private OutputSinkHints? _writeHints;
#if NETSTANDARD2_1_OR_GREATER
    private bool PerformHardFlush => _options.Atomic && (_writeHints?.MultipleWritesExpected == true);
    private bool UseWriteThrough => _options.Atomic && (_writeHints == null || _writeHints?.MultipleWritesExpected == false);
#else
    private static bool PerformHardFlush => false; // Flush(True) is not available on .NET Standard 2.0
    private bool UseWriteThrough => _options.Atomic;  // WriteThrough should be
#endif

#if NET6_0_OR_GREATER
    private long PreallocationSize => (_writeHints?.MultipleWritesExpected == true && _writeHints?.ExpectedSize > 0) ? (long)(_writeHints?.ExpectedSize ?? 0) : 0;
#else
    private long PreallocationSize => 0;
#endif
    private readonly bool _useFileStream;


    private FileStream? _stream;
    private SafeFileHandle? _handle;

    private int _writeCallCount;
    private long? _position;
    private long? _pendingPosition;

    private bool _finishCalled;

    public void Finished()
    {
        if (PerformHardFlush)
        {
#if NET6_0_OR_GREATER
            if (_handle != null) RandomAccess.FlushToDisk(_handle);
#endif
#if NETSTANDARD2_1_OR_GREATER
            _stream?.Flush(true);
#endif
        }
        _stream?.Dispose(); //Auto soft flushes
        _stream = null;
        _handle?.Dispose();
        _handle = null;
        _finishCalled = true;
    }

    public ValueTask FinishedAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        _finishCalled = true;
        if (_handle != null)
        {
            if (PerformHardFlush)
            {
#if NET6_0_OR_GREATER
                RandomAccess.FlushToDisk(_handle);
#endif
            }
            _handle.Dispose();
            _handle = null;
        }
        if (_stream != null)
        {
            var tempStreamRef = _stream;
            _stream = null;

            if (PerformHardFlush)
            {
#if NETSTANDARD2_1_OR_GREATER
                return new ValueTask(Task.Run(() =>
                {
                    tempStreamRef.Flush(true);
                    tempStreamRef.Dispose();
                }, cancellationToken));
#endif
            } else {
#if NETSTANDARD2_1_OR_GREATER
                // It will flush before disposal, just not the OS buffers.
                return tempStreamRef.DisposeAsync();
#else
                tempStreamRef.Dispose(); // We can't access DisposeAsync OR Flush(true)
#endif
            }

        }
        return default;
    }
    /// <summary>
    /// Hopefully, you're calling FinishedAsync instead.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        if (_finishCalled) throw new InvalidOperationException("FileDestination.Finished already called, no more writes allowed.");
        if (_stream != null)
        {
            if (PerformHardFlush)
            {
#if NETSTANDARD2_1_OR_GREATER
                return Task.Run(
                        () => _stream!.Flush(true),
                    cancellationToken);
#endif
            }
            else
            {
                return _stream.FlushAsync(cancellationToken);
            }
        }
        if (_handle != null)
        {
            if (PerformHardFlush)
            {
#if NET6_0_OR_GREATER
                RandomAccess.FlushToDisk(_handle);
#endif
            }
        }
        return Task.CompletedTask;
    }

    private bool _disposed;
    public void Dispose()
    {
        _disposed = true;
        _position = null;
        _pendingPosition = null;
        if (!_finishCalled)
        {
            Finished();
        }
    }

    private FileStream CreateOpen(bool asynchronous, long firstWriteSize)
    {
        if (_writeHints == null) throw new InvalidOperationException("Write hints must be set before opening the file.");
        var fileOptions = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        if (PerformHardFlush) fileOptions |= FileOptions.WriteThrough;


        int bufferSize = 80 * 1024;
        if (_writeHints?.MultipleWritesExpected == false) {
            bufferSize = 1;
        }
        bufferSize = Math.Max(bufferSize, (int)(_writeHints?.ExpectedSize ?? 0));

        var fileShare = _options.ShareReadAccess ? FileShare.Read : FileShare.None;

#if NET6_0_OR_GREATER
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = fileShare,
            BufferSize = bufferSize,
            Options = fileOptions,
            PreallocationSize = PreallocationSize,

        };
        return new FileStream(Path, options);
#else

        return new FileStream(Path, FileMode.Create, FileAccess.Write, fileShare, bufferSize, fileOptions);
#endif
    }

#if NET6_0_OR_GREATER
    private SafeFileHandle OpenHandle(bool asynchronous)
    {

        var fileOptions = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        if (UseWriteThrough) fileOptions |= FileOptions.WriteThrough;
        return File.OpenHandle(Path, FileMode.Create, FileAccess.Write, _options.ShareReadAccess ? FileShare.Read : FileShare.None, fileOptions, PreallocationSize);
    }
#endif

    private void Prepare(long firstWriteSize, bool asynchronous)
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
        if (_finishCalled) throw new InvalidOperationException("FileDestination.Finished already called, no more writes allowed.");
        if (_useFileStream)
        {
            if (_writeHints == null){
                _writeHints = new OutputSinkHints(
                    ExpectedSize: null,
                    MultipleWritesExpected: false,
                    Asynchronous: asynchronous
                );
            }
            _stream ??= CreateOpen(asynchronous, firstWriteSize);
        }
        else
        {
#if NET6_0_OR_GREATER
            _handle ??= OpenHandle(asynchronous);
#else
            throw new NotSupportedException("RandomAccess not supported on this .NET version");
#endif
        }
        _position ??= 0;
    }

    public Task RequestCapacityAsync(int bytes)
    {
        _writeHints ??= new OutputSinkHints(ExpectedSize: bytes, MultipleWritesExpected: false, Asynchronous: true);
        _writeHints = _writeHints with { ExpectedSize = Math.Max(_writeHints!.ExpectedSize ?? 0, (long) bytes) };
        return Task.CompletedTask;
    }
    public void SetHints(OutputSinkHints hints)
    {
        if (_writeHints != null) throw new InvalidOperationException("FileDestination.SetHints cannot be called twice, or after a Write/FastWriteAsync/WriteAsync or RequestCapacityAsync.");
        _writeHints = hints;
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
        if (_useFileStream)
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
        if (_useFileStream)
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

}

