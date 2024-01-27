using Imageflow.Bindings;

namespace Imageflow.Fluent
{
    /// <summary>
    /// Represents a color with or without transparency.
    /// </summary>
    public struct AnyColor
    {
        private ColorKind _kind;
        private SrgbColor _srgb;
        public static AnyColor Black => new AnyColor {_kind = ColorKind.Black};
        public static AnyColor Transparent => new AnyColor {_kind = ColorKind.Transparent};
        /// <summary>
        /// Parses color in RGB, RGBA, RRGGBB or RRGGBBAA format
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static AnyColor FromHexSrgb(string hex) => new AnyColor {_kind = ColorKind.Srgb, _srgb = SrgbColor.FromHex(hex)};
        public static AnyColor Srgb(SrgbColor c) => new AnyColor {_kind = ColorKind.Srgb, _srgb = c};

        public object ToImageflowDynamic()
        {
            switch (_kind)
            {
                case ColorKind.Black: return new {black = (string?)null};
                case ColorKind.Transparent: return new {transparent = (string?)null };
                case ColorKind.Srgb: return new {srgb = new { hex = _srgb.ToHexUnprefixed()}};
                default: throw new ImageflowAssertionFailed("default");
            }
        }
    }
}