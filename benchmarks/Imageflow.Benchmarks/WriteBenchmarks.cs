using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using Imageflow.Fluent;
using System.Threading.Tasks;

namespace Imageflow.Benchmarks;

// dotnet run --configuration Release --project .\benchmarks\Imageflow.Benchmarks\ --framework net8.0 -- --filter *Write*


[MemoryDiagnoser]
//[SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 20)]
[SimpleJob(RunStrategy.ColdStart, warmupCount: 2, launchCount:1, iterationCount: 10)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class WriteBenchmarks
{
    private byte[]? _sourceData;
    private string? _inputPath;
    //private readonly List<string> _outputPaths = new();

    private string? _tempOutputFolder;


    [Params(20*1024*1024, 1*1024*1024)]
    public int FileSize { get; set; }


    //[Params(false)]
    public bool Atomic { get; set; } = false;

    // [Params(true, false)]
    public bool Async { get; set; } = true;

    //[Params(false)]
    public bool ShareReadAccess { get; set; } = false;

    [Params(100,1)]
    public int WriteCount { get; set; }

    public static byte[] GenerateRandomData(int size) {
        int chunkSize = Math.Min(2 * 1024, size);         // 2 KB

        // Generate one 2 KB pseudorandom block
        byte[] chunk = new byte[chunkSize];

        #if NET8_0_OR_GREATER
        Random.Shared.NextBytes(chunk);

        // Allocate 10 MB output
        byte[] buffer = GC.AllocateUninitializedArray<byte>(size);
        #else
        Random rand = new Random();
        rand.NextBytes(chunk);
        byte[] buffer = new byte[size];
        #endif

        // Copy repeatedly
        for (int offset = 0; offset < size; offset += chunkSize)
        {
            Buffer.BlockCopy(chunk, 0, buffer, offset, chunkSize);
        }
        return buffer;

    }

    [GlobalSetup]
    public void Setup()
    {
        // Create a 10MB file with random data
        _sourceData = GenerateRandomData(FileSize);


        var runStartTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _tempOutputFolder = Path.Combine(Path.GetTempPath(), "Imageflow.Benchmarks", runStartTimeString);
        Directory.CreateDirectory(_tempOutputFolder);

        _inputPath = Path.Combine(_tempOutputFolder, "input.jpg");
        File.WriteAllBytes(_inputPath, _sourceData);

    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // sleep for 100ms to let the disk queue empty
        Thread.Sleep(100);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Directory.Delete(_tempOutputFolder, true);
    }
    private int _outputPathIndex;
    private string GetNewOutputPath() {
        var path = Path.Combine(_tempOutputFolder, $"output_{_outputPathIndex++}.jpg");

        return path;
    }

    private int LoopCount => FileSize / 1048576 * 250;

    private async Task RunWriteBenchmark( string path,FileDestinationOptions destOptions)
    {
        for (int loop = 0; loop < LoopCount; loop++)
        {
            var dest = FileDestination.ToPath(path, destOptions);
            dest.SetHints(new OutputSinkHints(ExpectedSize: FileSize, MultipleWritesExpected: WriteCount > 1, Asynchronous: Async));
            var blockSize = FileSize / WriteCount;
            if (Async)
            {
                for (int i = 0; i < WriteCount; i++)
                {
                    var remaining = Math.Min(FileSize - i * blockSize, blockSize);
                    await dest.FastWriteAsync(_sourceData.AsMemory(i * blockSize, remaining), default).ConfigureAwait(false);
                }
                await dest.FinishedAsync(default).ConfigureAwait(false);
                dest.Dispose();
            }
            else
            {
                for (int i = 0; i < WriteCount; i++)
                {
                    var remaining = Math.Min(FileSize - i * blockSize, blockSize);
                    dest.Write(_sourceData.AsSpan(i * blockSize, remaining));
                }
                dest.Finished();
                dest.Dispose();
            }
        }
    }

    private async Task RunFileStreamBenchmarkAsync(int bufferSize)
    {
        #if NETSTANDARD2_1_OR_GREATER
        var isOnlyNetStandard2 = false;
        #else
        var isOnlyNetStandard2 = true;
        #endif
        for (int loop = 0; loop < LoopCount; loop++){
            var writeThrough = Atomic && (WriteCount <= 1 || isOnlyNetStandard2);
            var fileOptions = (Async ? FileOptions.Asynchronous : FileOptions.None) | (writeThrough ? FileOptions.WriteThrough : FileOptions.None);
            var fileShare = ShareReadAccess ? FileShare.Read : FileShare.None;
            var blockSize = FileSize / WriteCount;
            if (Async)
            {

                using var stream = new FileStream(GetNewOutputPath(), FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, fileOptions);
                for (int i = 0; i < WriteCount; i++)
                {
                    var remaining = Math.Min(FileSize - i*blockSize, blockSize);
                    #if NET6_0_OR_GREATER
                    await stream.WriteAsync(_sourceData.AsMemory(i*blockSize, remaining), default).ConfigureAwait(false);
                    #else
                    await stream.WriteAsync(_sourceData, i*blockSize, remaining, default).ConfigureAwait(false);
                    #endif
                }
                await stream.FlushAsync(default).ConfigureAwait(false);
                #if NETSTANDARD2_1_OR_GREATER
                if (!writeThrough && Atomic){
                    stream.Flush(true);
                }
                #endif
                stream.Dispose();
            }
            else
            {
                using var stream = new FileStream(GetNewOutputPath(), FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, fileOptions);
                for (int i = 0; i < WriteCount; i++)
                {
                    var remaining = Math.Min(FileSize - i*blockSize, blockSize);
                    #if NET6_0_OR_GREATER
                    stream.Write(_sourceData.AsSpan(i*blockSize, remaining));
                    #else
                    stream.Write(_sourceData, i*blockSize, remaining);
                    #endif
                }
                #if NETSTANDARD2_1_OR_GREATER
                if (!writeThrough && Atomic){
                    stream.Flush(true);
                }
                #endif
                stream.Dispose();
            }
        }
    }


#if NET6_0_OR_GREATER
    [BenchmarkCategory("Write"), Benchmark]
    public Task SafeFileHandle() => RunWriteBenchmark(GetNewOutputPath(), new FileDestinationOptions(PreferRandomAccessApi: true, Atomic: Atomic, ShareReadAccess: ShareReadAccess));

#endif

    [BenchmarkCategory("Write"), Benchmark]
    public Task FileDestinationFileStream() => RunWriteBenchmark(GetNewOutputPath(), new FileDestinationOptions(PreferRandomAccessApi: false, Atomic: Atomic, ShareReadAccess: ShareReadAccess));


    [BenchmarkCategory("Write"),  Benchmark(Baseline = true)]
    public Task FileStream_80k() => RunFileStreamBenchmarkAsync(80*1024);

    [BenchmarkCategory("Write"),  Benchmark]
    public Task FileStream_16k() => RunFileStreamBenchmarkAsync(16*1024);
}
