using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Imageflow.Net.IO
{

    internal interface ITemporaryFileProvider
    {
        ITemporaryFile Create(long capacity);
    }

    internal interface ITemporaryFile : IDisposable
    {
        string Path { get; }
        Stream WriteFromBeginning();
        Stream ReadFromBeginning();
    }

    internal class TemporaryMemoryFile : ITemporaryFile, ITemporaryFileProvider
    {
        private readonly MemoryMappedFile _file;
        public string Path { get; }


        private TemporaryMemoryFile(MemoryMappedFile file, string path)
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
            return new TemporaryMemoryFile(null, null);
        }

        public ITemporaryFile Create(long capacity)
        {
            var name = Guid.NewGuid().ToString();
            var file = MemoryMappedFile.CreateNew(name, capacity);
            return new TemporaryMemoryFile(file, name);
        }

        public void Dispose()
        {
            _file?.Dispose();
        }
    }

    internal class TemporaryFile : ITemporaryFile, ITemporaryFileProvider
    {
        private readonly List<WeakReference<IDisposable>> _cleanup = new List<WeakReference<IDisposable>>(2);

        private TemporaryFile(string path)
        {
            Path = path;
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

        public ITemporaryFile Create(long capacity)
        {
            return new TemporaryFile(System.IO.Path.GetTempFileName());
        }

        public static ITemporaryFileProvider CreateProvider()
        {
            return new TemporaryFile(null);
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
            if (Path != null)
            {
                File.Delete(Path);
            }
            Path = null;
        }
    }
}
