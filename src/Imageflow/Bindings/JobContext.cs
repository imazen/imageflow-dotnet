using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Text.Json.Nodes;

using Imageflow.Fluent;
using Imageflow.Internal.Helpers;

namespace Imageflow.Bindings
{

    public sealed class JobContext : CriticalFinalizerObject, IDisposable, IAssertReady
    {
        private readonly JobContextHandle _handle;
        private List<MemoryHandle>? _pinnedMemory;
        private List<IDisposable>? _toDispose;

        private JobContextHandle Handle
        {
            get
            {
                if (!_handle.IsValid) throw new ObjectDisposedException("Imageflow JobContext");
                return _handle;
            }
        }

        private enum IoKind
        {
            InputBuffer,
            OutputBuffer
        }

        internal bool IsInput(int ioId) => _ioSet.ContainsKey(ioId) && _ioSet[ioId] == IoKind.InputBuffer;
        internal bool IsOutput(int ioId) => _ioSet.ContainsKey(ioId) && _ioSet[ioId] == IoKind.OutputBuffer;
        internal int LargestIoId => _ioSet.Keys.DefaultIfEmpty().Max();

        private readonly Dictionary<int, IoKind> _ioSet = new Dictionary<int, IoKind>();

        public JobContext()
        {
            _handle = new JobContextHandle();
        }

        private void AddPinnedData(MemoryHandle handle)
        {
            _pinnedMemory ??= [];
            _pinnedMemory.Add(handle);
        }

        public bool HasError => NativeMethods.imageflow_context_has_error(Handle);

        [Obsolete("Use SerializeNode instead for AOT compatibility")]

        [RequiresUnreferencedCode("Use SerializeNode instead for AOT compatibility")]
        [RequiresDynamicCode("Use SerializeNode instead for AOT compatibility")]
        private static byte[] ObsoleteSerializeToJson<T>(T obj)
        {
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

        internal static void WriteSerializedNode(IBufferWriter<byte> bufferWriter, JsonNode node, bool indented = true)
        {
            // Use System.Text.Json for serialization
            using var utf8JsonWriter = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions
            {
                Indented = indented
            });
            node.WriteTo(utf8JsonWriter);
            // flushes on disposal
        }

        internal static byte[] SerializeNode(JsonNode node, bool indented = true)
        {
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
        public IJsonResponseProvider SendMessage<T>(string method, T message)
        {
            AssertReady();
            return InvokeInternal(method, ObsoleteSerializeToJson(message));
        }

        [Obsolete("Use ExecuteJsonNode instead for AOT compatibility")]
        [RequiresUnreferencedCode("Use ExecuteJsonNode instead for AOT compatibility")]
        [RequiresDynamicCode("Use ExecuteJsonNode instead for AOT compatibility")]
        public IJsonResponseProvider Execute<T>(T message)
        {
            AssertReady();
            return InvokeInternal(ImageflowMethods.Execute, ObsoleteSerializeToJson(message));
        }

        [Obsolete("Use Invoke(string method, JsonNode message) instead.")]
        public IJsonResponseProvider SendMessage(string method, JsonNode message)
        {
            AssertReady();
            return InvokeInternal(method, message);
        }


        [Obsolete("Use .InvokeExecute(JsonNode message) instead")]
        public IJsonResponseProvider ExecuteJsonNode(JsonNode message)
        {
            AssertReady();
            return InvokeInternal(ImageflowMethods.Execute, message);
        }

        [Obsolete("Use .Invoke(string method, ReadOnlySpan<byte> utf8Json) instead")]
        public IJsonResponseProvider SendJsonBytes(string method, byte[] utf8Json)
            => InvokeInternal(method, utf8Json.AsSpan());

        public ImageInfo GetImageInfo(int ioId)
        {
            var node = InvokeAndParse(ImageflowMethods.GetImageInfo, new JsonObject() { { "io_id", ioId } });
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
                if (dataValue == null)
                    throw new ImageflowAssertionFailed("get_image_info response does not have a data property");
                var imageInfoValue =
                    (dataValue.AsObject().TryGetPropertyValue("image_info", out var imageInfoValueObj))
                        ? imageInfoValueObj
                        : null;

                if (imageInfoValue == null)
                    throw new ImageflowAssertionFailed(
                        "get_image_info response does not have an image_info property");
                return ImageInfo.FromDynamic(imageInfoValue);
            }
            else
            {
                throw new ImageflowAssertionFailed("get_image_info response does not have a success property");
            }
        }

        public VersionInfo GetVersionInfo()
        {
            AssertReady();

            var node = InvokeAndParse(ImageflowMethods.GetVersionInfo);
            if (node == null) throw new ImageflowAssertionFailed("get_version_info response is null");
            var responseObj = node.AsObject();
            if (responseObj == null)
                throw new ImageflowAssertionFailed("get_version_info response is not an object");
            if (responseObj.TryGetPropertyValue("success", out var successValue))
            {
                if (successValue?.GetValue<bool>() != true)
                {
                    throw ImageflowException.FromContext(Handle);
                }

                var dataValue = responseObj.TryGetPropertyValue("data", out var dataValueObj) ? dataValueObj : null;
                if (dataValue == null)
                    throw new ImageflowAssertionFailed("get_version_info response does not have a data property");
                var versionInfoValue =
                    (dataValue.AsObject().TryGetPropertyValue("version_info", out var versionInfoValueObj))
                        ? versionInfoValueObj
                        : null;

                if (versionInfoValue == null)
                    throw new ImageflowAssertionFailed(
                        "get_version_info response does not have an version_info property");
                return VersionInfo.FromNode(versionInfoValue);
            }
            else
            {
                throw new ImageflowAssertionFailed("get_version_info response does not have a success property");
            }
        }



        public IJsonResponse Invoke(string method, ReadOnlySpan<byte> utf8Json)
        {
            return InvokeInternal(method, utf8Json);
        }
        public IJsonResponse Invoke(string method)
        {
            return InvokeInternal(method, "{}"u8);
        }

        public JsonNode? InvokeAndParse(string method, JsonNode message)
        {
            using var response = InvokeInternal(method, message);
            return response.Parse();
        }
        public JsonNode? InvokeAndParse(string method)
        {
            using var response = InvokeInternal(method);
            return response.Parse();
        }


        public IJsonResponse InvokeExecute(JsonNode message)
        {
            AssertReady();
            return InvokeInternal(ImageflowMethods.Execute, message);
        }

        private ImageflowJsonResponse InvokeInternal(string method)
        {
            AssertReady();
            return InvokeInternal(method, "{}"u8);
        }


        private ImageflowJsonResponse InvokeInternal(ReadOnlySpan<byte> nullTerminatedMethod, JsonNode message)
        {
            AssertReady();
#if NETSTANDARD2_1_OR_GREATER
            // MAYBE: Use ArrayPoolBufferWriter instead? Adds CommunityToolkit.HighPerformance dependency
            var writer = new ArrayBufferWriter<byte>(4096);
            var utf8JsonWriter = new Utf8JsonWriter(writer, new JsonWriterOptions
            {
                Indented = true
            });
            message.WriteTo(utf8JsonWriter);
            utf8JsonWriter.Flush();
            return InvokeInternal(nullTerminatedMethod, writer.WrittenSpan);
#else

            // Use System.Text.Json for serialization
            var ms = new MemoryStream();
            var utf8JsonWriter = new Utf8JsonWriter(ms, new JsonWriterOptions
            {
                Indented = true
            });
            message.WriteTo(utf8JsonWriter);
            utf8JsonWriter.Flush();
            return ms.TryGetBufferSliceAllWrittenData(out var buffer) ?
                InvokeInternal(nullTerminatedMethod, buffer) :
                InvokeInternal(nullTerminatedMethod, ms.ToArray());

#endif
        }

        private ImageflowJsonResponse InvokeInternal(string method, JsonNode message)
        {
            AssertReady();
            var methodBuffer = method.Length < 128 ? stackalloc byte[method.Length + 1] : new byte[method.Length + 1];
            if (!TextHelpers.TryEncodeAsciiNullTerminated(method.AsSpan(), methodBuffer, out var nullTerminatedBytes))
            {
                throw new ArgumentException("Method must only contain ASCII characters", nameof(method));
            }
            return InvokeInternal(nullTerminatedBytes, message);
        }

        private ImageflowJsonResponse InvokeInternal(string method, ReadOnlySpan<byte> utf8Json)
        {
            AssertReady();
            var methodBuffer = method.Length < 128 ? stackalloc byte[method.Length + 1] : new byte[method.Length + 1];
            if (!TextHelpers.TryEncodeAsciiNullTerminated(method.AsSpan(), methodBuffer, out var nullTerminatedBytes))
            {
                throw new ArgumentException("Method must only contain ASCII characters", nameof(method));
            }
            return InvokeInternal(nullTerminatedBytes, utf8Json);
        }

        private unsafe ImageflowJsonResponse InvokeInternal(ReadOnlySpan<byte> nullTerminatedMethod, ReadOnlySpan<byte> utf8Json)
        {
            if (utf8Json.Length < 0) throw new ArgumentException("utf8Json cannot be empty", nameof(utf8Json));
            if (nullTerminatedMethod.Length == 0) throw new ArgumentException("Method cannot be empty", nameof(nullTerminatedMethod));
            if (nullTerminatedMethod[^1] != 0) throw new ArgumentException("Method must be null terminated", nameof(nullTerminatedMethod));
            fixed (byte* methodPtr = nullTerminatedMethod)
            {
                fixed (byte* jsonPtr = utf8Json)
                {
                    AssertReady();
                    var ptr = NativeMethods.imageflow_context_send_json(Handle, new IntPtr(methodPtr), new IntPtr(jsonPtr),
                        new UIntPtr((ulong)utf8Json.Length));
                    // check HasError, throw exception with our input JSON too
                    if (HasError) throw ImageflowException.FromContext(Handle, 2048, "JSON:\n" + TextHelpers.Utf8ToString(utf8Json));

                    AssertReady();
                    return new ImageflowJsonResponse(new JsonResponseHandle(_handle, ptr));
                }
            }
        }




        public void AssertReady()
        {
            if (!_handle.IsValid) throw new ObjectDisposedException("Imageflow JobContext");
            if (HasError) throw ImageflowException.FromContext(Handle);
        }

        [Obsolete("Obsolete: use the Fluent API instead")]
        public IJsonResponseProvider ExecuteImageResizer4CommandString(int inputId, int outputId, string commands)
        {
            AssertReady();
            return ExecuteImageResizer4CommandStringInternal(inputId, outputId, commands);
        }
        internal ImageflowJsonResponse ExecuteImageResizer4CommandStringInternal(int inputId, int outputId, string commands)
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

            return InvokeInternal(ImageflowMethods.Execute, message);
        }

        /// <summary>
        /// Copies the given data into Imageflow's memory.  Use AddInputBytesPinned to avoid copying.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        public void AddInputBytes(int ioId, byte[] buffer)
        {
            AddInputBytes(ioId, buffer.AsSpan());
        }
        /// <summary>
        /// Copies the given data into Imageflow's memory.  Use AddInputBytesPinned to avoid copying.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddInputBytes(int ioId, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer), "Array cannot be null");
            AddInputBytes(ioId, buffer.Array, buffer.Offset, buffer.Count);
        }
        /// <summary>
        /// Copies the given data into Imageflow's memory.  Use AddInputBytesPinned to avoid copying.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddInputBytes(int ioId, byte[] buffer, long offset, long count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(offset), "offset must be less than or equal to int.MaxValue");
            if (count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(count), " count must be less than or equal to int.MaxValue");
            if (offset < 0 || offset > buffer.LongLength - 1) throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within array bounds");
            if (count < 0 || offset + count > buffer.LongLength) throw new ArgumentOutOfRangeException(nameof(count), count, "offset + count must be within array bounds. count cannot be negative");

            AddInputBytes(ioId, buffer.AsSpan((int)offset, (int)count));
        }
        /// <summary>
        /// Copies the given data into Imageflow's memory.  Use AddInputBytesPinned to avoid copying.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ImageflowAssertionFailed"></exception>
        public void AddInputBytes(int ioId, ReadOnlySpan<byte> data)
        {
            AssertReady();
            if (ContainsIoId(ioId)) throw new ArgumentException($"ioId {ioId} already in use", nameof(ioId));

            var length = (ulong)data.Length;
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    // OutlivesFunctionCall tells imageflow to copy the data.
                    if (!NativeMethods.imageflow_context_add_input_buffer(Handle, ioId, new IntPtr(ptr),
                            new UIntPtr(length),
                            NativeMethods.Lifetime.OutlivesFunctionCall))
                    {
                        AssertReady();
                        throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
                    }

                    _ioSet.Add(ioId, IoKind.InputBuffer);
                }
            }
        }

        /// <summary>
        /// Pins the given data in managed memory and gives Imageflow a pointer to it. The data must not be modified until after the job is disposed.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddInputBytesPinned(int ioId, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            AddInputBytesPinned(ioId, new ReadOnlyMemory<byte>(buffer), MemoryLifetimePromise.MemoryIsOwnedByRuntime);
        }
        /// <summary>
        /// Pins the given data in managed memory and gives Imageflow a pointer to it. The data must not be modified until after the job is disposed.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddInputBytesPinned(int ioId, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer));
            AddInputBytesPinned(ioId, buffer.Array, buffer.Offset, buffer.Count);
        }
        /// <summary>
        /// Pins the given data in managed memory and gives Imageflow a pointer to it. The data must not be modified until after the job is disposed.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddInputBytesPinned(int ioId, byte[] buffer, long offset, long count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset must be less than or equal to int.MaxValue");
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count), " count must be less than or equal to int.MaxValue");

            if (offset < 0 || offset > buffer.LongLength - 1)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within array bounds");
            if (count < 0 || offset + count > buffer.LongLength)
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    "offset + count must be within array bounds. count cannot be negative");


            var rom = new ReadOnlyMemory<byte>(buffer, (int)offset, (int)count);
            AddInputBytesPinned(ioId, rom, MemoryLifetimePromise.MemoryIsOwnedByRuntime);
        }

        /// <summary>
        /// Pines the given Memory and gives Imageflow a pointer to it. You must promise that the
        /// memory will remain valid until after the JobContext is disposed.
        /// </summary>
        /// <param name="ioId"></param>
        /// <param name="data"></param>
        /// <param name="callerPromise"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ImageflowAssertionFailed"></exception>
        public unsafe void AddInputBytesPinned(int ioId, ReadOnlyMemory<byte> data, MemoryLifetimePromise callerPromise)
        {
            if (callerPromise == MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource)
                throw new ArgumentException("callerPromise cannot be MemoryLifetimePromise.MemoryOwnerDisposedByMemorySource", nameof(callerPromise));
            AssertReady();
            if (ContainsIoId(ioId)) throw new ArgumentException($"ioId {ioId} already in use", nameof(ioId));

            var pinned = data.Pin();
            try
            {
                var length = (ulong)data.Length;
                AddPinnedData(pinned);

                var addr = new IntPtr(pinned.Pointer);
                if (!NativeMethods.imageflow_context_add_input_buffer(Handle, ioId, addr, new UIntPtr(length),
                        NativeMethods.Lifetime.OutlivesContext))
                {
                    AssertReady();
                    throw new ImageflowAssertionFailed("AssertReady should raise an exception if method fails");
                }

                _ioSet.Add(ioId, IoKind.InputBuffer);
            }
            catch
            {
                _pinnedMemory?.Remove(pinned);
                pinned.Dispose();
                throw;
            }
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
        [Obsolete("Use a higher-level wrapper like the Fluent API instead; they can use faster code paths")]
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
            return new ImageflowUnmanagedReadStream(this, this._handle, buffer, bufferSize);
        }

        /// <summary>
        /// The memory remains valid only until the JobContext is disposed.
        /// </summary>
        /// <param name="ioId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ImageflowAssertionFailed"></exception>
        internal unsafe Span<byte> BorrowOutputBuffer(int ioId)
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
            return new Span<byte>((void*)buffer, (int)bufferSize);
        }

        /// <summary>
        /// Returns an IMemoryOwner&lt;byte> that will keep the Imageflow Job in memory until both it and the JobContext are disposed.
        /// The memory should be treated as read-only.
        /// </summary>
        /// <param name="ioId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ImageflowAssertionFailed"></exception>
        internal IMemoryOwner<byte> BorrowOutputBufferMemoryAndAddReference(int ioId)
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
            return SafeHandleMemoryManager.BorrowFromHandle(_handle, buffer, (uint)bufferSize);
        }

        private int _refCount;
        internal void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        internal void RemoveRef()
        {
            Interlocked.Decrement(ref _refCount);
        }

        public bool IsDisposed => !_handle.IsValid;
        public void Dispose()
        {
            if (IsDisposed) throw new ObjectDisposedException("Imageflow JobContext");

            if (Interlocked.Exchange(ref _refCount, 0) > 0)
            {
                throw new InvalidOperationException("Cannot dispose a JobContext that is still in use.  ");
            }

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
            //Unpin
            if (_pinnedMemory != null)
            {
                var toDispose = _pinnedMemory;
                _pinnedMemory = null;
                foreach (var active in toDispose)
                {
                    active.Dispose();
                }
            }
        }

        ~JobContext()
        {
            //Don't dispose managed objects; they have their own finalizers
            // _handle specifically handles it's own disposal and finalizer
            UnpinAll();
        }

    }
}
