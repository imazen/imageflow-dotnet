using System.Text.Json.Nodes;
using Imageflow.Bindings;

namespace Imageflow.Internal.Helpers.Unused;

#pragma warning disable CS0618
internal class StreamJsonSpanProvider : IJsonResponseProvider, IJsonResponseSpanProvider, IJsonResponse
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly MemoryStream _ms;

    public StreamJsonSpanProvider(int statusCode, MemoryStream ms)
    {
        ImageflowErrorCode = statusCode;
        _ms = ms;
    }
    public void Dispose() => _ms.Dispose();
    public Stream GetStream() => _ms;
    public ReadOnlySpan<byte> BorrowBytes()
    {
        return _ms.TryGetBufferSliceAllWrittenData(out var slice) ? slice : _ms.ToArray();
    }

    public int ImageflowErrorCode { get; }
    public string CopyString()
    {
        return BorrowBytes().Utf8ToString();
    }

    public JsonNode? Parse()
    {
        return BorrowBytes().ParseJsonNode();
    }

    public byte[] CopyBytes()
    {
        return BorrowBytes().ToArray();
    }
}
