using System.Text;
using System.Text.Json.Nodes;

using Imageflow.Bindings;
using Imageflow.Fluent;

using Newtonsoft.Json;

using Xunit;
using Xunit.Abstractions;
#if NET8_0_OR_GREATER
using JsonNamingPolicy = System.Text.Json.JsonNamingPolicy;
#endif
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Imageflow.Test;

public static class IJsonResponseProviderExtensions
{

    [Obsolete("Use DeserializeJsonNode() instead")]
    public static T? Deserialize<T>(this IJsonResponseProvider p) where T : class
    {
        using var readStream = p.GetStream();
        using var ms = new MemoryStream(readStream.CanSeek ? (int)readStream.Length : 0);
        readStream.CopyTo(ms);
        var allBytes = ms.ToArray();
#pragma warning disable CA1869
        var options = new JsonSerializerOptions
        {
#if NET8_0_OR_GREATER
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
#endif
        };
        var v = System.Text.Json.JsonSerializer.Deserialize<T>(allBytes, options);
#pragma warning restore CA1869
        return v;
    }

    [Obsolete("Use Deserialize<T> or DeserializeJsonNode() instead")]
    public static dynamic? DeserializeDynamic(this IJsonResponseProvider p)
    {
        using var reader = new StreamReader(p.GetStream(), Encoding.UTF8);
        return JsonSerializer.Create().Deserialize(new JsonTextReader(reader));
    }
}

public class TestContext
{
    private readonly ITestOutputHelper _output;

    public TestContext(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestCreateDestroyContext()
    {
        using (var c = new JobContext())
        {
            c.AssertReady();
        }
    }

    [Fact]
    public void TestGetImageInfoMessage()
    {
        using (var c = new JobContext())
        {
            c.AddInputBytesPinned(0,
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

            var response = c.SendMessage("v0.1/get_image_info", //new {io_id = 0});
                new JsonObject()
                {
                    ["io_id"] = 0
                });

            dynamic data = response.DeserializeDynamic()!;

            _output.WriteLine(response.GetString());

            Assert.Equal(200, (int)data.code);
            Assert.True((bool)data.success);
            Assert.Equal(1, (int)data.data.image_info.image_width);
            Assert.Equal(1, (int)data.data.image_info.image_height);
            Assert.Equal("image/png", (string)data.data.image_info.preferred_mime_type);
            Assert.Equal("png", (string)data.data.image_info.preferred_extension);
        }
    }

    [Fact]
    public void TestGetImageInfo()
    {
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
        using (var c = new JobContext())
        {
            c.AddInputBytes(0, imageBytes);
            var result = c.GetImageInfo(0);

            Assert.Equal(1, result.ImageWidth);
            Assert.Equal(1, result.ImageHeight);
            Assert.Equal("png", result.PreferredExtension);
            Assert.Equal("image/png", result.PreferredMimeType);
            Assert.Equal(PixelFormat.Bgra_32, result.FrameDecodesInto);
        }
    }

    [Fact]
    public void TestGetVersionInfo()
    {
        using (var c = new JobContext())
        {
            var info = c.GetVersionInfo();
            Assert.NotNull(info.LastGitCommit);
            Assert.NotNull(info.LongVersionString);
            Assert.NotNull(info.GitDescribeAlways);
            var unused = info.BuildDate;
            Assert.NotEqual(default, info.BuildDate);
            var unused2 = info.GitTag;
            Assert.NotNull(info.GitTag);
            var unused3 = info.DirtyWorkingTree;

        }
    }

    [Fact]
    public void TestExecute()
    {
        using (var c = new JobContext())
        {
            c.AddInputBytesPinned(0,
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

            c.AddOutputBuffer(1);

            var message = new
            {
                framewise = new
                {
                    steps = new object[]
                    {
                            new
                            {
                                decode = new
                                {
                                    io_id = 0
                                }
                            },
                            "flip_v",
                            new
                            {
                                encode = new
                                {
                                    io_id=1,
                                    preset = new
                                    {
                                        libjpegturbo = new
                                        {
                                            quality = 90
                                        }
                                    }
                                }
                            }
                    }
                }
            };

            var response = c.SendMessage("v0.1/execute", message);

            dynamic data = response.DeserializeDynamic()!;

            _output.WriteLine(response.GetString());

            Assert.Equal(200, (int)data.code);
            Assert.True((bool)data.success);
        }
    }

    [Fact]
    public void TestIr4Execute()
    {
        using (var c = new JobContext())
        {
            c.AddInputBytesPinned(0,
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));
            c.AddOutputBuffer(1);
            var response = c.ExecuteImageResizer4CommandString(0, 1, "w=200&h=200&scale=both&format=jpg");

            dynamic data = response.DeserializeDynamic()!;

            _output.WriteLine(response.GetString());

            Assert.Equal(200, (int)data.code);
            Assert.True((bool)data.success);
        }
    }

    [Fact]
    public void TestIr4Build()
    {
        using (var c = new JobContext())
        {
            var message = new
            {
                io = new object[]
                {
                        new
                        {
                            direction = "in",
                            io_id = 0,
                            io = new
                            {
                                base_64 =
                                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="
                            }
                        },
                        new
                        {
                            direction = "out",
                            io_id = 1,
                            io = "output_base_64"
                        }
                },
                framewise = new
                {
                    steps = new object[]
                    {
                            new
                            {
                                command_string = new
                                {
                                    kind = "ir4",
                                    value = "w=200&h=200&scale=both&format=jpg",
                                    decode = 0,
                                    encode = 1
                                }
                            }
                    }
                }
            };

            var response = c.SendMessage("v0.1/build", message);

            dynamic data = response.DeserializeDynamic()!;

            _output.WriteLine(response.GetString());

            Assert.Equal(200, (int)data.code);
            Assert.True((bool)data.success);
        }
    }

    [Fact]
    public void TestLowLevelCancellationWithPreCancelledToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before starting the job

        using var c = new JobContext();
        c.AddInputBytesPinned(0,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

        c.AddOutputBuffer(1);

        var message = new JsonObject
        {
            ["framewise"] = new JsonObject
            {
                ["steps"] = new JsonArray
                {
                    new JsonObject { ["decode"] = new JsonObject { ["io_id"] = 0 } },
                    "flip_v",
                    new JsonObject
                    {
                        ["encode"] = new JsonObject
                        {
                            ["io_id"] = 1,
                            ["preset"] = new JsonObject
                            {
                                ["libjpegturbo"] = new JsonObject { ["quality"] = 90 }
                            }
                        }
                    }
                }
            }
        };

        Assert.Throws<OperationCanceledException>(() =>
        {
            c.InvokeExecute(message, cts.Token);
        });
    }

    [Fact]
    public void TestLowLevelCancellationDuringExecution()
    {
        using var cts = new CancellationTokenSource();
        using var c = new JobContext();
        c.AddInputBytesPinned(0,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

        c.AddOutputBuffer(1);

        var message = new JsonObject
        {
            ["framewise"] = new JsonObject
            {
                ["steps"] = new JsonArray
                {
                    new JsonObject { ["decode"] = new JsonObject { ["io_id"] = 0 } },
                    "flip_v",
                    new JsonObject
                    {
                        ["encode"] = new JsonObject
                        {
                            ["io_id"] = 1,
                            ["preset"] = new JsonObject
                            {
                                ["libjpegturbo"] = new JsonObject { ["quality"] = 90 }
                            }
                        }
                    }
                }
            }
        };

        // Schedule cancellation after 1ms
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        // For a simple 1x1 image, this might complete before cancellation
        try
        {
            using var response = c.InvokeExecute(message, cts.Token);
            var data = response.Parse();

            // If we got here, the job completed before cancellation
            Assert.NotNull(data);
            Assert.Equal(200, (int)data["code"]!);
            Assert.True((bool)data["success"]!);
        }
        catch (OperationCanceledException)
        {
            // This is expected if cancellation happened during execution
            Assert.True(cts.IsCancellationRequested);
        }
    }

    [Fact]
    public void TestLowLevelCancellationWithInvokeMethod()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before starting

        using var c = new JobContext();
        c.AddInputBytesPinned(0,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

        Assert.Throws<OperationCanceledException>(() =>
        {
            c.InvokeAndParse("v0.1/get_image_info",
                new JsonObject { ["io_id"] = 0 },
                cts.Token);
        });
    }

    [Fact]
    public void TestLowLevelCancellationDoesNotAffectCompletedJob()
    {
        using var cts = new CancellationTokenSource();
        using var c = new JobContext();
        c.AddInputBytesPinned(0,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));

        c.AddOutputBuffer(1);

        var message = new JsonObject
        {
            ["framewise"] = new JsonObject
            {
                ["steps"] = new JsonArray
                {
                    new JsonObject { ["decode"] = new JsonObject { ["io_id"] = 0 } },
                    "flip_v",
                    new JsonObject
                    {
                        ["encode"] = new JsonObject
                        {
                            ["io_id"] = 1,
                            ["preset"] = new JsonObject
                            {
                                ["libjpegturbo"] = new JsonObject { ["quality"] = 90 }
                            }
                        }
                    }
                }
            }
        };

        using var response = c.InvokeExecute(message, cts.Token);

        // Cancel after the job completes
        cts.Cancel();

        // Response should still be accessible
        var data = response.Parse();
        Assert.NotNull(data);
        Assert.Equal(200, (int)data["code"]!);
        Assert.True((bool)data["success"]!);
    }

    [Fact]
    public void TestLowLevelInvokeAndParseWithCancellationToken()
    {
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

        using var cts = new CancellationTokenSource();
        using var c = new JobContext();

        c.AddInputBytes(0, imageBytes);

        // Test that InvokeAndParse works with a non-cancelled token
        var response = c.InvokeAndParse("v0.1/get_image_info",
            new JsonObject { ["io_id"] = 0 },
            cts.Token);

        Assert.NotNull(response);
        Assert.Equal(200, (int)response["code"]!);
        Assert.True((bool)response["success"]!);
    }

    [Fact]
    public void TestLowLevelInvokeAndParseWithCancelledToken()
    {
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before starting

        using var c = new JobContext();
        c.AddInputBytes(0, imageBytes);

        Assert.Throws<OperationCanceledException>(() =>
        {
            c.InvokeAndParse("v0.1/get_image_info",
                new JsonObject { ["io_id"] = 0 },
                cts.Token);
        });
    }

    [Fact]
    public void TestLowLevelNativeCancellationWithComplexJob()
    {
        using var cts = new CancellationTokenSource();
        using var c = new JobContext();

        // Create a large output buffer for a 2000x2000 image
        c.AddOutputBuffer(1);

        // Build a complex job with many operations
        var steps = new JsonArray();
        steps.Add(new JsonObject
        {
            ["create_canvas"] = new JsonObject
            {
                ["w"] = 2000,
                ["h"] = 2000,
                ["format"] = "bgra_32",
                ["color"] = "black"
            }
        });

        // Add many flip operations to increase processing time
        for (int i = 0; i < 20; i++)
        {
            steps.Add("flip_v");
            steps.Add("flip_h");
        }

        steps.Add(new JsonObject
        {
            ["encode"] = new JsonObject
            {
                ["io_id"] = 1,
                ["preset"] = new JsonObject
                {
                    ["mozjpeg"] = new JsonObject { ["quality"] = 95 }
                }
            }
        });

        var message = new JsonObject
        {
            ["framewise"] = new JsonObject
            {
                ["steps"] = steps
            }
        };

        // Cancel after 1ms to try to catch during processing
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        try
        {
            using var response = c.InvokeExecute(message, cts.Token);
            var data = response.Parse();

            // If we got here, the job completed before cancellation
            _output.WriteLine("Job completed before native cancellation could be triggered");
            Assert.NotNull(data);
            Assert.Equal(200, (int)data["code"]!);
        }
        catch (ImageflowException ex)
        {
            // This is the native cancellation response we're looking for
            _output.WriteLine($"Native cancellation triggered! Exception: {ex.Message}");
            Assert.True(ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("abort", StringComparison.OrdinalIgnoreCase),
                       $"Expected cancellation-related error, but got: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            // This happens if cancellation was checked before native call
            _output.WriteLine("Cancellation detected before native call");
        }
    }

}
