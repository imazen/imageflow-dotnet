using System.Drawing;
using System.Linq;

namespace Imageflow.Fluent
{

    
    public class DecodeCommands
    {
        public Size? JpegDownscaleHint { get; set; }

        public DecoderDownscalingMode JpegDownscalingMode { get; set; } = DecoderDownscalingMode.Unspecified;

        public Size? WebpDownscaleHint { get; set; }
        
        public bool DiscardColorProfile { get; set; }

        public object[] ToImageflowDynamic()
        {
            object downscale = JpegDownscaleHint.HasValue ? new { 
                jpeg_downscale_hints = new {
                    width = JpegDownscaleHint.Value.Width,
                    height  = JpegDownscaleHint.Value.Height,
                    scale_luma_spatially = JpegDownscalingMode == DecoderDownscalingMode.SpatialLumaScaling || JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling,
                    gamma_correct_for_srgb_during_spatial_luma_scaling = JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling
                } 
             }: null;
            object downscaleWebp = WebpDownscaleHint.HasValue
                ? new
                {
                    webp_decoder_hints = new
                    {
                        width = WebpDownscaleHint.Value.Width,
                        height = WebpDownscaleHint.Value.Height
                    }
                }
                : null;
            
                
            object ignore = DiscardColorProfile ? new {discard_color_profile = (string) null} : null;
            return new [] {downscale, ignore, downscaleWebp}.Where(obj => obj != null).ToArray();
        }
    }
}
