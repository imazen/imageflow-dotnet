namespace Imageflow.Fluent;

/// <summary>
/// Named encoders that can be individually allowed/denied via
/// <see cref="CodecKillbits"/>.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::NamedEncoderName</c>. Wire form is
/// snake_case (e.g. <c>"mozjpeg_encoder"</c>). The native enum is
/// <c>#[non_exhaustive]</c>: new backends may be added in later imageflow
/// releases. Code that receives raw codec names from
/// <see cref="NetSupportResponse"/> should keep the string form.
///
/// Whether an encoder in this enum is actually available at runtime depends
/// on build-time feature flags on the native side; query
/// <see cref="ImageflowContext.GetNetSupport"/> for the effective grid.
/// </remarks>
public enum NamedEncoderName
{
    MozjpegEncoder,
    ZenJpegEncoder,
    MozjpegRsEncoder,
    LibpngEncoder,
    LodepngEncoder,
    PngquantEncoder,
    ZenPngEncoder,
    WebpEncoder,
    ZenWebpEncoder,
    GifEncoder,
    ZenGifEncoder,
    ZenAvifEncoder,
    ZenJxlEncoder,
    ZenBmpEncoder,
}

/// <summary>
/// Named decoders that can be individually allowed/denied via
/// <see cref="CodecKillbits"/>.
/// </summary>
/// <remarks>
/// Mirrors <c>imageflow_types::killbits::NamedDecoderName</c>. See
/// <see cref="NamedEncoderName"/> for notes on non-exhaustiveness and
/// runtime availability.
/// </remarks>
public enum NamedDecoderName
{
    MozjpegRsDecoder,
    ImageRsJpegDecoder,
    ZenJpegDecoder,
    LibpngDecoder,
    ImageRsPngDecoder,
    ZenPngDecoder,
    GifRsDecoder,
    ZenGifDecoder,
    WebpDecoder,
    ZenWebpDecoder,
    ZenAvifDecoder,
    ZenJxlDecoder,
    ZenBmpDecoder,
}

internal static class NamedEncoderExtensions
{
    public static string ToSnakeCase(this NamedEncoderName codec) => codec switch
    {
        NamedEncoderName.MozjpegEncoder => "mozjpeg_encoder",
        NamedEncoderName.ZenJpegEncoder => "zen_jpeg_encoder",
        NamedEncoderName.MozjpegRsEncoder => "mozjpeg_rs_encoder",
        NamedEncoderName.LibpngEncoder => "libpng_encoder",
        NamedEncoderName.LodepngEncoder => "lodepng_encoder",
        NamedEncoderName.PngquantEncoder => "pngquant_encoder",
        NamedEncoderName.ZenPngEncoder => "zen_png_encoder",
        NamedEncoderName.WebpEncoder => "webp_encoder",
        NamedEncoderName.ZenWebpEncoder => "zen_webp_encoder",
        NamedEncoderName.GifEncoder => "gif_encoder",
        NamedEncoderName.ZenGifEncoder => "zen_gif_encoder",
        NamedEncoderName.ZenAvifEncoder => "zen_avif_encoder",
        NamedEncoderName.ZenJxlEncoder => "zen_jxl_encoder",
        NamedEncoderName.ZenBmpEncoder => "zen_bmp_encoder",
        _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown NamedEncoderName"),
    };

    public static bool TryParse(string? snakeCase, out NamedEncoderName codec)
    {
        switch (snakeCase)
        {
            case "mozjpeg_encoder": codec = NamedEncoderName.MozjpegEncoder; return true;
            case "zen_jpeg_encoder": codec = NamedEncoderName.ZenJpegEncoder; return true;
            case "mozjpeg_rs_encoder": codec = NamedEncoderName.MozjpegRsEncoder; return true;
            case "libpng_encoder": codec = NamedEncoderName.LibpngEncoder; return true;
            case "lodepng_encoder": codec = NamedEncoderName.LodepngEncoder; return true;
            case "pngquant_encoder": codec = NamedEncoderName.PngquantEncoder; return true;
            case "zen_png_encoder": codec = NamedEncoderName.ZenPngEncoder; return true;
            case "webp_encoder": codec = NamedEncoderName.WebpEncoder; return true;
            case "zen_webp_encoder": codec = NamedEncoderName.ZenWebpEncoder; return true;
            case "gif_encoder": codec = NamedEncoderName.GifEncoder; return true;
            case "zen_gif_encoder": codec = NamedEncoderName.ZenGifEncoder; return true;
            case "zen_avif_encoder": codec = NamedEncoderName.ZenAvifEncoder; return true;
            case "zen_jxl_encoder": codec = NamedEncoderName.ZenJxlEncoder; return true;
            case "zen_bmp_encoder": codec = NamedEncoderName.ZenBmpEncoder; return true;
            default: codec = default; return false;
        }
    }
}

internal static class NamedDecoderExtensions
{
    public static string ToSnakeCase(this NamedDecoderName codec) => codec switch
    {
        NamedDecoderName.MozjpegRsDecoder => "mozjpeg_rs_decoder",
        NamedDecoderName.ImageRsJpegDecoder => "image_rs_jpeg_decoder",
        NamedDecoderName.ZenJpegDecoder => "zen_jpeg_decoder",
        NamedDecoderName.LibpngDecoder => "libpng_decoder",
        NamedDecoderName.ImageRsPngDecoder => "image_rs_png_decoder",
        NamedDecoderName.ZenPngDecoder => "zen_png_decoder",
        NamedDecoderName.GifRsDecoder => "gif_rs_decoder",
        NamedDecoderName.ZenGifDecoder => "zen_gif_decoder",
        NamedDecoderName.WebpDecoder => "webp_decoder",
        NamedDecoderName.ZenWebpDecoder => "zen_webp_decoder",
        NamedDecoderName.ZenAvifDecoder => "zen_avif_decoder",
        NamedDecoderName.ZenJxlDecoder => "zen_jxl_decoder",
        NamedDecoderName.ZenBmpDecoder => "zen_bmp_decoder",
        _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown NamedDecoderName"),
    };

    public static bool TryParse(string? snakeCase, out NamedDecoderName codec)
    {
        switch (snakeCase)
        {
            case "mozjpeg_rs_decoder": codec = NamedDecoderName.MozjpegRsDecoder; return true;
            case "image_rs_jpeg_decoder": codec = NamedDecoderName.ImageRsJpegDecoder; return true;
            case "zen_jpeg_decoder": codec = NamedDecoderName.ZenJpegDecoder; return true;
            case "libpng_decoder": codec = NamedDecoderName.LibpngDecoder; return true;
            case "image_rs_png_decoder": codec = NamedDecoderName.ImageRsPngDecoder; return true;
            case "zen_png_decoder": codec = NamedDecoderName.ZenPngDecoder; return true;
            case "gif_rs_decoder": codec = NamedDecoderName.GifRsDecoder; return true;
            case "zen_gif_decoder": codec = NamedDecoderName.ZenGifDecoder; return true;
            case "webp_decoder": codec = NamedDecoderName.WebpDecoder; return true;
            case "zen_webp_decoder": codec = NamedDecoderName.ZenWebpDecoder; return true;
            case "zen_avif_decoder": codec = NamedDecoderName.ZenAvifDecoder; return true;
            case "zen_jxl_decoder": codec = NamedDecoderName.ZenJxlDecoder; return true;
            case "zen_bmp_decoder": codec = NamedDecoderName.ZenBmpDecoder; return true;
            default: codec = default; return false;
        }
    }
}
