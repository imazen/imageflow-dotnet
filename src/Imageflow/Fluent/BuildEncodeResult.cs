namespace Imageflow.Fluent;

public record BuildEncodeResult
{
    // internal BuildEncodeResult(string preferredMimeType, 
    //     string preferredExtension, int ioId, int width, int height, IOutputDestination destination)
    // {
    //     
    //     PreferredMimeType = preferredMimeType;
    //     PreferredExtension = preferredExtension;
    //     IoId = ioId;
    //     Width = width;
    //     Height = height;
    //     Destination = destination;
    // }

    internal BuildEncodeResult()
    {
    }
    // maps to "preferred_mime_type" in json
    public required string PreferredMimeType { get; init; }

    // maps to "preferred_extension" in json
    public required string PreferredExtension { get; init; }
    public required int IoId { get; init; }
    // maps to "w" in json
    public required int Width { get; init; }
    // maps to "h" in json
    public required int Height { get; init; }

    public required IOutputDestination Destination { get; init; }

    /// <summary>
    /// Forward-extensible annotation bag attached to this encode step.
    /// Populated when the dispatcher substituted the requested codec,
    /// surfaced a unit warning, or otherwise has non-error information
    /// about how this particular encode was served. <c>null</c> when the
    /// native side emitted no <c>annotations</c> object for this
    /// encode (the field is <c>skip_serializing_if = Option::is_none</c>
    /// on the wire).
    ///
    /// Maps to <c>"annotations"</c> in json.
    /// </summary>
    public EncodeAnnotations? Annotations { get; init; }

    /// <summary>
    /// If this Destination is a BytesDestination, returns the ArraySegment - otherwise null
    /// Returns the byte segment for the given output ID (if that output is a BytesDestination)
    /// </summary>
    public ArraySegment<byte>? TryGetBytes() => (Destination is BytesDestination d) ? d.GetBytes() : default;
}
// Width = er.w,
// Height = er.h,
// IoId = er.io_id,
// PreferredExtension = er.preferred_extension,
// PreferredMimeType = er.preferred_mime_type,
// Destination = outputs[(int)er.io_id.Value]
