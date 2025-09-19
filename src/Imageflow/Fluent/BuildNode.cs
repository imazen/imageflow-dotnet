using System.Drawing;
using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

public class BuildNode : BuildItemBase
{
    internal static BuildNode StartNode(ImageJob graph, JsonNode data)
        => new BuildNode(graph, data, null, null);

    /// <summary>
    /// Encode the result to the given destination (such as a BytesDestination or StreamDestination)
    /// </summary>
    /// <param name="destination">Where to write the bytes</param>
    /// <param name="ioId"></param>
    /// <param name="encoderPreset">An encoder class, such as `new MozJpegEncoder()`</param>
    /// <returns></returns>
    public BuildEndpoint Encode(IOutputDestination destination, int ioId, IEncoderPreset encoderPreset)
    {
        Builder.AddOutput(ioId, destination);
        // return new BuildEndpoint(Builder,
        //    new {encode = new {io_id = ioId, preset = encoderPreset?.ToImageflowDynamic()}}, this, null);
        return new BuildEndpoint(Builder,
            new JsonObject() { ["encode"] = new JsonObject() { ["io_id"] = ioId, ["preset"] = encoderPreset.ToJsonNode() } }, this, null);
    }
    /// <summary>
    /// Encode the result to the given destination (such as a BytesDestination or StreamDestination)
    /// </summary>
    /// <param name="destination">Where to write the bytes</param>
    /// <param name="encoderPreset">An encoder class, such as `new MozJpegEncoder()`</param>
    /// <returns></returns>
    public BuildEndpoint Encode(IOutputDestination destination, IEncoderPreset encoderPreset) =>
        Encode(destination, Builder.GenerateIoId(), encoderPreset);

    [Obsolete("Use Encode(IOutputDestination destination, int ioId, IEncoderPreset encoderPreset)")]
    public BuildEndpoint EncodeToBytes(int ioId, IEncoderPreset encoderPreset) =>
        Encode(new BytesDestination(), ioId, encoderPreset);
    public BuildEndpoint EncodeToBytes(IEncoderPreset encoderPreset) =>
        Encode(new BytesDestination(), encoderPreset);

    [Obsolete("Use Encode(IOutputDestination destination, int ioId, IEncoderPreset encoderPreset)")]
    public BuildEndpoint EncodeToStream(Stream stream, bool disposeStream, int ioId, IEncoderPreset encoderPreset) =>
        Encode(new StreamDestination(stream, disposeStream), ioId, encoderPreset);
    public BuildEndpoint EncodeToStream(Stream stream, bool disposeStream, IEncoderPreset encoderPreset) =>
        Encode(new StreamDestination(stream, disposeStream), encoderPreset);

    public BuildEndpoint EncodeToFile(string path, IEncoderPreset encoderPreset) =>
        Encode(FileDestination.ToPath(path), encoderPreset);

    public BuildEndpoint EncodeToFile(string path, int ioId, IEncoderPreset encoderPreset) =>
        Encode(FileDestination.ToPath(path), ioId, encoderPreset);

    private BuildNode(ImageJob builder, JsonNode nodeData, BuildNode? inputNode, BuildNode? canvasNode) : base(builder, nodeData, inputNode,
        canvasNode)
    { }

    // private BuildNode To(object data) => new BuildNode(Builder, data, this, null);

    private BuildNode To(JsonNode data) => new BuildNode(Builder, data, this, null);
    // private BuildNode NodeWithCanvas(BuildNode canvas, object data) => new BuildNode(Builder, data, this, canvas);
    private BuildNode NodeWithCanvas(BuildNode canvas, JsonNode data) => new BuildNode(Builder, data, this, canvas);

    /// <summary>
    /// Downscale the image to fit within the given dimensions, but do not upscale. See Constrain() for more options.
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public BuildNode ConstrainWithin(uint? w, uint? h)
    {
        var jsonObject = new JsonObject
        {
            ["constrain"] = new JsonObject
            {
                ["mode"] = "within",
                ["w"] = w,
                ["h"] = h,
            }
        };
        return To(jsonObject);
    }
    /// <summary>
    /// Downscale the image to fit within the given dimensions, but do not upscale. See Constrain() for more options.
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="hints"></param>
    /// <returns></returns>
    public BuildNode ConstrainWithin(uint? w, uint? h, ResampleHints? hints)
    {
        var jsonObject = new JsonObject
        {
            ["constrain"] = new JsonObject
            {
                ["mode"] = "within",
                ["w"] = w,
                ["h"] = h,
                ["hints"] = hints?.ToJsonNode()
            }
        };
        return To(jsonObject);
    }

    /// <summary>
    /// Scale an image using the given Constraint object.
    /// </summary>
    /// <param name="constraint"></param>
    /// <returns></returns>
    public BuildNode Constrain(Constraint constraint)
        //=> To(new { constrain = constraint.ToImageflowDynamic() });
        => To(new JsonObject { ["constrain"] = constraint.ToJsonNode() });
    /// <summary>
    /// Distort the image to exactly the given dimensions.
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public BuildNode Distort(uint w, uint h) => Distort(w, h, null);
    /// <summary>
    /// Distort the image to exactly the given dimensions.
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="hints"></param>
    /// <returns></returns>
    public BuildNode Distort(uint w, uint h, ResampleHints? hints)
        // => To(new
        // {
        //     resample_2d = new
        //     {
        //         w,
        //         h,
        //         hints = hints?.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["resample_2d"] = new JsonObject
            {
                ["w"] = w,
                ["h"] = h,
                ["hints"] = hints?.ToJsonNode()
            }
        });

    /// <summary>
    /// Crops the image to the given coordinates
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    public BuildNode Crop(int x1, int y1, int x2, int y2)
        // => To(new
        // {
        //     crop = new
        //     {
        //         x1,
        //         y1,
        //         x2,
        //         y2
        //     }
        // });
        => To(new JsonObject
        {
            ["crop"] = new JsonObject
            {
                ["x1"] = x1,
                ["y1"] = y1,
                ["x2"] = x2,
                ["y2"] = y2
            }
        });

    /// <summary>
    /// Region is like a crop command, but you can specify coordinates outside of the image and
    /// thereby add padding. It's like a window. Coordinates are in pixels.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public BuildNode Region(int x1, int y1, int x2, int y2, AnyColor backgroundColor)
        // => To(new
        // {
        //     region = new
        //     {
        //         x1,
        //         y1,
        //         x2,
        //         y2,
        //         background_color = backgroundColor.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["region"] = new JsonObject
            {
                ["x1"] = x1,
                ["y1"] = y1,
                ["x2"] = x2,
                ["y2"] = y2,
                ["background_color"] = backgroundColor.ToJsonNode()
            }
        });

    /// <summary>
    /// Region is like a crop command, but you can specify coordinates outside of the image and
    /// thereby add padding. It's like a window.
    /// You can specify a region as a percentage of the image's width and height.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public BuildNode RegionPercent(float x1, float y1, float x2, float y2, AnyColor backgroundColor)
        // => To(new
        // {
        //     region_percent = new
        //     {
        //         x1,
        //         y1,
        //         x2,
        //         y2,
        //         background_color = backgroundColor.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["region_percent"] = new JsonObject
            {
                ["x1"] = x1,
                ["y1"] = y1,
                ["x2"] = x2,
                ["y2"] = y2,
                ["background_color"] = backgroundColor.ToJsonNode()
            }
        });

    /// <summary>
    /// Crops away whitespace of any color at the edges of the image.
    /// </summary>
    /// <param name="threshold">(1..255). determines how much noise/edges to tolerate before cropping
    /// is finalized. 80 is a good starting point.</param>
    /// <param name="percentPadding">determines how much of the image to restore after cropping to
    /// provide some padding. 0.5 (half a percent) is a good starting point.</param>
    /// <returns></returns>
    public BuildNode CropWhitespace(int threshold, float percentPadding)
        // => To(new
        // {
        //     crop_whitespace = new
        //     {
        //         threshold,
        //         percent_padding = percentPadding
        //     }
        // });
        => To(new JsonObject
        {
            ["crop_whitespace"] = new JsonObject
            {
                ["threshold"] = threshold,
                ["percent_padding"] = percentPadding
            }
        });

    /// <summary>
    /// Does not honor encoding or decoding parameters. Use ImageJob.BuildCommandString() instead unless
    /// you are actually combining this node with others in a job.
    /// </summary>
    /// <param name="commandString"></param>
    /// <returns></returns>
    public BuildNode ResizerCommands(string commandString)
        // => To(new
        // {
        //     command_string = new
        //     {
        //         kind = "ir4",
        //         value = commandString
        //     }
        // });
        => To(new JsonObject
        {
            ["command_string"] = new JsonObject
            {
                ["kind"] = "ir4",
                ["value"] = commandString
            }
        });

    /// <summary>
    /// Flips the image vertically
    /// </summary>
    /// <returns></returns>
    public BuildNode FlipVertical()
        // => To(new {flip_v = (string?)null});
        => To(new JsonObject { ["flip_v"] = null });
    /// <summary>
    /// Flips the image horizontally
    /// </summary>
    /// <returns></returns>
    public BuildNode FlipHorizontal() // => To(new {flip_h = (string?)null });
        => To(new JsonObject { ["flip_h"] = null });

    /// <summary>
    /// Rotates the image 90 degrees clockwise.
    /// </summary>
    /// <returns></returns>
    public BuildNode Rotate90() // => To(new {rotate_90 = (string?)null });
        => To(new JsonObject { ["rotate_90"] = null });
    /// <summary>
    /// Rotates the image 180 degrees clockwise.
    /// </summary>
    /// <returns></returns>
    public BuildNode Rotate180() // To(new {rotate_180 = (string?)null });
        => To(new JsonObject { ["rotate_180"] = null });
    /// <summary>
    /// Rotates the image 270 degrees clockwise. (same as 90 degrees counterclockwise).
    /// </summary>
    /// <returns></returns>
    public BuildNode Rotate270() //To(new {rotate_270 = (string?)null });
        => To(new JsonObject { ["rotate_270"] = null });
    /// <summary>
    /// Swaps the x and y dimensions of the image
    /// </summary>
    /// <returns></returns>
    public BuildNode Transpose() //=> To(new {transpose = (string?)null });
        => To(new JsonObject { ["transpose"] = null });

    /// <summary>
    /// Allows you to generate multiple outputs by branching the graph
    /// <code>
    /// var r = await b.Decode(imageBytes)
    ///     .Branch(f => f.EncodeToBytes(new WebPLosslessEncoder()))
    ///     .Branch(f => f.EncodeToBytes(new WebPLossyEncoder(50)))
    ///     .EncodeToBytes(new LibPngEncoder())
    ///     .Finish().InProcessAsync();
    /// </code>
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public BuildNode Branch(Func<BuildNode, BuildEndpoint> f)
    {
        f(this);
        return this;
    }

    /// <summary>
    /// Copies (not composes) the given rectangle from input to canvas.
    /// You cannot copy from a BGRA input to a BGR canvas.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="area"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public BuildNode CopyRectTo(BuildNode canvas, Rectangle area, Point to)
        //     => NodeWithCanvas(canvas, new
        // {
        //     copy_rect_to_canvas = new
        //     {
        //         from_x = area.X,
        //         from_y = area.Y,
        //         w = area.Width,
        //         h = area.Height,
        //         x = to.X,
        //         y = to.Y
        //     }
        // });
        => NodeWithCanvas(canvas, new JsonObject
        {
            ["copy_rect_to_canvas"] = new JsonObject
            {
                ["from_x"] = area.X,
                ["from_y"] = area.Y,
                ["w"] = area.Width,
                ["h"] = area.Height,
                ["x"] = to.X,
                ["y"] = to.Y
            }
        });

    /// <summary>
    /// Draws the input image to the given rectangle on the canvas, distorting if the aspect ratios differ.
    ///
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="to"></param>
    /// <param name="hints"></param>
    /// <param name="blend"></param>
    /// <returns></returns>
    public BuildNode DrawImageExactTo(BuildNode canvas, Rectangle to, ResampleHints? hints, CompositingMode? blend)
        //     => NodeWithCanvas(canvas, new
        // {
        //     draw_image_exact = new
        //     {
        //         w = to.Width,
        //         h = to.Height,
        //         x = to.X,
        //         y = to.Y,
        //         blend = blend?.ToString()?.ToLowerInvariant(),
        //         hints = hints?.ToImageflowDynamic()
        //     }
        // });
        => NodeWithCanvas(canvas, new JsonObject
        {
            ["draw_image_exact"] = new JsonObject
            {
                ["w"] = to.Width,
                ["h"] = to.Height,
                ["x"] = to.X,
                ["y"] = to.Y,
                ["blend"] = blend?.ToString().ToLowerInvariant(),
                ["hints"] = hints?.ToJsonNode()
            }
        });

    /// <summary>
    /// Rounds all 4 corners using the given radius in pixels
    /// </summary>
    /// <param name="radiusPixels"></param>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public BuildNode RoundAllImageCorners(int radiusPixels, AnyColor backgroundColor)
        // => To(new
        // {
        //     round_image_corners = new
        //     {
        //         radius = new
        //         {
        //             pixels = radiusPixels
        //         },
        //         background_color = backgroundColor.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["round_image_corners"] = new JsonObject
            {
                ["radius"] = new JsonObject
                {
                    ["pixels"] = radiusPixels
                },
                ["background_color"] = backgroundColor.ToJsonNode()
            }
        });

    /// <summary>
    /// Rounds all 4 corners by a percentage. 100% would make a circle if the image was square.
    /// </summary>
    /// <param name="radiusPercent"></param>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public BuildNode RoundAllImageCornersPercent(float radiusPercent, AnyColor backgroundColor)
        // => To(new
        // {
        //     round_image_corners = new
        //     {
        //         radius = new
        //         {
        //             percentage = radiusPercent
        //         },
        //         background_color = backgroundColor.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["round_image_corners"] = new JsonObject
            {
                ["radius"] = new JsonObject
                {
                    ["percentage"] = radiusPercent
                },
                ["background_color"] = backgroundColor.ToJsonNode()
            }
        });

    /// <summary>
    /// Fills the given rectangle with the specified color
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public BuildNode FillRectangle(int x1, int y1, int x2, int y2, AnyColor color)
        // => To(new
        // {
        //     fill_rect = new
        //     {
        //         x1,
        //         y1,
        //         x2,
        //         y2,
        //         color = color.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["fill_rect"] = new JsonObject
            {
                ["x1"] = x1,
                ["y1"] = y1,
                ["x2"] = x2,
                ["y2"] = y2,
                ["color"] = color.ToJsonNode()
            }
        });

    /// <summary>
    /// Adds padding of the given color by enlarging the canvas on the sides specified.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public BuildNode ExpandCanvas(int left, int top, int right, int bottom, AnyColor color)
        // => To(new
        // {
        //     expand_canvas = new
        //     {
        //         left,
        //         top,
        //         right,
        //         bottom,
        //         color = color.ToImageflowDynamic()
        //     }
        // });
        => To(new JsonObject
        {
            ["expand_canvas"] = new JsonObject
            {
                ["left"] = left,
                ["top"] = top,
                ["right"] = right,
                ["bottom"] = bottom,
                ["color"] = color.ToJsonNode()
            }
        });
    /// <summary>
    /// This command is not endorsed as it operates in the sRGB space and does not produce perfect results.
    /// </summary>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public BuildNode WhiteBalanceSrgb(int threshold)
        // => To(new
        // {
        //     white_balance_histogram_area_threshold_srgb = new
        //     {
        //         threshold
        //     }
        // });
        => To(new JsonObject
        {
            ["white_balance_histogram_area_threshold_srgb"] = new JsonObject
            {
                ["threshold"] = threshold
            }
        });

    /// <summary>
    /// Set the transparency of the image from 0 (transparent) to 1 (opaque)
    /// </summary>
    /// <param name="opacity"></param>
    /// <returns></returns>
    public BuildNode TransparencySrgb(float opacity)
        // => To(new
        // {
        //     color_filter_srgb = new
        //     {
        //         alpha = opacity
        //     }
        // });
        => To(new JsonObject
        {
            ["color_filter_srgb"] = new JsonObject
            {
                ["alpha"] = opacity
            }
        });

    /// <summary>
    /// Adjust contrast between -1 and 1.
    /// </summary>
    /// <param name="amount">-1...1</param>
    /// <returns></returns>
    public BuildNode ContrastSrgb(float amount)
        // => To(new
        // {
        //     color_filter_srgb = new
        //     {
        //         contrast = amount
        //     }
        // });
        => To(new JsonObject
        {
            ["color_filter_srgb"] = new JsonObject
            {
                ["contrast"] = amount
            }
        });

    /// <summary>
    /// Adjust brightness between -1 and 1.
    /// </summary>
    /// <param name="amount">-1...1</param>
    /// <returns></returns>
    public BuildNode BrightnessSrgb(float amount)
        // => To(new
        // {
        //     color_filter_srgb = new
        //     {
        //         brightness = amount
        //     }
        // });
        => To(new JsonObject
        {
            ["color_filter_srgb"] = new JsonObject
            {
                ["brightness"] = amount
            }
        });

    /// <summary>
    /// Adjust saturation between -1 and 1.
    /// </summary>
    /// <param name="amount">-1...1</param>
    /// <returns></returns>
    public BuildNode SaturationSrgb(float amount)
        // => To(new
        // {
        //     color_filter_srgb = new
        //     {
        //         saturation = amount
        //     }
        // });
        => To(new JsonObject
        {
            ["color_filter_srgb"] = new JsonObject
            {
                ["saturation"] = amount
            }
        });

    /// <summary>
    /// Apply filters like grayscale, sepia, or inversion in the sRGB color space
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public BuildNode ColorFilterSrgb(ColorFilterSrgb filter)
        // => To(new
        // {
        //     color_filter_srgb = filter.ToString().ToLowerInvariant()
        // });
        => To(new JsonObject
        {
            ["color_filter_srgb"] = filter.ToString().ToLowerInvariant()
        });

    /// <summary>
    /// Draw a watermark from the given BytesSource or StreamSource
    /// </summary>
    /// <param name="source"></param>
    /// <param name="watermark"></param>
    /// <returns></returns>
    [Obsolete("Use Watermark(IMemorySource source, ..) instead. BufferedStreamSource and MemorySource are preferred over BytesSource and StreamSource.")]
    public BuildNode Watermark(IBytesSource source, WatermarkOptions watermark) =>
        Watermark(source.ToMemorySource(), null, watermark);

    /// <summary>
    /// Draw a watermark from the given BufferedStreamSource or MemorySource
    /// </summary>
    /// <param name="source"></param>
    /// <param name="watermark"></param>
    /// <returns></returns>
    public BuildNode Watermark(IAsyncMemorySource source, WatermarkOptions watermark) =>
        Watermark(source, null, watermark);

    /// <summary>
    /// Draw a watermark from the given BytesSource or StreamSource
    /// </summary>
    /// <param name="source"></param>
    /// <param name="ioId"></param>
    /// <param name="watermark"></param>
    /// <returns></returns>
    [Obsolete("Use Watermark(IMemorySource source, ..) instead.  BufferedStreamSource and MemorySource are preferred over BytesSource and StreamSource.")]
    public BuildNode Watermark(IBytesSource source, int? ioId, WatermarkOptions watermark)
    {
        return Watermark(source.ToMemorySource(), ioId, watermark);
    }

    /// <summary>
    /// Draw a watermark from the given BufferedStreamSource or MemorySource
    /// </summary>
    /// <param name="source"></param>
    /// <param name="ioId"></param>
    /// <param name="watermark"></param>
    /// <returns></returns>
    public BuildNode Watermark(IAsyncMemorySource source, int? ioId, WatermarkOptions watermark)
    {
        ioId ??= Builder.GenerateIoId();
        Builder.AddInput(ioId.Value, source);
        // return To(new
        // {
        //     watermark = watermark.ToImageflowDynamic(ioId.Value)
        // });
        return To(new JsonObject
        {
            ["watermark"] = watermark.ToJsonNode(ioId.Value)
        });

    }

}
