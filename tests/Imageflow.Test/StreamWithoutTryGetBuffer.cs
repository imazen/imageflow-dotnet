// A MemoryStream subclass that returns false for TryGetBuffer but is otherwise identical to MemoryStream
namespace Imageflow.Test;

public class StreamWithoutTryGetBuffer : MemoryStream
{
    public StreamWithoutTryGetBuffer() { }

    public StreamWithoutTryGetBuffer(byte[] buffer) : base(buffer) { }

    public StreamWithoutTryGetBuffer(byte[] buffer, bool writable) : base(buffer, writable) { }

    public StreamWithoutTryGetBuffer(byte[] buffer, int index, int count) : base(buffer, index, count) { }

    public StreamWithoutTryGetBuffer(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable) { }

    public StreamWithoutTryGetBuffer(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) : base(buffer, index, count, writable, publiclyVisible) { }

    public StreamWithoutTryGetBuffer(int capacity) : base(capacity) { }

    public override bool TryGetBuffer(out ArraySegment<byte> buffer)
    {
        buffer = default;
        return false;
    }
}
