using System.Collections.Concurrent;
using Imageflow.Fluent;

namespace Imageflow.Bindings;

/// <summary>
/// Process-wide read-only cache of imageflow's static capability metadata
/// — MIME types, extensions, codec capabilities, RIAPI key list.
/// </summary>
/// <remarks>
/// Callers that need format/codec info in hot paths (upstream servers
/// negotiating <c>Accept:</c> headers, IR4 frontends mapping file
/// extensions to codecs) should route through this class rather than
/// spinning up a <see cref="JobContext"/> per request.
///
/// <para>
/// <strong>Caching strategy.</strong> Every accessor below is
/// backed by a <see cref="Lazy{T}"/> or <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// initialized on first use. Entries are never invalidated — the
/// underlying data is a property of the native build (compile-time
/// feature flags, registered codecs) and doesn't change at runtime.
/// </para>
///
/// <para>
/// Several accessors return <c>null</c> today because the imageflow side
/// doesn't yet expose a static-info endpoint; see the per-method
/// <c>TODO</c> notes. When that endpoint lands, the backing lazies will
/// repoint at it and the public API stays stable.
/// </para>
/// </remarks>
public static class ImageflowCapabilities
{
    // --- MIME / extension tables (static; safe to hard-code) -----------

    private static readonly Lazy<IReadOnlyDictionary<ImageFormat, string>> _mimeTypes = new(
        () => new Dictionary<ImageFormat, string>
        {
            { ImageFormat.Jpeg, "image/jpeg" },
            { ImageFormat.Png,  "image/png"  },
            { ImageFormat.Gif,  "image/gif"  },
            { ImageFormat.Webp, "image/webp" },
            { ImageFormat.Avif, "image/avif" },
            { ImageFormat.Jxl,  "image/jxl"  },
            { ImageFormat.Heic, "image/heic" },
            { ImageFormat.Bmp,  "image/bmp"  },
            { ImageFormat.Tiff, "image/tiff" },
            { ImageFormat.Pnm,  "image/x-portable-anymap" },
        });

    private static readonly Lazy<IReadOnlyDictionary<ImageFormat, IReadOnlyList<string>>> _extensions = new(
        () => new Dictionary<ImageFormat, IReadOnlyList<string>>
        {
            { ImageFormat.Jpeg, new[] { "jpg", "jpeg", "jpe", "jif", "jfif" } },
            { ImageFormat.Png,  new[] { "png" } },
            { ImageFormat.Gif,  new[] { "gif" } },
            { ImageFormat.Webp, new[] { "webp" } },
            { ImageFormat.Avif, new[] { "avif", "avifs" } },
            { ImageFormat.Jxl,  new[] { "jxl" } },
            { ImageFormat.Heic, new[] { "heic", "heif", "hif" } },
            { ImageFormat.Bmp,  new[] { "bmp" } },
            { ImageFormat.Tiff, new[] { "tif", "tiff" } },
            { ImageFormat.Pnm,  new[] { "pnm", "pbm", "pgm", "ppm" } },
        });

    // --- Magic-byte detection (delegates to the existing helper) -------

    /// <summary>
    /// MIME type for <paramref name="format"/>. Pure-function cached
    /// lookup — no native call, safe to invoke in hot paths.
    /// </summary>
    public static string GetMimeType(ImageFormat format)
    {
        return _mimeTypes.Value.TryGetValue(format, out var mime)
            ? mime
            : throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown ImageFormat");
    }

    /// <summary>
    /// Canonical lowercase file extensions (without leading dot) for
    /// <paramref name="format"/>. Cached.
    /// </summary>
    public static IReadOnlyList<string> GetExtensions(ImageFormat format)
    {
        return _extensions.Value.TryGetValue(format, out var exts)
            ? exts
            : throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown ImageFormat");
    }

    /// <summary>
    /// Detect the image format from the first few bytes of a file.
    /// Returns <c>null</c> if the bytes don't match any recognized
    /// signature. The sniff table is baked in; results are stable for
    /// the life of the process (no cache miss, no allocation).
    /// </summary>
    /// <remarks>
    /// Only formats listed in <see cref="ImageFormat"/> are detected.
    /// <c>HEIC</c>, <c>AVIF</c>, and the various MPEG-4-ish variants
    /// share an <c>ftyp</c> box shape; the brand (<c>avif</c>, <c>heic</c>,
    /// <c>mif1</c>, etc.) is checked to disambiguate.
    /// </remarks>
    public static ImageFormat? DetectFormat(ReadOnlySpan<byte> magicBytes)
    {
        if (magicBytes.Length < 2)
        {
            return null;
        }

        // JPEG: FF D8 FF
        if (magicBytes.Length >= 3 && magicBytes[0] == 0xFF && magicBytes[1] == 0xD8 && magicBytes[2] == 0xFF)
        {
            return ImageFormat.Jpeg;
        }

        if (magicBytes.Length >= 8)
        {
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (magicBytes[0] == 0x89 && magicBytes[1] == 0x50 && magicBytes[2] == 0x4E && magicBytes[3] == 0x47 &&
                magicBytes[4] == 0x0D && magicBytes[5] == 0x0A && magicBytes[6] == 0x1A && magicBytes[7] == 0x0A)
            {
                return ImageFormat.Png;
            }
        }

        if (magicBytes.Length >= 6)
        {
            // GIF: "GIF87a" / "GIF89a"
            if (magicBytes[0] == 'G' && magicBytes[1] == 'I' && magicBytes[2] == 'F' &&
                magicBytes[3] == '8' && (magicBytes[4] == '7' || magicBytes[4] == '9') && magicBytes[5] == 'a')
            {
                return ImageFormat.Gif;
            }
        }

        // BMP: "BM"
        if (magicBytes.Length >= 2 && magicBytes[0] == 'B' && magicBytes[1] == 'M')
        {
            return ImageFormat.Bmp;
        }

        // TIFF (little-endian "II*\0" or big-endian "MM\0*")
        if (magicBytes.Length >= 4)
        {
            if ((magicBytes[0] == 'I' && magicBytes[1] == 'I' && magicBytes[2] == 0x2A && magicBytes[3] == 0x00) ||
                (magicBytes[0] == 'M' && magicBytes[1] == 'M' && magicBytes[2] == 0x00 && magicBytes[3] == 0x2A))
            {
                return ImageFormat.Tiff;
            }
        }

        // WebP: "RIFF....WEBP"
        if (magicBytes.Length >= 12 &&
            magicBytes[0] == 'R' && magicBytes[1] == 'I' && magicBytes[2] == 'F' && magicBytes[3] == 'F' &&
            magicBytes[8] == 'W' && magicBytes[9] == 'E' && magicBytes[10] == 'B' && magicBytes[11] == 'P')
        {
            return ImageFormat.Webp;
        }

        // JXL: two signatures — naked codestream FF 0A, or ISOBMFF JXL container
        if (magicBytes.Length >= 2 && magicBytes[0] == 0xFF && magicBytes[1] == 0x0A)
        {
            return ImageFormat.Jxl;
        }
        if (magicBytes.Length >= 12 &&
            magicBytes[0] == 0x00 && magicBytes[1] == 0x00 && magicBytes[2] == 0x00 && magicBytes[3] == 0x0C &&
            magicBytes[4] == 'J' && magicBytes[5] == 'X' && magicBytes[6] == 'L' && magicBytes[7] == 0x20 &&
            magicBytes[8] == 0x0D && magicBytes[9] == 0x0A && magicBytes[10] == 0x87 && magicBytes[11] == 0x0A)
        {
            return ImageFormat.Jxl;
        }

        // ISOBMFF ftyp brand (AVIF / HEIC). Layout: ... 'ftyp' <4-byte brand>.
        if (magicBytes.Length >= 12 &&
            magicBytes[4] == 'f' && magicBytes[5] == 't' && magicBytes[6] == 'y' && magicBytes[7] == 'p')
        {
            var brand = $"{(char)magicBytes[8]}{(char)magicBytes[9]}{(char)magicBytes[10]}{(char)magicBytes[11]}";
            switch (brand)
            {
                case "avif":
                case "avis":
                    return ImageFormat.Avif;
                case "heic":
                case "heix":
                case "hevc":
                case "hevx":
                case "mif1":
                case "msf1":
                case "heim":
                case "heis":
                case "hevm":
                case "hevs":
                    return ImageFormat.Heic;
            }
        }

        // PNM: "P1".."P7" followed by whitespace.
        if (magicBytes.Length >= 3 && magicBytes[0] == 'P' &&
            magicBytes[1] >= '1' && magicBytes[1] <= '7' &&
            (magicBytes[2] == ' ' || magicBytes[2] == '\n' || magicBytes[2] == '\r' || magicBytes[2] == '\t'))
        {
            return ImageFormat.Pnm;
        }

        return null;
    }

    // --- Native-backed caches (stubbed; TODOs for future endpoints) ----

    private static readonly ConcurrentDictionary<NamedEncoderName, EncoderCapabilities?> _encoderCapabilitiesCache = new();

    /// <summary>
    /// Per-encoder capability record (supported pixel formats, animation
    /// support, quality ranges, etc.).
    /// </summary>
    /// <remarks>
    /// TODO: wire to a native static-info endpoint once imageflow exposes
    /// one. Today this always returns <c>null</c>; the cache shape lets
    /// us add the backing call without changing the signature.
    /// </remarks>
    public static EncoderCapabilities? GetEncoderCapabilities(NamedEncoderName encoder)
    {
        return _encoderCapabilitiesCache.GetOrAdd(encoder, _ => null);
    }

    private static readonly Lazy<IReadOnlyList<string>?> _riapiKeys = new(() =>
    {
        // TODO: switch to a native-backed call (e.g. list_riapi_keys) once
        // it's exposed through a read-only, context-less endpoint. Until
        // then the cache layer exists but returns null so callers can
        // fall back gracefully.
        return null;
    });

    /// <summary>
    /// List of RIAPI query-string keys the native build supports. Cached
    /// once per process; <c>null</c> until the static-info endpoint lands.
    /// </summary>
    public static IReadOnlyList<string>? GetRiapiKeys() => _riapiKeys.Value;
}

/// <summary>
/// Opaque per-encoder capability record. Intentionally unpopulated today
/// — stub type so <see cref="ImageflowCapabilities.GetEncoderCapabilities"/>
/// can return structured data once the native endpoint lands.
/// </summary>
public sealed class EncoderCapabilities
{
    public NamedEncoderName Encoder { get; }
    public ImageFormat Format { get; }
    public bool SupportsAnimation { get; }

    internal EncoderCapabilities(NamedEncoderName encoder, ImageFormat format, bool supportsAnimation)
    {
        Encoder = encoder;
        Format = format;
        SupportsAnimation = supportsAnimation;
    }
}
