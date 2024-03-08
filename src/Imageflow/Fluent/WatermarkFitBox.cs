using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    /// <summary>
    /// Defines 
    /// </summary>
    public class WatermarkFitBox : IWatermarkConstraintBox
    {
        public WatermarkFitBox() { }

        public WatermarkFitBox(WatermarkAlign relativeTo, float x1, float y1, float x2, float y2)
        {
            RelativeTo = relativeTo;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
        public WatermarkAlign RelativeTo { get; set; } = WatermarkAlign.Image;

        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; } = 100;
        public float Y2 { get; set; } = 100;

        public WatermarkFitBox SetRelativeTo(WatermarkAlign relativeTo)
        {
            RelativeTo = relativeTo;
            return this;
        }

        public WatermarkFitBox SetTopLeft(float x1, float y1)
        {
            X1 = x1;
            Y1 = y1;
            return this;
        }
        public WatermarkFitBox SetBottomRight(float x2, float y2)
        {
            X2 = x2;
            Y2 = y2;
            return this;
        }
        [Obsolete("Use ToJsonNode() instead")]
        public object ToImageflowDynamic()
        {
            switch (RelativeTo)
            {
                case WatermarkAlign.Canvas:
                    return new
                    {
                        canvas_percentage = new
                        {
                            x1 = X1,
                            y1 = Y1,
                            x2 = X2,
                            y2 = Y2
                        }

                    };
                case WatermarkAlign.Image:
                    return new
                    {
                        image_percentage = new
                        {
                            x1 = X1,
                            y1 = Y1,
                            x2 = X2,
                            y2 = Y2
                        }

                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public JsonNode ToJsonNode()
        {
            var node = new JsonObject();
            switch (RelativeTo)
            {
                case WatermarkAlign.Canvas:
                    node.Add("canvas_percentage", new JsonObject
                    {
                        {"x1", X1},
                        {"y1", Y1},
                        {"x2", X2},
                        {"y2", Y2}
                    });
                    break;
                case WatermarkAlign.Image:
                    node.Add("image_percentage", new JsonObject
                    {
                        {"x1", X1},
                        {"y1", Y1},
                        {"x2", X2},
                        {"y2", Y2}
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return node;
        }
    }
}
