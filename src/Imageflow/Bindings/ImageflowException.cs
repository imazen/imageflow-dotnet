using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Imageflow.Bindings
{
    public class ImageflowException : Exception
    {
        private const int MaxBufferSize = 8096 * 4;

        internal ImageflowException(string message) : base(message)
        {
            
        }

        private enum ErrorFetchResult
        {
            BufferTooSmall,
            ContextInvalid,
            NoError,
            Success
        }
        private static ErrorFetchResult TryGetErrorString(JobContextHandle c, ulong bufferSize, out string message)
        {
            message = null;
            if (c.IsClosed || c.IsInvalid)
            {
                return ErrorFetchResult.ContextInvalid;
            }
            if (!NativeMethods.imageflow_context_has_error(c))
            {
                return ErrorFetchResult.NoError;
            }
            var buffer = new byte[bufferSize];
            var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            
            try
            {
                var everythingWritten = NativeMethods.imageflow_context_error_write_to_buffer(c,
                    pinned.AddrOfPinnedObject(), new UIntPtr((ulong) buffer.LongLength), out var bytesWritten);

                message = bytesWritten.ToUInt64() > 0 
                    ? Encoding.UTF8.GetString(buffer, 0, (int)Math.Min(bytesWritten.ToUInt64(), bufferSize)) 
                    : "";

                return everythingWritten ? ErrorFetchResult.Success : ErrorFetchResult.BufferTooSmall;
            }
            finally
            {
                pinned.Free();
            }
        }
        
        internal static ImageflowException FromContext(JobContextHandle c, ulong defaultBufferSize = 2048)
        {
            var result = ErrorFetchResult.BufferTooSmall;
            for (var bufferSize = defaultBufferSize; bufferSize < MaxBufferSize; bufferSize *= 2)
            {
                result = TryGetErrorString(c, bufferSize, out var message);
                switch (result)
                {
                    case ErrorFetchResult.Success:
                        return new ImageflowException(message);
                    case ErrorFetchResult.ContextInvalid:
                        return null;
                    case ErrorFetchResult.NoError:
                        return null;
                    case ErrorFetchResult.BufferTooSmall: break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            throw new ImageflowAssertionFailed(
                    $"Imageflow error and stacktrace exceeded {MaxBufferSize} bytes");
        }
    }
}
