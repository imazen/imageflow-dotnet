using System.Text.Json.Nodes;

namespace Imageflow.Bindings;

/// <summary>
/// Extension methods on <see cref="JobContext"/> for querying codec capabilities,
/// detecting formats, and listing querystring keys.
///
/// These endpoints are fast (cached on the native side after first call).
/// </summary>
public static class CodecCapabilities
{
    // ─── Codec list ───

    /// <summary>
    /// List all supported image codecs with their capabilities.
    /// Returns format name, MIME types, extensions, alpha/lossless/animation support,
    /// and decode/encode availability.
    /// </summary>
    /// <remarks>
    /// Cached on the native side — first call initializes, subsequent calls are fast.
    /// </remarks>
    public static CodecInfo[] GetCodecs(this JobContext ctx)
    {
        var node = ctx.InvokeAndParse("v3/codecs/list");
        var data = ExtractData(node);
        var codecs = data?["codecs"]?.AsArray();
        if (codecs == null) return [];
        return codecs
            .Where(c => c != null)
            .Select(c => CodecInfo.FromJsonNode(c!))
            .ToArray();
    }

    /// <summary>
    /// Get codec info for a specific format name (e.g., "jpeg", "webp").
    /// Returns null if the format is not known.
    /// </summary>
    public static CodecInfo? GetCodec(this JobContext ctx, string formatName)
    {
        return ctx.GetCodecs().FirstOrDefault(c =>
            c.Name.Equals(formatName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all formats that can be decoded.
    /// </summary>
    public static CodecInfo[] GetDecodableFormats(this JobContext ctx)
        => ctx.GetCodecs().Where(c => c.CanDecode).ToArray();

    /// <summary>
    /// Get all formats that can be encoded.
    /// </summary>
    public static CodecInfo[] GetEncodableFormats(this JobContext ctx)
        => ctx.GetCodecs().Where(c => c.CanEncode).ToArray();

    // ─── Format detection ───

    /// <summary>
    /// Detect image format from a peek buffer (first 16–32 bytes of a file).
    /// Uses magic byte patterns — no full decode needed.
    /// </summary>
    /// <param name="ctx">The job context.</param>
    /// <param name="peekBytes">First bytes of the image file.</param>
    /// <returns>Detection result with format info, or Detected=false if unknown.</returns>
    public static FormatDetectionResult DetectFormat(this JobContext ctx, byte[] peekBytes)
    {
        var bytesArray = new JsonArray();
        foreach (var b in peekBytes)
            bytesArray.Add((int)b);

        var request = new JsonObject { ["bytes"] = bytesArray };
        var node = ctx.InvokeAndParse("v3/codecs/detect", request);
        var data = ExtractData(node);
        if (data == null) return new FormatDetectionResult { Detected = false };
        return FormatDetectionResult.FromJsonNode(data);
    }

    /// <summary>
    /// Detect image format from a stream (reads first 32 bytes, then resets position).
    /// </summary>
    public static FormatDetectionResult DetectFormat(this JobContext ctx, Stream stream)
    {
        var peek = new byte[32];
        var pos = stream.Position;
        var read = stream.Read(peek, 0, peek.Length);
        stream.Position = pos; // Reset to original position
        var actual = new byte[read];
        Array.Copy(peek, actual, read);
        return ctx.DetectFormat(actual);
    }

    // ─── Querystring keys ───

    /// <summary>
    /// Get all RIAPI querystring keys grouped by node.
    /// Includes key names, aliases, labels, descriptions, and value schemas.
    /// </summary>
    /// <remarks>
    /// Cached on the native side — first call initializes, subsequent calls are fast.
    /// </remarks>
    public static QuerystringKeyGroup[] GetQuerystringKeys(this JobContext ctx)
    {
        var node = ctx.InvokeAndParse("v3/schema/querystring/keys");
        var data = ExtractData(node);
        var nodesObj = data?["nodes"]?.AsObject();
        if (nodesObj == null) return [];

        return nodesObj.Select(kvp =>
        {
            var nodeInfo = kvp.Value?.AsObject();
            var keys = nodeInfo?["keys"]?.AsArray() ?? new JsonArray();

            return new QuerystringKeyGroup
            {
                NodeId = kvp.Key,
                Label = nodeInfo?["label"]?.GetValue<string>() ?? kvp.Key,
                Keys = keys.Where(k => k != null).Select(k =>
                {
                    var keyObj = k!.AsObject();
                    return new QuerystringKeyInfo
                    {
                        Key = keyObj["key"]?.GetValue<string>() ?? "",
                        Param = keyObj["param"]?.GetValue<string>() ?? "",
                        Label = keyObj["label"]?.GetValue<string>() ?? "",
                        Description = keyObj["description"]?.GetValue<string>(),
                        Aliases = keyObj["aliases"]?.AsArray()
                            .Select(a => a?.GetValue<string>() ?? "")
                            .ToArray() ?? [],
                    };
                }).ToArray()
            };
        }).ToArray();
    }

    /// <summary>
    /// Get all supported RIAPI querystring key names (flat list).
    /// Includes both primary keys and aliases.
    /// </summary>
    public static string[] GetAllQuerystringKeyNames(this JobContext ctx)
    {
        return ctx.GetQuerystringKeys()
            .SelectMany(g => g.Keys)
            .SelectMany(k => new[] { k.Key }.Concat(k.Aliases))
            .Distinct()
            .OrderBy(k => k)
            .ToArray();
    }

    // ─── Helper ───

    /// <summary>
    /// Extract the "data" field from the standard response wrapper.
    /// The native lib wraps responses as {"code":200,"success":true,"data":{...}}.
    /// </summary>
    private static JsonNode? ExtractData(JsonNode? node)
    {
        if (node is JsonObject obj && obj.TryGetPropertyValue("data", out var data))
            return data;
        return node; // Fallback: return as-is (maybe no wrapper)
    }
}
