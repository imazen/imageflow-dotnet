// using System.Buffers;
// using System.Runtime.InteropServices;
//
// namespace Imageflow.Fluent;
//
//
// internal class OutputDestinationToSinkAdapter: IOutputSink, IAsyncOutputSink
// {
//     private readonly IOutputDestination _dest;
//     private readonly bool _disposeUnderlying;
//         
//
//     public OutputDestinationToSinkAdapter(IOutputDestination dest, bool disposeUnderlying)
//     {
//         _dest = dest;
//         _disposeUnderlying = disposeUnderlying;
//             
//     }
//     public async ValueTask FastRequestCapacityAsync(int bytes)
//     {
//         await _dest.RequestCapacityAsync(bytes);
//     }
//
//     public async ValueTask FastWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
//     {
//         if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment))
//         {
//             await _dest.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
//             return;
//         }
//         
//         var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920,data.Length));
//         try
//         {
//             for (int i = 0; i < data.Length; i += rent.Length)
//             {
//                 int len = Math.Min(rent.Length, data.Length - i);
//                 data.Span.Slice(i, len).CopyTo(rent);
//                 await _dest.WriteAsync(new ArraySegment<byte>(rent, 0, len), cancellationToken).ConfigureAwait(false);
//             }
//         }
//         finally
//         {
//             ArrayPool<byte>.Shared.Return(rent);
//         }
//             
//     }
//     public void Write(ReadOnlySpan<byte> data)
//     {
//
//         var rent = ArrayPool<byte>.Shared.Rent(Math.Min(81920,data.Length));
//         try
//         {
//             for (int i = 0; i < data.Length; i += rent.Length)
//             {
//                 int len = Math.Min(rent.Length, data.Length - i);
//                 data.Slice(i, len).CopyTo(rent);
//                 _dest.WriteAsync(new ArraySegment<byte>(rent, 0, len), default).Wait();
//             }
//         }
//         finally
//         {
//             ArrayPool<byte>.Shared.Return(rent);
//         }
//     }
//
//     public async ValueTask FastFlushAsync(CancellationToken cancellationToken)
//     {
//         await _dest.FlushAsync(cancellationToken);
//     }
//
//     public void RequestCapacity(int bytes)
//     {
//         _dest.RequestCapacityAsync(bytes).Wait();
//     }
//
//    
//     public void Flush()
//     {
//         _dest.FlushAsync(default).Wait();
//     }
//
//     public void Dispose()
//     {
//         if (_disposeUnderlying) _dest?.Dispose();
//     }
// }
//
