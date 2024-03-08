using System.Buffers;
using System.Runtime.CompilerServices;

namespace Imageflow.Internal.Helpers.Unused;

internal unsafe class UnmanagedMemoryManager : MemoryManager<byte>
{
    private readonly uint _length;
    private IntPtr _pointer;
    private readonly Action<IntPtr, uint> _onDispose;

    public UnmanagedMemoryManager(IntPtr pointer, uint length, Action<IntPtr, uint> onDispose)
    {
        _onDispose = onDispose;
        _pointer = pointer;
        _length = length;

        if (length != 0)
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
        if (pointer != IntPtr.Zero && _length != 0)
        {
            _onDispose(pointer, _length);
            GC.RemoveMemoryPressure(_length);
        }
    }
}
