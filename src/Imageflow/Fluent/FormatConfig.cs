using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

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
/// Controls which image formats are enabled for decoding.
/// Application order: preset → enable → disable.
/// </summary>
/// <remarks>
/// Format identifiers use the shared <see cref="ImageFormat"/> enum (the
/// three-layer killbits mirror of <c>imageflow_types::killbits::ImageFormat</c>).
/// </remarks>
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
            node.Add("preset", DecodeFormatConfigSerializer.ToSnakeCase(Preset.Value));
        if (Enable is { Count: > 0 })
            node.Add("enable", new JsonArray(Enable.Select(f => (JsonNode)JsonValue.Create(f.ToSnakeCase())).ToArray()));
        if (Disable is { Count: > 0 })
            node.Add("disable", new JsonArray(Disable.Select(f => (JsonNode)JsonValue.Create(f.ToSnakeCase())).ToArray()));
        return node;
    }
}

/// <summary>
/// Controls which image formats are enabled for encoding.
/// Application order: preset → enable → disable.
/// </summary>
/// <remarks>
/// Format identifiers use the shared <see cref="ImageFormat"/> enum (the
/// three-layer killbits mirror of <c>imageflow_types::killbits::ImageFormat</c>).
/// </remarks>
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
            node.Add("enable", new JsonArray(Enable.Select(f => (JsonNode)JsonValue.Create(f.ToSnakeCase())).ToArray()));
        if (Disable is { Count: > 0 })
            node.Add("disable", new JsonArray(Disable.Select(f => (JsonNode)JsonValue.Create(f.ToSnakeCase())).ToArray()));
        return node;
    }
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
