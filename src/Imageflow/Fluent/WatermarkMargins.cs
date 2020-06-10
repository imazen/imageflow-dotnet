using System;

namespace Imageflow.Fluent
{
    public class WatermarkMargins : IWatermarkConstraintBox
    {
        public WatermarkMargins(){}

        public WatermarkMargins(WatermarkAlign relativeTo, uint left, uint top, uint right, uint bottom)
        {
            RelativeTo = relativeTo;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        public WatermarkAlign RelativeTo { get; set; } = WatermarkAlign.Image;
        public uint Left { get; set; } = 0;
        public uint Top { get; set; } = 0;
        public uint Right { get; set; } = 0;
        public uint Bottom { get; set; } = 0;
        
        public object ToImageflowDynamic()
        {
            switch (RelativeTo)
            {
                case WatermarkAlign.Canvas:
                    return new
                    {
                        canvas_margins = new {
                            left = Left,
                            top = Top,
                            right = Right,
                            bottom = Bottom
                        }

                    };
                case WatermarkAlign.Image:
                    return new
                    {
                        image_margins = new {
                            left = Left,
                            top = Top,
                            right = Right,
                            bottom = Bottom
                        }

                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}