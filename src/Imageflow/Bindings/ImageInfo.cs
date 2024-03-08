using System.Text.Json.Nodes;

using Imageflow.Fluent;

namespace Imageflow.Bindings;

public class ImageInfo
{
    private ImageInfo(JsonNode imageInfo)
    {
        var obj = imageInfo.AsObject();
        // ImageWidth = imageInfo.image_width.Value;
        // ImageHeight = imageInfo.image_height.Value;
        // PreferredMimeType = imageInfo.preferred_mime_type.Value;
        // PreferredExtension = imageInfo.preferred_extension.Value;
        // FrameDecodesInto = Enum.Parse(typeof(Fluent.PixelFormat), imageInfo.frame_decodes_into.Value,
        //     true);
        const string widthMsg = "Imageflow get_image_info responded with null image_info.image_width";
        ImageWidth = obj.TryGetPropertyValue("image_width", out var imageWidthValue)
            ? imageWidthValue?.GetValue<long>() ?? throw new ImageflowAssertionFailed(widthMsg)
            : throw new ImageflowAssertionFailed(widthMsg);

        const string heightMsg = "Imageflow get_image_info responded with null image_info.image_height";
        ImageHeight = obj.TryGetPropertyValue("image_height", out var imageHeightValue)
            ? imageHeightValue?.GetValue<long>() ?? throw new ImageflowAssertionFailed(heightMsg)
            : throw new ImageflowAssertionFailed(heightMsg);

        const string mimeMsg = "Imageflow get_image_info responded with null image_info.preferred_mime_type";
        PreferredMimeType = obj.TryGetPropertyValue("preferred_mime_type", out var preferredMimeTypeValue)
            ? preferredMimeTypeValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(mimeMsg)
            : throw new ImageflowAssertionFailed(mimeMsg);

        const string extMsg = "Imageflow get_image_info responded with null image_info.preferred_extension";
        PreferredExtension = obj.TryGetPropertyValue("preferred_extension", out var preferredExtensionValue)
            ? preferredExtensionValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(extMsg)
            : throw new ImageflowAssertionFailed(extMsg);

        const string frameMsg = "Imageflow get_image_info responded with null image_info.frame_decodes_into";
        FrameDecodesInto = obj.TryGetPropertyValue("frame_decodes_into", out var frameDecodesIntoValue)
            ? PixelFormatParser.Parse(frameDecodesIntoValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(frameMsg))
            : throw new ImageflowAssertionFailed(frameMsg);

    }
    internal static ImageInfo FromDynamic(JsonNode imageInfo)
    {
        return new ImageInfo(imageInfo);
    }

    public PixelFormat FrameDecodesInto { get; private init; }
    public long ImageWidth { get; private init; }
    public long ImageHeight { get; private init; }
    public string PreferredMimeType { get; private init; }
    public string PreferredExtension { get; private init; }

}
