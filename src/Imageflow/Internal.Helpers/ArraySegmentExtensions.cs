namespace Imageflow.Internal.Helpers;

internal static class ArraySegmentExtensions
{
    internal static bool TryGetBufferSliceAfterPosition(this MemoryStream stream, out ArraySegment<byte> segment)
    {
        if (stream.TryGetBuffer(out var wholeStream) && wholeStream is { Count: > 0, Array: not null })
        {
            var remainingStreamLength = stream.Length - (int)stream.Position;
            if (remainingStreamLength > int.MaxValue || stream.Position > int.MaxValue)
                throw new OverflowException("Streams cannot exceed 2GB");
            if (stream.Position < 0) throw new InvalidOperationException("Stream position cannot be negative");
            segment = new ArraySegment<byte>(wholeStream.Array, (int)stream.Position, (int)remainingStreamLength);
            return true;
        }

        segment = default;
        return false;
    }

    internal static bool TryGetBufferSliceAllWrittenData(this MemoryStream stream, out ArraySegment<byte> segment)
    {
        if (stream.TryGetBuffer(out var wholeStream) && wholeStream is { Count: > 0, Array: not null })
        {
            if (stream.Length > int.MaxValue) throw new OverflowException("Streams cannot exceed 2GB");
            segment = new ArraySegment<byte>(wholeStream.Array, 0, (int)stream.Length);
            return true;
        }

        segment = default;
        return false;
    }

    internal static ReadOnlyMemory<byte> GetWrittenMemory(this MemoryStream stream)
    {
        return stream.TryGetBufferSliceAllWrittenData(out var segment) ? new ReadOnlyMemory<byte>(segment.Array, segment.Offset, segment.Count) : stream.ToArray();
    }
}
