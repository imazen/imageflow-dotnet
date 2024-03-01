using System.Buffers;
using System.Text;
using System.Text.Json.Nodes;
using Imageflow.Internal.Helpers;
using Utf8JsonReader = System.Text.Json.Utf8JsonReader;

namespace Imageflow.Bindings
{
    [Obsolete("Use Imageflow.Bindings.IJsonResponse instead")]
    public interface IJsonResponseProvider : IDisposable
    {
        [Obsolete("Do not read the JSON directly; utilize helper methods instead")]
        Stream GetStream();
    }

    public interface IJsonResponse: IDisposable
    {
        public int ImageflowErrorCode { get; }
        public string CopyString();
        public JsonNode? Parse();
        public byte[] CopyBytes();
    }
    
    internal interface IJsonResponseSpanProvider : IDisposable
    {
        /// <summary>
        /// This data is only valid until the JsonResponse or JobContext is disposed.
        /// </summary>
        /// <returns></returns>
        ReadOnlySpan<byte> BorrowBytes();
    }

    internal sealed class MemoryJsonResponse : IJsonResponse, IJsonResponseSpanProvider
    {
        internal MemoryJsonResponse(int statusCode, ReadOnlyMemory<byte> memory)
        {
            ImageflowErrorCode = statusCode;
            _memory = memory;
        }
        
        private readonly ReadOnlyMemory<byte> _memory;

        public int ImageflowErrorCode { get; }

        public string CopyString()
        {
            return _memory.Span.Utf8ToString();
        }
        
        public JsonNode? Parse()
        {
            return _memory.Span.ParseJsonNode();
        }
        
        public byte[] CopyBytes()
        {
            return _memory.ToArray();
        }
        
        public ReadOnlySpan<byte> BorrowBytes()
        {
            return _memory.Span;
        }
        
        public void Dispose()
        {
            // no-op
        }
    }
    
    /// <summary>
    /// Readable even if the JobContext is in an error state.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    internal sealed class ImageflowJsonResponse : IJsonResponseProvider, IAssertReady, IJsonResponseSpanProvider, IJsonResponse
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly JsonResponseHandle _handle;
        private int? _statusCode;

        private JsonResponseHandle Handle
        {
            get
            {
                AssertReady();
                return _handle;
            }
        }

        internal ImageflowJsonResponse(JsonResponseHandle ptr)
        {
            ptr.ParentContext.AssertReady();
            ptr.AssertReady();
            _handle = ptr;
        }

        public void AssertReady()
        {
            _handle.ParentContext.AssertReady();
            if (!_handle.IsValid) throw new ObjectDisposedException("Imageflow JsonResponse");
        }

        private void Read(out int statusCode, out IntPtr utf8Buffer, out UIntPtr bufferSize)
        {
            AssertReady();
            NativeMethods.imageflow_json_response_read(_handle.ParentContext, Handle, out statusCode, out utf8Buffer,
                out bufferSize);
            AssertReady();
        }



        /// <summary>
        /// The stream will become invalid if the JsonResponse or JobContext is disposed.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Do not read the JSON directly; utilize helper methods instead")]
        public Stream GetStream()
        {
            Read(out var _, out var utf8Buffer, out var bufferSize);
            return new ImageflowUnmanagedReadStream(this, _handle, utf8Buffer, bufferSize);
        }
        
        public unsafe ReadOnlySpan<byte> BorrowBytes()
        {
            unsafe
            {
                Read(out var _, out var utf8Buffer, out var bufferSize);
                if (utf8Buffer == IntPtr.Zero) return ReadOnlySpan<byte>.Empty;
                if (bufferSize == UIntPtr.Zero) return ReadOnlySpan<byte>.Empty;
                if (bufferSize.ToUInt64() > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(bufferSize));

                return new ReadOnlySpan<byte>((void*)utf8Buffer, (int)bufferSize);
            }
        }
        
        public MemoryManager<byte> BorrowMemory()
        {
            Read(out var _, out var utf8Buffer, out var bufferSize);
            if (bufferSize.ToUInt64() > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            return SafeHandleMemoryManager.BorrowFromHandle(_handle, utf8Buffer, (uint)bufferSize);
        }


        public bool IsDisposed => !_handle.IsValid;

        public void Dispose()
        {
            _handle.Dispose();
        }
        
        
        public int GetStatusCode()
        {
            Read(out var statusCode, out var _, out var _);
            return statusCode;
        }
        
        public int ImageflowErrorCode => _statusCode ??= GetStatusCode();
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

    // ReSharper disable once InconsistentNaming
    public static class IJsonResponseProviderExtensions
    {
        internal static JsonNode? ParseJsonNode(this ReadOnlySpan<byte> buffer)
        {
    
            var reader3 = new Utf8JsonReader(buffer);
            return JsonNode.Parse(ref reader3);
        }

        
        // [Obsolete("Use DeserializeJsonNode() instead")]
        // public static T? Deserialize<T>(this IJsonResponseProvider p) where T : class
        // {
        //     using var readStream = p.GetStream();
        //     using var ms = new MemoryStream(readStream.CanSeek ? (int)readStream.Length : 0);
        //     readStream.CopyTo(ms);
        //     var allBytes = ms.ToArray();
        //     var options = new JsonSerializerOptions
        //     {
        //         PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        //     };
        //     var v = System.Text.Json.JsonSerializer.Deserialize<T>(allBytes, options);
        //     return v;
        //}
        //
        // [Obsolete("Use Deserialize<T> or DeserializeJsonNode() instead")]
        // public static dynamic? DeserializeDynamic(this IJsonResponseProvider p)
        // {
        //     using var reader = new StreamReader(p.GetStream(), Encoding.UTF8);
        //     //return JsonSerializer.Create().Deserialize(new JsonTextReader(reader));
        // }
        
        [Obsolete("Use IJsonResponse.Parse()? instead")]
        public static JsonNode? DeserializeJsonNode(this IJsonResponseProvider p)
        {
            if (p is IJsonResponseSpanProvider j)
            {
                var reader3 = new Utf8JsonReader(j.BorrowBytes());
                return JsonNode.Parse(ref reader3);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            using var reader = new StreamReader(p.GetStream(), Encoding.UTF8);
#pragma warning restore CS0618 // Type or member is obsolete
            var json = reader.ReadToEnd();
            var reader2 = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            return JsonNode.Parse(ref reader2);
        }
        

        [Obsolete("IJsonResponseProvider is deprecated; use IJsonResponse instead")]
        public static string GetString(this IJsonResponseProvider p)
        {
            if (p is IJsonResponseSpanProvider j)
            {
                return j.BorrowBytes().Utf8ToString();
            }
            using var s = new StreamReader(p.GetStream(), Encoding.UTF8);
            return s.ReadToEnd();
        }
    }
}
