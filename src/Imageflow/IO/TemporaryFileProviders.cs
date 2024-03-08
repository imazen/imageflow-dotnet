using System.IO.MemoryMappedFiles;

namespace Imageflow.IO;

internal interface ITemporaryFileProvider
{
    ITemporaryFile Create(bool cleanup, long capacity);
}

internal interface ITemporaryFile : IDisposable
{
    string Path { get; }
    Stream WriteFromBeginning();
    Stream ReadFromBeginning();
}

internal class TemporaryMemoryFileProvider : ITemporaryFileProvider
{
    public ITemporaryFile Create(bool cleanup, long capacity)
    {
        if (!cleanup)
        {
            throw new InvalidOperationException("Memory Mapped Files cannot be persisted");
        }

        var name = Guid.NewGuid().ToString();
        var file = MemoryMappedFile.CreateNew(name, capacity);
        return new TemporaryMemoryFile(file, name);
    }
}

internal class TemporaryMemoryFile : ITemporaryFile
{
    private readonly MemoryMappedFile _file;
    public string Path { get; }

    internal TemporaryMemoryFile(MemoryMappedFile file, string path)
    {
        _file = file;
        Path = path;
    }

    public Stream WriteFromBeginning()
    {
        return ReadFromBeginning();
    }

    public Stream ReadFromBeginning()
    {
        var stream = _file.CreateViewStream();
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public static ITemporaryFileProvider CreateProvider()
    {
        return new TemporaryMemoryFileProvider();
    }

    public void Dispose()
    {
        _file?.Dispose();
    }
}

internal class TemporaryFileProvider : ITemporaryFileProvider
{
    public ITemporaryFile Create(bool cleanup, long capacity)
    {
        return new TemporaryFile(Path.GetTempFileName(), cleanup);
    }

}

internal class TemporaryFile : ITemporaryFile
{
    private readonly List<WeakReference<IDisposable>> _cleanup = new List<WeakReference<IDisposable>>(2);
    private bool _deleteOnDispose;

    internal TemporaryFile(string path, bool deleteOnDispose)
    {
        Path = path;
        _deleteOnDispose = deleteOnDispose;
    }

    public string Path { get; private set; }

    public Stream WriteFromBeginning()
    {
        var fs = new FileStream(Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, true);
        _cleanup.Add(new WeakReference<IDisposable>(fs));
        return fs;
    }

    public Stream ReadFromBeginning()
    {
        var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        _cleanup.Add(new WeakReference<IDisposable>(fs));
        return fs;
    }

    public static ITemporaryFileProvider CreateProvider()
    {
        return new TemporaryFileProvider();
    }

    public void Dispose()
    {
        foreach (var d in _cleanup)
        {
            if (d.TryGetTarget(out var disposable))
            {
                disposable.Dispose();
            }
        }
        _cleanup.Clear();
        if (_deleteOnDispose)
        {
            File.Delete(Path);
            _deleteOnDispose = false;
        }
    }
}
