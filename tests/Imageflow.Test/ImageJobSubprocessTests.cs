using Imageflow.Fluent;
using Xunit;

namespace Imageflow.Test;
public class ImageJobSubprocessTests
{
    [Fact]
    public async Task Can_execute_simple_job_in_subprocess()
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
}

