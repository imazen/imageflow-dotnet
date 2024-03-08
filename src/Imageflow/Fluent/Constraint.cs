using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

public class Constraint
{
    public Constraint(uint? w, uint? h) : this(ConstraintMode.Within, w, h) { }
    public Constraint(ConstraintMode mode, uint? w, uint? h, ResampleHints hints, AnyColor? canvasColor)
    {
        Mode = mode;
        W = w;
        H = h;
        Hints = hints;
        CanvasColor = canvasColor;
        if (w == null && h == null)
        {
            throw new ArgumentNullException(nameof(w), "Either w or h must be non-null.");
        }
    }

    public Constraint(ConstraintMode mode, uint? w, uint? h)
    {
        Mode = mode;
        W = w;
        H = h;
        if (w == null && h == null)
        {
            throw new ArgumentNullException(nameof(w), "Either w or h must be non-null.");
        }
    }
    public ConstraintMode Mode { get; set; }
    public uint? W { get; set; }
    public uint? H { get; set; }
    public ResampleHints? Hints { get; set; }
    public AnyColor? CanvasColor { get; set; }

    public ConstraintGravity? Gravity { get; set; }

    public Constraint SetConstraintMode(ConstraintMode mode)
    {
        Mode = mode;
        return this;
    }

    public Constraint SetHints(ResampleHints hints)
    {
        Hints = hints;
        return this;
    }

    public Constraint SetCanvasColor(AnyColor? canvasColor)
    {
        CanvasColor = canvasColor;
        return this;
    }

    public Constraint SetGravity(ConstraintGravity gravity)
    {
        Gravity = gravity;

        return this;
    }

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic()
    {
        return new
        {
            mode = Mode.ToString().ToLowerInvariant(),
            w = W,
            h = H,
            hints = Hints?.ToImageflowDynamic(),
            canvas_color = CanvasColor?.ToImageflowDynamic(),
            gravity = Gravity?.ToImageflowDynamic()
        };
    }

    internal JsonNode ToJsonNode()
    {
        var node = new JsonObject { { "mode", Mode.ToString().ToLowerInvariant() } };
        if (W != null)
        {
            node.Add("w", W);
        }

        if (H != null)
        {
            node.Add("h", H);
        }

        if (Hints != null)
        {
            node.Add("hints", Hints.ToJsonNode());
        }

        if (CanvasColor != null)
        {
            node.Add("canvas_color", CanvasColor?.ToJsonNode());
        }

        if (Gravity != null)
        {
            node.Add("gravity", Gravity.ToJsonNode());
        }

        return node;
    }
}
