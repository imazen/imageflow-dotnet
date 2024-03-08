using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

public class SecurityOptions
{

    public FrameSizeLimit? MaxDecodeSize { get; set; }

    public FrameSizeLimit? MaxFrameSize { get; set; }

    public FrameSizeLimit? MaxEncodeSize { get; set; }

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

        return node;
    }
}
