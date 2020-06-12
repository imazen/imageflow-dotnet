namespace Imageflow.Fluent
{
    /// <summary>
    /// An interface for encode presets. Concrete examples are GifEncoder, LibPngEncoder, PngQuantEncoder, LodePngEncoder, MozJpegEncoder, WebPLossyEncoder, WebPLosslessEncoder
    /// </summary>
    public interface IEncoderPreset
    {
        object ToImageflowDynamic();
    }
    
    public class GifEncoder : IEncoderPreset
    {
        public object ToImageflowDynamic() => new {gif = (string)null};
    } 
    /// <summary>
    /// Use LodePngEncoder instead
    /// </summary>
    public class LibPngEncoder : IEncoderPreset
    {
        public AnyColor? Matte { get; set; }
        public int? ZlibCompression { get; set; }
        public PngBitDepth? BitDepth { get; set; }
        public object ToImageflowDynamic() => new {libpng = new { depth = BitDepth?.ToString().ToLowerInvariant(), zlib_compression = ZlibCompression, matte = Matte?.ToImageflowDynamic()}};
    } 
    
    public class PngQuantEncoder : IEncoderPreset
    {
        /// <summary>
        /// Try to quantize the PNG first, falling back to lossless PNG if the minimumQuality value cannot be reached
        /// </summary>
        /// <param name="quality">The target visual quality</param>
        /// <param name="minimumQuality">The minimum visual quality below which to revert to lossless encoding</param>
        public PngQuantEncoder(int? quality, int? minimumQuality)
        {
            Quality = quality;
            MinimumQuality = minimumQuality;
        }
        public int? Quality { get; set; }
        
        public int? MinimumQuality { get; set; }
        
        public int? Speed { get; set; }
        /// <summary>
        /// When true, uses drastically more CPU time for a 1-2% reduction in file size
        /// </summary>
        public bool? MaximumDeflate { get; set; }
        public object ToImageflowDynamic() => new {pngquant = new
        {
            quality = Quality,
            minimum_quality = MinimumQuality,
            speed = Speed,
            maximum_deflate = MaximumDeflate
        }};
    } 


    public class LodePngEncoder : IEncoderPreset
    {
        /// <summary>
        /// When true, uses drastically more CPU time for a 1-2% reduction in file size
        /// </summary>
        public bool? MaximumDeflate { get; set; }
        public object ToImageflowDynamic() => new {lodepng = new { maximum_deflate = MaximumDeflate}};
    } 


    /// <summary>
    /// Deprecated. Use MozJpegEncoder instead
    /// </summary>
    public class LibJpegTurboEncoder : IEncoderPreset
    {
        public int? Quality { get; set; }
        public bool? Progressive { get; set; }
        public bool? OptimizeHuffmanCoding { get; set; }
        
        public object ToImageflowDynamic() => new {libjpegturbo = new { quality = Quality, progressive = Progressive, optimize_huffman_coding = OptimizeHuffmanCoding}};
    }

    public class MozJpegEncoder : IEncoderPreset
    {
        public MozJpegEncoder(int quality)
        {
            Quality = quality;
        }
        public MozJpegEncoder(int quality, bool progressive)
        {
            Quality = quality;
            Progressive = progressive;
        }
        public int? Quality { get; set; }
        public bool? Progressive { get; set; }
        
        public object ToImageflowDynamic() => new {mozjpeg = new { quality = Quality, progressive = Progressive}};
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
