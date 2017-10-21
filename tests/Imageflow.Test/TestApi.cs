using System;
using System.Drawing;
using System.IO;
using Xunit;
using System.Threading.Tasks;
using Imageflow.Bindings;
using Imageflow.Fluent;
 using Xunit.Abstractions;

namespace Imageflow.Test
{
    public class TestApi
    {
        private readonly ITestOutputHelper output;

        public TestApi(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task TestBuildJob()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes).FlipHorizontal().Rotate90().Distort(30, 20).ConstrainWithin(5, 5)
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
            
        }

        [Fact]
        public async Task TestFilesystemJobPrep()
        {

            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                string jsonPath;
                using (var job = await b.Decode(imageBytes).FlipHorizontal().Rotate90().Distort(30, 20)
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new GifEncoder()).FinishWithTimeout(2000)
                    .WriteJsonJobAndInputs(true))
                {
                    jsonPath = job.JsonPath;

                    Assert.True(File.Exists(jsonPath));
                }
                Assert.False(File.Exists(jsonPath));
            }
        }

        [Fact]
        public async Task TestBuildJobSubprocess()
        {
            string imageflowTool = Environment.GetEnvironmentVariable("IMAGEFLOW_TOOL");

            if (!string.IsNullOrWhiteSpace(imageflowTool))
            {
                var imageBytes = Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
                using (var b = new FluentBuildJob())
                {
                    var r = await b.Decode(imageBytes).FlipHorizontal().Rotate90().Distort(30, 20).ConstrainWithin(5, 5)
                        .EncodeToBytes(new GifEncoder()).FinishWithTimeout(2000)
                        .InSubprocessAsync(imageflowTool);
                           
                    // ExecutableLocator.FindExecutable("imageflow_tool", new [] {"/home/n/Documents/imazen/imageflow/target/release/"})

                    Assert.Equal(5, r.First.Width);
                    Assert.True(r.First.TryGetBytes().HasValue);
                }
            }
        }
        
        [Fact]
        public async Task TestCustomDownscaling()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var cmd = new DecodeCommands
                {
                    DownscaleHint = new Size(20, 20),
                    DownscalingMode = DecderDownscalingMode.Fastest,
                    DiscardColorProfile = true
                };
                var r = await b.Decode(new BytesSource(imageBytes), 0, cmd)
                    .Distort(30, 20, 50.0f, InterpolationFilter.RobidouxFast, InterpolationFilter.Cubic)
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new LibPngEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
    }
}