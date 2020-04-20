namespace Imageflow.Fluent
{
    public interface IEncoderPreset
    {
        object ToImageflowDynamic();
    }
    
    public class GifEncoder : IEncoderPreset
    {
        public object ToImageflowDynamic() => new {gif = (string)null};
    } 
    public class LibPngEncoder : IEncoderPreset
    {
        public AnyColor? Matte { get; set; }
        public int? ZlibCompression { get; set; }
        public PngBitDepth? BitDepth { get; set; }
        public object ToImageflowDynamic() => new {libpng = new { depth = BitDepth?.ToString().ToLowerInvariant(), zlib_compression = ZlibCompression, matte = Matte?.ToImageflowDynamic()}};
    } 

    public class LibJpegTurboEncoder : IEncoderPreset
    {
        public int? Quality { get; set; }
        public bool? Progressive { get; set; }
        public bool? OptimizeHuffmanCoding { get; set; }
        
        public object ToImageflowDynamic() => new {libjpegturbo = new { quality = Quality, progressive = Progressive, optimize_huffman_coding = OptimizeHuffmanCoding}};
    }

    public class WebPLossyEncoder : IEncoderPreset
    {
        public WebPLossyEncoder(float quality)
        {
            Quality = quality;
        }
        public float? Quality { get; set; }

        public object ToImageflowDynamic() => new { webplossy = new { quality = Quality } };
    }
    public class WebPLosslessEncoder : IEncoderPreset
    {
        public object ToImageflowDynamic() => new { webplossless = (string)null };
    }
}
