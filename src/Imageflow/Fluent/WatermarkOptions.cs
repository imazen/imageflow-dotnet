using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    public class WatermarkOptions
    {
        
        public ConstraintGravity? Gravity { get; set; }
        public IWatermarkConstraintBox? FitBox { get; set; }
        public WatermarkConstraintMode? FitMode { get; set; }
        
        /// <summary>
        /// Range 0..1, where 1 is fully opaque and 0 is transparent.
        /// </summary>
        public float? Opacity { get; set; }
        public ResampleHints? Hints { get; set; }
        
        public uint? MinCanvasWidth { get; set; }
        public uint? MinCanvasHeight { get; set; }

        [Obsolete("Use Set___ methods instead")]
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
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions WithOpacity(float opacity)
        {
            Opacity = opacity;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions WithMargins(WatermarkMargins margins)
        {
            FitBox = margins;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions WithFitBox(WatermarkFitBox fitBox)
        {
            FitBox = fitBox;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions WithFitMode(WatermarkConstraintMode fitMode)
        {
            FitMode = fitMode;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions WithGravity(ConstraintGravity gravity)
        {
            Gravity = gravity;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions LayoutWithFitBox(WatermarkFitBox fitBox, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = fitBox;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }
        [Obsolete("Use Set___ methods instead")]
        public WatermarkOptions LayoutWithMargins(WatermarkMargins margins, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = margins;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }

        /// <summary>
        /// Hide the watermark if the canvas is smaller in either dimension
        /// </summary>
        /// <param name="minWidth"></param>
        /// <param name="minHeight"></param>
        /// <returns></returns>
        [Obsolete("Use Set___ methods instead")] 
        public WatermarkOptions WithMinCanvasSize(uint? minWidth, uint? minHeight)
        {
            MinCanvasWidth = minWidth;
            MinCanvasHeight = minHeight;
            return this;
        }

         public WatermarkOptions SetHints(ResampleHints hints)
        {
            Hints = hints;
            return this; 
        }

        /// <summary>
        /// Set the opacity to a value between 0 and 1
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public WatermarkOptions SetOpacity(float opacity)
        {
            Opacity = opacity;
            return this;
        }

        public WatermarkOptions SetMargins(WatermarkMargins margins)
        {
            FitBox = margins;
            return this;
        }
        
        public WatermarkOptions SetFitBox(WatermarkFitBox fitBox)
        {
            FitBox = fitBox;
            return this;
        }

        public WatermarkOptions SetFitMode(WatermarkConstraintMode fitMode)
        {
            FitMode = fitMode;
            return this;
        }

        public WatermarkOptions SetGravity(ConstraintGravity gravity)
        {
            Gravity = gravity;
            return this;
        }

        public WatermarkOptions SetFitBoxLayout(WatermarkFitBox fitBox, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = fitBox;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }
        
        public WatermarkOptions SetMarginsLayout(WatermarkMargins margins, WatermarkConstraintMode fitMode,
            ConstraintGravity gravity)
        {
            FitBox = margins;
            FitMode = fitMode;
            Gravity = gravity;
            return this;
        }

        /// <summary>
        /// Hide the watermark if the canvas is smaller in either dimension
        /// </summary>
        /// <param name="minWidth"></param>
        /// <param name="minHeight"></param>
        /// <returns></returns>
        public WatermarkOptions SetMinCanvasSize(uint? minWidth, uint? minHeight)
        {
            MinCanvasWidth = minWidth;
            MinCanvasHeight = minHeight;
            return this;
        }

        [Obsolete("Use ToJsonNode() instead")]
        public object ToImageflowDynamic(int? ioId)
        {
            return new
            {
                io_id = ioId,
                gravity = Gravity?.ToImageflowDynamic(),
                fit_box = FitBox?.ToImageflowDynamic(),
                fit_mode = FitMode?.ToString().ToLowerInvariant(),
                min_canvas_width = MinCanvasWidth,
                min_canvas_height = MinCanvasWidth,
                opacity = Opacity,
                hints = Hints?.ToImageflowDynamic()
            };
        }

        internal JsonNode ToJsonNode(int? ioId)
        {
            var node = new JsonObject { { "io_id", ioId } };
            if (Gravity != null) node.Add("gravity", Gravity.ToJsonNode());
            if (FitBox != null) node.Add("fit_box", FitBox.ToJsonNode());
            if (FitMode != null) node.Add("fit_mode", FitMode?.ToString().ToLowerInvariant());
            if (MinCanvasWidth != null) node.Add("min_canvas_width", MinCanvasWidth);
            if (MinCanvasHeight != null) node.Add("min_canvas_height", MinCanvasHeight);
            if (Opacity != null) node.Add("opacity", Opacity);
            if (Hints != null) node.Add("hints", Hints.ToJsonNode());
            return node;
        }
    }
}
