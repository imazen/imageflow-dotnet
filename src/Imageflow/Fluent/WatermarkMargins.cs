namespace Imageflow.Fluent
{
    /// <summary>
    /// Defines a set of margins in terms of pixels inward from the edges of the canvas or image
    /// </summary>
    public class WatermarkMargins : IWatermarkConstraintBox
    {
        public WatermarkMargins()
        {
        }

        /// <summary>
        /// Apply margins in terms of pixels from the edge of the canvas or image
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public WatermarkMargins(WatermarkAlign relativeTo, uint left, uint top, uint right, uint bottom)
        {
            RelativeTo = relativeTo;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public WatermarkAlign RelativeTo { get; set; } = WatermarkAlign.Image;
        public uint Left { get; set; }
        public uint Top { get; set; }
        public uint Right { get; set; }
        public uint Bottom { get; set; }

        public WatermarkMargins SetRelativeTo(WatermarkAlign relativeTo)
        {
            RelativeTo = relativeTo;
            return this;
        }
        
        public WatermarkMargins SetLeft(uint pixels)
        {
            Left = pixels;
            return this;
        }
        public WatermarkMargins SetTop(uint pixels)
        {
            Top = pixels;
            return this;
        }
        public WatermarkMargins SetRight(uint pixels)
        {
            Right = pixels;
            return this;
        }
        public WatermarkMargins SetBottom(uint pixels)
        {
            Bottom = pixels;
            return this;
        }
        
        
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