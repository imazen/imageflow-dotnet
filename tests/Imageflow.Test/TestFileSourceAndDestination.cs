#pragma warning disable CA2007
using Imageflow.Fluent;
using Xunit;

namespace Imageflow.Test;

public class TestFileSourceAndDestination
{

    private static async Task<BuildJobResult> TestGivenFunction(Func<string, string, string, Task<BuildJobResult>> func)
    {
        // Create a temp input file, and a temp output path
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            await GenerateDemoSourceImage(inputPath, 1000, 1000);
            // Decode, process, and encode the file
            var r = await func(inputPath, outputPath, "format=jpg&quality=90");

            Assert.True(File.Exists(outputPath));
            Assert.NotEmpty(r.PerformanceDetails.GetFirstFrameSummary());
            Assert.Equal("jpg", r.First!.PreferredExtension);
            Assert.True(r.First.TryGetBytes().HasValue);
            return r;
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    private static async Task<BuildJobResult> GenerateDemoSourceImage(string path, int width, int height)
    {
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        // We create a canvas, draw rectangles on it, save as jpeg
        return await b.CreateCanvasBgr32((uint)width, (uint)height, AnyColor.Black)
            .FillRectangle(0, 0, width / 2, height / 2, AnyColor.FromHexSrgb("FF0000FF"))
            .FillRectangle(width / 2, height / 2, width, height, AnyColor.FromHexSrgb("FF00FF00"))
            .Encode(FileDestination.ToPath(path), new MozJpegEncoder(100, true))
            .Finish()
            .InProcessAsync()
            ;
    }

    [Fact]
    public static async Task TestProcessBufferedStreamSourceToStreamDestination()
    {
        await TestGivenFunction(ProcessBufferedStreamSourceToStreamDestination);
    }
    private static async Task<BuildJobResult> ProcessBufferedStreamSourceToStreamDestination(string inputPath, string outputPath, string commandString)
    {
        using var stream = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        var r = await b.BuildCommandString(
            source: BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream),
            dest: new StreamDestination(output, disposeUnderlying: true),
            commandString)
            .Finish()
            .InProcessAsync()
            ;
        return r;
    }

    [Fact]
    public static async Task TestProcessFileToFileDefault()
    {
        await TestGivenFunction(ProcessFileToFileDefault);
    }
    private static async Task<BuildJobResult> ProcessFileToFileDefault(string inputPath, string outputPath, string commandString)
    {
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        var r = await b.BuildCommandString(
            source: FileSource.FromPath(inputPath),
            dest: FileDestination.ToPath(outputPath),
            commandString)
            .Finish()
            .InProcessAsync()
            ;
        return r;
    }
    [Fact]
    public static async Task TestProcessFileToFileExclusiveAccess()
    {
        await TestGivenFunction(ProcessFileToFileExclusiveAccess);
    }
    private static async Task<BuildJobResult> ProcessFileToFileExclusiveAccess(string inputPath, string outputPath, string commandString)
    {
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        var options = new FileDestinationOptions(Atomic: false, ShareReadAccess: false, PreferRandomAccessApi: false);
        var r = await b.BuildCommandString(
            source: FileSource.FromPath(inputPath),
            dest: FileDestination.ToPath(outputPath, options),
            commandString)
            .Finish()
            .InProcessAsync()
            ;
        return r;
    }
    [Fact]
    public static async Task TestProcessFileToFileAtomic()
    {
        await TestGivenFunction(ProcessFileToFileAtomic);
    }
    private static async Task<BuildJobResult> ProcessFileToFileAtomic(string inputPath, string outputPath, string commandString)
    {
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        var options = new FileDestinationOptions(Atomic: true, ShareReadAccess: false, PreferRandomAccessApi: false);
        var r = await b.BuildCommandString(
            source: FileSource.FromPath(inputPath),
            dest: FileDestination.ToPath(outputPath, options),
            commandString)
            .Finish()
            .InProcessAsync()
            ;
        return r;
    }

    [Fact]
    public static async Task TestProcessFileToFileFileStream()
    {
        await TestGivenFunction(ProcessFileToFileFileStream);
    }
    private static async Task<BuildJobResult> ProcessFileToFileFileStream(string inputPath, string outputPath, string commandString)
    {
        using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
        var options = new FileDestinationOptions(Atomic: false, ShareReadAccess: false, PreferRandomAccessApi: false);
        var r = await b.BuildCommandString(
            source: FileSource.FromPath(inputPath),
            dest: FileDestination.ToPath(outputPath, options),
            commandString)
            .Finish()
            .InProcessAsync()
            ;
        return r;
    }
}
