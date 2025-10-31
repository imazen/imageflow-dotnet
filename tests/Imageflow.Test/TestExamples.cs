#pragma warning disable CA2007
using Imageflow.Fluent;
using Xunit;

namespace Imageflow.Test;

public class TestExamples
{
    [Fact]
    public async Task TestBuildCommandStringFileToFile()
    {
        // Create a temp input file, and a temp output path
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            await GenerateDemoSourceImage(inputPath, 1000, 1000);
            // Decode, process, and encode the file
            using var b = new ImageJob(); // Make sure ImageJob is always disposed with using
            var r = await b.BuildCommandString(
                source: FileSource.FromPath(inputPath),
                dest: FileDestination.ToPath(outputPath),
                "format=jpg&quality=90")
                .Finish()
                .InProcessAsync();
            Assert.True(File.Exists(outputPath));
            Assert.NotEmpty(r.PerformanceDetails.GetFirstFrameSummary());
            Assert.Equal("jpg", r.First!.PreferredExtension);
            Assert.True(r.First.TryGetBytes().HasValue);
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
            .InProcessAsync();
    }

}
