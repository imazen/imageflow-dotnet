using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

/// <summary>
/// Image format identifier for format-level enable/disable.
/// </summary>
public enum ImageFormat
{
    Jpeg,
    Png,
    Gif,
    Webp,
    Bmp,
    Pnm,
    Farbfeld,
    Jxl,
    Avif,
    Heic,
}

/// <summary>
/// Named format preset for controlling which image formats are enabled.
/// </summary>
public enum FormatPreset
{
    /// <summary>Only JPEG, PNG, GIF. Universally supported by all browsers.</summary>
    WebSafe,
    /// <summary>JPEG, PNG, GIF, WebP. Supported by 97%+ of browsers.</summary>
    ModernWebSafe,
    /// <summary>All formats enabled.</summary>
    All,
}

/// <summary>
/// Named codec implementation preset. Controls which codec implementations are available
/// and their priority ordering (e.g., mozjpeg vs zenjpeg).
/// </summary>
public enum CodecPreset
{
    /// <summary>C codecs only. No pure-Rust zen codecs. Pre-zen behavior.</summary>
    Legacy,
    /// <summary>C codecs primary. Zen codecs supplement with JXL/AVIF/HEIC and as fallbacks.</summary>
    Transitional,
    /// <summary>Zen codecs primary. C codecs as fallback. This is the default.</summary>
    Modern,
    /// <summary>Pure Rust only. No C codec libraries used.</summary>
    Experimental,
}

/// <summary>
/// Identifies a specific codec implementation (decoder or encoder).
/// </summary>
public enum CodecName
{
    // Decoders
    MozjpegDecoder,
    ImageRsJpegDecoder,
    LibpngDecoder,
    ImageRsPngDecoder,
    LibwebpDecoder,
    GifRsDecoder,
    ZenjpegDecoder,
    ZenwebpDecoder,
    ZengifDecoder,
    ZenjxlDecoder,
    ZenavifDecoder,
    ZenheicDecoder,
    ZenbmpDecoder,
    ZenpnmDecoder,
    ZenfarbfeldDecoder,

    // Encoders
    MozjpegEncoder,
    LibpngEncoder,
    LibwebpEncoder,
    PngquantEncoder,
    LodepngEncoder,
    GifEncoder,
    ZenjpegEncoder,
    ZenwebpEncoder,
    ZengifEncoder,
    ZenjxlEncoder,
    ZenavifEncoder,
    ZenbmpEncoder,
    ZenpnmEncoder,
    ZenfarbfeldEncoder,
}

/// <summary>
/// Controls which image formats are enabled for decoding.
/// Application order: preset → enable → disable.
/// </summary>
public class DecodeFormatConfig
{
    /// <summary>
    /// Named preset. When absent, all compiled-in formats are enabled.
    /// </summary>
    public FormatPreset? Preset { get; set; }

    /// <summary>
    /// Enable decoding for these formats (applied after preset).
    /// </summary>
    public IList<ImageFormat>? Enable { get; set; }

    /// <summary>
    /// Disable decoding for these formats (applied after enable).
    /// </summary>
    public IList<ImageFormat>? Disable { get; set; }

    internal JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Preset != null)
            node.Add("preset", ToSnakeCase(Preset.Value));
        if (Enable is { Count: > 0 })
            node.Add("enable", new JsonArray(Enable.Select(f => (JsonNode)JsonValue.Create(ToSnakeCase(f))).ToArray()));
        if (Disable is { Count: > 0 })
            node.Add("disable", new JsonArray(Disable.Select(f => (JsonNode)JsonValue.Create(ToSnakeCase(f))).ToArray()));
        return node;
    }

    private static string ToSnakeCase(FormatPreset p) => p switch
    {
        FormatPreset.WebSafe => "web_safe",
        FormatPreset.ModernWebSafe => "modern_web_safe",
        FormatPreset.All => "all",
        _ => p.ToString().ToLowerInvariant(),
    };

    private static string ToSnakeCase(ImageFormat f) => f.ToString().ToLowerInvariant();
}

/// <summary>
/// Controls which image formats are enabled for encoding.
/// Application order: preset → enable → disable.
/// </summary>
public class EncodeFormatConfig
{
    /// <summary>
    /// Named preset. When absent, all compiled-in formats are enabled.
    /// </summary>
    public FormatPreset? Preset { get; set; }

    /// <summary>
    /// Enable encoding for these formats (applied after preset).
    /// </summary>
    public IList<ImageFormat>? Enable { get; set; }

    /// <summary>
    /// Disable encoding for these formats (applied after enable).
    /// </summary>
    public IList<ImageFormat>? Disable { get; set; }

    internal JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Preset != null)
            node.Add("preset", DecodeFormatConfigSerializer.ToSnakeCase(Preset.Value));
        if (Enable is { Count: > 0 })
            node.Add("enable", new JsonArray(Enable.Select(f => (JsonNode)JsonValue.Create(f.ToString().ToLowerInvariant())).ToArray()));
        if (Disable is { Count: > 0 })
            node.Add("disable", new JsonArray(Disable.Select(f => (JsonNode)JsonValue.Create(f.ToString().ToLowerInvariant())).ToArray()));
        return node;
    }
}

/// <summary>
/// Controls which codec implementations are used and their priority.
/// Application order: preset → enable → disable → prefer.
/// This selects which implementations handle each format (e.g., mozjpeg vs zenjpeg),
/// not which formats are enabled.
/// </summary>
public class CodecConfig
{
    /// <summary>
    /// Named preset for initial codec ordering. Default: Modern.
    /// </summary>
    public CodecPreset? Preset { get; set; }

    /// <summary>
    /// Re-add codec implementations removed by the preset (applied after preset).
    /// </summary>
    public IList<CodecName>? Enable { get; set; }

    /// <summary>
    /// Remove these codec implementations entirely (applied after enable).
    /// </summary>
    public IList<CodecName>? Disable { get; set; }

    /// <summary>
    /// Move these codec implementations to the front of their format group (applied after disable).
    /// </summary>
    public IList<CodecName>? Prefer { get; set; }

    internal JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (Preset != null)
            node.Add("preset", Preset.Value.ToString().ToLowerInvariant());
        if (Enable is { Count: > 0 })
            node.Add("enable", new JsonArray(Enable.Select(c => (JsonNode)JsonValue.Create(CodecNameToSnake(c))).ToArray()));
        if (Disable is { Count: > 0 })
            node.Add("disable", new JsonArray(Disable.Select(c => (JsonNode)JsonValue.Create(CodecNameToSnake(c))).ToArray()));
        if (Prefer is { Count: > 0 })
            node.Add("prefer", new JsonArray(Prefer.Select(c => (JsonNode)JsonValue.Create(CodecNameToSnake(c))).ToArray()));
        return node;
    }

    private static string CodecNameToSnake(CodecName name) => name switch
    {
        CodecName.MozjpegDecoder => "mozjpeg_decoder",
        CodecName.ImageRsJpegDecoder => "image_rs_jpeg_decoder",
        CodecName.LibpngDecoder => "libpng_decoder",
        CodecName.ImageRsPngDecoder => "image_rs_png_decoder",
        CodecName.LibwebpDecoder => "libwebp_decoder",
        CodecName.GifRsDecoder => "gif_rs_decoder",
        CodecName.ZenjpegDecoder => "zenjpeg_decoder",
        CodecName.ZenwebpDecoder => "zenwebp_decoder",
        CodecName.ZengifDecoder => "zengif_decoder",
        CodecName.ZenjxlDecoder => "zenjxl_decoder",
        CodecName.ZenavifDecoder => "zenavif_decoder",
        CodecName.ZenheicDecoder => "zenheic_decoder",
        CodecName.ZenbmpDecoder => "zenbmp_decoder",
        CodecName.ZenpnmDecoder => "zenpnm_decoder",
        CodecName.ZenfarbfeldDecoder => "zenfarbfeld_decoder",
        CodecName.MozjpegEncoder => "mozjpeg_encoder",
        CodecName.LibpngEncoder => "libpng_encoder",
        CodecName.LibwebpEncoder => "libwebp_encoder",
        CodecName.PngquantEncoder => "pngquant_encoder",
        CodecName.LodepngEncoder => "lodepng_encoder",
        CodecName.GifEncoder => "gif_encoder",
        CodecName.ZenjpegEncoder => "zenjpeg_encoder",
        CodecName.ZenwebpEncoder => "zenwebp_encoder",
        CodecName.ZengifEncoder => "zengif_encoder",
        CodecName.ZenjxlEncoder => "zenjxl_encoder",
        CodecName.ZenavifEncoder => "zenavif_encoder",
        CodecName.ZenbmpEncoder => "zenbmp_encoder",
        CodecName.ZenpnmEncoder => "zenpnm_encoder",
        CodecName.ZenfarbfeldEncoder => "zenfarbfeld_encoder",
        _ => name.ToString().ToLowerInvariant(),
    };
}

internal static class DecodeFormatConfigSerializer
{
    internal static string ToSnakeCase(FormatPreset p) => p switch
    {
        FormatPreset.WebSafe => "web_safe",
        FormatPreset.ModernWebSafe => "modern_web_safe",
        FormatPreset.All => "all",
        _ => p.ToString().ToLowerInvariant(),
    };
}
