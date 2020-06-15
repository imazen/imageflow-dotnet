using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Fluent
{
    public class Constraint
    {
        public Constraint(uint? w, uint? h) : this(ConstraintMode.Within, w, h){}
        public Constraint(ConstraintMode mode, uint? w, uint? h, ResampleHints hints, AnyColor? canvasColor)
        {
            Mode = mode;
            W = w;
            H = h;
            Hints = hints;
            CanvasColor = canvasColor;
            if (w == null && h == null)
                throw new ArgumentNullException(nameof(w), "Either w or h must be non-null.");
        }

        public Constraint(ConstraintMode mode, uint? w, uint? h)
        {
            Mode = mode;
            W = w;
            H = h;
            if (w == null && h == null)
                throw new ArgumentNullException(nameof(w), "Either w or h must be non-null.");
        }
        public ConstraintMode Mode { get; set; }
        public uint? W { get; set; }
        public uint? H { get; set; }
        public ResampleHints Hints { get; set; }
        public AnyColor? CanvasColor { get; set; }

        public Constraint SetConstraintMode(ConstraintMode mode)
        {
            Mode = mode;
            return this;
        }
        
        public Constraint SetHints(ResampleHints hints)
        {
            this.Hints = hints;
            return this;
        }

        public Constraint SetCanvasColor(AnyColor? canvasColor)
        {
            CanvasColor = canvasColor;
            return this;
        }
        
        public object ToImageflowDynamic()
        {
            return new
            {
                mode = Mode.ToString()?.ToLowerInvariant(),
                w = W,
                h = H,
                hints = Hints?.ToImageflowDynamic(),
                canvas_color = CanvasColor?.ToImageflowDynamic()

            };
        }
    }
}
