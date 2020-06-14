using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Fluent
{
    public class Constraint
    {
        public Constraint(ConstraintMode mode, uint? w, uint? h, ResampleHints hints, AnyColor? canvasColor)
        {
            Mode = mode;
            W = w;
            H = h;
            Hints = hints;
            CanvasColor = canvasColor;
        }

        public Constraint(ConstraintMode mode, uint? w, uint? h)
        {
            Mode = mode;
            W = w;
            H = h;
        }
        public ConstraintMode Mode { get; set; }
        public uint? W { get; set; }
        public uint? H { get; set; }
        public ResampleHints Hints { get; set; }
        public AnyColor? CanvasColor { get; set; }

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
