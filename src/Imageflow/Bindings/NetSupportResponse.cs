using System.Text.Json.Nodes;

namespace Imageflow.Bindings;

/// <summary>
/// Per-format decode/encode availability in a <see cref="NetSupportResponse"/>.
/// </summary>
/// <remarks>
/// <see cref="DecodeReasons"/> / <see cref="EncodeReasons"/> are
/// machine-readable identifiers (snake_case) explaining why a cell ended
/// up denied — e.g. <c>no_available_encoder</c>, <c>denied_by_trusted_policy</c>.
/// Empty when the cell is allowed.
/// </remarks>
public sealed class FormatSupport
{
    public bool Decode { get; }
    public bool Encode { get; }
    public IReadOnlyList<string> DecodeReasons { get; }
    public IReadOnlyList<string> EncodeReasons { get; }

    internal FormatSupport(bool decode, bool encode, IReadOnlyList<string> decodeReasons, IReadOnlyList<string> encodeReasons)
    {
        Decode = decode;
        Encode = encode;
        DecodeReasons = decodeReasons;
        EncodeReasons = encodeReasons;
    }

    internal static FormatSupport FromNode(JsonNode node)
    {
        var obj = node.AsObject();
        var decode = TryBool(obj, "decode") ?? false;
        var encode = TryBool(obj, "encode") ?? false;
        var decodeReasons = ReadStringArray(obj, "decode_reasons");
        var encodeReasons = ReadStringArray(obj, "encode_reasons");
        return new FormatSupport(decode, encode, decodeReasons, encodeReasons);
    }

    private static bool? TryBool(JsonObject obj, string key)
    {
        return obj.TryGetPropertyValue(key, out var v) && v != null ? v.GetValue<bool>() : null;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var v) || v is not JsonArray arr)
        {
            return Array.Empty<string>();
        }
        var list = new List<string>(arr.Count);
        foreach (var item in arr)
        {
            if (item != null)
            {
                list.Add(item.GetValue<string>());
            }
        }
        return list;
    }
}

/// <summary>
/// Per-codec availability entry.
/// </summary>
public sealed class CodecSupportEntry
{
    public bool Available { get; }
    /// <summary>Snake-case format this codec targets (e.g. <c>"jpeg"</c>).</summary>
    public string Format { get; }
    /// <summary>Role — <c>"encoder"</c> or <c>"decoder"</c>.</summary>
    public string Role { get; }
    public IReadOnlyList<string> Reasons { get; }

    internal CodecSupportEntry(bool available, string format, string role, IReadOnlyList<string> reasons)
    {
        Available = available;
        Format = format;
        Role = role;
        Reasons = reasons;
    }

    internal static CodecSupportEntry FromNode(JsonNode node)
    {
        var obj = node.AsObject();
        var available = obj.TryGetPropertyValue("available", out var a) && a != null ? a.GetValue<bool>() : false;
        var format = obj.TryGetPropertyValue("format", out var f) && f != null ? f.GetValue<string>() : string.Empty;
        var role = obj.TryGetPropertyValue("role", out var r) && r != null ? r.GetValue<string>() : string.Empty;
        var reasons = ReadStringArray(obj, "reasons");
        return new CodecSupportEntry(available, format, role, reasons);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var v) || v is not JsonArray arr)
        {
            return Array.Empty<string>();
        }
        var list = new List<string>(arr.Count);
        foreach (var item in arr)
        {
            if (item != null)
            {
                list.Add(item.GetValue<string>());
            }
        }
        return list;
    }
}

/// <summary>
/// Snapshot of formats and codecs whose compile-time, trusted-policy and
/// job-level gates all allow them. Keys are the snake_case wire names —
/// unknown values (new formats/codecs added in later imageflow releases)
/// are kept in string form so forward compatibility isn't lost.
/// </summary>
/// <remarks>
/// Instances are immutable and cheap to hand out to multiple callers. The
/// <see cref="ImageflowContext"/> caches the last-seen response and
/// invalidates only on <see cref="ImageflowContext.SetPolicy"/>.
/// </remarks>
public sealed class NetSupportResponse
{
    /// <summary>Per-format grid keyed by snake_case format name (e.g. <c>"jpeg"</c>).</summary>
    public IReadOnlyDictionary<string, FormatSupport> Formats { get; }

    /// <summary>Per-codec grid keyed by snake_case codec name (e.g. <c>"mozjpeg_encoder"</c>).</summary>
    public IReadOnlyDictionary<string, CodecSupportEntry> Codecs { get; }

    /// <summary>
    /// Formats / features reported under the <c>compile_ceiling</c> key of
    /// <c>v1/context/get_net_support</c>. <c>null</c> when the response came
    /// from <c>v1/context/set_policy</c> (which doesn't include it).
    /// </summary>
    public CompileCeilingInfo? CompileCeiling { get; }

    /// <summary><c>true</c> if a trusted policy has been set on the owning context.</summary>
    public bool TrustedPolicySet { get; }

    internal NetSupportResponse(
        IReadOnlyDictionary<string, FormatSupport> formats,
        IReadOnlyDictionary<string, CodecSupportEntry> codecs,
        CompileCeilingInfo? compileCeiling,
        bool trustedPolicySet)
    {
        Formats = formats;
        Codecs = codecs;
        CompileCeiling = compileCeiling;
        TrustedPolicySet = trustedPolicySet;
    }

    /// <summary>
    /// Convenience lookup by snake-case format name. Returns <c>null</c> for
    /// unknown formats; decode/encode callers should treat a missing entry
    /// the same as a denial.
    /// </summary>
    public FormatSupport? GetFormat(string snakeCaseName)
    {
        return Formats.TryGetValue(snakeCaseName, out var v) ? v : null;
    }

    /// <summary>Convenience lookup by snake-case codec name. Returns <c>null</c> for unknown codecs.</summary>
    public CodecSupportEntry? GetCodec(string snakeCaseName)
    {
        return Codecs.TryGetValue(snakeCaseName, out var v) ? v : null;
    }

    internal static NetSupportResponse ParseSetPolicyResponse(JsonNode responseData, bool locked)
    {
        // SetPolicyV1Response: { ok, locked, net_support: { formats, codecs } }
        var obj = responseData.AsObject();
        var netSupport = obj.TryGetPropertyValue("net_support", out var ns) && ns != null
            ? ns
            : throw new ImageflowAssertionFailed("set_policy response missing net_support");
        var (formats, codecs) = ParseGrid(netSupport);
        // After a successful set_policy call, the trusted policy is set.
        return new NetSupportResponse(formats, codecs, compileCeiling: null, trustedPolicySet: true);
    }

    internal static NetSupportResponse ParseGetNetSupportResponse(JsonNode responseData)
    {
        // GetNetSupportV1Response: { ok, net_support, trusted_policy_set, compile_ceiling }
        var obj = responseData.AsObject();
        var netSupport = obj.TryGetPropertyValue("net_support", out var ns) && ns != null
            ? ns
            : throw new ImageflowAssertionFailed("get_net_support response missing net_support");
        var (formats, codecs) = ParseGrid(netSupport);

        var trustedPolicySet = obj.TryGetPropertyValue("trusted_policy_set", out var tp) && tp != null
            && tp.GetValue<bool>();

        CompileCeilingInfo? compileCeiling = null;
        if (obj.TryGetPropertyValue("compile_ceiling", out var cc) && cc != null)
        {
            compileCeiling = CompileCeilingInfo.FromNode(cc);
        }

        return new NetSupportResponse(formats, codecs, compileCeiling, trustedPolicySet);
    }

    private static (IReadOnlyDictionary<string, FormatSupport>, IReadOnlyDictionary<string, CodecSupportEntry>) ParseGrid(JsonNode netSupport)
    {
        var obj = netSupport.AsObject();
        var formatsDict = new Dictionary<string, FormatSupport>(StringComparer.Ordinal);
        if (obj.TryGetPropertyValue("formats", out var formatsNode) && formatsNode is JsonObject formatsObj)
        {
            foreach (var kv in formatsObj)
            {
                if (kv.Value != null)
                {
                    formatsDict[kv.Key] = FormatSupport.FromNode(kv.Value);
                }
            }
        }

        var codecsDict = new Dictionary<string, CodecSupportEntry>(StringComparer.Ordinal);
        if (obj.TryGetPropertyValue("codecs", out var codecsNode) && codecsNode is JsonObject codecsObj)
        {
            foreach (var kv in codecsObj)
            {
                if (kv.Value != null)
                {
                    codecsDict[kv.Key] = CodecSupportEntry.FromNode(kv.Value);
                }
            }
        }

        return (formatsDict, codecsDict);
    }
}

/// <summary>
/// Ceiling imposed by the native build (feature gates + compile-time deny
/// arrays). Returned from <c>v1/context/get_net_support</c>.
/// </summary>
public sealed class CompileCeilingInfo
{
    public IReadOnlyList<string> DeniedDecode { get; }
    public IReadOnlyList<string> DeniedEncode { get; }
    public IReadOnlyList<string> FeaturesMissing { get; }

    internal CompileCeilingInfo(
        IReadOnlyList<string> deniedDecode,
        IReadOnlyList<string> deniedEncode,
        IReadOnlyList<string> featuresMissing)
    {
        DeniedDecode = deniedDecode;
        DeniedEncode = deniedEncode;
        FeaturesMissing = featuresMissing;
    }

    internal static CompileCeilingInfo FromNode(JsonNode node)
    {
        var obj = node.AsObject();
        return new CompileCeilingInfo(
            ReadStringArray(obj, "denied_decode"),
            ReadStringArray(obj, "denied_encode"),
            ReadStringArray(obj, "features_missing"));
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var v) || v is not JsonArray arr)
        {
            return Array.Empty<string>();
        }
        var list = new List<string>(arr.Count);
        foreach (var item in arr)
        {
            if (item != null)
            {
                list.Add(item.GetValue<string>());
            }
        }
        return list;
    }
}

/// <summary>
/// Response from <c>v1/context/set_policy</c> — the net-support grid plus
/// a <c>locked</c> flag.
/// </summary>
public sealed class SetPolicyResponse
{
    /// <summary><c>true</c> once a trusted policy has been set on the context.</summary>
    public bool Locked { get; }
    public NetSupportResponse NetSupport { get; }

    internal SetPolicyResponse(bool locked, NetSupportResponse netSupport)
    {
        Locked = locked;
        NetSupport = netSupport;
    }
}
