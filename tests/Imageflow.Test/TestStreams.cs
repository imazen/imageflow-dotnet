namespace Imageflow.Test;
using System;
using System.IO;
using Microsoft.IO;
using Xunit;

public class MemoryStreamTests
{
    [Theory]
    [InlineData(typeof(MemoryStream))]
    [InlineData(typeof(RecyclableMemoryStream))]
    public void TryGetBuffer_ReturnsDataFromPositionZero(Type streamType)
    {
        // Arrange
        var manager = new RecyclableMemoryStreamManager();
        using var stream = streamType == typeof(MemoryStream) ? new MemoryStream() : manager.GetStream();
        byte[] data = { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, data.Length);
        stream.Position = 2; // Set a positive position in the stream

        // Act
        bool canGetBuffer = stream.TryGetBuffer(out ArraySegment<byte> buffer);

        // Assert
        Assert.True(canGetBuffer);
        Assert.Equal(0, buffer.Offset); // Data starts from position 0
        Assert.Equal(data.Length, buffer.Count); // Buffer length equals data written
        for (int i = 0; i < data.Length; i++)
        {
            Assert.Equal(data[i], buffer.Array![i + buffer.Offset]); // Data integrity check
        }
    }

    [Theory]
    [InlineData(typeof(MemoryStream))]
    [InlineData(typeof(RecyclableMemoryStream))]
    public void TryGetBuffer_DoesNotReturnMoreBytesThanWritten(Type streamType)
    {
        // Arrange
        var manager = new RecyclableMemoryStreamManager();
        using var stream = streamType == typeof(MemoryStream) ? new MemoryStream() : manager.GetStream();
        byte[] data = { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, data.Length);
        stream.Position = data.Length; // Move to the end to mimic more writes that don't happen

        // Act
        bool canGetBuffer = stream.TryGetBuffer(out ArraySegment<byte> buffer);

        // Assert
        Assert.True(canGetBuffer);
        Assert.Equal(data.Length, buffer.Count); // Ensures no more bytes are reported than actually written
    }
}

