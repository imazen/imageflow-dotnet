using Imageflow.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Fluent
{
    public class ConstraintResamplingHints
    {
        public SharpenWhen? SharpenWhen { get; set; }
        public ResampleWhen? ResampleWhen { get; set; }
        public ScalingFloatspace? InterpolationColorspace { get; set; }
        public InterpolationFilter? UpFilter { get; set; }
        public InterpolationFilter? DownFilter { get; set; }
        public float? SharpenPercent { get; set; }

        public ConstraintResamplingHints(float? sharpenPercent, SharpenWhen? sharpenWhen, ResampleWhen? resampleWhen, InterpolationFilter? downFilter, InterpolationFilter? upFilter, ScalingFloatspace? interpolationColorspace)
        {
            SharpenPercent = sharpenPercent;
            DownFilter = downFilter;
            UpFilter = upFilter;
            InterpolationColorspace = interpolationColorspace;
            ResampleWhen = resampleWhen;
            SharpenWhen = sharpenWhen;
        }

        public ConstraintResamplingHints()
        {

        }

        public ConstraintResamplingHints Sharpen(float? sharpenPercent, SharpenWhen? sharpenWhen)
        {
            SharpenPercent = sharpenPercent;
            SharpenWhen = sharpenWhen;
            return this;
        }

        public ConstraintResamplingHints ResampleFilter(InterpolationFilter? downFilter, InterpolationFilter? upFilter)
        {
            DownFilter = downFilter;
            UpFilter = upFilter;
            return this;
        }

        public ConstraintResamplingHints Resample(ResampleWhen? resampleWhen)
        {

            ResampleWhen = resampleWhen;
            return this;
        }

        public ConstraintResamplingHints ResampleColorspace( ScalingFloatspace? interpolationColorspace)
        {
            InterpolationColorspace = interpolationColorspace;
            return this;
        }

        public object ToImageflowDynamic()
        {
            return new
            {
                sharpen_percent = SharpenPercent,
                down_filter = DownFilter?.ToString(),
                up_filter = UpFilter?.ToString(),
                scaling_colorspace = InterpolationColorspace?.ToString().ToLowerInvariant(),
                resample_when = ResampleWhen?.ToString().ToLowerInvariant(),
                sharpen_when = SharpenWhen?.ToString().ToLowerInvariant()
            };
        }
    }

}