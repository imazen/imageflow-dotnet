using System.Buffers;

using Imageflow.Internal.Helpers;

namespace Imageflow.Fluent;

public sealed class MemorySource : IAsyncMemorySource, IMemorySource
{
    private readonly ReadOnlyMemory<byte>? _borrowedMemory;
    private readonly IMemoryOwner<byte>? _ownedMemory;

    public static IAsyncMemorySource TakeOwnership(IMemoryOwner<byte> ownedMemory, MemoryLifetimePromise promise)
    {
        Argument.ThrowIfNull(ownedMemory);
        if (promise != MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource)
        {
            throw new ArgumentException(
                "MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource is required for TakeOwnership");
        }

        return new MemorySource(null, ownedMemory, promise);
    }

    private MemorySource(ReadOnlyMemory<byte>? borrowedMemory, IMemoryOwner<byte>? ownedMemory,
        MemoryLifetimePromise promise)
    {
        if (promise == MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource)
        {
            Argument.ThrowIfNull(ownedMemory);
            if (borrowedMemory.HasValue)
            {
                throw new ArgumentException(
                    "MemoryLifetimePromise.MemoryNowOwnedByMemorySource is not valid for BorrowMemory");
            }
        }

        if (!borrowedMemory.HasValue)
        {
            throw new ArgumentNullException(nameof(borrowedMemory));
        }

        Argument.ThrowIfNull(borrowedMemory);
        _borrowedMemory = borrowedMemory;
        _ownedMemory = ownedMemory;
    }

    /// <summary>
    /// Prefer MemorySource.Borrow() instead for clarity and more overloads
    /// </summary>
    /// <param name="bytes"></param>
    public MemorySource(byte[] bytes)
    {
        _borrowedMemory = new ReadOnlyMemory<byte>(bytes);
    }

    internal MemorySource(ArraySegment<byte> bytes)
    {
        _borrowedMemory = new ReadOnlyMemory<byte>(bytes.Array, bytes.Offset, bytes.Count);
    }

    public static IAsyncMemorySource Borrow(ReadOnlyMemory<byte> borrowedMemory, MemoryLifetimePromise promise)
    {
        if (promise == MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource)
        {
            throw new ArgumentException(
                "MemoryLifetimePromise.MemoryNowOwnedByMemorySource is not valid for Borrow");
        }

        return new MemorySource(borrowedMemory, null, promise);
    }

    public static IAsyncMemorySource Borrow(byte[] borrowedMemory, MemoryLifetimePromise promise)
        => Borrow(borrowedMemory.AsMemory(), promise);

    public static IAsyncMemorySource Borrow(byte[] borrowedMemory)
        => Borrow(borrowedMemory.AsMemory(), MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed);

    public static IAsyncMemorySource Borrow(ArraySegment<byte> borrowedMemory, MemoryLifetimePromise promise)
    {
        return new MemorySource(borrowedMemory, null, promise);
    }
    public static IAsyncMemorySource Borrow(byte[] borrowedMemory, int offset, int length, MemoryLifetimePromise promise)
    {
        var memory = new ReadOnlyMemory<byte>(borrowedMemory, offset, length);
        return Borrow(memory, promise);
    }

    public void Dispose()
    {
        _ownedMemory?.Dispose();
    }

    public ValueTask<ReadOnlyMemory<byte>> BorrowReadOnlyMemoryAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<ReadOnlyMemory<byte>>(_borrowedMemory ?? _ownedMemory!.Memory);
    }

    public ReadOnlyMemory<byte> BorrowReadOnlyMemory()
    {
        return _borrowedMemory ?? _ownedMemory!.Memory;
    }

    public bool AsyncPreferred => false;
}
