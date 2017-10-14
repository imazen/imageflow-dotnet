using System.Drawing;
using System.Linq;

namespace Imageflow.Fluent
{
    public enum DecderDownscalingMode
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
        /// A slower (but more accurate) scaling method is employed; the DCT blocks are fully decoded, then a true resampling kernel is applied.
        /// </summary>
        SpatialLumaScaling = 2,
        /// <summary>
        /// Like SpatialLumaScaling, but gamma correction is applied before the resampling kernel, then removed afterwards.
        /// Has the effect of linear-light scaling
        /// </summary>
        GammaCorrectSpatialLumaScaling = 6,
        Best = 6,
    }
    
    public class DecodeCommands
    {
        public Size? DownscaleHint { get; set; } = Size.Empty;

        public DecderDownscalingMode DownscalingMode { get; set; } = DecderDownscalingMode.Unspecified;

        public bool DiscardColorProfile { get; set; }

        public object[] ToImageflowDynamic()
        {
            object downscale = DownscaleHint.HasValue ? new { 
                jpeg_downscale_hints = new {
                    width = DownscaleHint.Value.Width,
                    height  = DownscaleHint.Value.Height,
                    scale_luma_spatially = DownscalingMode == DecderDownscalingMode.SpatialLumaScaling || DownscalingMode == DecderDownscalingMode.GammaCorrectSpatialLumaScaling,
                    gamma_correct_for_srgb_during_spatial_luma_scaling = DownscalingMode == DecderDownscalingMode.GammaCorrectSpatialLumaScaling
                } 
             }: null;
            object ignore = DiscardColorProfile ? new {discard_color_profile = (string) null} : null;
            return new [] {downscale, ignore}.Where(obj => obj != null).ToArray();
        }
    }
}
