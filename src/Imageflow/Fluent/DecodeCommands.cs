using System.Drawing;
using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{


    public class DecodeCommands
    {
        public Size? JpegDownscaleHint { get; set; }

        public DecoderDownscalingMode JpegDownscalingMode { get; set; } = DecoderDownscalingMode.Unspecified;

        [Obsolete("Use WebPDownscaleHint instead")]
        public Size? WebpDownscaleHint { get => WebPDownscaleHint; set => WebPDownscaleHint = value; }

        public Size? WebPDownscaleHint { get; set; }

        public bool DiscardColorProfile { get; set; }

        public bool IgnoreColorProfileErrors { get; set; }

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
            WebPDownscaleHint = new Size(targetWidthHint, targetHeightHint);
            return this;
        }

        public DecodeCommands SetDiscardColorProfile(bool value)
        {
            DiscardColorProfile = value;
            return this;
        }
        public DecodeCommands SetIgnoreColorProfileErrors(bool value)
        {
            IgnoreColorProfileErrors = value;
            return this;
        }

        public object[] ToImageflowDynamic()
        {
            object? downscale = JpegDownscaleHint.HasValue ? new
            {
                jpeg_downscale_hints = new
                {
                    width = JpegDownscaleHint.Value.Width,
                    height = JpegDownscaleHint.Value.Height,
                    scale_luma_spatially = JpegDownscalingMode == DecoderDownscalingMode.SpatialLumaScaling || JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling,
                    gamma_correct_for_srgb_during_spatial_luma_scaling = JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling
                }
            } : null;
            object? downscaleWebP = WebPDownscaleHint.HasValue
                ? new
                {
                    webp_decoder_hints = new
                    {
                        width = WebPDownscaleHint.Value.Width,
                        height = WebPDownscaleHint.Value.Height
                    }
                }
                : null;


            object? ignore = DiscardColorProfile ? new { discard_color_profile = (string?)null } : null;
            object? ignoreErrors = IgnoreColorProfileErrors ? new { ignore_color_profile_errors = (string?)null } : null;
            return new[] { downscale, ignore, ignoreErrors, downscaleWebP }.Where(obj => obj != null).Cast<object>().ToArray();
        }

        public JsonArray ToJsonNode()
        {
            var node = new JsonArray();
            if (JpegDownscaleHint.HasValue)
            {
                node.Add((JsonNode)new JsonObject
                {
                    {"jpeg_downscale_hints", new JsonObject
                    {
                        {"width", JpegDownscaleHint.Value.Width},
                        {"height", JpegDownscaleHint.Value.Height},
                        {"scale_luma_spatially", JpegDownscalingMode == DecoderDownscalingMode.SpatialLumaScaling || JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling},
                        {"gamma_correct_for_srgb_during_spatial_luma_scaling", JpegDownscalingMode == DecoderDownscalingMode.GammaCorrectSpatialLumaScaling}
                    }}
                });
            }

            if (WebPDownscaleHint.HasValue)
            {
                node.Add((JsonNode)new JsonObject
                {
                    {"webp_decoder_hints", new JsonObject
                    {
                        {"width", WebPDownscaleHint.Value.Width},
                        {"height", WebPDownscaleHint.Value.Height}
                    }}
                });
            }

            if (DiscardColorProfile)
            {
                node.Add((JsonNode)new JsonObject
                {
                    {"discard_color_profile", null}
                });
            }

            if (IgnoreColorProfileErrors)
            {
                node.Add((JsonNode)new JsonObject
                {
                    {"ignore_color_profile_errors", null}
                });
            }

            return node;
        }
    }
}
