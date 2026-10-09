using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

/// <summary>
/// Reason the dispatcher served a specific-codec <c>EncoderPreset</c> via
/// a different codec than the one the caller named.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::SubstitutionReason</c>. Wire form
/// on the <c>reason</c> field of <see cref="CodecSubstitutionAnnotation"/>
/// is snake_case — serde's <c>rename_all = "snake_case"</c> stringifies
/// each variant as its lowercase-underscore name. That is the form
/// <see cref="TryParse"/> accepts and <see cref="ToSnakeCaseWire"/> emits.
///
/// The dotted form returned by <see cref="ToDottedMessage"/> is the
/// structured error-body form (<c>codec_killbits.deny_encoders</c>)
/// useful for human-readable log lines.
///
/// The native enum is <c>#[non_exhaustive]</c>: future imageflow releases
/// may add reasons. Unknown snake_case strings round-trip through
/// <see cref="CodecSubstitutionAnnotation.ReasonRaw"/> rather than this
/// enum.
/// </remarks>
public enum SubstitutionReason
{
    /// <summary>The requested codec was denied by trusted/job <c>deny_encoders</c> / <c>deny_decoders</c>.</summary>
    CodecKillbitsDenyEncoders,
    /// <summary>The requested codec wasn't in the trusted/job <c>allow_encoders</c> / <c>allow_decoders</c> list.</summary>
    CodecKillbitsAllowEncodersExcludes,
    /// <summary>The build didn't compile in the requested codec (feature gate).</summary>
    CompileFeatureMissing,
    /// <summary>The build-time <c>COMPILE_DENY_*</c> list denies the format family.</summary>
    CompileCodecConstDenied,
    /// <summary>The codec isn't in the runtime <c>enabled_codecs</c> registry.</summary>
    NotRegistered,
}

/// <summary>
/// Build-time codec-priority flavor that selected the substitution
/// order when the dispatcher re-routed an encode.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::build_killbits::CodecPriority</c>. Wire
/// form is snake_case, transported as a free-form string on
/// <see cref="CodecSubstitutionAnnotation.CodecPriorityRaw"/> so older
/// or newer native builds can roundtrip unknown values without loss.
/// Upstream / V3 forks default to <c>v3_zen_first</c>; V2 forks ship
/// with <c>v2_classic_first</c>.
/// </remarks>
public enum CodecPriority
{
    /// <summary>V3 default — prefer pure-Rust zen codecs over legacy C backends.</summary>
    V3ZenFirst,
    /// <summary>V2 / legacy flavor — prefer the C backends that shipped in V2 forks.</summary>
    V2ClassicFirst,
}

internal static class SubstitutionReasonExtensions
{
    /// <summary>Wire form used in the <c>reason</c> field (serde <c>rename_all = "snake_case"</c>).</summary>
    public static string ToSnakeCaseWire(this SubstitutionReason reason) => reason switch
    {
        SubstitutionReason.CodecKillbitsDenyEncoders => "codec_killbits_deny_encoders",
        SubstitutionReason.CodecKillbitsAllowEncodersExcludes => "codec_killbits_allow_encoders_excludes",
        SubstitutionReason.CompileFeatureMissing => "compile_feature_missing",
        SubstitutionReason.CompileCodecConstDenied => "compile_codec_const_denied",
        SubstitutionReason.NotRegistered => "not_registered",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown SubstitutionReason"),
    };

    /// <summary>
    /// Dotted form used in structured error payloads and the
    /// <c>Describe()</c> log helper (e.g. <c>codec_killbits.deny_encoders</c>).
    /// Mirrors <c>SubstitutionReason::as_snake()</c> on the native side.
    /// </summary>
    public static string ToDottedMessage(this SubstitutionReason reason) => reason switch
    {
        SubstitutionReason.CodecKillbitsDenyEncoders => "codec_killbits.deny_encoders",
        SubstitutionReason.CodecKillbitsAllowEncodersExcludes => "codec_killbits.allow_encoders_excludes",
        SubstitutionReason.CompileFeatureMissing => "compile.feature_missing",
        SubstitutionReason.CompileCodecConstDenied => "compile.codec_const_denied",
        SubstitutionReason.NotRegistered => "not_registered",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown SubstitutionReason"),
    };

    public static bool TryParse(string? wire, out SubstitutionReason reason)
    {
        switch (wire)
        {
            case "codec_killbits_deny_encoders": reason = SubstitutionReason.CodecKillbitsDenyEncoders; return true;
            case "codec_killbits_allow_encoders_excludes": reason = SubstitutionReason.CodecKillbitsAllowEncodersExcludes; return true;
            case "compile_feature_missing": reason = SubstitutionReason.CompileFeatureMissing; return true;
            case "compile_codec_const_denied": reason = SubstitutionReason.CompileCodecConstDenied; return true;
            case "not_registered": reason = SubstitutionReason.NotRegistered; return true;
            default: reason = default; return false;
        }
    }
}

internal static class CodecPriorityExtensions
{
    public static string ToSnakeCaseWire(this CodecPriority priority) => priority switch
    {
        CodecPriority.V3ZenFirst => "v3_zen_first",
        CodecPriority.V2ClassicFirst => "v2_classic_first",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown CodecPriority"),
    };

    public static bool TryParse(string? wire, out CodecPriority priority)
    {
        switch (wire)
        {
            case "v3_zen_first": priority = CodecPriority.V3ZenFirst; return true;
            case "v2_classic_first": priority = CodecPriority.V2ClassicFirst; return true;
            default: priority = default; return false;
        }
    }
}

/// <summary>
/// Annotation attached to a single encoded image when the dispatcher
/// routed the caller's specific-codec <c>EncoderPreset</c> to a
/// different backend that emits the same wire format.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::CodecSubstitutionAnnotation</c>.
/// Surfaces on <see cref="BuildEncodeResult.Annotations"/> via the
/// <see cref="EncodeAnnotations"/> envelope. A substitution is never an
/// error — the bytes on <see cref="BuildEncodeResult.Destination"/> are
/// valid output for the advertised format. The annotation only reports
/// which backend produced them and why the requested one was skipped.
///
/// Unknown wire values for <c>reason</c> and <c>codec_priority</c> are
/// preserved on <see cref="ReasonRaw"/> / <see cref="CodecPriorityRaw"/>
/// so forward-compatibility holds when a newer native side introduces
/// a variant this client doesn't recognize.
/// </remarks>
public sealed class CodecSubstitutionAnnotation
{
    /// <summary>Wire name of the codec the <c>EncoderPreset</c> named (e.g. <c>mozjpeg_encoder</c>).</summary>
    public required NamedEncoderName Requested { get; init; }

    /// <summary>Wire name of the codec that actually produced output (e.g. <c>zen_jpeg_encoder</c>).</summary>
    public required NamedEncoderName Actual { get; init; }

    /// <summary>
    /// Parsed reason. <c>null</c> when the native side emits a reason
    /// string this client doesn't recognize — check
    /// <see cref="ReasonRaw"/> in that case.
    /// </summary>
    public required SubstitutionReason? Reason { get; init; }

    /// <summary>Raw wire form of the reason — always populated, preserves unknown values.</summary>
    public required string ReasonRaw { get; init; }

    /// <summary>
    /// Parsed codec priority. <c>null</c> when the native side emits a
    /// priority string this client doesn't recognize — check
    /// <see cref="CodecPriorityRaw"/> in that case.
    /// </summary>
    public required CodecPriority? CodecPriority { get; init; }

    /// <summary>Raw wire form of the codec priority — always populated, preserves unknown values.</summary>
    public required string CodecPriorityRaw { get; init; }

    /// <summary>
    /// Human-readable translation notes — one per preset field that was
    /// remapped onto the substitute codec's configuration
    /// (e.g. <c>"preset.quality → zen.quality"</c>). Empty list when
    /// the field was absent or empty on the wire.
    /// </summary>
    public required IReadOnlyList<string> FieldTranslations { get; init; }

    /// <summary>
    /// Field values from the request that were dropped because the
    /// substitute codec doesn't support them
    /// (e.g. <c>"preset.zlib_compression"</c> on the lodepng fallback path).
    /// Empty list when the field was absent or empty on the wire.
    /// </summary>
    public required IReadOnlyList<string> DroppedFields { get; init; }

    /// <summary>
    /// Serializes to the wire form imageflow emits. Mirrors
    /// <c>serde_json::to_value(CodecSubstitutionAnnotation)</c>:
    /// enum wire strings, empty <c>field_translations</c> /
    /// <c>dropped_fields</c> omitted.
    /// </summary>
    public JsonNode ToJsonNode()
    {
        var obj = new JsonObject
        {
            ["requested"] = Requested.ToSnakeCase(),
            ["actual"] = Actual.ToSnakeCase(),
            ["reason"] = ReasonRaw,
            ["codec_priority"] = CodecPriorityRaw,
        };
        if (FieldTranslations.Count > 0)
        {
            var arr = new JsonArray();
            foreach (var f in FieldTranslations)
            {
                arr.Add((JsonNode?)JsonValue.Create(f));
            }
            obj["field_translations"] = arr;
        }
        if (DroppedFields.Count > 0)
        {
            var arr = new JsonArray();
            foreach (var f in DroppedFields)
            {
                arr.Add((JsonNode?)JsonValue.Create(f));
            }
            obj["dropped_fields"] = arr;
        }
        return obj;
    }

    /// <summary>
    /// Parses a single <c>codec_substitution</c> node. Returns null if
    /// <paramref name="node"/> is null. Throws
    /// <see cref="ArgumentException"/> when required fields are
    /// missing or the encoder names don't parse.
    /// </summary>
    public static CodecSubstitutionAnnotation? FromJsonNode(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }
        var obj = node.AsObject();

        var requestedStr = ReadString(obj, "requested") ?? throw new ArgumentException("codec_substitution.requested missing");
        var actualStr = ReadString(obj, "actual") ?? throw new ArgumentException("codec_substitution.actual missing");
        var reasonStr = ReadString(obj, "reason") ?? throw new ArgumentException("codec_substitution.reason missing");

        if (!NamedEncoderExtensions.TryParse(requestedStr, out var requested))
        {
            throw new ArgumentException($"codec_substitution.requested '{requestedStr}' is not a recognized named encoder");
        }
        if (!NamedEncoderExtensions.TryParse(actualStr, out var actual))
        {
            throw new ArgumentException($"codec_substitution.actual '{actualStr}' is not a recognized named encoder");
        }

        // codec_priority is serde-defaulted on the native side; tolerate absence by falling
        // back to the V3 default wire value (matches `default_codec_priority_wire()`).
        var priorityStr = ReadString(obj, "codec_priority") ?? "v3_zen_first";

        var reasonMatched = SubstitutionReasonExtensions.TryParse(reasonStr, out var parsedReason);
        var priorityMatched = CodecPriorityExtensions.TryParse(priorityStr, out var parsedPriority);

        var fieldTranslations = ReadStringArray(obj, "field_translations");
        var droppedFields = ReadStringArray(obj, "dropped_fields");

        return new CodecSubstitutionAnnotation
        {
            Requested = requested,
            Actual = actual,
            Reason = reasonMatched ? parsedReason : null,
            ReasonRaw = reasonStr,
            CodecPriority = priorityMatched ? parsedPriority : null,
            CodecPriorityRaw = priorityStr,
            FieldTranslations = fieldTranslations,
            DroppedFields = droppedFields,
        };
    }

    /// <summary>
    /// One-line human-readable description suitable for log output:
    /// <c>"mozjpeg_encoder → mozjpeg_rs_encoder: codec_killbits.deny_encoders (v3_zen_first)"</c>.
    /// Uses the dotted reason form and the raw codec-priority wire so
    /// unknown values are preserved verbatim.
    /// </summary>
    public string Describe()
    {
        var reasonText = Reason.HasValue ? Reason.Value.ToDottedMessage() : ReasonRaw;
        return $"{Requested.ToSnakeCase()} \u2192 {Actual.ToSnakeCase()}: {reasonText} ({CodecPriorityRaw})";
    }

    private static string? ReadString(JsonObject obj, string key)
    {
        return obj.TryGetPropertyValue(key, out var v) && v != null ? v.GetValue<string>() : null;
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
/// Forward-extensible annotation envelope attached to one encoded image
/// in <see cref="BuildEncodeResult.Annotations"/>.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::EncodeAnnotations</c>. Each
/// field is optional; new annotation kinds can be added by the native
/// side without breaking older clients that ignore them. Callers
/// should treat unknown fields as safe to ignore.
/// </remarks>
public sealed class EncodeAnnotations
{
    /// <summary>
    /// Set iff the dispatcher substituted the requested codec with a
    /// different one that produces the same wire format.
    /// </summary>
    public CodecSubstitutionAnnotation? CodecSubstitution { get; init; }

    /// <summary><c>true</c> iff at least one annotation field is populated.</summary>
    public bool IsEmpty => CodecSubstitution == null;

    /// <summary>
    /// Serializes to the wire form imageflow emits: omits
    /// <c>codec_substitution</c> when null, producing <c>{}</c> for an
    /// empty envelope.
    /// </summary>
    public JsonNode ToJsonNode()
    {
        var obj = new JsonObject();
        if (CodecSubstitution != null)
        {
            obj["codec_substitution"] = CodecSubstitution.ToJsonNode();
        }
        return obj;
    }

    /// <summary>
    /// Parses an <c>annotations</c> node from an encode result.
    /// Returns null if <paramref name="node"/> is null. An empty
    /// object parses to a non-null envelope with <see cref="IsEmpty"/>
    /// <c>== true</c>.
    /// </summary>
    public static EncodeAnnotations? FromJsonNode(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }
        var obj = node.AsObject();
        CodecSubstitutionAnnotation? substitution = null;
        if (obj.TryGetPropertyValue("codec_substitution", out var substNode) && substNode != null)
        {
            substitution = CodecSubstitutionAnnotation.FromJsonNode(substNode);
        }
        return new EncodeAnnotations
        {
            CodecSubstitution = substitution,
        };
    }
}
