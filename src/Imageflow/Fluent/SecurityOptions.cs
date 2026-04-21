using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

/// <summary>
/// Scalar decode/encode resource limits plus the three-layer codec killbits.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::ExecutionSecurity</c>.
///
/// The same class is used for two things:
///
/// <list type="bullet">
///   <item><description>Trusted-context policy, set once via
///     <see cref="ImageflowContext.SetPolicy"/>. Any allow-list or table-with-<c>true</c>
///     form is permitted here.</description></item>
///   <item><description>Per-job security attached through the fluent API (existing
///     behavior). Only deny-style narrowing is legal at this layer;
///     <see cref="ValidateJobLevel"/> enforces it before the request
///     crosses the ABI boundary.</description></item>
/// </list>
/// </remarks>
public class SecurityOptions
{

    public FrameSizeLimit? MaxDecodeSize { get; set; }

    public FrameSizeLimit? MaxFrameSize { get; set; }

    public FrameSizeLimit? MaxEncodeSize { get; set; }

    /// <summary>
    /// Maximum bytes for a single codec input stream. Default on the
    /// native side: 256 MiB. <c>null</c> = no change at this layer.
    /// </summary>
    public ulong? MaxInputFileBytes { get; set; }

    /// <summary>
    /// Maximum bytes for a JSON payload before deserialization. Default
    /// on the native side: 64 MiB. <c>null</c> = no change at this layer.
    /// </summary>
    public ulong? MaxJsonBytes { get; set; }

    /// <summary>
    /// Maximum decoded pixels summed across every frame in a file. Default
    /// on the native side: 400 megapixels. <c>null</c> = no change at this
    /// layer.
    /// </summary>
    public ulong? MaxTotalFilePixels { get; set; }

    /// <summary>Per-format decode/encode killbits.</summary>
    public FormatKillbits? Formats { get; set; }

    /// <summary>Per-codec decode/encode killbits.</summary>
    public CodecKillbits? Codecs { get; set; }

    public SecurityOptions SetMaxDecodeSize(FrameSizeLimit? limit)
    {
        MaxDecodeSize = limit;
        return this;
    }
    public SecurityOptions SetMaxFrameSize(FrameSizeLimit? limit)
    {
        MaxFrameSize = limit;
        return this;
    }
    public SecurityOptions SetMaxEncodeSize(FrameSizeLimit? limit)
    {
        MaxEncodeSize = limit;
        return this;
    }

    public SecurityOptions SetMaxInputFileBytes(ulong? bytes)
    {
        MaxInputFileBytes = bytes;
        return this;
    }

    public SecurityOptions SetMaxJsonBytes(ulong? bytes)
    {
        MaxJsonBytes = bytes;
        return this;
    }

    public SecurityOptions SetMaxTotalFilePixels(ulong? pixels)
    {
        MaxTotalFilePixels = pixels;
        return this;
    }

    public SecurityOptions SetFormatKillbits(FormatKillbits? killbits)
    {
        Formats = killbits;
        return this;
    }

    public SecurityOptions SetCodecKillbits(CodecKillbits? killbits)
    {
        Codecs = killbits;
        return this;
    }

    /// <summary>
    /// Run the same mutual-exclusion checks the native side applies at
    /// <c>v1/context/set_policy</c> deserialize time.
    /// </summary>
    public void Validate()
    {
        Formats?.Validate();
        Codecs?.Validate();
    }

    /// <summary>
    /// Stricter check for job-level use: no allow-lists anywhere, no
    /// table-with-true entries. The native side rejects these when found
    /// under <c>Build001.security</c> / <c>Execute001.security</c>, so
    /// calling this before sending lets the caller fail fast with a clear
    /// message instead of a generic deserialization error.
    /// </summary>
    public void ValidateJobLevel()
    {
        Formats?.ValidateJobLevel();
        Codecs?.ValidateJobLevel();
    }

    [Obsolete("Use ToJsonNode() instead")]
    internal object ToImageflowDynamic()
    {
        return new
        {
            max_decode_size = MaxDecodeSize?.ToImageflowDynamic(),
            max_frame_size = MaxFrameSize?.ToImageflowDynamic(),
            max_encode_size = MaxEncodeSize?.ToImageflowDynamic()
        };
    }

    internal JsonNode ToJsonNode()
    {
        var node = new JsonObject();
        if (MaxDecodeSize != null)
        {
            node.Add("max_decode_size", MaxDecodeSize?.ToJsonNode());
        }

        if (MaxFrameSize != null)
        {
            node.Add("max_frame_size", MaxFrameSize?.ToJsonNode());
        }

        if (MaxEncodeSize != null)
        {
            node.Add("max_encode_size", MaxEncodeSize?.ToJsonNode());
        }

        if (MaxInputFileBytes != null)
        {
            node.Add("max_input_file_bytes", MaxInputFileBytes.Value);
        }

        if (MaxJsonBytes != null)
        {
            node.Add("max_json_bytes", MaxJsonBytes.Value);
        }

        if (MaxTotalFilePixels != null)
        {
            node.Add("max_total_file_pixels", MaxTotalFilePixels.Value);
        }

        if (Formats != null)
        {
            node.Add("formats", Formats.ToJsonNode());
        }

        if (Codecs != null)
        {
            node.Add("codecs", Codecs.ToJsonNode());
        }

        return node;
    }
}
