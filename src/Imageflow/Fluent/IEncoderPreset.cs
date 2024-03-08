using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

/// <summary>
/// An interface for encode presets. Concrete examples are GifEncoder, PngQuantEncoder, LodePngEncoder, MozJpegEncoder, WebPLossyEncoder, WebPLosslessEncoder
/// </summary>
public interface IEncoderPreset
{
    object ToImageflowDynamic();

    JsonNode ToJsonNode();
}

/// <summary>
/// Encodes the image as a .gif
/// </summary>
public class GifEncoder : IEncoderPreset
{
    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { gif = (string?)null };

    public JsonNode ToJsonNode() => new JsonObject() { { "gif", (string?)null } };
}
/// <summary>
/// Use LodePngEncoder instead
/// </summary>
[Obsolete("Use PngQuantEncoder or LodePngEncoder instead")]
public class LibPngEncoder : IEncoderPreset
{
    public AnyColor? Matte { get; set; }
    public int? ZlibCompression { get; set; }
    public PngBitDepth? BitDepth { get; set; }

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { libpng = new { depth = BitDepth?.ToString().ToLowerInvariant(), zlib_compression = ZlibCompression, matte = Matte?.ToImageflowDynamic() } };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (BitDepth != null)
        {
            node.Add("depth", BitDepth?.ToString().ToLowerInvariant());
        }

        if (ZlibCompression != null)
        {
            node.Add("zlib_compression", ZlibCompression);
        }

        if (Matte != null)
        {
            node.Add("matte", Matte?.ToJsonNode());
        }

        return new JsonObject() { { "libpng", node } };
    }
}

public class PngQuantEncoder : IEncoderPreset
{
    public PngQuantEncoder() : this(null, null) { }
    /// <summary>
    /// Try to quantize the PNG first, falling back to lossless PNG if the minimumQuality value cannot be reached
    /// </summary>
    /// <param name="quality">The target visual quality</param>
    /// <param name="minimumQuality">The minimum visual quality below which to revert to lossless encoding</param>
    public PngQuantEncoder(int? quality, int? minimumQuality)
    {
        Quality = quality;
        MinimumQuality = minimumQuality;
    }
    /// <summary>
    /// (0..100) The target visual quality. Try to quantize the PNG first, falling back to lossless PNG if the MinimumQuality value cannot be reached
    /// </summary>
    public int? Quality { get; set; }

    /// <summary>
    /// (0..100) The minimum visual quality below which to revert to lossless encoding
    /// </summary>
    public int? MinimumQuality { get; set; }

    /// <summary>
    /// speed: 1..10 controls the speed/quality trade-off for encoding.
    /// </summary>
    public int? Speed { get; set; }
    /// <summary>
    /// When true, uses drastically more CPU time for a 1-2% reduction in file size
    /// </summary>
    public bool? MaximumDeflate { get; set; }

    /// <summary>
    /// (0..100) The target visual quality. Try to quantize the PNG first, falling back to lossless PNG if the MinimumQuality value cannot be reached
    /// </summary>
    /// <param name="quality"></param>
    /// <returns></returns>
    public PngQuantEncoder SetQuality(int? quality)
    {
        Quality = quality;
        return this;
    }
    /// <summary>
    /// (0..100) The minimum visual quality below which to revert to lossless encoding
    /// </summary>
    /// <param name="minimumQuality"></param>
    /// <returns></returns>
    public PngQuantEncoder SetMinimumQuality(int? minimumQuality)
    {
        MinimumQuality = minimumQuality;
        return this;
    }

    /// <summary>
    /// speed: 1..10 controls the speed/quality trade-off for encoding.
    /// </summary>
    /// <param name="speed"></param>
    /// <returns></returns>
    public PngQuantEncoder SetSpeed(int? speed)
    {
        Speed = speed;
        return this;
    }

    /// <summary>
    /// Not suggested; only saves 1-2% on file size but takes 10x CPU time.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public PngQuantEncoder SetMaximumDeflate(bool value)
    {
        MaximumDeflate = value;
        return this;
    }
    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new
    {
        pngquant = new
        {
            quality = Quality,
            minimum_quality = MinimumQuality,
            speed = Speed,
            maximum_deflate = MaximumDeflate
        }
    };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Quality != null)
        {
            node.Add("quality", Quality);
        }

        if (MinimumQuality != null)
        {
            node.Add("minimum_quality", MinimumQuality);
        }

        if (Speed != null)
        {
            node.Add("speed", Speed);
        }

        if (MaximumDeflate != null)
        {
            node.Add("maximum_deflate", MaximumDeflate);
        }

        return new JsonObject() { { "pngquant", node } };
    }
}

public class LodePngEncoder : IEncoderPreset
{
    /// <summary>
    /// When true, uses drastically more CPU time for a 1-2% reduction in file size
    /// </summary>
    public bool? MaximumDeflate { get; set; }

    /// <summary>
    /// Not suggested; only saves 1-2% on file size but takes 10x CPU time.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public LodePngEncoder SetMaximumDeflate(bool value)
    {
        MaximumDeflate = value;
        return this;
    }
    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { lodepng = new { maximum_deflate = MaximumDeflate } };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (MaximumDeflate != null)
        {
            node.Add("maximum_deflate", MaximumDeflate);
        }

        return new JsonObject() { { "lodepng", node } };
    }
}

/// <summary>
/// Deprecated. Use MozJpegEncoder instead
/// </summary>
[Obsolete("Use MozJpegEncoder instead for smaller files")]
public class LibJpegTurboEncoder : IEncoderPreset
{
    public int? Quality { get; set; }
    public bool? Progressive { get; set; }
    public bool? OptimizeHuffmanCoding { get; set; }

    public AnyColor? Matte { get; set; }

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { libjpegturbo = new { quality = Quality, progressive = Progressive, optimize_huffman_coding = OptimizeHuffmanCoding, matte = Matte?.ToImageflowDynamic() } };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Quality != null)
        {
            node.Add("quality", Quality);
        }

        if (Progressive != null)
        {
            node.Add("progressive", Progressive);
        }

        if (OptimizeHuffmanCoding != null)
        {
            node.Add("optimize_huffman_coding", OptimizeHuffmanCoding);
        }

        if (Matte != null)
        {
            node.Add("matte", Matte?.ToJsonNode());
        }

        return new JsonObject() { { "libjpegturbo", node } };
    }
}

public class MozJpegEncoder : IEncoderPreset
{
    public MozJpegEncoder(int quality)
    {
        Quality = quality;
    }
    public MozJpegEncoder(int quality, bool progressive)
    {
        Quality = quality;
        Progressive = progressive;
    }
    public int? Quality { get; set; }
    public bool? Progressive { get; set; }

    public AnyColor? Matte { get; set; }
    public MozJpegEncoder SetProgressive(bool progressive)
    {
        Progressive = progressive;
        return this;
    }

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { mozjpeg = new { quality = Quality, progressive = Progressive, matte = Matte?.ToImageflowDynamic() } };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Quality != null)
        {
            node.Add("quality", Quality);
        }

        if (Progressive != null)
        {
            node.Add("progressive", Progressive);
        }

        if (Matte != null)
        {
            node.Add("matte", Matte?.ToJsonNode());
        }

        return new JsonObject() { { "mozjpeg", node } };
    }
}

public class WebPLossyEncoder(float quality) : IEncoderPreset
{
    public float? Quality { get; set; } = quality;

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { webplossy = new { quality = Quality } };

    public JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Quality != null)
        {
            node.Add("quality", Quality);
        }

        return new JsonObject() { { "webplossy", node } };
    }
}
public class WebPLosslessEncoder : IEncoderPreset
{
    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic() => new { webplossless = (string?)null };

    public JsonNode ToJsonNode() => new JsonObject() { { "webplossless", (string?)null } };
}
