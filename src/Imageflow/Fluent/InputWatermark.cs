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

        public object ToImageflowDynamic()
        {
            return Watermark.ToImageflowDynamic(IoId.Value);
        }
    }
}