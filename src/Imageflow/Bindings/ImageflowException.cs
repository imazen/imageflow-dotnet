using System.Runtime.InteropServices;
using System.Text;

namespace Imageflow.Bindings;

public class ImageflowException : Exception
{
    private const ulong MaxBufferSize = 8096 * 4;

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
    private static ErrorFetchResult TryGetErrorString(JobContextHandle c, ulong bufferSize, out string? message)
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
                pinned.AddrOfPinnedObject(), new UIntPtr((ulong)buffer.LongLength), out var bytesWritten);

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

    internal static Exception FromContext(JobContextHandle c, ulong defaultBufferSize = 2048, string? additionalInfo = null)
    {
        string? lastMessage = null;
        for (var bufferSize = defaultBufferSize; bufferSize <= MaxBufferSize; bufferSize *= 2)
        {
            var result = TryGetErrorString(c, bufferSize, out var message);
            switch (result)
            {
                case ErrorFetchResult.Success:
                    return WrapMessage(message, additionalInfo);
                case ErrorFetchResult.ContextInvalid:
                    return new ImageflowException("Imageflow context (JobContextHandle) is invalid");
                case ErrorFetchResult.NoError:
                    return new ImageflowException("Imageflow context has no error stored; cannot fetch error message");
                case ErrorFetchResult.BufferTooSmall:
                    lastMessage = message;
                    break;
                default:
                    throw new NotImplementedException($"Unknown error fetching error: {result}");
            }
        }

        // Return what we have with a truncation marker
        return WrapMessage((lastMessage ?? "Unknown Imageflow Error") + "\n[..truncated]", additionalInfo);
    }

    private static Exception WrapMessage(string? message, string? additionalInfo)
    {
        var fullMessage = (message ?? "Unknown Imageflow Error") + (additionalInfo != null ? $"\nAdditional info: {additionalInfo}" : "");

        if (message != null && message.StartsWith("OperationCancelled", StringComparison.Ordinal))
        {
            return new OperationCanceledException(fullMessage);
        }

        // Recognize the killbits structured envelope so callers can catch a
        // typed exception with the net-support grid attached.
        if (message != null)
        {
            var killbits = KillbitsDeniedException.TryParse(fullMessage);
            if (killbits != null)
            {
                return killbits;
            }
        }

        return new ImageflowException(fullMessage);
    }
}
