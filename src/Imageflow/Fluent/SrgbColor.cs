using System.Globalization;
using Imageflow.Bindings;

namespace Imageflow.Fluent
{
    /// <summary>
    /// Represents a color in the sRGB colorspace
    /// </summary>
    public struct SrgbColor
    {
        
        private uint value;

        public byte A => Mask8(value, 0);
        public byte R => Mask8(value, 1);
        public byte G => Mask8(value, 2);
        public byte B => Mask8(value, 3);

        private static byte Mask8(uint v, int index)
        {
            var shift = index * 8; 
            var mask = 0xff << shift;
            var result = (v & mask) >> shift;
            if (result > 255) throw new ImageflowAssertionFailed("Integer overflow in color parsing");
            return (byte) result;
        }
        private static byte Expand4(uint v, int index)
        {
            var shift = index * 4; 
            var mask = 0xf << shift;
            var result = (v & mask) >> shift;
            result = result | result << 4; // Duplicate lower 4 bits into upper
            if (result > 255) throw new ImageflowAssertionFailed("Integer overflow in color parsing");
            return (byte) result;
        }
        
        /// <summary>
        /// Parses a hexadecimal color in the form RGB, RGBA, RRGGBB, or RRGGBBAA
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="ImageflowAssertionFailed"></exception>
        public static SrgbColor FromHex(string s)
        {
            s = s.TrimStart('#');
            var v = uint.Parse(s, NumberStyles.HexNumber);
            switch (s.Length){
                case 3: return RGBA(Expand4(v, 2), Expand4(v, 1), Expand4(v, 0), 0xff);
                case 6: return RGBA(Mask8(v, 2), Mask8(v, 1), Mask8(v, 0), 0xff);
                case 4: return RGBA(Expand4(v, 3), Expand4(v, 2), Expand4(v, 1), Expand4(v, 0));
                case 8: return RGBA(Mask8(v, 3), Mask8(v, 2), Mask8(v, 1), Mask8(v, 0));
                default: throw new ImageflowAssertionFailed("TODO: invalid hex color");
            }
        }

        public string ToHexUnprefixed() => A == 0xff ? $"{R:x2}{G:x2}{B:x2}" : $"{R:x2}{G:x2}{B:x2}{A:x2}";

        public static SrgbColor BGRA(byte b, byte g, byte r, byte a) => new SrgbColor()
        {
            value = (uint) (b << 24 | g << 16 | r << 8 | a)
        };
        public static SrgbColor RGBA(byte r, byte g, byte b, byte a) => new SrgbColor()
        {
            value = (uint) (b << 24 | g << 16 | r << 8 | a)
        };
        public static SrgbColor RGB(byte r, byte g, byte b) => new SrgbColor()
        {
            value = (uint) (b << 24 | g << 16 | r << 8 | 0xff)
        };

    }
}