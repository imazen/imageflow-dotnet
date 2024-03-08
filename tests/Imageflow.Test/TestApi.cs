using System.Drawing;

using Imageflow.Bindings;
using Imageflow.Fluent;

using Xunit;
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
        [Obsolete("Obsolete")]
        public async void TestGetImageInfoLegacy()
        {
            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

            var info = await ImageJob.GetImageInfo(new BytesSource(imageBytes));

            Assert.Equal(1, info.ImageWidth);
            Assert.Equal(1, info.ImageHeight);
            Assert.Equal("png", info.PreferredExtension);
            Assert.Equal("image/png", info.PreferredMimeType);
            Assert.Equal(PixelFormat.Bgra_32, info.FrameDecodesInto);
        }

        // test the new GetImageInfoAsync
        [Fact]
        public async void TestGetImageInfoAsync()
        {
            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

            var info = await ImageJob.GetImageInfoAsync(new MemorySource(imageBytes), SourceLifetime.NowOwnedAndDisposedByTask);

            Assert.Equal(1, info.ImageWidth);
            Assert.Equal(1, info.ImageHeight);
            Assert.Equal("png", info.PreferredExtension);
            Assert.Equal("image/png", info.PreferredMimeType);
            Assert.Equal(PixelFormat.Bgra_32, info.FrameDecodesInto);
        }

        // Test GetImageInfo 
        [Fact]
        public void TestGetImageInfoSync()
        {
            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

            var info = ImageJob.GetImageInfo(new MemorySource(imageBytes), SourceLifetime.NowOwnedAndDisposedByTask);

            Assert.Equal(1, info.ImageWidth);
            Assert.Equal(1, info.ImageHeight);
            Assert.Equal("png", info.PreferredExtension);
            Assert.Equal("image/png", info.PreferredMimeType);
            Assert.Equal(PixelFormat.Bgra_32, info.FrameDecodesInto);
        }


        [Fact]
        public async Task TestBuildJob()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var r = await b.Decode(imageBytes)
                    .FlipHorizontal()
                    .Rotate90()
                    .Distort(30, 20)
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First!.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
        }

        [Fact]
        public async Task TestAllJob()
        {
            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var r = await b.Decode(imageBytes)
                    .FlipVertical()
                    .FlipHorizontal()
                    .Rotate90()
                    .Rotate180()
                    .Rotate270()
                    .Transpose()
                    .CropWhitespace(80, 0.5f)
                    .Distort(30, 20)
                    .Crop(0, 0, 10, 10)
                    .Region(-5, -5, 10, 10, AnyColor.Black)
                    .RegionPercent(-10f, -10f, 110f, 110f, AnyColor.Transparent)
                    .BrightnessSrgb(-1f)
                    .ContrastSrgb(1f)
                    .SaturationSrgb(1f)
                    .WhiteBalanceSrgb(80)
                    .ColorFilterSrgb(ColorFilterSrgb.Invert)
                    .ColorFilterSrgb(ColorFilterSrgb.Sepia)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Bt709)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Flat)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Ntsc)
                    .ColorFilterSrgb(ColorFilterSrgb.Grayscale_Ry)
                    .ExpandCanvas(5, 5, 5, 5, AnyColor.FromHexSrgb("FFEECCFF"))
                    .FillRectangle(2, 2, 8, 8, AnyColor.Black)
                    .ResizerCommands("width=10&height=10&mode=crop")
                    .RoundAllImageCornersPercent(100, AnyColor.Black)
                    .RoundAllImageCorners(1, AnyColor.Transparent)
                    .ConstrainWithin(5, 5)
                    .Watermark(new MemorySource(imageBytes),
                        new WatermarkOptions()
                            .SetMarginsLayout(
                                new WatermarkMargins(WatermarkAlign.Image, 1, 1, 1, 1),
                                WatermarkConstraintMode.Within,
                                new ConstraintGravity(90, 90))
                            .SetOpacity(0.5f)
                            .SetHints(new ResampleHints().SetSharpen(15f, SharpenWhen.Always))
                            .SetMinCanvasSize(1, 1))
                    .EncodeToBytes(new MozJpegEncoder(80, true))
                    .Finish().InProcessAsync();

                Assert.Equal(5, r.First!.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
        }
        [Fact]
        public async Task TestConstraints()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var r = await b.Decode(imageBytes).
                    Constrain(new Constraint(ConstraintMode.Fit_Crop, 10, 20)
                    {
                        CanvasColor = null,
                        H = 20,
                        W = 10,
                        Hints = new ResampleHints()
                        {
                            InterpolationColorspace = ScalingFloatspace.Linear,
                            DownFilter = InterpolationFilter.Mitchell,
                            ResampleWhen = ResampleWhen.Size_Differs_Or_Sharpening_Requested,
                            SharpenWhen = SharpenWhen.Always,
                            SharpenPercent = 15,
                            UpFilter = InterpolationFilter.Ginseng
                        },
                        Mode = ConstraintMode.Fit_Crop
                    })
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(10, r.First!.Width);
                Assert.Equal(20, r.First.Height);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
        [Fact]
        public async Task TestMultipleOutputs()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var r = await b.Decode(imageBytes).
                    Constrain(new Constraint(ConstraintMode.Fit, 160, 120))
                    .Branch(f => f.ConstrainWithin(80, 60).EncodeToBytes(new WebPLosslessEncoder()))
                    .Branch(f => f.ConstrainWithin(40, 30).EncodeToBytes(new WebPLossyEncoder(50)))
                    .EncodeToBytes(new LodePngEncoder())
                    .Finish().InProcessAsync();


                Assert.Equal(60, r.TryGet(1)!.Width);
                Assert.Equal(30, r.TryGet(2)!.Width);
                Assert.Equal(120, r.TryGet(3)!.Width);
                Assert.True(r.First!.TryGetBytes().HasValue);
            }

        }
        [Fact]
        public async Task TestMultipleInputs()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {

                var canvas = b.Decode(imageBytes)
                    .Distort(30, 30);

                var r = await b.Decode(imageBytes)
                    .Distort(20, 20)
                    .FillRectangle(5, 5, 15, 15, AnyColor.FromHexSrgb("FFEECC"))
                    .TransparencySrgb(0.5f)
                    .DrawImageExactTo(canvas,
                        new Rectangle(5, 5, 25, 25),
                        new ResampleHints(),
                        CompositingMode.Compose)
                    .EncodeToBytes(new LodePngEncoder())
                    .Finish().InProcessAsync();

                Assert.Equal(30, r.TryGet(2)!.Width);
                Assert.True(r.First!.TryGetBytes().HasValue);
            }

        }





        [Fact]
        public async Task TestJobWithCommandString()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var r = await b.Decode(imageBytes).ResizerCommands("width=3&height=2&mode=stretch&scale=both")
                    .EncodeToBytes(new GifEncoder()).Finish().InProcessAsync();

                Assert.Equal(3, r.First!.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        [Fact]
        public async Task TestEncodeSizeLimit()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var e = await Assert.ThrowsAsync<ImageflowException>(async () =>
                {
                    await b.Decode(imageBytes)
                        .ResizerCommands("width=3&height=2&mode=stretch&scale=both")
                        .EncodeToBytes(new GifEncoder())
                        .Finish()
                        .SetSecurityOptions(new SecurityOptions().SetMaxEncodeSize(new FrameSizeLimit(1, 1, 1)))
                        .InProcessAsync();

                });
                Assert.StartsWith("ArgumentInvalid: SizeLimitExceeded: Frame width 3 exceeds max_encode_size.w", e.Message);
            }

        }


        [Fact]
        public async Task TestBuildCommandString()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            // We wrap the job in a using() statement to free memory faster
            using (var b = new ImageJob())
            {

                var r = await b.BuildCommandString(
                    new MemorySource(imageBytes), // or new StreamSource(Stream stream, bool disposeStream)
                    new BytesDestination(), // or new StreamDestination
                    "width=3&height=2&mode=stretch&scale=both&format=webp")
                    .Finish().InProcessAsync();

                Assert.NotEmpty(r.PerformanceDetails.GetFirstFrameSummary());
                Assert.Equal(3, r.First!.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }
        }

        [Fact]
        [Obsolete("Obsolete")]
        public async Task TestBuildCommandStringWithWatermarksLegacy()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var watermarks = new List<InputWatermark>();
                watermarks.Add(new InputWatermark(new BytesSource(imageBytes), new WatermarkOptions()));
                watermarks.Add(new InputWatermark(new BytesSource(imageBytes), new WatermarkOptions().SetGravity(new ConstraintGravity(100, 100))));

                var r = await b.BuildCommandString(
                    new BytesSource(imageBytes),
                    new BytesDestination(),
                    "width=3&height=2&mode=stretch&scale=both&format=webp", watermarks).Finish().InProcessAsync();

                Assert.Equal(3, r.First!.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        [Fact]
        public async Task TestBuildCommandStringWithWatermarks()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var watermarks = new List<InputWatermark>();
                watermarks.Add(new InputWatermark(new MemorySource(imageBytes), new WatermarkOptions()));
                watermarks.Add(new InputWatermark(new MemorySource(imageBytes), new WatermarkOptions().SetGravity(new ConstraintGravity(100, 100))));

                var r = await b.BuildCommandString(
                    new MemorySource(imageBytes),
                    new BytesDestination(),
                    "width=3&height=2&mode=stretch&scale=both&format=webp", watermarks).Finish().InProcessAsync();

                Assert.Equal(3, r.First!.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }
        [Fact]
        [Obsolete("Obsolete")]
        public async Task TestBuildCommandStringWithStreamsAndWatermarksLegacy()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            // var stream1 = new BufferedStream(new System.IO.MemoryStream(imageBytes));
            // var stream2 = new BufferedStream(new System.IO.MemoryStream(imageBytes));
            var stream1 = new BufferedStream(new MemoryStream(imageBytes));
            Assert.Equal(137, stream1.ReadByte());
            stream1.Seek(0, SeekOrigin.Begin);
            var stream2 = new BufferedStream(new MemoryStream(imageBytes));
            var stream3 = new BufferedStream(new MemoryStream(imageBytes));
            using (var b = new ImageJob())
            {
                var watermarks = new List<InputWatermark>();
                watermarks.Add(new InputWatermark(new StreamSource(stream1, true), new WatermarkOptions()));
                watermarks.Add(new InputWatermark(new StreamSource(stream2, true), new WatermarkOptions().SetGravity(new ConstraintGravity(100, 100))));

                var r = await b.BuildCommandString(
                    new StreamSource(stream3, true),
                    new BytesDestination(),
                    "width=3&height=2&mode=stretch&scale=both&format=webp", watermarks).Finish().InProcessAsync();

                Assert.Equal(3, r.First!.Width);
                Assert.Equal("webp", r.First.PreferredExtension);
                Assert.True(r.First.TryGetBytes().HasValue);
            }

        }

        [Fact]
        public async Task TestBuildCommandStringWithStreamsAndWatermarks()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            // var stream1 = new BufferedStream(new System.IO.MemoryStream(imageBytes));
            // var stream2 = new BufferedStream(new System.IO.MemoryStream(imageBytes));
            var stream1 = new BufferedStream(new MemoryStream(imageBytes));
            Assert.Equal(137, stream1.ReadByte());
            stream1.Seek(0, SeekOrigin.Begin);
            var stream2 = new BufferedStream(new MemoryStream(imageBytes));
            var stream3 = new BufferedStream(new MemoryStream(imageBytes));
            using var b = new ImageJob();
            var watermarks = new List<InputWatermark>();
            watermarks.Add(new InputWatermark(BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream1), new WatermarkOptions()));
            watermarks.Add(new InputWatermark(BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream2), new WatermarkOptions().SetGravity(new ConstraintGravity(100, 100))));

            var r = await b.BuildCommandString(
                BufferedStreamSource.UseEntireStreamAndDisposeWithSource(stream3),
                new BytesDestination(),
                "width=3&height=2&mode=stretch&scale=both&format=webp", watermarks).Finish().InProcessAsync();

            Assert.Equal(3, r.First!.Width);
            Assert.Equal("webp", r.First.PreferredExtension);
            Assert.True(r.First.TryGetBytes().HasValue);
        }

        [Fact]
        public async Task TestFilesystemJobPrep()
        {
            var isUnix = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

            var imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                string jsonPath;
                using (var job = await b.Decode(imageBytes).FlipHorizontal().Rotate90().Distort(30, 20)
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new GifEncoder()).Finish().WithCancellationTimeout(2000)
                    .WriteJsonJobAndInputs(true))
                {
                    jsonPath = job.JsonPath;

                    if (isUnix)
                    {
                        Assert.True(File.Exists(jsonPath));
                    }
                    else
                    {
#pragma warning disable CA1416
                        using (var file = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(jsonPath))
#pragma warning restore CA1416
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

#pragma warning disable CA1416
                        using (var file = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(jsonPath))
#pragma warning restore CA1416
                        {
                        }
                    });
                }
            }
        }

        [Fact]
        public async Task TestBuildJobSubprocess()
        {
            string? imageflowTool = Environment.GetEnvironmentVariable("IMAGEFLOW_TOOL");

            if (!string.IsNullOrWhiteSpace(imageflowTool))
            {
                var imageBytes = Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
                using (var b = new ImageJob())
                {
                    var r = await b.Decode(imageBytes).FlipHorizontal().Rotate90().Distort(30, 20).ConstrainWithin(5, 5)
                        .EncodeToBytes(new GifEncoder())
                        .Finish()
                        .WithCancellationTimeout(2000)
                        .InSubprocessAsync(imageflowTool);

                    // ExecutableLocator.FindExecutable("imageflow_tool", new [] {"/home/n/Documents/imazen/imageflow/target/release/"})

                    Assert.Equal(5, r.First!.Width);
                    Assert.True(r.First.TryGetBytes().HasValue);
                }
            }
        }

        [Fact]
        public async Task TestCustomDownscalingAndDecodeEncodeResults()
        {
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
            using (var b = new ImageJob())
            {
                var cmd = new DecodeCommands
                {
                    JpegDownscaleHint = new Size(20, 20),
                    JpegDownscalingMode = DecoderDownscalingMode.Fastest,
                    DiscardColorProfile = true
                };
                var r = await b.Decode(new MemorySource(imageBytes), 0, cmd)
                    .Distort(30, 20, new ResampleHints().SetSharpen(50.0f, SharpenWhen.Always).SetResampleFilters(InterpolationFilter.Robidoux_Fast, InterpolationFilter.Cubic))
                    .ConstrainWithin(5, 5)
                    .EncodeToBytes(new LodePngEncoder()).Finish().InProcessAsync();

                Assert.Equal(5, r.First!.Width);
                Assert.True(r.First.TryGetBytes().HasValue);
                Assert.Equal(1, r.DecodeResults.First()!.Width);
                Assert.Equal(1, r.DecodeResults.First().Height);
                Assert.Equal("png", r.DecodeResults.First().PreferredExtension);
                Assert.Equal("image/png", r.DecodeResults.First().PreferredMimeType);

                Assert.Equal(5, r.EncodeResults.First().Width);
                Assert.Equal(3, r.EncodeResults.First().Height);
                Assert.Equal("png", r.EncodeResults.First().PreferredExtension);
                Assert.Equal("image/png", r.EncodeResults.First().PreferredMimeType);
            }

        }


        [Fact]
        public void TestContentTypeDetection()
        {
            var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal("image/png", ImageJob.GetContentTypeForBytes(pngBytes));

            Assert.True(ImageJob.CanDecodeBytes(pngBytes));

            var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.Equal("image/jpeg", ImageJob.GetContentTypeForBytes(jpegBytes));
            Assert.True(ImageJob.CanDecodeBytes(jpegBytes));

            var gifBytes = new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0, 0, 0, 0, 0, 0, 0 };
            Assert.Equal("image/gif", ImageJob.GetContentTypeForBytes(gifBytes));
            Assert.True(ImageJob.CanDecodeBytes(gifBytes));

            var webpBytes = new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' };
            Assert.Equal("image/webp", ImageJob.GetContentTypeForBytes(webpBytes));
            Assert.True(ImageJob.CanDecodeBytes(webpBytes));

            var nonsenseBytes = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', 0, 0, 0, 0, 0, 0, 0 };
            Assert.Null(ImageJob.GetContentTypeForBytes(nonsenseBytes));
            Assert.False(ImageJob.CanDecodeBytes(nonsenseBytes));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
