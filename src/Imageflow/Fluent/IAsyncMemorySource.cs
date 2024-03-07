namespace Imageflow.Fluent;

public interface IAsyncMemorySource: IDisposable
{
    /// <summary>
    /// Implementations should return a reference to a byte array that (at least until the IMemorySource implementation is disposed) will remain valid, immutable, and pinnable.
    /// </summary>
    /// <returns></returns>
    ValueTask<ReadOnlyMemory<byte>> BorrowReadOnlyMemoryAsync(CancellationToken cancellationToken);
}

public interface IMemorySource: IDisposable
{
    /// <summary>
    /// Implementations should return a reference to a byte array that (at least until the IMemorySource implementation is disposed) will remain valid, immutable, and pinnable.
    /// </summary>
    /// <returns></returns>
    ReadOnlyMemory<byte> BorrowReadOnlyMemory();
    
    /// <summary>
    /// If true, implementations should prefer to use async methods over sync methods.
    /// </summary>
    bool AsyncPreferred { get; }
}