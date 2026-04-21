namespace Imageflow.Fluent;

/// <summary>
/// Image formats recognized by the three-layer codec killbits system on the
/// native imageflow side.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::ImageFormat</c>. The serialized
/// wire form is snake_case (e.g. <c>"jpeg"</c>, <c>"png"</c>).
///
/// The native enum is <c>#[non_exhaustive]</c>; new formats may appear in
/// later imageflow releases. Callers that receive an unknown format string
/// from <see cref="NetSupportResponse"/> should surface it as opaque text
/// rather than assuming a closed set.
/// </remarks>
public enum ImageFormat
{
    Jpeg,
    Png,
    Gif,
    Webp,
    Avif,
    Jxl,
    Heic,
    Bmp,
    Tiff,
    Pnm,
}

internal static class ImageFormatExtensions
{
    public static string ToSnakeCase(this ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => "jpeg",
        ImageFormat.Png => "png",
        ImageFormat.Gif => "gif",
        ImageFormat.Webp => "webp",
        ImageFormat.Avif => "avif",
        ImageFormat.Jxl => "jxl",
        ImageFormat.Heic => "heic",
        ImageFormat.Bmp => "bmp",
        ImageFormat.Tiff => "tiff",
        ImageFormat.Pnm => "pnm",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown ImageFormat"),
    };

    /// <summary>
    /// Try to parse a snake_case format string. Returns <c>false</c> for
    /// values the current binding doesn't recognize (the native enum is
    /// non-exhaustive — callers that hold the raw string should preserve it
    /// as-is rather than flattening unknown values to a default).
    /// </summary>
    public static bool TryParse(string? snakeCase, out ImageFormat format)
    {
        switch (snakeCase)
        {
            case "jpeg": format = ImageFormat.Jpeg; return true;
            case "png": format = ImageFormat.Png; return true;
            case "gif": format = ImageFormat.Gif; return true;
            case "webp": format = ImageFormat.Webp; return true;
            case "avif": format = ImageFormat.Avif; return true;
            case "jxl": format = ImageFormat.Jxl; return true;
            case "heic": format = ImageFormat.Heic; return true;
            case "bmp": format = ImageFormat.Bmp; return true;
            case "tiff": format = ImageFormat.Tiff; return true;
            case "pnm": format = ImageFormat.Pnm; return true;
            default: format = default; return false;
        }
    }

    /// <summary>All known formats in stable wire order, matching <c>ImageFormat::ALL</c> on the native side.</summary>
    public static readonly ImageFormat[] All =
    [
        ImageFormat.Jpeg,
        ImageFormat.Png,
        ImageFormat.Gif,
        ImageFormat.Webp,
        ImageFormat.Avif,
        ImageFormat.Jxl,
        ImageFormat.Heic,
        ImageFormat.Bmp,
        ImageFormat.Tiff,
        ImageFormat.Pnm,
    ];
}
