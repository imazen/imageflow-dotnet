using System;

namespace Imageflow.Fluent
{
    public class BuildEncodeResult
    {
        public string PreferredMimeType { get; internal set; }
        public string PreferredExtension { get;internal set; }
        public int IoId { get; internal set;}
        public int Width { get; internal set;}
        public int Height { get; internal set;}
        
        public IOutputDestination Destination { get; internal set;}
        
        /// <summary>
        /// If this Destination is a BytesDestination, returns the ArraySegment - otherwise null
        /// Returns the byte segment for the given output ID (if that output is a BytesDestination)
        /// </summary>
        public ArraySegment<byte>? TryGetBytes() => (Destination is BytesDestination d) ? d.GetBytes() : (ArraySegment<byte>?)null;
    }

}