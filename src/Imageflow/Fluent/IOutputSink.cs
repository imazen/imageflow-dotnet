namespace Imageflow.Fluent;

internal interface IOutputSink : IDisposable
{
    void RequestCapacity(int bytes);
    void Write(ReadOnlySpan<byte> data);
    /// <summary>
    /// Called after writes are complete - it is invalid to call any other write/flush/requestCapacity methods after this.
    /// No need to call Flush before this.
    /// </summary>
    void Finished();
}

internal interface IAsyncOutputSink : IDisposable
{
    ValueTask FastRequestCapacityAsync(int bytes);
    ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Called after writes are complete - it is invalid to call any other write/flush/requestCapacity methods after this.
    /// No need to call Flush before this.
    /// </summary>
    ValueTask FinishedAsync(CancellationToken cancellationToken);
}

internal static class OutputSinkExtensions
{
    public static async ValueTask WriteAllAsync(this IAsyncOutputSink sink, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await sink.FastRequestCapacityAsync(data.Length).ConfigureAwait(false);
        await sink.FastWriteAsync(data, cancellationToken).ConfigureAwait(false);
        await sink.FinishedAsync(cancellationToken).ConfigureAwait(false);
    }
}
