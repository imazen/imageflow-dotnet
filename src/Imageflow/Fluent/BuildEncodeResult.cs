namespace Imageflow.Fluent
{
    public class BuildEncodeResult
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
        public required int IoId { get; init;}
        // maps to "w" in json
        public required int Width { get; init;}
        // maps to "h" in json
        public required int Height { get; init;}
        
        public required IOutputDestination Destination { get; init;}
        
        /// <summary>
        /// If this Destination is a BytesDestination, returns the ArraySegment - otherwise null
        /// Returns the byte segment for the given output ID (if that output is a BytesDestination)
        /// </summary>
        public ArraySegment<byte>? TryGetBytes() => (Destination is BytesDestination d) ? d.GetBytes() : (ArraySegment<byte>?)null;
    }
    // Width = er.w,
    // Height = er.h,
    // IoId = er.io_id,
    // PreferredExtension = er.preferred_extension,
    // PreferredMimeType = er.preferred_mime_type,
    // Destination = outputs[(int)er.io_id.Value]
}