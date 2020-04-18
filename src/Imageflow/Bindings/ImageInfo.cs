using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Bindings
{
    public class ImageInfo
    {
        internal ImageInfo() { }
        internal static ImageInfo FromDynamic(dynamic imageInfo)
        {
            var info = new ImageInfo();

            info.ImageWidth = imageInfo.image_width.Value;
            info.ImageHeight = imageInfo.image_width.Value;
            info.PreferredMimeType = imageInfo.preferred_mime_type.Value;
            info.PreferredExtension = imageInfo.preferred_extension.Value;
            info.FrameDecodesInto = Enum.Parse(typeof(Imageflow.Fluent.PixelFormat), imageInfo.frame_decodes_into.Value, true);
            return info;
        }

        public Imageflow.Fluent.PixelFormat FrameDecodesInto { get; private set; }
        public long ImageWidth { get; private set; }
        public long ImageHeight { get; private set; }
        public string PreferredMimeType { get; private set; }
        public string PreferredExtension { get; private set; }

    }
}
