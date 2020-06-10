using System;
using System.Collections.Generic;
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
        public void TestGetImageInfo()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var c = new JobContext())
            {
                c.AddInputBytes(0, imageBytes);
                var result = c.GetImageInfo(0);

                Assert.Equal(result.ImageWidth, 1);
                Assert.Equal(result.ImageHeight, 1);
                Assert.Equal(result.PreferredExtension, "png");
                Assert.Equal(result.PreferredMimeType, "image/png");
                Assert.Equal(result.FrameDecodesInto, PixelFormat.Bgra_32);
            }

        }

        [Fact]
        public async Task TestBuildJob()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes)
                    .FlipHorizontal()
                    .Rotate90()
                    .Distort(30, 20)
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
        }
        
        [Fact]
        public async Task TestAllJob()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes)
                    .FlipVertical()
                    .FlipHorizontal()
                    .Rotate90()
                    .Rotate180()
                    .Rotate270()
                    .CropWhitespace(80, 0.5f)
                    .Distort(30, 20)
                    .Crop(0,0,10,10)
                    .Region(-5,-5,10,10, AnyColor.Black)
                    .RegionPercent(-10f, -10f, 110f, 110f, AnyColor.Transparent)    
                    .BrightnessSrgb(-1f)
                    .ContrastSrgb(1f)
                    .SaturationSrgb(1f)
                    .ColorFilterSrgb(ColorFilterSrgb.Invert)
                    .ColorFilterSrgb(ColorFilterSrgb.Sepia)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Bt709)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Flat)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Ntsc)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Ry)
                    .ExpandCanvas(5,5,5,5,AnyColor.FromHexSrgb("FFEECCFF"))
                    .Transpose()
                    .FillRectangle(2,2,8,8, AnyColor.Black)
                    .ResizerCommands("width=10&height=10&mode=crop")
                    .WhiteBalanceSrgb(80)
                    .ConstrainWithin(5, 5)
                    .Watermark(new BytesSource(imageBytes), 
                        new WatermarkOptions()
                            .LayoutWithMargins(
                                new WatermarkMargins(WatermarkAlign.Image, 1,1,1,1), 
                                WatermarkConstraintMode.Within, 
                                new ConstraintGravity(90,90))
                            .WithOpacity(0.5f)
                            .WithHints(new ResampleHints().Sharpen(15f, SharpenWhen.Always)))
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
        }
        [Fact]
        public async Task TestConstraints()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes).
                    Constrain(new Constraint(ConstraintMode.Fit_Crop,10,20))
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(10, r.First.Width);
                Assert.Equal(20, r.First.Height);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
        [Fact]
        public async Task TestMultipleOutputs()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes).
                    Constrain(new Constraint(ConstraintMode.Fit, 160, 120))
                    .Branch(f => f.ConstrainWithin(80, 60).EncodeToBytes(new WebPLosslessEncoder()))
                    .Branch(f => f.ConstrainWithin(40, 30).EncodeToBytes(new WebPLossyEncoder(50)))
                    .EncodeToBytes(new LibPngEncoder())
                    .Finish().InProcessAsync();

                Assert.Equal(60, r.TryGet(1).Width);
                Assert.Equal(30, r.TryGet(2).Width);
                Assert.Equal(120, r.TryGet(3).Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
        [Fact]
        public async Task TestMultipleInputs()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {

                var canvas = b.Decode(imageBytes)
                    .Distort(30, 30);
                    
                var r = await b.Decode(imageBytes)
                    .Distort(20, 20)
                    .FillRectangle(5, 5, 15, 15, AnyColor.FromHexSrgb("FFEECC"))
                    .TransparencySrgb(0.5f)
                    .DrawImageExactTo(canvas, 
                        new Rectangle(5,5,25,25),
                        new ResampleHints(),
                        CompositingMode.Compose)
                    .EncodeToBytes(new LodePngEncoder())
                    .Finish().InProcessAsync();

                Assert.Equal(30, r.TryGet(2).Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        



        [Fact]
        public async Task TestJobWithCommandString()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.Decode(imageBytes).ResizerCommands("width=3&height=2&mode=stretch&scale=both")
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(3, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        [Fact]
        public async Task TestBuildCommandString()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var r = await b.BuildCommandString(imageBytes, new BytesDestination(), "width=3&height=2&mode=stretch&scale=both&format=webp").Finish().InProcessAsync();

                Assert.Equal(3, r.First.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
        
        [Fact]
        public async Task TestBuildCommandStringWithWatermarks()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new FluentBuildJob())
            {
                var watermarks = new List<InputWatermark>();
                watermarks.Add(new InputWatermark(new BytesSource(imageBytes), new WatermarkOptions()));
                watermarks.Add(new InputWatermark(new BytesSource(imageBytes), new WatermarkOptions().WithGravity(new ConstraintGravity(100,100))));
                
                var r = await b.BuildCommandString(new BytesSource(imageBytes), new BytesDestination(), "width=3&height=2&mode=stretch&scale=both&format=webp",watermarks).Finish().InProcessAsync();

                Assert.Equal(3, r.First.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        [Fact]
        public async Task TestFilesystemJobPrep()
        {
            var isUnix = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

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

                    if (isUnix)
                    {
                        Assert.True(File.Exists(jsonPath));
                    }
                    else
                    {
                        using (var file = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(jsonPath))
                        {
                        } // Will throw filenotfoundexception if missing 
                    }
                }

                if (isUnix)
                {
                    Assert.False(File.Exists(jsonPath));
                }
                else
                {

                    Assert.Throws<FileNotFoundException>(delegate ()
                    {

                        using (var file = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(jsonPath))
                        {
                        }
                    });
                }
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
                    JpegDownscaleHint = new Size(20, 20),
                    JpegDownscalingMode = DecoderDownscalingMode.Fastest,
                    DiscardColorProfile = true
                };
                var r = await b.Decode(new BytesSource(imageBytes), 0, cmd)
                    .Distort(30, 20, new ResampleHints().Sharpen(50.0f, SharpenWhen.Always).ResampleFilter(InterpolationFilter.Robidoux_Fast, InterpolationFilter.Cubic))
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new LibPngEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
    }
}