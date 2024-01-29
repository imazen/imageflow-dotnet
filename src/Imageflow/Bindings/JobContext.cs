using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        
        [Obsolete("Use SerializeNode instead for AOT compatibility")]
        
        [RequiresUnreferencedCode("Use SerializeNode instead for AOT compatibility")]
        [RequiresDynamicCode("Use SerializeNode instead for AOT compatibility")]
        internal static byte[] SerializeToJson<T>(T obj){
            // Use System.Text.Json for serialization
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                #if NET8_0_OR_GREATER
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                #endif
            };
            var ms = new MemoryStream();
            var utf8JsonWriter = new Utf8JsonWriter(ms, new JsonWriterOptions
            {
                Indented = true
            });
            JsonSerializer.Serialize(utf8JsonWriter, obj, options);
            utf8JsonWriter.Flush();
            return ms.ToArray();
        }
        internal static byte[] SerializeNode(JsonNode node, bool indented = true){
            // Use System.Text.Json for serialization
            var ms = new MemoryStream();
            var utf8JsonWriter = new Utf8JsonWriter(ms, new JsonWriterOptions
            {
                Indented = indented
            });
            node.WriteTo(utf8JsonWriter);
            utf8JsonWriter.Flush();
            return ms.ToArray();
        }
        
        
        [Obsolete("Use SendMessage(JsonNode) instead for AOT compatibility")]
        [RequiresUnreferencedCode("Use SendMessage(string method, JsonNode message) instead for AOT compatibility")]
        [RequiresDynamicCode("Use SendMessage(string method, JsonNode message) instead for AOT compatibility")]
        public IJsonResponseProvider SendMessage<T>(string method, T message){
            AssertReady();
            return SendJsonBytes(method, SerializeToJson(message));
        }
        public IJsonResponseProvider SendMessage(string method, JsonNode message){
            AssertReady();
            return SendJsonBytes(method, SerializeNode(message));
        }

        [Obsolete("Use ExecuteJsonNode instead for AOT compatibility")]
        [RequiresUnreferencedCode("Use ExecuteJsonNode instead for AOT compatibility")]
        [RequiresDynamicCode("Use ExecuteJsonNode instead for AOT compatibility")]
        public IJsonResponseProvider Execute<T>(T message){
            AssertReady();
            return SendJsonBytes("v0.1/execute", SerializeToJson(message));
        }
        public IJsonResponseProvider ExecuteJsonNode(JsonNode message){
            AssertReady();
            return SendJsonBytes("v0.1/execute", SerializeNode(message));
        }
        
        internal IJsonResponseProvider Execute(byte[] utf8Message){
            AssertReady();
            return SendJsonBytes("v0.1/execute", utf8Message);
        }
        public ImageInfo GetImageInfo(int ioId)
        {
            AssertReady();
            using (var response = SendJsonBytes("v0.1/get_image_info", SerializeNode(new JsonObject(){ {"io_id", ioId} })))
            {
                var node = response.DeserializeJsonNode();
                if (node == null) throw new ImageflowAssertionFailed("get_image_info response is null");
                var responseObj = node.AsObject();
                if (responseObj == null) throw new ImageflowAssertionFailed("get_image_info response is not an object");
                if (responseObj.TryGetPropertyValue("success", out var successValue))
                {
                    if (successValue?.GetValue<bool>() != true)
                    {
                        throw ImageflowException.FromContext(Handle);
                    }
                    var dataValue = responseObj.TryGetPropertyValue("data", out var dataValueObj) ? dataValueObj : null;
                    if (dataValue == null) throw new ImageflowAssertionFailed("get_image_info response does not have a data property");
                    var imageInfoValue = (dataValue.AsObject().TryGetPropertyValue("image_info", out var imageInfoValueObj)) ? imageInfoValueObj : null;
                    
                    if (imageInfoValue == null) throw new ImageflowAssertionFailed("get_image_info response does not have an image_info property");
                    return ImageInfo.FromDynamic(imageInfoValue);
                }
                else
                {
                    throw new ImageflowAssertionFailed("get_image_info response does not have a success property");
                }
                
                //
                // var responseDynamic = response.DeserializeDynamic();
                // if (responseDynamic?.success.Value == true)
                // {
                //     return ImageInfo.FromDynamic(responseDynamic.data.image_info);
                // }
                // else
                // {
                //     throw ImageflowException.FromContext(this.Handle);
                // }
            }
        }

        public VersionInfo GetVersionInfo()
        {
            AssertReady();
            using (var response = SendJsonBytes("v1/get_version_info", SerializeNode(new JsonObject())))
            {
                var node = response.DeserializeJsonNode();
                if (node == null) throw new ImageflowAssertionFailed("get_version_info response is null");
                var responseObj = node.AsObject();
                if (responseObj == null) throw new ImageflowAssertionFailed("get_version_info response is not an object");
                if (responseObj.TryGetPropertyValue("success", out var successValue))
                {
                    if (successValue?.GetValue<bool>() != true)
                    {
                        throw ImageflowException.FromContext(Handle);
                    }
                    var dataValue = responseObj.TryGetPropertyValue("data", out var dataValueObj) ? dataValueObj : null;
                    if (dataValue == null) throw new ImageflowAssertionFailed("get_version_info response does not have a data property");
                    var versionInfoValue = (dataValue.AsObject().TryGetPropertyValue("version_info", out var versionInfoValueObj)) ? versionInfoValueObj : null;
                    
                    if (versionInfoValue == null) throw new ImageflowAssertionFailed("get_version_info response does not have an version_info property");
                    return VersionInfo.FromNode(versionInfoValue);
                }
                else
                {
                    throw new ImageflowAssertionFailed("get_version_info response does not have a success property");
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
                // check HasError, throw exception with our input JSON too
                if (HasError) throw ImageflowException.FromContext(Handle, 2048, "JSON:\n" + Encoding.UTF8.GetString(utf8Json));
                
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
            // var message = new
            // {
            //     framewise = new
            //     {
            //         steps = new object[]
            //         {
            //             new
            //             {
            //                 command_string = new
            //                 {
            //                     kind = "ir4",
            //                     value = commands,
            //                     decode = inputId,
            //                     encode = outputId
            //                 }
            //             }
            //         }
            //     }
            // };
            var message = new JsonObject()
            {
                {"framewise", new JsonObject()
                {
                    {"steps", new JsonArray()
                    {
                        (JsonNode)new JsonObject()
                        {
                            {"command_string", new JsonObject()
                            {
                                {"kind", "ir4"},
                                {"value", commands},
                                {"decode", inputId},
                                {"encode", outputId}
                            }}
                        }
                    }}
                }}
            };
                
            return ExecuteJsonNode( message);
        }



        internal void AddToDisposeQueue(IDisposable d)
        {
            _toDispose ??= new List<IDisposable>(1);
            _toDispose.Add(d);
        }
        
        public void AddInputBytes(int ioId, byte[] buffer)
        {
            AddInputBytes(ioId, buffer, 0, buffer.LongLength);
        }
        public void AddInputBytes(int ioId, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer), "Array cannot be null");
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
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer), "Array cannot be null");
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
