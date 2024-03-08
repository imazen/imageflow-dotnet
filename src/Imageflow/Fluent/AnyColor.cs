using System.Text.Json.Nodes;

using Imageflow.Bindings;

namespace Imageflow.Fluent;

/// <summary>
/// Represents a color with or without transparency.
/// </summary>
public readonly struct AnyColor
{
    private AnyColor(ColorKind kind, SrgbColor srgb = default)
    {
        _kind = kind;
        _srgb = srgb;
    }
    private readonly ColorKind _kind;
    private readonly SrgbColor _srgb;
    public static AnyColor Black => new AnyColor(ColorKind.Black);
    public static AnyColor Transparent => new AnyColor(ColorKind.Transparent);

    /// <summary>
    /// Parses color in RGB, RGBA, RRGGBB or RRGGBBAA format
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static AnyColor FromHexSrgb(string hex) => new AnyColor(ColorKind.Srgb, SrgbColor.FromHex(hex));

    public static AnyColor Srgb(SrgbColor c) => new AnyColor(ColorKind.Srgb, c);

    [Obsolete("Use ToJsonNode() instead")]
    public object ToImageflowDynamic()
    {
        switch (_kind)
        {
            case ColorKind.Black: return new { black = (string?)null };
            case ColorKind.Transparent: return new { transparent = (string?)null };
            case ColorKind.Srgb: return new { srgb = new { hex = _srgb.ToHexUnprefixed() } };
            default: throw new ImageflowAssertionFailed("default");
        }
    }

    public JsonNode ToJsonNode()
    {
        switch (_kind)
        {
            case ColorKind.Black: return new JsonObject() { { "black", (string?)null } };
            case ColorKind.Transparent: return new JsonObject() { { "transparent", (string?)null } };
            case ColorKind.Srgb: return new JsonObject() { { "srgb", new JsonObject() { { "hex", _srgb.ToHexUnprefixed() } } } };
            default: throw new ImageflowAssertionFailed("default");
        }
    }
}
