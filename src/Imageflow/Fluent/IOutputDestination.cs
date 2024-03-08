namespace Imageflow.Fluent
{
    public interface IOutputDestination : IDisposable
    {
        Task RequestCapacityAsync(int bytes);
        Task WriteAsync(ArraySegment<byte> bytes, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }



    // ReSharper disable once InconsistentNaming
}