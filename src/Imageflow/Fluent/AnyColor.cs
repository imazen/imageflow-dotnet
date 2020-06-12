using Imageflow.Bindings;

namespace Imageflow.Fluent
{
    /// <summary>
    /// Represents a color with or without transparency.
    /// </summary>
    public struct AnyColor
    {
        private ColorKind kind;
        private SrgbColor srgb;
        public static AnyColor Black => new AnyColor {kind = ColorKind.Black};
        public static AnyColor Transparent => new AnyColor {kind = ColorKind.Transparent};
        /// <summary>
        /// Parses color in RGB, RGBA, RRGGBB or RRGGBBAA format
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static AnyColor FromHexSrgb(string hex) => new AnyColor {kind = ColorKind.Srgb, srgb = SrgbColor.FromHex(hex)};
        public static AnyColor Srgb(SrgbColor c) => new AnyColor {kind = ColorKind.Srgb, srgb = c};

        public object ToImageflowDynamic()
        {
            switch (kind)
            {
                case ColorKind.Black: return new {black = (string)null};
                case ColorKind.Transparent: return new {transparent = (string)null };
                case ColorKind.Srgb: return new {srgb = new { hex = srgb.ToHexUnprefixed()}};
                default: throw new ImageflowAssertionFailed("default");
            }
        }
    }
}