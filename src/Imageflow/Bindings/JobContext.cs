using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace Imageflow.Bindings
{

    public sealed class JobContext: CriticalFinalizerObject, IDisposable, IAssertReady
    {
        private readonly JobContextHandle _handle;
        private List<GCHandle>? _pinned;
        private List<IDisposable>? _toDispose;

        private JobContextHandle Handle
        {
            get
            {
                if (!_handle.IsValid)  throw new ObjectDisposedException("Imageflow JobContext");
                return _handle;
            }
        }
        private enum IoKind { InputBuffer, OutputBuffer}

        internal bool IsInput(int ioId) => _ioSet.ContainsKey(ioId) && _ioSet[ioId] == IoKind.InputBuffer;
        internal bool IsOutput(int ioId) => _ioSet.ContainsKey(ioId) && _ioSet[ioId] == IoKind.OutputBuffer;
        internal int LargestIoId => _ioSet.Keys.DefaultIfEmpty().Max();
        
        private readonly Dictionary<int, IoKind> _ioSet = new Dictionary<int, IoKind>();

        public JobContext()
        {
            _handle = new JobContextHandle();
        }

        private void AddPinnedData(GCHandle handle)
        {
            _pinned ??= new List<GCHandle>();
            _pinned.Add(handle);
        }

        public bool HasError => NativeMethods.imageflow_context_has_error(Handle);
        
        private static byte[] SerializeToJson<T>(T obj){
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))){
                JsonSerializer.Create().Serialize(writer, obj);
                writer.Flush(); //Required or no bytes appear
                return stream.ToArray();
            }
        }
        
        private static string SerializeToString<T>(T obj){
            using (var writer = new StringWriter()){
                JsonSerializer.Create().Serialize(writer, obj);
                writer.Flush(); //Required or no bytes appear
                return writer.ToString();
            }
        }
        
        public IJsonResponseProvider SendMessage<T>(string method, T message){
            AssertReady();
            return SendJsonBytes(method, JobContext.SerializeToJson(message));
        }
        
        public IJsonResponseProvider Execute<T>(T message){
            AssertReady();
            return SendJsonBytes("v0.1/execute", JobContext.SerializeToJson(message));
        }

        public ImageInfo GetImageInfo(int ioId)
        {
            AssertReady();
            using (var response = SendJsonBytes("v0.1/get_image_info", JobContext.SerializeToJson(new { io_id = ioId })))
            {
                var responseDynamic = response.DeserializeDynamic();
                if (responseDynamic?.success.Value == true)
                {
                    return ImageInfo.FromDynamic(responseDynamic.data.image_info);
                }
                else
                {
                    throw ImageflowException.FromContext(this.Handle);
                }
            }
        }

        public VersionInfo GetVersionInfo()
        {
            AssertReady();
            using (var response = SendJsonBytes("v1/get_version_info", JobContext.SerializeToJson(new { })))
            {
                var responseDynamic = response.DeserializeDynamic();
                if (responseDynamic?.success.Value == true)
                {
                    return VersionInfo.FromDynamic(responseDynamic.data.version_info);
                }
                else
                {
                    throw ImageflowException.FromContext(this.Handle);
                }
            }
        }
        
        public IJsonResponseProvider SendJsonBytes(string method, byte[] utf8Json)
        {
            AssertReady();
            var pinnedJson = GCHandle.Alloc(utf8Json, GCHandleType.Pinned);
            var methodPinned = GCHandle.Alloc(Encoding.ASCII.GetBytes($"{method}\0"), GCHandleType.Pinned);
            try
            {
                AssertReady();
                var ptr = NativeMethods.imageflow_context_send_json(Handle, methodPinned.AddrOfPinnedObject(), pinnedJson.AddrOfPinnedObject(),
                    new UIntPtr((ulong) utf8Json.LongLength));
                AssertReady();
                return new JsonResponse(new JsonResponseHandle(_handle, ptr));
            }
            finally
            {
                pinnedJson.Free();
                methodPinned.Free();
            }
        }
        
        public void AssertReady()
        {
            if (!_handle.IsValid)  throw new ObjectDisposedException("Imageflow JobContext");
            if (HasError) throw ImageflowException.FromContext(Handle);
        }
        
        public IJsonResponseProvider ExecuteImageResizer4CommandString( int inputId, int outputId, string commands)
        {
            var message = new
            {
                framewise = new
                {
                    steps = new object[]
                    {
                        new
                        {
                            command_string = new
                            {
                                kind = "ir4",
                                value = commands,
                                decode = inputId,
                                encode = outputId
                            }
                        }
                    }
                }
            };
                
            return Execute( message);
        }



        internal void AddToDisposeQueue(IDisposable d)
        {
            if (_toDispose == null) _toDispose = new List<IDisposable>(1);
            _toDispose.Add(d);
        }
        
        public void AddInputBytes(int ioId, byte[] buffer)
        {
            AddInputBytes(ioId, buffer, 0, buffer.LongLength);
        }
        public void AddInputBytes(int ioId, ArraySegment<byte> buffer)
        {
            AddInputBytes(ioId, buffer.Array, buffer.Offset, buffer.Count);
        }
        public void AddInputBytes(int ioId, byte[] buffer, long offset, long count)
        {
            AssertReady();
            if (offset < 0 || offset > buffer.LongLength - 1) throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within array bounds");
            if (count < 0 || offset + count > buffer.LongLength) throw new ArgumentOutOfRangeException(nameof(count), count, "offset + count must be within array bounds. count cannot be negative");
            if (ContainsIoId(ioId)) throw new ArgumentException($"ioId {ioId} already in use", nameof(ioId));
            
            var fixedBytes = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var addr = new IntPtr(fixedBytes.AddrOfPinnedObject().ToInt64() + offset);

                if (!NativeMethods.imageflow_context_add_input_buffer(Handle, ioId, addr, new UIntPtr((ulong) count),
                    NativeMethods.Lifetime.OutlivesFunctionCall))
                {
                    AssertReady();
                    throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
                }
                _ioSet.Add(ioId, IoKind.InputBuffer);
            } finally{
                fixedBytes.Free();
            }
        }


        public void AddInputBytesPinned(int ioId, byte[] buffer)
        {
            AddInputBytesPinned(ioId, buffer, 0, buffer.LongLength);
        }
        public void AddInputBytesPinned(int ioId, ArraySegment<byte> buffer)
        {
            AddInputBytesPinned(ioId, buffer.Array, buffer.Offset, buffer.Count);
        }
        public void AddInputBytesPinned(int ioId, byte[] buffer, long offset, long count)
        {
            AssertReady();
            if (offset < 0 || offset > buffer.LongLength - 1)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within array bounds");
            if (count < 0 || offset + count > buffer.LongLength)
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    "offset + count must be within array bounds. count cannot be negative");
            if (ContainsIoId(ioId)) throw new ArgumentException($"ioId {ioId} already in use", nameof(ioId));

            var fixedBytes = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            AddPinnedData(fixedBytes);

            var addr = new IntPtr(fixedBytes.AddrOfPinnedObject().ToInt64() + offset);
            if (!NativeMethods.imageflow_context_add_input_buffer(Handle, ioId, addr, new UIntPtr((ulong) count),
                NativeMethods.Lifetime.OutlivesContext))
            {
                AssertReady();
                throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
            }
            _ioSet.Add(ioId, IoKind.InputBuffer);
        }


        public void AddOutputBuffer(int ioId)
        {
            AssertReady();
            if (ContainsIoId(ioId)) throw new ArgumentException($"ioId {ioId} already in use", nameof(ioId));
            if (!NativeMethods.imageflow_context_add_output_buffer(Handle, ioId))
            {
                AssertReady();
                throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
            }
            _ioSet.Add(ioId, IoKind.OutputBuffer);
        }

        public bool ContainsIoId(int ioId) => _ioSet.ContainsKey(ioId);
        
        /// <summary>
        /// Will raise an unrecoverable exception if this is not an output buffer.
        /// Stream is not valid after the JobContext is disposed
        /// </summary>
        /// <returns></returns>
        public Stream GetOutputBuffer(int ioId)
        {
            if (!_ioSet.ContainsKey(ioId) || _ioSet[ioId] != IoKind.OutputBuffer)
            {
                throw new ArgumentException($"ioId {ioId} does not correspond to an output buffer", nameof(ioId));
            }
            AssertReady();
            if (!NativeMethods.imageflow_context_get_output_buffer_by_id(Handle, ioId, out var buffer,
                out var bufferSize))
            {
                AssertReady();
                throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
            }
            return new ImageflowUnmanagedReadStream(this, buffer, bufferSize);
            
        }

        public bool IsDisposed => !_handle.IsValid;
        public void Dispose()
        {
            if (IsDisposed) throw new ObjectDisposedException("Imageflow JobContext");
            
            // Do not allocate or throw exceptions unless (disposing)
            Exception? e = null;
            try
            {
                e = _handle.DisposeAllowingException();
            }
            finally
            {
                UnpinAll();
                
                //Dispose all managed data held for context lifetime
                if (_toDispose != null)
                {
                    foreach (var active in _toDispose)
                        active.Dispose();
                    _toDispose = null;
                }
                GC.SuppressFinalize(this);
                if (e != null) throw e;
            } 
        }

        private void UnpinAll()
        {
            //Unpin GCHandles
            if (_pinned == null) return;
            
            foreach (var active in _pinned)
            {
                if (active.IsAllocated) active.Free();
            }
            _pinned = null;
        }

        ~JobContext()
        {
            //Don't dispose managed objects; they have their own finalizers
            UnpinAll();
        }
        
    }
}
