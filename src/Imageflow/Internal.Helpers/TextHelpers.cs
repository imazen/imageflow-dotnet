using System.Text;

namespace Imageflow.Internal.Helpers;

internal static class TextHelpers
{
    internal static string Utf8ToString(this ReadOnlySpan<byte> utf8)
    {
#if NETSTANDARD2_1_OR_GREATER
        return Encoding.UTF8.GetString(utf8);
#else
        unsafe
        {
            fixed (byte* ptr = utf8)
            {
                return Encoding.UTF8.GetString(ptr, utf8.Length);
            }
        }
#endif
    }

    /// <summary>
    /// Returns false if the text contains non-ASCII characters or nulls, or if the buffer is too small (it should be greater than the number of chars).
    /// </summary>
    /// <param name="text"></param>
    /// <param name="buffer"></param>
    /// <param name="resultingBufferSlice"></param>
    /// <returns></returns>
    internal static bool TryEncodeAsciiNullTerminated(ReadOnlySpan<char> text, Span<byte> buffer, out ReadOnlySpan<byte> resultingBufferSlice)
    {
        if (TryEncodeAsciiNullTerminatedIntoBuffer(text, buffer, out var bytesUsed))
        {
            resultingBufferSlice = buffer.Slice(0, bytesUsed);
            return true;
        }
        resultingBufferSlice = ReadOnlySpan<byte>.Empty;
        return false;
    }

    internal static bool TryEncodeAsciiNullTerminatedIntoBuffer(ReadOnlySpan<char> text, Span<byte> buffer, out int bytesUsed)
    {
        if (buffer.Length < text.Length + 1)
        {
            bytesUsed = 0;
            return false;
        }
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] > 127 || text[i] <= 0)
            {
                bytesUsed = 0;
                return false;
            }
            buffer[i] = (byte)text[i];
        }
        buffer[^1] = 0;
        bytesUsed = text.Length + 1;
        return true;
    }



}
