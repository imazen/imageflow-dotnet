using System;

namespace Imageflow.Bindings
{
    public class ImageInfo
    {
        private ImageInfo() { }
        internal static ImageInfo FromDynamic(dynamic imageInfo)
        {
            var info = new ImageInfo
            {
                ImageWidth = imageInfo.image_width.Value,
                ImageHeight = imageInfo.image_height.Value,
                PreferredMimeType = imageInfo.preferred_mime_type.Value,
                PreferredExtension = imageInfo.preferred_extension.Value,
                FrameDecodesInto = Enum.Parse(typeof(Imageflow.Fluent.PixelFormat), imageInfo.frame_decodes_into.Value,
                    true)
            };

            return info;
        }

        public Fluent.PixelFormat FrameDecodesInto { get; private set; }
        public long ImageWidth { get; private set; }
        public long ImageHeight { get; private set; }
        public string PreferredMimeType { get; private set; }
        public string PreferredExtension { get; private set; }

    }
}
