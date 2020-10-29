using System;
using Xunit;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Xunit.Abstractions;
namespace Imageflow.Test
{
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
                
                var response = c.SendMessage("v0.1/get_image_info", new {io_id = 0});

                var data = response.DeserializeDynamic();

                _output.WriteLine(response.GetString());


                Assert.Equal(200, (int)data.code );
                Assert.Equal(true, (bool)data.success);
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

                Assert.Equal(result.ImageWidth, 1);
                Assert.Equal(result.ImageHeight, 1);
                Assert.Equal(result.PreferredExtension, "png");
                Assert.Equal(result.PreferredMimeType, "image/png");
                Assert.Equal(result.FrameDecodesInto, PixelFormat.Bgra_32);
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

                var data = response.DeserializeDynamic();

                _output.WriteLine(response.GetString());

                Assert.Equal(200, (int)data.code);
                Assert.Equal(true, (bool)data.success);
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

                var data = response.DeserializeDynamic();

                _output.WriteLine(response.GetString());

                Assert.Equal(200, (int)data.code);
                Assert.Equal(true, (bool)data.success);
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
                        new {
                            direction = "in",
                            io_id = 0,
                            io = new
                            {
                                base_64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="
                            }
                        },
                        new {
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

               var response =  c.SendMessage("v0.1/build", message);

                var data = response.DeserializeDynamic();

                _output.WriteLine(response.GetString());

                Assert.Equal(200, (int)data.code);
                Assert.Equal(true, (bool)data.success);
            }
        }
        
    }
}
