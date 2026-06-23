using System.Text.Json.Nodes;

namespace Imageflow.Bindings;

/// <summary>
/// Reason the native side refused a decode/encode operation under the
/// three-layer killbits system.
/// </summary>
public enum KillbitsDenialKind
{
    /// <summary>An explicit named codec was requested but is denied (<c>codec_not_available</c>).</summary>
    CodecNotAvailable,
    /// <summary>Decode for this format is denied at one of the layers (<c>decode_not_available</c>).</summary>
    DecodeNotAvailable,
    /// <summary>Encode for this format is denied at one of the layers (<c>encode_not_available</c>).</summary>
    EncodeNotAvailable,
}

/// <summary>
/// Raised when imageflow rejects a job because the killbits grid denies
/// the requested decode/encode/codec.
/// </summary>
/// <remarks>
/// The native layer embeds a structured JSON body in its error message
/// when this happens:
///
/// <code>
/// { "error": "codec_not_available", "codec": "mozjpeg_encoder",
///   "format": "jpeg", "reasons": [...], "net_support": { ... } }
/// </code>
///
/// This exception parses that body and exposes the grid through
/// <see cref="NetSupport"/> so callers can surface actionable diagnostics
/// without re-querying <c>v1/context/get_net_support</c>.
///
/// If the JSON envelope can't be parsed (malformed / future-version shape),
/// the exception falls back to exposing the raw message in
/// <see cref="Exception.Message"/>; <see cref="NetSupport"/> will be
/// <c>null</c> in that case.
/// </remarks>
public sealed class KillbitsDeniedException : ImageflowException
{
    public KillbitsDenialKind DenialKind { get; }
    /// <summary>Snake-case codec name for <see cref="KillbitsDenialKind.CodecNotAvailable"/>; otherwise <c>null</c>.</summary>
    public string? Codec { get; }
    /// <summary>Snake-case format name (e.g. <c>"avif"</c>).</summary>
    public string? Format { get; }
    /// <summary>Machine-readable denial identifiers mirrored from the native grid.</summary>
    public IReadOnlyList<string> Reasons { get; }
    /// <summary>Effective net-support grid captured at rejection time, if the envelope was parseable.</summary>
    public NetSupportResponse? NetSupport { get; }

    internal KillbitsDeniedException(
        string message,
        KillbitsDenialKind kind,
        string? codec,
        string? format,
        IReadOnlyList<string> reasons,
        NetSupportResponse? netSupport)
        : base(message)
    {
        DenialKind = kind;
        Codec = codec;
        Format = format;
        Reasons = reasons;
        NetSupport = netSupport;
    }

    /// <summary>
    /// Inspect a raw error message. Returns <c>null</c> if the message
    /// isn't a killbits-shaped envelope.
    /// </summary>
    internal static KillbitsDeniedException? TryParse(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            return null;
        }
        // The envelope may be prefixed by "imageflow error: " or similar.
        // Find the first '{' and parse from there.
        var braceIdx = rawMessage.IndexOf('{');
        if (braceIdx < 0)
        {
            return null;
        }
        var slice = rawMessage.AsSpan(braceIdx).ToString();
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(slice);
        }
        catch
        {
            return null;
        }
        if (parsed is not JsonObject envelope)
        {
            return null;
        }
        var errorTag = envelope.TryGetPropertyValue("error", out var e) && e != null
            ? e.GetValue<string>()
            : null;
        KillbitsDenialKind kind = errorTag switch
        {
            "codec_not_available" => KillbitsDenialKind.CodecNotAvailable,
            "decode_not_available" => KillbitsDenialKind.DecodeNotAvailable,
            "encode_not_available" => KillbitsDenialKind.EncodeNotAvailable,
            _ => (KillbitsDenialKind)(-1),
        };
        if ((int)kind < 0)
        {
            return null;
        }

        var codec = envelope.TryGetPropertyValue("codec", out var c) && c != null ? c.GetValue<string>() : null;
        var format = envelope.TryGetPropertyValue("format", out var f) && f != null ? f.GetValue<string>() : null;
        var reasons = new List<string>();
        if (envelope.TryGetPropertyValue("reasons", out var r) && r is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    reasons.Add(item.GetValue<string>());
                }
            }
        }

        NetSupportResponse? grid = null;
        if (envelope.TryGetPropertyValue("net_support", out var ns) && ns is JsonObject nsObj)
        {
            try
            {
                // The envelope's net_support embeds only the grid (no
                // compile_ceiling / trusted_policy_set wrappers), so build
                // a synthetic response around it.
                var wrapper = new JsonObject
                {
                    ["net_support"] = nsObj.DeepClone(),
                };
                grid = NetSupportResponse.ParseGetNetSupportResponse(wrapper);
            }
            catch
            {
                grid = null;
            }
        }

        return new KillbitsDeniedException(rawMessage, kind, codec, format, reasons, grid);
    }
}
