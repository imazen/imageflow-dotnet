namespace Imageflow.Fluent
{
    public class BuildDecodeResult
    {
        public string? PreferredMimeType { get; internal set; }
        public string? PreferredExtension { get;internal set; }
        public int IoId { get; internal set;}
        public int Width { get; internal set;}
        public int Height { get; internal set;}

    }

}