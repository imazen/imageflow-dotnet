namespace Imageflow.Fluent
{
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

        internal object ToImageflowDynamic()
        {
            return new
            {
                max_decode_size = MaxDecodeSize?.ToImageflowDynamic(),
                max_frame_size = MaxFrameSize?.ToImageflowDynamic(),
                max_encode_size = MaxEncodeSize?.ToImageflowDynamic()
            };
        }
    }
}