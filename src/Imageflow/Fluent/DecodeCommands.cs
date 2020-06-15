using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Imageflow.Fluent
{

    
    public class DecodeCommands
    {
        public Size? JpegDownscaleHint { get; set; }

        public DecoderDownscalingMode JpegDownscalingMode { get; set; } = DecoderDownscalingMode.Unspecified;

        [Obsolete("Use WebPDownscaleHint instead")]
        public Size? WebpDownscaleHint { get => WebPDownscaleHint; set => WebPDownscaleHint = value; }
        
        public Size? WebPDownscaleHint { get; set; }

        public bool DiscardColorProfile { get; set; } = false;

        public DecodeCommands SetJpegDownscaling(int targetWidthHint,
            int targetHeightHint, DecoderDownscalingMode mode)
        {
            JpegDownscaleHint = new Size(targetWidthHint, targetHeightHint);
            JpegDownscalingMode = mode;
            return this;
        }

        public DecodeCommands SetWebPDownscaling(int targetWidthHint,
            int targetHeightHint)
        {
            this.WebPDownscaleHint = new Size(targetWidthHint, targetHeightHint);
            return this;
        }

        public DecodeCommands SetDiscardColorProfile(bool value)
        {
            DiscardColorProfile = value;
            return this;
        }

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
            object downscaleWebP = WebPDownscaleHint.HasValue
                ? new
                {
                    webp_decoder_hints = new
                    {
                        width = WebPDownscaleHint.Value.Width,
                        height = WebPDownscaleHint.Value.Height
                    }
                }
                : null;
            
                
            object ignore = DiscardColorProfile ? new {discard_color_profile = (string) null} : null;
            return new [] {downscale, ignore, downscaleWebP}.Where(obj => obj != null).ToArray();
        }
    }
}
