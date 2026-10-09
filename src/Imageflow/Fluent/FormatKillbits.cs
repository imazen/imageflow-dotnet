using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

/// <summary>
/// Per-format decode/encode permissions used inside
/// <see cref="FormatKillbits"/>'s table form.
/// </summary>
public readonly struct FormatPermissions(bool decode, bool encode)
{
    public bool Decode { get; } = decode;
    public bool Encode { get; } = encode;

    internal JsonNode ToJsonNode()
    {
        return new JsonObject
        {
            ["decode"] = Decode,
            ["encode"] = Encode,
        };
    }
}

/// <summary>
/// Fine-grained per-format decode/encode gating. One of the three layers
/// of the killbits system (build-time ceiling ⋂ trusted policy ⋂ job-level
/// request).
/// </summary>
/// <remarks>
/// Three mutually exclusive request shapes — exactly one group may be set
/// at a time:
///
/// <list type="bullet">
///   <item><description><see cref="AllowDecode"/> / <see cref="AllowEncode"/> — the listed formats
///     are the only ones permitted. Anything else is denied. Trusted-policy
///     layer only; the native side rejects allow-lists submitted at job level.</description></item>
///   <item><description><see cref="DenyDecode"/> / <see cref="DenyEncode"/> — deny the listed
///     formats, carry everything else over from the layer above. Safe at
///     any layer.</description></item>
///   <item><description><see cref="Formats"/> — explicit
///     <c>{format → {decode, encode}}</c> table. Trusted-policy layer only
///     for entries setting <c>true</c>; at job level every entry must be
///     <c>false</c>.</description></item>
/// </list>
///
/// <see cref="Validate"/> / <see cref="ValidateJobLevel"/> run the same
/// mutual-exclusion checks the native side performs at deserialize time.
/// Serialization also rejects mixed forms before crossing the ABI boundary.
/// </remarks>
public sealed class FormatKillbits
{
    /// <summary>Allow only these formats for decode; everything else is denied. Mutually exclusive with <see cref="DenyDecode"/> and <see cref="Formats"/>.</summary>
    public IReadOnlyList<ImageFormat>? AllowDecode { get; set; }
    /// <summary>Deny these formats for decode. Mutually exclusive with <see cref="AllowDecode"/>.</summary>
    public IReadOnlyList<ImageFormat>? DenyDecode { get; set; }
    /// <summary>Allow only these formats for encode. Mutually exclusive with <see cref="DenyEncode"/> and <see cref="Formats"/>.</summary>
    public IReadOnlyList<ImageFormat>? AllowEncode { get; set; }
    /// <summary>Deny these formats for encode. Mutually exclusive with <see cref="AllowEncode"/>.</summary>
    public IReadOnlyList<ImageFormat>? DenyEncode { get; set; }
    /// <summary>Per-format table form. Mutually exclusive with any of the list forms.</summary>
    public IReadOnlyDictionary<ImageFormat, FormatPermissions>? Formats { get; set; }

    /// <summary>
    /// Mutual-exclusion validation. Mirrors
    /// <c>FormatKillbits::validate</c> on the native side.
    /// </summary>
    /// <exception cref="ArgumentException">when invariants are violated.</exception>
    public void Validate()
    {
        if (AllowDecode != null && DenyDecode != null)
        {
            throw new ArgumentException("pick allow or deny for decode, not both", nameof(AllowDecode));
        }
        if (AllowEncode != null && DenyEncode != null)
        {
            throw new ArgumentException("pick allow or deny for encode, not both", nameof(AllowEncode));
        }
        var hasList = AllowDecode != null || DenyDecode != null || AllowEncode != null || DenyEncode != null;
        if (hasList && Formats != null)
        {
            throw new ArgumentException("pick a single form (allow/deny lists OR formats table)", nameof(Formats));
        }
    }

    /// <summary>
    /// Layer-3 check: no allow-lists, and every table entry must be all
    /// <c>false</c>. Job-level security may only narrow. Mirrors
    /// <c>FormatKillbits::validate_job_level</c>.
    /// </summary>
    public void ValidateJobLevel()
    {
        Validate();
        if (AllowDecode != null || AllowEncode != null)
        {
            throw new ArgumentException(
                "job-level security may only deny, never allow (layer 3 narrows only)");
        }
        if (Formats != null)
        {
            foreach (var entry in Formats)
            {
                if (entry.Value.Decode || entry.Value.Encode)
                {
                    throw new ArgumentException(
                        "job-level security may only deny, never allow (layer 3 narrows only)");
                }
            }
        }
    }

    internal JsonNode ToJsonNode()
    {
        Validate();
        var node = new JsonObject();
        if (AllowDecode != null)
        {
            node["allow_decode"] = ToJsonArray(AllowDecode);
        }
        if (DenyDecode != null)
        {
            node["deny_decode"] = ToJsonArray(DenyDecode);
        }
        if (AllowEncode != null)
        {
            node["allow_encode"] = ToJsonArray(AllowEncode);
        }
        if (DenyEncode != null)
        {
            node["deny_encode"] = ToJsonArray(DenyEncode);
        }
        if (Formats != null)
        {
            var table = new JsonObject();
            foreach (var kv in Formats)
            {
                table[kv.Key.ToSnakeCase()] = kv.Value.ToJsonNode();
            }
            node["formats"] = table;
        }
        return node;
    }

    private static JsonArray ToJsonArray(IReadOnlyList<ImageFormat> formats)
    {
        var arr = new JsonArray();
        foreach (var f in formats)
        {
            arr.Add((JsonNode?)JsonValue.Create(f.ToSnakeCase()));
        }
        return arr;
    }
}

/// <summary>
/// Per-codec decode/encode gating. Complements <see cref="FormatKillbits"/>
/// by letting operators allow or deny specific named encoder/decoder
/// backends within a format (e.g. forbid <c>mozjpeg_encoder</c> while keeping
/// <c>zen_jpeg_encoder</c>).
/// </summary>
/// <remarks>
/// Mutual-exclusion rules:
///
/// <list type="bullet">
///   <item><description><see cref="AllowEncoders"/> is mutually exclusive with <see cref="DenyEncoders"/>.</description></item>
///   <item><description><see cref="AllowDecoders"/> is mutually exclusive with <see cref="DenyDecoders"/>.</description></item>
///   <item><description>Job-level security may only set the deny forms.</description></item>
/// </list>
/// </remarks>
public sealed class CodecKillbits
{
    public IReadOnlyList<NamedEncoderName>? AllowEncoders { get; set; }
    public IReadOnlyList<NamedEncoderName>? DenyEncoders { get; set; }
    public IReadOnlyList<NamedDecoderName>? AllowDecoders { get; set; }
    public IReadOnlyList<NamedDecoderName>? DenyDecoders { get; set; }

    public void Validate()
    {
        if (AllowEncoders != null && DenyEncoders != null)
        {
            throw new ArgumentException("pick allow or deny for encoders, not both", nameof(AllowEncoders));
        }
        if (AllowDecoders != null && DenyDecoders != null)
        {
            throw new ArgumentException("pick allow or deny for decoders, not both", nameof(AllowDecoders));
        }
    }

    public void ValidateJobLevel()
    {
        Validate();
        if (AllowEncoders != null || AllowDecoders != null)
        {
            throw new ArgumentException(
                "job-level security may only deny, never allow (layer 3 narrows only)");
        }
    }

    internal JsonNode ToJsonNode()
    {
        Validate();
        var node = new JsonObject();
        if (AllowEncoders != null)
        {
            node["allow_encoders"] = ToJsonArray(AllowEncoders, e => e.ToSnakeCase());
        }
        if (DenyEncoders != null)
        {
            node["deny_encoders"] = ToJsonArray(DenyEncoders, e => e.ToSnakeCase());
        }
        if (AllowDecoders != null)
        {
            node["allow_decoders"] = ToJsonArray(AllowDecoders, d => d.ToSnakeCase());
        }
        if (DenyDecoders != null)
        {
            node["deny_decoders"] = ToJsonArray(DenyDecoders, d => d.ToSnakeCase());
        }
        return node;
    }

    private static JsonArray ToJsonArray<T>(IReadOnlyList<T> items, Func<T, string> toSnake)
    {
        var arr = new JsonArray();
        foreach (var item in items)
        {
            arr.Add((JsonNode?)JsonValue.Create(toSnake(item)));
        }
        return arr;
    }
}
