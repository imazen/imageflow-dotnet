namespace Imageflow.Fluent
{
// ReSharper disable InconsistentNaming
    public enum PixelFormat {
        Bgra_32 = 4,
        Bgr_32 = 70,
//        Bgr_24 = 3,
        //    Gray_8 = 1,
    }
        
    public enum ResampleWhen{
        Size_Differs,
        Size_Differs_Or_Sharpening_Requested,
        Always
    }

    public enum SharpenWhen
    {
        Downscaling,
        Upscaling,
        Size_Differs,
        Always
    }
    // ReSharper enable InconsistentNaming

    public enum ScalingFloatspace {
        Srgb,
        Linear
    }
    public enum InterpolationFilter {
        Robidoux_Fast = 1,
        Robidoux = 2,
        Robidoux_Sharp = 3,
        Ginseng = 4,
        Ginseng_Sharp = 5,
        Lanczos = 6,
        Lanczos_Sharp = 7,
        Lanczos_2 = 8,
        Lanczos_2_Sharp = 9,
        Cubic = 11,
        Cubic_Sharp = 12,
        Catmull_Rom = 13,
        Mitchell = 14,
    
        Cubic_B_Spline = 15,
        Hermite = 16,
        Jinc = 17,
        Triangle = 22,
        Linear = 23,
        Box = 24,

        Fastest = 27,
    
        N_Cubic = 29,
        N_Cubic_Sharp = 30,
    }


    public enum ColorKind
    {
        Black,
        Transparent,
        Srgb,
    }

    public enum PngBitDepth {
        Png_32,
        Png_24,
    }

    public enum CompositingMode
    {
        Compose,
        Overwrite
    }

    public enum ColorFilterSrgb
    {
        Grayscale_Ntsc,
        Grayscale_Flat,
        Grayscale_Bt709,
        Grayscale_Ry,
        Sepia,
        Invert
    }

    public enum ConstraintMode
    {
        /// Distort the image to exactly the given dimensions.
        /// If only one dimension is specified, behaves like `fit`.
        Distort,
        /// Ensure the result fits within the provided dimensions. No up-scaling.
        Within,
        /// Fit the image within the dimensions, up-scaling if needed
        Fit,
        /// Ensure the image is larger than the given dimensions
        Larger_Than,
        /// Crop to desired aspect ratio if image is larger than requested, then downscale. Ignores smaller images.
        /// If only one dimension is specified, behaves like `within`.
        Within_Crop,
        /// Crop to desired aspect ratio, then downscale or upscale to fit.
        /// If only one dimension is specified, behaves like `fit`.
        Fit_Crop,
        /// Crop to desired aspect ratio, no up-scaling or downscaling. If only one dimension is specified, behaves like Fit.
        Aspect_Crop,
        /// Pad to desired aspect ratio if image is larger than requested, then downscale. Ignores smaller images.
        /// If only one dimension is specified, behaves like `within`
        Within_Pad,
        /// Pad to desired aspect ratio, then downscale or upscale to fit
        /// If only one dimension is specified, behaves like `fit`.
        Fit_Pad,
    }
    
    
    public enum WatermarkConstraintMode
    {
        /// Distort the image to exactly the given dimensions.
        /// If only one dimension is specified, behaves like `fit`.
        Distort,
        /// Ensure the result fits within the provided dimensions. No up-scaling.
        Within,
        /// Fit the image within the dimensions, up-scaling if needed
        Fit,
        /// Crop to desired aspect ratio if image is larger than requested, then downscale. Ignores smaller images.
        /// If only one dimension is specified, behaves like `within`.
        Within_Crop,
        /// Crop to desired aspect ratio, then downscale or upscale to fit.
        /// If only one dimension is specified, behaves like `fit`.
        Fit_Crop,
    }

    public enum WatermarkAlign
    {
        /// <summary>
        /// Aligns the watermark within the canvas box. This is only used when using a watermark in combination with a command string. Otherwise it behaves like ToImage.
        /// </summary>
        Canvas,
        /// <summary>
        /// Aligns the watermark within the image box
        /// </summary>
        Image
    }
    
    /// <summary>
    /// What quality level to use when downscaling the jpeg block-wise
    /// </summary>
    public enum DecoderDownscalingMode
    {
        /// <summary>
        /// Use the Imageflow default (usually highest quality)
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// Use the fastest method
        /// </summary>
        Fastest = 1,
        
        /// <summary>
        /// A slower (but more accurate) scaling method is employed; the DCT blocks are fully decoded, then a true re-sampling kernel is applied.
        /// </summary>
        SpatialLumaScaling = 2,
        /// <summary>
        /// Like SpatialLumaScaling, but gamma correction is applied before the re-sampling kernel, then removed afterwards.
        /// Has the effect of linear-light scaling
        /// </summary>
        GammaCorrectSpatialLumaScaling = 6,
        Best = 6,
    }

}
