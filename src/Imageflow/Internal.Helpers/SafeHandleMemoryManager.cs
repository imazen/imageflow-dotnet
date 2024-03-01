using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Imageflow.Internal.Helpers;

/// <summary>
/// Allows safely borrowing unmanaged memory that is exclusively owned and freed by a SafeHandle.
/// </summary>
internal sealed unsafe class SafeHandleMemoryManager : MemoryManager<byte>
{
    private readonly uint _length;
    private IntPtr _pointer;
    private readonly SafeHandle _outerHandle;
    private readonly SafeHandle? _innerHandle;
    
    /// <summary>
    /// Use this to create a MemoryManager that keeps a handle forced open until the MemoryManager is disposed.
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="pointer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static MemoryManager<byte> BorrowFromHandle(SafeHandle handle, IntPtr pointer, uint length)
    {
        return new SafeHandleMemoryManager(handle,null, pointer, length, true);
    }
    /// <summary>
    /// Use this to create a MemoryManager that keeps two handles forced open until the MemoryManager is disposed.
    /// The ref count is increased first on outerHandle, then on innerHandle.
    /// On disposal, the ref count is decreased first on innerHandle, then on outerHandle.
    /// </summary>
    /// <param name="outerHandle"></param>
    /// <param name="innerHandle"></param>
    /// <param name="pointer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static MemoryManager<byte> BorrowFromHandles(SafeHandle outerHandle, SafeHandle innerHandle, IntPtr pointer, uint length)
    {
        return new SafeHandleMemoryManager(outerHandle,innerHandle, pointer, length, true);
    }
    private SafeHandleMemoryManager(SafeHandle outerHandle, SafeHandle? innerHandle, IntPtr pointer, uint length, bool addGcPressure)
    {
        if (outerHandle.IsInvalid)
        {
            throw new ArgumentException("Handle is invalid", nameof(outerHandle));
        }
        if (pointer == IntPtr.Zero || pointer == new IntPtr(-1))
        {
            throw new ArgumentException("Pointer is invalid", nameof(pointer));
        }
        var addRefSucceeded = false;
        outerHandle.DangerousAddRef(ref addRefSucceeded);
        if (!addRefSucceeded)
        {
            throw new ArgumentException("SafeHandle.DangerousAddRef failed", nameof(outerHandle));
        }
        var addRefSucceeded2 = false;
        innerHandle?.DangerousAddRef(ref addRefSucceeded2);
        if (innerHandle != null && !addRefSucceeded2)
        {
            outerHandle.DangerousRelease();
            throw new ArgumentException("SafeHandle.DangerousAddRef failed for 2nd handle", nameof(innerHandle));
        }
        _outerHandle = outerHandle;
        _innerHandle = innerHandle;
        _pointer = pointer;
        _length = length;

        if (length != 0 && addGcPressure)
        {
            GC.AddMemoryPressure(length);
        }
    }

    public IntPtr Pointer => _pointer;

    public override Span<byte> GetSpan()
    {
        if (_length == 0)
        {
            return Span<byte>.Empty;
        }

        return new Span<byte>((void*)_pointer, (int)_length);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(elementIndex));
        }

        return new MemoryHandle(Unsafe.Add<byte>((void*)_pointer, elementIndex), default, this);
    }

    public override void Unpin()
    {
    }

    protected override void Dispose(bool disposing)
    {
        var pointer = Interlocked.Exchange(ref _pointer, IntPtr.Zero);
        if (pointer != IntPtr.Zero)
        {
            // Now release the handle(s)
            _innerHandle?.DangerousRelease();
            _outerHandle.DangerousRelease();
            
            if (_length != 0){
                GC.RemoveMemoryPressure(_length);
            }
        }
    }
    
    
}
