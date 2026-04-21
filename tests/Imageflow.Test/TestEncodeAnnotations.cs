using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Imageflow.Bindings;
using Imageflow.Fluent;
using Xunit;
using Xunit.Abstractions;

namespace Imageflow.Test;

/// <summary>
/// Round-trip tests for <see cref="EncodeAnnotations"/> and
/// <see cref="CodecSubstitutionAnnotation"/>. Mirrors the serde
/// round-trip tests on the native side
/// (<c>imageflow_types::killbits</c>) so wire compatibility is
/// validated in both directions.
/// </summary>
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task")]
public class TestEncodeAnnotations
{
    private readonly ITestOutputHelper _output;

    public TestEncodeAnnotations(ITestOutputHelper output)
    {
        _output = output;
    }

    // --- Enum wire forms ---------------------------------------------

    [Theory]
    [InlineData(SubstitutionReason.CodecKillbitsDenyEncoders, "codec_killbits_deny_encoders")]
    [InlineData(SubstitutionReason.CodecKillbitsAllowEncodersExcludes, "codec_killbits_allow_encoders_excludes")]
    [InlineData(SubstitutionReason.CompileFeatureMissing, "compile_feature_missing")]
    [InlineData(SubstitutionReason.CompileCodecConstDenied, "compile_codec_const_denied")]
    [InlineData(SubstitutionReason.NotRegistered, "not_registered")]
    public void SubstitutionReason_SnakeCaseWire_Matches(SubstitutionReason reason, string expected)
    {
        Assert.Equal(expected, reason.ToSnakeCaseWire());
        Assert.True(SubstitutionReasonExtensions.TryParse(expected, out var parsed));
        Assert.Equal(reason, parsed);
    }

    [Theory]
    [InlineData(SubstitutionReason.CodecKillbitsDenyEncoders, "codec_killbits.deny_encoders")]
    [InlineData(SubstitutionReason.CodecKillbitsAllowEncodersExcludes, "codec_killbits.allow_encoders_excludes")]
    [InlineData(SubstitutionReason.CompileFeatureMissing, "compile.feature_missing")]
    [InlineData(SubstitutionReason.CompileCodecConstDenied, "compile.codec_const_denied")]
    [InlineData(SubstitutionReason.NotRegistered, "not_registered")]
    public void SubstitutionReason_DottedMessageForm_Matches(SubstitutionReason reason, string expected)
    {
        Assert.Equal(expected, reason.ToDottedMessage());
    }

    [Fact]
    public void SubstitutionReason_UnknownValue_ReturnsFalse()
    {
        Assert.False(SubstitutionReasonExtensions.TryParse("future_reason_xyz", out _));
        Assert.False(SubstitutionReasonExtensions.TryParse(null, out _));
    }

    [Theory]
    [InlineData(CodecPriority.V3ZenFirst, "v3_zen_first")]
    [InlineData(CodecPriority.V2ClassicFirst, "v2_classic_first")]
    public void CodecPriority_WireRoundTrips(CodecPriority priority, string expected)
    {
        Assert.Equal(expected, priority.ToSnakeCaseWire());
        Assert.True(CodecPriorityExtensions.TryParse(expected, out var parsed));
        Assert.Equal(priority, parsed);
    }

    [Fact]
    public void CodecPriority_UnknownValue_ReturnsFalse()
    {
        Assert.False(CodecPriorityExtensions.TryParse("v4_future", out _));
    }

    // --- CodecSubstitutionAnnotation round-trip -----------------------

    [Fact]
    public void CodecSubstitutionAnnotation_FullShape_RoundTrips()
    {
        var ann = new CodecSubstitutionAnnotation
        {
            Requested = NamedEncoderName.MozjpegEncoder,
            Actual = NamedEncoderName.ZenJpegEncoder,
            Reason = SubstitutionReason.CodecKillbitsDenyEncoders,
            ReasonRaw = "codec_killbits_deny_encoders",
            CodecPriority = Fluent.CodecPriority.V3ZenFirst,
            CodecPriorityRaw = "v3_zen_first",
            FieldTranslations = new[] { "preset.quality \u2192 zen.quality", "preset.progressive \u2192 zen.progressive" },
            DroppedFields = Array.Empty<string>(),
        };
        var json = ann.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"requested\":\"mozjpeg_encoder\"", json);
        Assert.Contains("\"actual\":\"zen_jpeg_encoder\"", json);
        Assert.Contains("\"reason\":\"codec_killbits_deny_encoders\"", json);
        Assert.Contains("\"codec_priority\":\"v3_zen_first\"", json);
        Assert.Contains("\"field_translations\":[", json);
        // dropped_fields empty - omitted per skip_serializing_if=Vec::is_empty
        Assert.DoesNotContain("dropped_fields", json);

        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Equal(NamedEncoderName.MozjpegEncoder, parsed!.Requested);
        Assert.Equal(NamedEncoderName.ZenJpegEncoder, parsed.Actual);
        Assert.Equal(SubstitutionReason.CodecKillbitsDenyEncoders, parsed.Reason);
        Assert.Equal("codec_killbits_deny_encoders", parsed.ReasonRaw);
        Assert.Equal(Fluent.CodecPriority.V3ZenFirst, parsed.CodecPriority);
        Assert.Equal("v3_zen_first", parsed.CodecPriorityRaw);
        Assert.Equal(2, parsed.FieldTranslations.Count);
        Assert.Empty(parsed.DroppedFields);
    }

    [Fact]
    public void CodecSubstitutionAnnotation_DroppedFields_SerializesWhenNonEmpty()
    {
        var ann = new CodecSubstitutionAnnotation
        {
            Requested = NamedEncoderName.PngquantEncoder,
            Actual = NamedEncoderName.LodepngEncoder,
            Reason = SubstitutionReason.CodecKillbitsDenyEncoders,
            ReasonRaw = "codec_killbits_deny_encoders",
            CodecPriority = Fluent.CodecPriority.V3ZenFirst,
            CodecPriorityRaw = "v3_zen_first",
            FieldTranslations = new[] { "preset.quality \u2192 (dropped)" },
            DroppedFields = new[] { "preset.quality" },
        };
        var json = ann.ToJsonNode().ToJsonString();
        Assert.Contains("\"dropped_fields\":[\"preset.quality\"]", json);

        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Single(parsed!.DroppedFields);
        Assert.Equal("preset.quality", parsed.DroppedFields[0]);
    }

    [Fact]
    public void CodecSubstitutionAnnotation_AbsentCodecPriority_DefaultsToV3ZenFirst()
    {
        // Older native payloads lacking codec_priority must still parse;
        // the native side uses serde default = V3 wire form.
        var json = @"{
            ""requested"": ""mozjpeg_encoder"",
            ""actual"": ""zen_jpeg_encoder"",
            ""reason"": ""codec_killbits_deny_encoders""
        }";
        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Equal(Fluent.CodecPriority.V3ZenFirst, parsed!.CodecPriority);
        Assert.Equal("v3_zen_first", parsed.CodecPriorityRaw);
        Assert.Empty(parsed.FieldTranslations);
        Assert.Empty(parsed.DroppedFields);
    }

    [Fact]
    public void CodecSubstitutionAnnotation_V2ClassicFirstPriority_Parses()
    {
        var json = @"{
            ""requested"": ""zen_jpeg_encoder"",
            ""actual"": ""mozjpeg_encoder"",
            ""reason"": ""not_registered"",
            ""codec_priority"": ""v2_classic_first""
        }";
        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Equal(Fluent.CodecPriority.V2ClassicFirst, parsed!.CodecPriority);
        Assert.Equal("v2_classic_first", parsed.CodecPriorityRaw);
        Assert.Equal(SubstitutionReason.NotRegistered, parsed.Reason);
    }

    [Fact]
    public void CodecSubstitutionAnnotation_UnknownReason_PreservedAsRaw()
    {
        // Forward-compat: native side may add new reason variants.
        var json = @"{
            ""requested"": ""mozjpeg_encoder"",
            ""actual"": ""zen_jpeg_encoder"",
            ""reason"": ""future_reason_xyz"",
            ""codec_priority"": ""v3_zen_first""
        }";
        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Null(parsed!.Reason);
        Assert.Equal("future_reason_xyz", parsed.ReasonRaw);
        // Describe still works — falls back to raw text.
        Assert.Contains("future_reason_xyz", parsed.Describe());
    }

    [Fact]
    public void CodecSubstitutionAnnotation_UnknownCodecPriority_PreservedAsRaw()
    {
        var json = @"{
            ""requested"": ""mozjpeg_encoder"",
            ""actual"": ""zen_jpeg_encoder"",
            ""reason"": ""not_registered"",
            ""codec_priority"": ""v4_future""
        }";
        var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.Null(parsed!.CodecPriority);
        Assert.Equal("v4_future", parsed.CodecPriorityRaw);
    }

    [Fact]
    public void CodecSubstitutionAnnotation_AllSubstitutionReasonVariantsParse()
    {
        foreach (SubstitutionReason reason in Enum.GetValues(typeof(SubstitutionReason)))
        {
            var wire = reason.ToSnakeCaseWire();
            var json = $@"{{
                ""requested"": ""mozjpeg_encoder"",
                ""actual"": ""zen_jpeg_encoder"",
                ""reason"": ""{wire}"",
                ""codec_priority"": ""v3_zen_first""
            }}";
            var parsed = CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json));
            Assert.NotNull(parsed);
            Assert.Equal(reason, parsed!.Reason);
            Assert.Equal(wire, parsed.ReasonRaw);
        }
    }

    [Fact]
    public void CodecSubstitutionAnnotation_MissingRequired_Throws()
    {
        var json = @"{""actual"": ""zen_jpeg_encoder"", ""reason"": ""not_registered""}";
        Assert.Throws<ArgumentException>(() => CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json)));
    }

    [Fact]
    public void CodecSubstitutionAnnotation_UnknownEncoderName_Throws()
    {
        var json = @"{
            ""requested"": ""some_future_encoder"",
            ""actual"": ""zen_jpeg_encoder"",
            ""reason"": ""not_registered""
        }";
        var ex = Assert.Throws<ArgumentException>(() => CodecSubstitutionAnnotation.FromJsonNode(JsonNode.Parse(json)));
        Assert.Contains("some_future_encoder", ex.Message);
    }

    // --- Describe() helper -------------------------------------------

    [Fact]
    public void Describe_HasStandardFormat()
    {
        var ann = new CodecSubstitutionAnnotation
        {
            Requested = NamedEncoderName.MozjpegEncoder,
            Actual = NamedEncoderName.MozjpegRsEncoder,
            Reason = SubstitutionReason.CodecKillbitsDenyEncoders,
            ReasonRaw = "codec_killbits_deny_encoders",
            CodecPriority = Fluent.CodecPriority.V3ZenFirst,
            CodecPriorityRaw = "v3_zen_first",
            FieldTranslations = Array.Empty<string>(),
            DroppedFields = Array.Empty<string>(),
        };
        Assert.Equal(
            "mozjpeg_encoder \u2192 mozjpeg_rs_encoder: codec_killbits.deny_encoders (v3_zen_first)",
            ann.Describe());
    }

    // --- EncodeAnnotations envelope ----------------------------------

    [Fact]
    public void EncodeAnnotations_Empty_SerializesToEmptyObject()
    {
        var env = new EncodeAnnotations();
        Assert.True(env.IsEmpty);
        Assert.Null(env.CodecSubstitution);
        Assert.Equal("{}", env.ToJsonNode().ToJsonString());
    }

    [Fact]
    public void EncodeAnnotations_ParseEmptyObject_IsEmptyEnvelope()
    {
        var env = EncodeAnnotations.FromJsonNode(JsonNode.Parse("{}"));
        Assert.NotNull(env);
        Assert.True(env!.IsEmpty);
        Assert.Null(env.CodecSubstitution);
    }

    [Fact]
    public void EncodeAnnotations_ParseNull_ReturnsNull()
    {
        Assert.Null(EncodeAnnotations.FromJsonNode(null));
    }

    [Fact]
    public void EncodeAnnotations_WithSubstitution_RoundTrips()
    {
        var env = new EncodeAnnotations
        {
            CodecSubstitution = new CodecSubstitutionAnnotation
            {
                Requested = NamedEncoderName.PngquantEncoder,
                Actual = NamedEncoderName.LodepngEncoder,
                Reason = SubstitutionReason.CodecKillbitsDenyEncoders,
                ReasonRaw = "codec_killbits_deny_encoders",
                CodecPriority = Fluent.CodecPriority.V3ZenFirst,
                CodecPriorityRaw = "v3_zen_first",
                FieldTranslations = new[] { "preset.quality \u2192 (dropped)" },
                DroppedFields = new[] { "preset.quality" },
            },
        };
        Assert.False(env.IsEmpty);
        var json = env.ToJsonNode().ToJsonString();
        _output.WriteLine(json);
        Assert.Contains("\"codec_substitution\"", json);

        var parsed = EncodeAnnotations.FromJsonNode(JsonNode.Parse(json));
        Assert.NotNull(parsed);
        Assert.False(parsed!.IsEmpty);
        Assert.NotNull(parsed.CodecSubstitution);
        Assert.Equal(NamedEncoderName.PngquantEncoder, parsed.CodecSubstitution!.Requested);
        Assert.Single(parsed.CodecSubstitution.DroppedFields);
    }

    // --- Full response round-trip through BuildJobResult.From ---------

    [Fact]
    public void BuildJobResult_ParsesEncodeResultWithAnnotations()
    {
        var payload = JsonNode.Parse(@"{
            ""success"": true,
            ""code"": 200,
            ""data"": {
                ""job_result"": {
                    ""encodes"": [
                        {
                            ""io_id"": 1,
                            ""w"": 8,
                            ""h"": 8,
                            ""preferred_extension"": ""jpg"",
                            ""preferred_mime_type"": ""image/jpeg"",
                            ""annotations"": {
                                ""codec_substitution"": {
                                    ""requested"": ""mozjpeg_encoder"",
                                    ""actual"": ""zen_jpeg_encoder"",
                                    ""reason"": ""codec_killbits_deny_encoders"",
                                    ""codec_priority"": ""v3_zen_first"",
                                    ""field_translations"": [""preset.quality \u2192 zen.quality""]
                                }
                            }
                        }
                    ],
                    ""decodes"": []
                }
            }
        }");
        Assert.NotNull(payload);

        var dest = new BytesDestination();
        var outputs = new Dictionary<int, IOutputDestination> { { 1, dest } };
        var response = new FakeJsonResponse(payload!);

        var result = BuildJobResult.From(response, outputs);
        Assert.Single(result.EncodeResults);
        var er = result.First!;
        Assert.Equal(1, er.IoId);
        Assert.NotNull(er.Annotations);
        Assert.NotNull(er.Annotations!.CodecSubstitution);
        Assert.Equal(NamedEncoderName.MozjpegEncoder, er.Annotations.CodecSubstitution!.Requested);
        Assert.Equal(NamedEncoderName.ZenJpegEncoder, er.Annotations.CodecSubstitution.Actual);
        Assert.Equal(SubstitutionReason.CodecKillbitsDenyEncoders, er.Annotations.CodecSubstitution.Reason);
        Assert.Equal(Fluent.CodecPriority.V3ZenFirst, er.Annotations.CodecSubstitution.CodecPriority);
        Assert.Single(er.Annotations.CodecSubstitution.FieldTranslations);
    }

    [Fact]
    public void BuildJobResult_AbsentAnnotations_DeserializesToNull()
    {
        // Older native builds don't emit `annotations` at all (or pre-substitution encodes
        // on any build). Must not break the parse path.
        var payload = JsonNode.Parse(@"{
            ""success"": true,
            ""code"": 200,
            ""data"": {
                ""job_result"": {
                    ""encodes"": [
                        {
                            ""io_id"": 1,
                            ""w"": 8,
                            ""h"": 8,
                            ""preferred_extension"": ""jpg"",
                            ""preferred_mime_type"": ""image/jpeg""
                        }
                    ],
                    ""decodes"": []
                }
            }
        }");
        Assert.NotNull(payload);

        var dest = new BytesDestination();
        var outputs = new Dictionary<int, IOutputDestination> { { 1, dest } };
        var response = new FakeJsonResponse(payload!);

        var result = BuildJobResult.From(response, outputs);
        var er = result.First!;
        Assert.Null(er.Annotations);
    }

    private sealed class FakeJsonResponse : IJsonResponse
    {
        private readonly JsonNode _node;
        public FakeJsonResponse(JsonNode node) { _node = node; }
        public int ImageflowErrorCode => 200;
        public string CopyString() => _node.ToJsonString();
        public JsonNode? Parse() => _node;
        public byte[] CopyBytes() => System.Text.Encoding.UTF8.GetBytes(_node.ToJsonString());
        public void Dispose() { }
    }
}
