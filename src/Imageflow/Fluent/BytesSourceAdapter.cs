namespace Imageflow.Fluent;

#pragma warning disable CS0618 // Type or member is obsolete
public sealed class BytesSourceAdapter(IBytesSource source) : IAsyncMemorySource, IMemorySource
#pragma warning restore CS0618 // Type or member is obsolete
{
    public void Dispose()
    {
        source.Dispose();
    }

    public async ValueTask<ReadOnlyMemory<byte>> BorrowReadOnlyMemoryAsync(CancellationToken cancellationToken)
    {
        var bytes = await source.GetBytesAsync(cancellationToken).ConfigureAwait(false);
        return new ReadOnlyMemory<byte>(bytes.Array, bytes.Offset, bytes.Count);
    }

    public ReadOnlyMemory<byte> BorrowReadOnlyMemory()
    {
        var bytes = source.GetBytesAsync(default).Result;
        return new ReadOnlyMemory<byte>(bytes.Array, bytes.Offset, bytes.Count);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public bool AsyncPreferred => source is not (BytesSource or StreamSource { AsyncPreferred: true });
#pragma warning restore CS0618 // Type or member is obsolete
}
