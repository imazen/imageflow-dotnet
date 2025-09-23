using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using Imageflow.Fluent;
using System.Threading.Tasks;

namespace Imageflow.Benchmarks;


[MemoryDiagnoser]
//[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 20)]
[SimpleJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class ReadBenchmarks
{
    private byte[]? _sourceData;
    private string? _inputPath;

    private string? _tempOutputFolder;


    [Params(20 * 1024 * 1024, 1 * 1024 * 1024, 100 * 1024)]
    public int FileSize { get; set; }

    [Params(1024 * 4, 81920, 0)]
    public int BufferSize { get; set; }


    public static byte[] GenerateRandomData(int size)
    {
        return WriteBenchmarks.GenerateRandomData(size);
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

    [GlobalCleanup]
    public void Cleanup()
    {
        Directory.Delete(_tempOutputFolder, true);
    }


    [BenchmarkCategory("Read", "Async"), Benchmark(Baseline = true)]
    public async Task<long> Read_FileStream()
    {
        using var stream = new FileStream(_inputPath!, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        var streamSource = BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream);
        var readMemory = await streamSource.BorrowReadOnlyMemoryAsync(default).ConfigureAwait(false);
        return readMemory.Length;
    }


    [BenchmarkCategory("Read", "Sync"), Benchmark(Baseline = true)]
    public long Read_FileStreamSync()
    {
        using var stream = new FileStream(_inputPath!, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        var streamSource = BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream) as IMemorySource;
        var readMemory = streamSource!.BorrowReadOnlyMemory();
        return readMemory.Length;
    }


    [BenchmarkCategory("Read", "Async"), Benchmark]
    public async Task<long> Read_FileSource()
    {
        var source = FileSource.FromPath(_inputPath!);

        var readMemory = await source.BorrowReadOnlyMemoryAsync(default).ConfigureAwait(false);
        return readMemory.Length;
    }

    [BenchmarkCategory("Read", "Sync"), Benchmark]
    public long Read_FileSourceSync()
    {
        var source = FileSource.FromPath(_inputPath!);

        var readMemory = source.BorrowReadOnlyMemory();
        return readMemory.Length;
    }
}
