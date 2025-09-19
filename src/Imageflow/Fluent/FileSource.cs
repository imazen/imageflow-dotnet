namespace Imageflow.Fluent;

public sealed class FileSource : IAsyncMemorySource, IMemorySource
{
    // We use File.ReadAllBytesAsync on .net standard 2.1, falling back to     BufferedStreamSource on .net standard 2.0

    private readonly string _path;
    private IAsyncMemorySource? _underlying;

    public FileSource(string path)
    {
        _path = path;
    }

    public static FileSource FromPath(string path) => new(path);

    public bool AsyncPreferred => true;

    // Opens the file unbuffered, since we provide one massive buffer ourselves.
    private static FileStream ReadUnbuffered(string path)
    {
#if NET6_0_OR_GREATER
        var options = new System.IO.FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = 0,
            Options = FileOptions.Asynchronous | (IsWindows ? FileOptions.SequentialScan : FileOptions.None),
        };
        return File.Open(path, options);
#else
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.Asynchronous | (IsWindows ? FileOptions.SequentialScan : FileOptions.None));
#endif
    }

    public async ValueTask<ReadOnlyMemory<byte>> BorrowReadOnlyMemoryAsync(CancellationToken cancellationToken)
    {
#if NETSTANDARD2_1
    return await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
#else
        _underlying ??= BufferedStreamSource.UseEntireStreamAndDisposeWithSource(ReadUnbuffered(_path));
        return await _underlying.BorrowReadOnlyMemoryAsync(cancellationToken).ConfigureAwait(false);
#endif
    }

    public ReadOnlyMemory<byte> BorrowReadOnlyMemory()
    {
        return File.ReadAllBytes(_path);
    }

    public void Dispose()
    {
        _underlying?.Dispose();
        _underlying = null;
    }

    internal static bool IsWindows =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsWindows();
#else
        System.Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif
}
