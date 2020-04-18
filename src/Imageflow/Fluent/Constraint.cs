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
        ConstraintMode Mode { get; set; } = ConstraintMode.Within;
        uint? W { get; set; }
        uint? H { get; set; }
        ResampleHints Hints { get; set; }
        AnyColor? CanvasColor { get; set; }

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
