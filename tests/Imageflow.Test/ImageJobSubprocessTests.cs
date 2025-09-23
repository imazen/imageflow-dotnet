using System.Runtime.InteropServices;
using Imageflow.Fluent;
using Xunit;

namespace Imageflow.Test;
public class ImageJobSubprocessTests
{
    [Fact]
    public async Task Can_execute_simple_job_in_subprocess()
    {
        try
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

            // Arrange
            var job = new ImageJob();
            var dest = new BytesDestination();

            // Act
            var result = await job.Decode(imageBytes)
                .Encode(dest, new LodePngEncoder())
                .Finish()
                .InSubprocessAsync();

            // Assert
            Assert.Single(result.EncodeResults);
            Assert.True(dest.GetBytes().Count > 0);
        }
        catch (System.DllNotFoundException ex)
        {
            if (!LogAndForgive(ex))
            {
                throw;
            }
        }
    }

    public static bool LogAndForgive(Exception ex)
    {
        if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase))
        {
            // We are on .NET Framework. The test runner may not be able to locate native dependencies.
            // We skip this test by returning, AFTER telling stderr
            // Use ANSI escape codes for color, as Console.ForegroundColor is ignored by the test runner.
            Console.Error.WriteLine($"\x1b[31mTest skipped on {RuntimeInformation.FrameworkDescription}, as could not correctly locate native dependencies from NuGet packages.\x1b[0m");
            Console.Error.WriteLine($"\x1b[31mException: {ex.Message}\x1b[0m");
            return true;
        }
        return false;
    }
}

