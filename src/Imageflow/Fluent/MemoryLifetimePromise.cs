namespace Imageflow.Fluent;

public enum MemoryLifetimePromise:byte
{
    /// <summary>
    /// The caller guarantees that the provided ReadOnlyMemory&lt;byte> will remain valid until after the job is disposed, across
    /// any async boundaries that occur. 
    /// </summary>
    MemoryValidUntilAfterJobDisposed,
    /// <summary>
    /// The caller guarantees that it has eliminated all other references to the IMemoryOwner, and that the IMemoryOwner will be disposed exclusively by Imageflow.
    /// </summary>
    MemoryOwnerDisposedByMemorySource,
    /// <summary>
    /// The caller guarantees that the provided ReadOnlyMemory is "owner-less" or owned by the garbage collector; no IMemoryOwner reference exists 
    /// </summary>
    MemoryIsOwnedByRuntime
}