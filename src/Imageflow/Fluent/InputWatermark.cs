using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    public class InputWatermark
    {
        public InputWatermark(IBytesSource source, int ioId, WatermarkOptions watermark)
        {
            Source = source;
            IoId = ioId;
            Watermark = watermark;
        }
        
        public InputWatermark(IBytesSource source, WatermarkOptions watermark)
        {
            Source = source;
            Watermark = watermark;
        }
        
        public IBytesSource Source { get; set; }
        public int? IoId { get; set; }
        public WatermarkOptions Watermark { get; set;  }

        [Obsolete("Use ToJsonNode() methods instead")]
        public object ToImageflowDynamic()
        {
            return Watermark.ToImageflowDynamic(IoId ?? throw new InvalidOperationException("InputWatermark.ToImageflowDynamic() cannot be called without an IoId value assigned"));
        }
        
        internal JsonNode ToJsonNode()
        {
            return Watermark.ToJsonNode(IoId ?? throw new InvalidOperationException("InputWatermark.ToJson() cannot be called without an IoId value assigned"));
        }

    }
}