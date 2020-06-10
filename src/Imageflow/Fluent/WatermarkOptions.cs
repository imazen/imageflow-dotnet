namespace Imageflow.Fluent
{
    public class WatermarkOptions
    {
        public WatermarkOptions()
        {
            
        }
        
        
        public ConstraintGravity Gravity { get; set; }
        public IWatermarkConstraintBox FitBox { get; set; }
        public WatermarkConstraintMode? FitMode { get; set; }
        
        /// <summary>
        /// Range 0..1, where 1 is fully opaque and 0 is transparent.
        /// </summary>
        public float? Opacity { get; set; }
        public ResampleHints Hints { get; set; }

        public WatermarkOptions WithHints(ResampleHints hints)
        {
            Hints = hints;
            return this; 
        }

        /// <summary>
        /// Set the opacity to a value between 0 and 1
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public WatermarkOptions WithOpacity(float opacity)
        {
            Opacity = opacity;
            return this;
        }

        public WatermarkOptions WithMargins(WatermarkMargins margins)
        {
            FitBox = margins;
            return this;
        }
        
        public WatermarkOptions WithFitBox(WatermarkFitBox fitBox)
        {
            FitBox = fitBox;
            return this;
        }

        public WatermarkOptions WithFitMode(WatermarkConstraintMode fitMode)
        {
            FitMode = fitMode;
            return this;
        }

        public WatermarkOptions WithGravity(ConstraintGravity gravity)
        {
            Gravity = gravity;
            return this;
        }

        public WatermarkOptions LayoutWithFitBox(WatermarkFitBox fitBox, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = fitBox;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }
        
        public WatermarkOptions LayoutWithMargins(WatermarkMargins margins, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = margins;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }


        public object ToImageflowDynamic(int ioId)
        {
            return new
            {
                io_id = ioId,
                gravity = Gravity?.ToImageflowDynamic(),
                fit_box = FitBox?.ToImageflowDynamic(),
                fit_mode = FitMode?.ToString().ToLowerInvariant(),
                opacity = Opacity,
                hints = Hints?.ToImageflowDynamic()
            };
        }
    }
}
