using System.Runtime.InteropServices;

using Imageflow.Internal.Helpers;

namespace Imageflow.Bindings;

/// <summary>
/// An UnmanagedMemoryStream that checks that the underlying Imageflow context isn't in a disposed or errored state
/// </summary>
/// <inheritdoc cref="UnmanagedMemoryStream"/>\
///
[Obsolete("This class will be removed in a future version; it has no benefit over Memory<byte> and IMemoryOwner<byte>")]
public sealed class ImageflowUnmanagedReadStream : UnmanagedMemoryStream
{
    private readonly IAssertReady _underlying;
    private SafeHandle? _handle;
    private int _handleReferenced;

    internal unsafe ImageflowUnmanagedReadStream(IAssertReady underlying, SafeHandle handle, IntPtr buffer, UIntPtr length) : base((byte*)buffer.ToPointer(), (long)length.ToUInt64(), (long)length.ToUInt64(), FileAccess.Read)
    {
        _underlying = underlying;
        _handle = handle;
        var addRefSucceeded = false;
        _handle.DangerousAddRef(ref addRefSucceeded);
        _handleReferenced = addRefSucceeded ? 1 : 0;
        if (!addRefSucceeded)
        {
            throw new ArgumentException("SafeHandle.DangerousAddRef failed", nameof(handle));
        }
    }

    private void CheckSafe()
    {
        _underlying.AssertReady();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        CheckSafe();
        return base.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        CheckSafe();
        return base.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override int ReadByte()
    {
        CheckSafe();
        return base.ReadByte();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        CheckSafe();
        return base.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        CheckSafe();
        return base.EndRead(asyncResult);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        CheckSafe();
        return base.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        // Interlocked exchange to only release ref once
        if (1 == Interlocked.Exchange(ref _handleReferenced, 0))
        {
            _handle?.DangerousRelease();
            _handle = null;
        }
    }
}
