using System.Text.Json;
using System.Text.Json.Nodes;

namespace Imageflow.Bindings;

/// <summary>
/// Information about a supported image format/codec.
/// </summary>
public sealed class CodecInfo
{
    /// <summary>Format name (e.g., "jpeg", "png", "webp").</summary>
    public string Name { get; init; } = "";
    /// <summary>Primary MIME type (e.g., "image/jpeg").</summary>
    public string MimeType { get; init; } = "";
    /// <summary>All accepted MIME types.</summary>
    public string[] MimeTypes { get; init; } = [];
    /// <summary>Primary file extension without dot (e.g., "jpg").</summary>
    public string Extension { get; init; } = "";
    /// <summary>All accepted file extensions.</summary>
    public string[] Extensions { get; init; } = [];
    /// <summary>Whether this format supports alpha channels.</summary>
    public bool SupportsAlpha { get; init; }
    /// <summary>Whether this format supports lossless compression.</summary>
    public bool SupportsLossless { get; init; }
    /// <summary>Whether this format supports animation/multi-frame.</summary>
    public bool SupportsAnimation { get; init; }
    /// <summary>Whether decoding is available (compiled in and enabled).</summary>
    public bool CanDecode { get; init; }
    /// <summary>Whether encoding is available (compiled in and enabled).</summary>
    public bool CanEncode { get; init; }

    internal static CodecInfo FromJsonNode(JsonNode node)
    {
        return new CodecInfo
        {
            Name = node["name"]?.GetValue<string>() ?? "",
            MimeType = node["mime_type"]?.GetValue<string>() ?? "",
            MimeTypes = node["mime_types"]?.AsArray().Select(n => n?.GetValue<string>() ?? "").ToArray() ?? [],
            Extension = node["extension"]?.GetValue<string>() ?? "",
            Extensions = node["extensions"]?.AsArray().Select(n => n?.GetValue<string>() ?? "").ToArray() ?? [],
            SupportsAlpha = node["supports_alpha"]?.GetValue<bool>() ?? false,
            SupportsLossless = node["supports_lossless"]?.GetValue<bool>() ?? false,
            SupportsAnimation = node["supports_animation"]?.GetValue<bool>() ?? false,
            CanDecode = node["can_decode"]?.GetValue<bool>() ?? false,
            CanEncode = node["can_encode"]?.GetValue<bool>() ?? false,
        };
    }
}

/// <summary>
/// Result of format detection from a peek buffer.
/// </summary>
public sealed class FormatDetectionResult
{
    /// <summary>Whether a format was detected.</summary>
    public bool Detected { get; init; }
    /// <summary>The detected format info (null if not detected).</summary>
    public CodecInfo? Format { get; init; }

    internal static FormatDetectionResult FromJsonNode(JsonNode node)
    {
        var detected = node["detected"]?.GetValue<bool>() ?? false;
        if (!detected) return new FormatDetectionResult { Detected = false };

        return new FormatDetectionResult
        {
            Detected = true,
            Format = new CodecInfo
            {
                Name = node["name"]?.GetValue<string>() ?? "",
                MimeType = node["mime_type"]?.GetValue<string>() ?? "",
                Extension = node["extension"]?.GetValue<string>() ?? "",
                SupportsAlpha = node["supports_alpha"]?.GetValue<bool>() ?? false,
                SupportsLossless = node["supports_lossless"]?.GetValue<bool>() ?? false,
                SupportsAnimation = node["supports_animation"]?.GetValue<bool>() ?? false,
                CanDecode = node["can_decode"]?.GetValue<bool>() ?? false,
                CanEncode = node["can_encode"]?.GetValue<bool>() ?? false,
            }
        };
    }
}

/// <summary>
/// A single RIAPI querystring key with its metadata.
/// </summary>
public sealed class QuerystringKeyInfo
{
    /// <summary>The querystring key (e.g., "w", "jpeg.quality").</summary>
    public string Key { get; init; } = "";
    /// <summary>The parameter name on the node (e.g., "w").</summary>
    public string Param { get; init; } = "";
    /// <summary>Human-readable label (e.g., "Width").</summary>
    public string Label { get; init; } = "";
    /// <summary>Description of what this key does.</summary>
    public string? Description { get; init; }
    /// <summary>Alias keys that map to the same parameter.</summary>
    public string[] Aliases { get; init; } = [];
}

/// <summary>
/// Querystring keys grouped by node.
/// </summary>
public sealed class QuerystringKeyGroup
{
    /// <summary>Node ID (e.g., "zenresize.constrain").</summary>
    public string NodeId { get; init; } = "";
    /// <summary>Human-readable node label.</summary>
    public string Label { get; init; } = "";
    /// <summary>Keys handled by this node.</summary>
    public QuerystringKeyInfo[] Keys { get; init; } = [];
}
