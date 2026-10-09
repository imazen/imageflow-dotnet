using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Xunit;
using Xunit.Abstractions;

namespace Imageflow.Test;

/// <summary>
/// Unit tests for the three-layer killbits DTOs and client-side cache
/// behavior. Integration tests (actual native round-trips through
/// <c>v1/context/set_policy</c> / <c>v1/context/get_net_support</c>) are
/// in <see cref="TestKillbitsIntegration"/> and are skipped on native
/// runtimes older than the prerelease that includes imageflow#720.
/// </summary>
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task")]
public class TestKillbits
{
    private readonly ITestOutputHelper _output;

    public TestKillbits(ITestOutputHelper output)
    {
        _output = output;
    }

    // --- DTO round-trip ------------------------------------------------

    [Fact]
    public void FormatKillbits_DenyList_RoundTrips()
    {
        var kb = new FormatKillbits
        {
            DenyDecode = new[] { ImageFormat.Avif, ImageFormat.Jxl },
            DenyEncode = new[] { ImageFormat.Avif },
        };
        var json = kb.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"deny_decode\"", json);
        Assert.Contains("\"avif\"", json);
        Assert.Contains("\"jxl\"", json);
        Assert.Contains("\"deny_encode\"", json);
        Assert.DoesNotContain("\"allow_", json);
        Assert.DoesNotContain("\"formats\"", json);
    }

    [Fact]
    public void FormatKillbits_TableForm_RoundTrips()
    {
        var kb = new FormatKillbits
        {
            Formats = new Dictionary<ImageFormat, FormatPermissions>
            {
                { ImageFormat.Jpeg, new FormatPermissions(decode: true, encode: false) },
                { ImageFormat.Avif, new FormatPermissions(decode: false, encode: false) },
            },
        };
        var json = kb.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"jpeg\"", json);
        Assert.Contains("\"avif\"", json);
        Assert.Contains("\"encode\":false", json);
    }

    [Fact]
    public void FormatKillbits_MixedListAndTable_Rejected()
    {
        var kb = new FormatKillbits
        {
            DenyDecode = new[] { ImageFormat.Avif },
            Formats = new Dictionary<ImageFormat, FormatPermissions>
            {
                { ImageFormat.Jpeg, new FormatPermissions(decode: true, encode: false) },
            },
        };
        var ex = Assert.Throws<ArgumentException>(() => kb.Validate());
        Assert.Contains("single form", ex.Message);
    }

    [Fact]
    public void FormatKillbits_AllowAndDenyDecode_Rejected()
    {
        var kb = new FormatKillbits
        {
            AllowDecode = new[] { ImageFormat.Jpeg },
            DenyDecode = new[] { ImageFormat.Png },
        };
        var ex = Assert.Throws<ArgumentException>(() => kb.Validate());
        Assert.Contains("decode", ex.Message);
    }

    [Fact]
    public void FormatKillbits_AllowAndDenyEncode_Rejected()
    {
        var kb = new FormatKillbits
        {
            AllowEncode = new[] { ImageFormat.Jpeg },
            DenyEncode = new[] { ImageFormat.Png },
        };
        var ex = Assert.Throws<ArgumentException>(() => kb.Validate());
        Assert.Contains("encode", ex.Message);
    }

    [Fact]
    public void FormatKillbits_JobLevel_RejectsAllowList()
    {
        var kb = new FormatKillbits
        {
            AllowDecode = new[] { ImageFormat.Jpeg },
        };
        var ex = Assert.Throws<ArgumentException>(() => kb.ValidateJobLevel());
        Assert.Contains("layer 3", ex.Message);
    }

    [Fact]
    public void FormatKillbits_JobLevel_RejectsTableTrue()
    {
        var kb = new FormatKillbits
        {
            Formats = new Dictionary<ImageFormat, FormatPermissions>
            {
                { ImageFormat.Jpeg, new FormatPermissions(decode: true, encode: false) },
            },
        };
        var ex = Assert.Throws<ArgumentException>(() => kb.ValidateJobLevel());
        Assert.Contains("layer 3", ex.Message);
    }

    [Fact]
    public void FormatKillbits_JobLevel_AcceptsTableAllFalse()
    {
        var kb = new FormatKillbits
        {
            Formats = new Dictionary<ImageFormat, FormatPermissions>
            {
                { ImageFormat.Jpeg, new FormatPermissions(decode: false, encode: false) },
            },
        };
        kb.ValidateJobLevel();
    }

    [Fact]
    public void CodecKillbits_DenyEncoders_RoundTrips()
    {
        var kb = new CodecKillbits
        {
            DenyEncoders = new[] { NamedEncoderName.MozjpegEncoder },
            DenyDecoders = new[] { NamedDecoderName.ZenJpegDecoder },
        };
        var json = kb.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"mozjpeg_encoder\"", json);
        Assert.Contains("\"zen_jpeg_decoder\"", json);
        Assert.Contains("\"deny_encoders\"", json);
        Assert.Contains("\"deny_decoders\"", json);
    }

    [Fact]
    public void CodecKillbits_AllowAndDenyEncoders_Rejected()
    {
        var kb = new CodecKillbits
        {
            AllowEncoders = new[] { NamedEncoderName.MozjpegEncoder },
            DenyEncoders = new[] { NamedEncoderName.ZenJpegEncoder },
        };
        Assert.Throws<ArgumentException>(() => kb.Validate());
    }

    [Fact]
    public void CodecKillbits_JobLevel_RejectsAllowList()
    {
        var kb = new CodecKillbits
        {
            AllowEncoders = new[] { NamedEncoderName.MozjpegEncoder },
        };
        Assert.Throws<ArgumentException>(() => kb.ValidateJobLevel());
    }

    [Fact]
    public void SecurityOptions_SerializesScalarAndKillbits()
    {
        var opts = new SecurityOptions
        {
            MaxDecodeSize = new FrameSizeLimit(12000, 12000, 100f),
            MaxInputFileBytes = 10 * 1024 * 1024,
            MaxJsonBytes = 4 * 1024 * 1024,
            MaxTotalFilePixels = 400_000_000,
            Formats = new FormatKillbits { DenyEncode = new[] { ImageFormat.Avif } },
            Codecs = new CodecKillbits { DenyDecoders = new[] { NamedDecoderName.ZenAvifDecoder } },
        };
        var json = opts.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"max_decode_size\"", json);
        Assert.Contains("\"max_input_file_bytes\":10485760", json);
        Assert.Contains("\"max_json_bytes\":4194304", json);
        Assert.Contains("\"max_total_file_pixels\":400000000", json);
        Assert.Contains("\"formats\"", json);
        Assert.Contains("\"codecs\"", json);
        Assert.Contains("\"avif\"", json);
        Assert.Contains("\"zen_avif_decoder\"", json);
    }

    // --- Format/codec name snake_case contract -------------------------

    [Theory]
    [InlineData(ImageFormat.Jpeg, "jpeg")]
    [InlineData(ImageFormat.Png, "png")]
    [InlineData(ImageFormat.Gif, "gif")]
    [InlineData(ImageFormat.Webp, "webp")]
    [InlineData(ImageFormat.Avif, "avif")]
    [InlineData(ImageFormat.Jxl, "jxl")]
    [InlineData(ImageFormat.Heic, "heic")]
    [InlineData(ImageFormat.Bmp, "bmp")]
    [InlineData(ImageFormat.Tiff, "tiff")]
    [InlineData(ImageFormat.Pnm, "pnm")]
    public void ImageFormat_ToSnakeCase(ImageFormat f, string expected)
    {
        Assert.Equal(expected, f.ToSnakeCase());
        Assert.True(ImageFormatExtensions.TryParse(expected, out var parsed));
        Assert.Equal(f, parsed);
    }

    [Fact]
    public void NamedEncoderName_ToSnakeCase_AllVariants()
    {
        foreach (NamedEncoderName e in Enum.GetValues(typeof(NamedEncoderName)))
        {
            var snake = e.ToSnakeCase();
            Assert.True(NamedEncoderExtensions.TryParse(snake, out var round));
            Assert.Equal(e, round);
        }
    }

    [Fact]
    public void NamedDecoderName_ToSnakeCase_AllVariants()
    {
        foreach (NamedDecoderName d in Enum.GetValues(typeof(NamedDecoderName)))
        {
            var snake = d.ToSnakeCase();
            Assert.True(NamedDecoderExtensions.TryParse(snake, out var round));
            Assert.Equal(d, round);
        }
    }

    // --- Killbits exception parsing -----------------------------------

    [Fact]
    public void KillbitsDeniedException_Parses_CodecNotAvailable()
    {
        const string envelope =
            "{\"error\": \"codec_not_available\", \"codec\": \"mozjpeg_encoder\", \"format\": \"jpeg\", " +
            "\"reasons\": [\"denied_by_trusted_policy\"], " +
            "\"net_support\": {\"formats\":{\"jpeg\":{\"decode\":true,\"encode\":false}}," +
            "\"codecs\":{\"mozjpeg_encoder\":{\"available\":false,\"format\":\"jpeg\",\"role\":\"encoder\"," +
            "\"reasons\":[\"denied_by_trusted_policy\"]}}}}";
        var ex = KillbitsDeniedException.TryParse(envelope);
        Assert.NotNull(ex);
        Assert.Equal(KillbitsDenialKind.CodecNotAvailable, ex!.DenialKind);
        Assert.Equal("mozjpeg_encoder", ex.Codec);
        Assert.Equal("jpeg", ex.Format);
        Assert.Contains("denied_by_trusted_policy", ex.Reasons);
        Assert.NotNull(ex.NetSupport);
        Assert.True(ex.NetSupport!.Formats.ContainsKey("jpeg"));
        Assert.False(ex.NetSupport.Codecs["mozjpeg_encoder"].Available);
    }

    [Fact]
    public void KillbitsDeniedException_Parses_EncodeNotAvailable()
    {
        const string envelope =
            "{\"error\": \"encode_not_available\", \"format\": \"avif\", " +
            "\"reasons\": [\"no_available_encoder\"], " +
            "\"net_support\": {\"formats\":{\"avif\":{\"decode\":false,\"encode\":false}}," +
            "\"codecs\":{}}}";
        var ex = KillbitsDeniedException.TryParse(envelope);
        Assert.NotNull(ex);
        Assert.Equal(KillbitsDenialKind.EncodeNotAvailable, ex!.DenialKind);
        Assert.Null(ex.Codec);
        Assert.Equal("avif", ex.Format);
    }

    [Fact]
    public void KillbitsDeniedException_NonKillbitsMessage_ReturnsNull()
    {
        Assert.Null(KillbitsDeniedException.TryParse("some unrelated error text"));
        Assert.Null(KillbitsDeniedException.TryParse(string.Empty));
        Assert.Null(KillbitsDeniedException.TryParse("{\"error\":\"something_else\"}"));
    }

    // --- Capability cache ---------------------------------------------

    [Fact]
    public void ImageflowCapabilities_MimeTypes()
    {
        Assert.Equal("image/jpeg", ImageflowCapabilities.GetMimeType(ImageFormat.Jpeg));
        Assert.Equal("image/avif", ImageflowCapabilities.GetMimeType(ImageFormat.Avif));
        Assert.Equal("image/jxl", ImageflowCapabilities.GetMimeType(ImageFormat.Jxl));
    }

    [Fact]
    public void ImageflowCapabilities_Extensions_JpegIncludesCommonAliases()
    {
        var exts = ImageflowCapabilities.GetExtensions(ImageFormat.Jpeg);
        Assert.Contains("jpg", exts);
        Assert.Contains("jpeg", exts);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, ImageFormat.Jpeg)]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ImageFormat.Png)]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, ImageFormat.Gif)]
    [InlineData(new byte[] { (byte)'B', (byte)'M' }, ImageFormat.Bmp)]
    [InlineData(new byte[] { 0xFF, 0x0A }, ImageFormat.Jxl)]
    public void ImageflowCapabilities_DetectFormat(byte[] magic, ImageFormat expected)
    {
        Assert.Equal(expected, ImageflowCapabilities.DetectFormat(magic));
    }

    [Fact]
    public void ImageflowCapabilities_DetectFormat_Webp()
    {
        var bytes = new byte[12];
        bytes[0] = (byte)'R'; bytes[1] = (byte)'I'; bytes[2] = (byte)'F'; bytes[3] = (byte)'F';
        bytes[8] = (byte)'W'; bytes[9] = (byte)'E'; bytes[10] = (byte)'B'; bytes[11] = (byte)'P';
        Assert.Equal(ImageFormat.Webp, ImageflowCapabilities.DetectFormat(bytes));
    }

    [Fact]
    public void ImageflowCapabilities_DetectFormat_AvifBrand()
    {
        var bytes = new byte[] { 0, 0, 0, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
                                 (byte)'a', (byte)'v', (byte)'i', (byte)'f' };
        Assert.Equal(ImageFormat.Avif, ImageflowCapabilities.DetectFormat(bytes));
    }

    [Fact]
    public void ImageflowCapabilities_DetectFormat_HeicBrand()
    {
        var bytes = new byte[] { 0, 0, 0, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
                                 (byte)'h', (byte)'e', (byte)'i', (byte)'c' };
        Assert.Equal(ImageFormat.Heic, ImageflowCapabilities.DetectFormat(bytes));
    }

    [Fact]
    public void ImageflowCapabilities_DetectFormat_UnknownReturnsNull()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        Assert.Null(ImageflowCapabilities.DetectFormat(bytes));
    }

    [Fact]
    public void ImageflowCapabilities_GetRiapiKeys_StubReturnsNull()
    {
        // TODO: flip expectation once imageflow exposes a static-info
        // endpoint for the RIAPI key list.
        Assert.Null(ImageflowCapabilities.GetRiapiKeys());
    }

    [Fact]
    public void ImageflowCapabilities_GetEncoderCapabilities_StubReturnsNull()
    {
        // TODO: flip expectation once imageflow exposes per-codec capability info.
        Assert.Null(ImageflowCapabilities.GetEncoderCapabilities(NamedEncoderName.MozjpegEncoder));
    }

    // --- NetSupport response parsing -----------------------------------

    [Fact]
    public void NetSupportResponse_ParsesGetNetSupportShape()
    {
        var payload = JsonNode.Parse(
            "{\"ok\":true,\"trusted_policy_set\":true," +
            "\"net_support\":{" +
              "\"formats\":{\"jpeg\":{\"decode\":true,\"encode\":true,\"decode_reasons\":[],\"encode_reasons\":[]}," +
                          "\"avif\":{\"decode\":false,\"encode\":false,\"decode_reasons\":[\"denied_by_trusted_policy\"],\"encode_reasons\":[\"denied_by_trusted_policy\"]}}," +
              "\"codecs\":{\"mozjpeg_encoder\":{\"available\":true,\"format\":\"jpeg\",\"role\":\"encoder\",\"reasons\":[]}}}," +
            "\"compile_ceiling\":{\"denied_decode\":[],\"denied_encode\":[],\"features_missing\":[\"heic\"]}}");
        Assert.NotNull(payload);
        var parsed = NetSupportResponse.ParseGetNetSupportResponse(payload!);
        Assert.True(parsed.TrustedPolicySet);
        Assert.True(parsed.Formats["jpeg"].Decode);
        Assert.False(parsed.Formats["avif"].Decode);
        Assert.Contains("denied_by_trusted_policy", parsed.Formats["avif"].DecodeReasons);
        Assert.True(parsed.Codecs["mozjpeg_encoder"].Available);
        Assert.Equal("encoder", parsed.Codecs["mozjpeg_encoder"].Role);
        Assert.NotNull(parsed.CompileCeiling);
        Assert.Contains("heic", parsed.CompileCeiling!.FeaturesMissing);
    }

    [Fact]
    public void NetSupportResponse_GetFormatReturnsNullForUnknown()
    {
        var payload = JsonNode.Parse(
            "{\"net_support\":{\"formats\":{\"jpeg\":{\"decode\":true,\"encode\":true}},\"codecs\":{}}," +
            "\"trusted_policy_set\":false}");
        Assert.NotNull(payload);
        var parsed = NetSupportResponse.ParseGetNetSupportResponse(payload!);
        Assert.NotNull(parsed.GetFormat("jpeg"));
        Assert.Null(parsed.GetFormat("nonexistent_format_xyz"));
    }

    // --- Job-level validation at ImageJob boundary ---------------------

    [Fact]
    public async Task ImageJob_JobLevelSecurity_RejectsAllowListBeforeAbi()
    {
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/TQBcNTh/AAAAAXRSTlPM0jRW/QAAAApJREFUeJxjYgAAAAYAAzY3fKgAAAAASUVORK5CYII=");
        var job = new ImageJob();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await job.Decode(new BytesSource(imageBytes))
                .EncodeToBytes(new GifEncoder())
                .Finish()
                .SetSecurityOptions(new SecurityOptions
                {
                    Formats = new FormatKillbits { AllowDecode = new[] { ImageFormat.Jpeg } },
                })
                .InProcessAsync();
        });
    }
}
