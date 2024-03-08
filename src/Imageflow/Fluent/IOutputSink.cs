namespace Imageflow.Fluent;

internal interface IOutputSink : IDisposable
{
    void RequestCapacity(int bytes);
    void Write(ReadOnlySpan<byte> data);
    void Flush();
}

internal interface IAsyncOutputSink : IDisposable
{
    ValueTask FastRequestCapacityAsync(int bytes);
    ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    ValueTask FastFlushAsync(CancellationToken cancellationToken);
}

internal static class OutputSinkExtensions
{
    public static async ValueTask WriteAllAsync(this IAsyncOutputSink sink, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await sink.FastRequestCapacityAsync(data.Length).ConfigureAwait(false);
        await sink.FastWriteAsync(data, cancellationToken).ConfigureAwait(false);
        await sink.FastFlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
