namespace Imageflow.Fluent
{
    /// <summary>
    /// An interface for encode presets. Concrete examples are GifEncoder, PngQuantEncoder, LodePngEncoder, MozJpegEncoder, WebPLossyEncoder, WebPLosslessEncoder
    /// </summary>
    public interface IEncoderPreset
    {
        object ToImageflowDynamic();
    }
    
    
    /// <summary>
    /// Encodes the image as a .gif
    /// </summary>
    public class GifEncoder : IEncoderPreset
    {
        public object ToImageflowDynamic() => new {gif = (string?)null};
    } 
    /// <summary>
    /// Use LodePngEncoder instead
    /// </summary>
    [Obsolete("Use PngQuantEncoder or LodePngEncoder instead")]
    public class LibPngEncoder : IEncoderPreset
    {
        public AnyColor? Matte { get; set; }
        public int? ZlibCompression { get; set; }
        public PngBitDepth? BitDepth { get; set; }
        public object ToImageflowDynamic() => new {libpng = new { depth = BitDepth?.ToString().ToLowerInvariant(), zlib_compression = ZlibCompression, matte = Matte?.ToImageflowDynamic()}};
    } 
    
    public class PngQuantEncoder : IEncoderPreset
    {
        public PngQuantEncoder(): this(null,null){}
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
        /// <summary>
        /// (0..100) The target visual quality. Try to quantize the PNG first, falling back to lossless PNG if the MinimumQuality value cannot be reached
        /// </summary>
        public int? Quality { get; set; }

       
        /// <summary>
        /// (0..100) The minimum visual quality below which to revert to lossless encoding
        /// </summary>
        public int? MinimumQuality { get; set; }


        /// <summary>
        /// speed: 1..10 controls the speed/quality trade-off for encoding.
        /// </summary>
        public int? Speed { get; set; }
        /// <summary>
        /// When true, uses drastically more CPU time for a 1-2% reduction in file size
        /// </summary>
        public bool? MaximumDeflate { get; set; }


        /// <summary>
        /// (0..100) The target visual quality. Try to quantize the PNG first, falling back to lossless PNG if the MinimumQuality value cannot be reached
        /// </summary>
        /// <param name="quality"></param>
        /// <returns></returns>
        public PngQuantEncoder SetQuality(int? quality)
        {
            Quality = quality;
            return this; 
        }
        /// <summary>
        /// (0..100) The minimum visual quality below which to revert to lossless encoding
        /// </summary>
        /// <param name="minimumQuality"></param>
        /// <returns></returns>
        public PngQuantEncoder SetMinimumQuality(int? minimumQuality)
        {
            MinimumQuality = minimumQuality;
            return this;
        }
        
        /// <summary>
        /// speed: 1..10 controls the speed/quality trade-off for encoding.
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public PngQuantEncoder SetSpeed(int? speed)
        {
            Speed = speed;
            return this;
        }
        
        /// <summary>
        /// Not suggested; only saves 1-2% on file size but takes 10x CPU time.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PngQuantEncoder SetMaximumDeflate(bool value)
        {
            MaximumDeflate = value;
            return this;
        }
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

        /// <summary>
        /// Not suggested; only saves 1-2% on file size but takes 10x CPU time.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LodePngEncoder SetMaximumDeflate(bool value)
        {
            MaximumDeflate = value;
            return this;
        }
        public object ToImageflowDynamic() => new {lodepng = new { maximum_deflate = MaximumDeflate}};
    } 


    /// <summary>
    /// Deprecated. Use MozJpegEncoder instead
    /// </summary>
    [Obsolete("Use MozJpegEncoder instead for smaller files")]
    public class LibJpegTurboEncoder : IEncoderPreset
    {
        public int? Quality { get; set; }
        public bool? Progressive { get; set; }
        public bool? OptimizeHuffmanCoding { get; set; }
        
        public AnyColor? Matte { get; set; }
        
        public object ToImageflowDynamic() => new {libjpegturbo = new { quality = Quality, progressive = Progressive, optimize_huffman_coding = OptimizeHuffmanCoding, matte = Matte?.ToImageflowDynamic()}};
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

        public AnyColor? Matte { get; set; }
        public MozJpegEncoder SetProgressive(bool progressive)
        {
            Progressive = progressive;
            return this;
        }
        
        public object ToImageflowDynamic() => new {mozjpeg = new { quality = Quality, progressive = Progressive, matte = Matte?.ToImageflowDynamic()}};
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
        public object ToImageflowDynamic() => new { webplossless = (string?)null };
    }
}
