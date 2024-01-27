namespace Imageflow.Bindings
{
    public class ImageInfo
    {
        private ImageInfo(dynamic imageInfo)
        {
            ImageWidth = imageInfo.image_width.Value;
            ImageHeight = imageInfo.image_height.Value;
            PreferredMimeType = imageInfo.preferred_mime_type.Value;
            PreferredExtension = imageInfo.preferred_extension.Value;
            FrameDecodesInto = Enum.Parse(typeof(Fluent.PixelFormat), imageInfo.frame_decodes_into.Value,
                true);
            
            
        }
        internal static ImageInfo FromDynamic(dynamic imageInfo)
        {
            return new ImageInfo(imageInfo);
        }

        public Fluent.PixelFormat FrameDecodesInto { get; private init; }
        public long ImageWidth { get; private init; }
        public long ImageHeight { get; private init; }
        public string PreferredMimeType { get; private init; }
        public string PreferredExtension { get; private init; }

    }
}
