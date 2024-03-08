namespace Imageflow.Fluent;

public class BuildDecodeResult
{
    public string? PreferredMimeType { get; internal init; }
    public string? PreferredExtension { get; internal init; }
    public int IoId { get; internal init; }
    public int Width { get; internal init; }
    public int Height { get; internal init; }

}
