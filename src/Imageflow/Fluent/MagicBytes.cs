using System;

namespace Imageflow.Fluent
{
    internal static class MagicBytes
    {
        internal enum ImageFormat
        {
            Jpeg,
            Gif,
            Png,
            WebP
        }

        /// <summary>
        /// Returns null if not a recognized file type
        /// </summary>
        /// <param name="first12Bytes">First 12 or more bytes of the file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static ImageFormat? GetImageFormat(byte[] first12Bytes)
        {
            var bytes = first12Bytes;
            if (bytes.Length < 12) throw new ArgumentException("The byte array must contain at least 12 bytes", 
                nameof(first12Bytes));

            if (bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
            {
                return ImageFormat.Jpeg;
            }

            if (bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == '8' &&
                (bytes[4] == '9' || bytes[4] == '7') && bytes[5] == 'a')
            {
                return ImageFormat.Gif;
            }

            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47 &&
                bytes[4] == 0x0d && bytes[5] == 0x0a && bytes[6] == 0x1a && bytes[7] == 0x0a)
            {
                return ImageFormat.Png;
            }

            if (bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
                bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
            {
                return ImageFormat.WebP;
            }

            return null;
        }

        internal static string GetImageContentType(byte[] first12Bytes)
        {
            switch (GetImageFormat(first12Bytes))
            {
                case ImageFormat.Jpeg:
                    return "image/jpeg";
                case ImageFormat.Gif:
                    return "image/gif";
                case ImageFormat.Png:
                    return "image/png";
                case ImageFormat.WebP:
                    return "image/webp";
                case null:
                    return null;
                default:
                    throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// Returns true if Imageflow can likely decode the image based on the given file header
        /// </summary>
        /// <param name="first12Bytes"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool IsDecodable(byte[] first12Bytes)
        {
            switch (GetImageFormat(first12Bytes))
            {
                case ImageFormat.Jpeg:
                case ImageFormat.Gif:
                case ImageFormat.Png:
                case ImageFormat.WebP:
                    return true;
                case null:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}