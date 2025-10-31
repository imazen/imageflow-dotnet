namespace Imageflow.Test;

using System;
using System.IO;

public class NonSeekableReadStream : Stream
{
    private readonly byte[] data;
    private long position; // =0 Current position within the data

    public NonSeekableReadStream(byte[] dataSource)
    {
        data = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => data.Length;

    public override long Position
    {
        get => throw new NotSupportedException("Seeking not supported.");
        set => throw new NotSupportedException("Seeking not supported.");
    }

    public override void Flush()
    {
        // No-op for read-only stream
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "offset or count is negative.");
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count is negative.");
        if (buffer.Length - offset < count) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");

        int available = data.Length - (int)position;
        if (available <= 0) return 0; // End of stream

        int toCopy = Math.Min(available, count);
        Array.Copy(data, position, buffer, offset, toCopy);
        position += toCopy;
        return toCopy;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking not supported.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting length not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Writing not supported.");
    }
}
