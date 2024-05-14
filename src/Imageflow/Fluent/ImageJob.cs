using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Imageflow.Bindings;
using Imageflow.Internal.Helpers;
using Imageflow.IO;

namespace Imageflow.Fluent;

[Obsolete("Use ImageJob instead")]
public class FluentBuildJob : ImageJob;

public class ImageJob : IDisposable
{
    private bool _disposed;
    private readonly Dictionary<int, IAsyncMemorySource> _inputs = new Dictionary<int, IAsyncMemorySource>(2);
    private readonly Dictionary<int, IOutputDestination> _outputs = new Dictionary<int, IOutputDestination>(2);

    internal void AddInput(int ioId, IAsyncMemorySource source)
    {
        if (_inputs.ContainsKey(ioId) || _outputs.ContainsKey(ioId))
        {
            throw new ArgumentException($"ioId {ioId} has already been assigned", nameof(ioId));
        }

        _inputs.Add(ioId, source);
    }
    internal void AddOutput(int ioId, IOutputDestination destination)
    {
        if (_inputs.ContainsKey(ioId) || _outputs.ContainsKey(ioId))
        {
            throw new ArgumentException($"ioId {ioId} has already been assigned", nameof(ioId));
        }

        _outputs.Add(ioId, destination);
    }

    [Obsolete("IBytesSource is obsolete; use a class that implements IMemorySource instead")]
    public BuildNode Decode(IBytesSource source, DecodeCommands commands) =>
        Decode(source.ToMemorySource(), GenerateIoId(), commands);

    [Obsolete("IBytesSource is obsolete; use a class that implements IMemorySource instead")]
    public BuildNode Decode(IBytesSource source, int ioId) => Decode(source.ToMemorySource(), ioId, null);

    [Obsolete("IBytesSource is obsolete; use a class that implements IMemorySource instead")]
    public BuildNode Decode(IBytesSource source) => Decode(source, GenerateIoId());

    [Obsolete("IBytesSource is obsolete; use a class that implements IMemorySource instead")]
    public BuildNode Decode(IBytesSource source, int ioId, DecodeCommands? commands)
    {
        return Decode(source.ToMemorySource(), ioId, commands);
    }

    public BuildNode Decode(Stream source, bool disposeStream) => Decode(source, disposeStream, GenerateIoId());

    public BuildNode Decode(Stream source, bool disposeStream, int ioId) =>
        Decode(disposeStream ? BufferedStreamSource.UseEntireStreamAndDisposeWithSource(source)
            : BufferedStreamSource.BorrowEntireStream(source), ioId);

    [Obsolete("Use Decode(MemorySource.Borrow(arraySegment, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed)) instead")]
    public BuildNode Decode(ArraySegment<byte> source) => Decode(MemorySource.Borrow(source, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed), GenerateIoId());

    [Obsolete("Use Decode(MemorySource.Borrow(arraySegment, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed), ioId) instead")]
    public BuildNode Decode(ArraySegment<byte> source, int ioId) => Decode(MemorySource.Borrow(source, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed), ioId);

    public BuildNode Decode(byte[] source) => Decode(new MemorySource(source), GenerateIoId());
    public BuildNode Decode(byte[] source, int ioId) => Decode(new MemorySource(source), ioId);

    public BuildNode Decode<T>(T source) where T : IAsyncMemorySource
    {
        return Decode(source, GenerateIoId(), null);
    }
    public BuildNode Decode<T>(T source, DecodeCommands commands) where T : IAsyncMemorySource =>
        Decode(source, GenerateIoId(), commands);

    public BuildNode Decode<T>(T source, int ioId) where T : IAsyncMemorySource
    {
        return Decode(source, ioId, null);
    }

    /// <param name="ioId"></param>
    /// <param name="commands">Commands to the decoder, such as JPEG or WebP block-wise downscaling for performance, or to discard the color profile or ignore color profile errors</param>
    /// <param name="source"></param>
    /// <returns></returns>
    public BuildNode Decode<T>(T source, int ioId, DecodeCommands? commands) where T : IAsyncMemorySource
    {
        AddInput(ioId, source);
        if (commands == null)
        {
            return BuildNode.StartNode(this,
                // new
                // {
                //     decode = new
                //     {
                //         io_id = ioId
                //     }
                // });
                new JsonObject() { { "decode", new JsonObject() { { "io_id", ioId } } } });
        }
        return BuildNode.StartNode(this,
            // new
            // {
            //     decode = new
            //     {
            //         io_id = ioId,
            //         commands = commands.ToImageflowDynamic()
            //     }
            // });
            new JsonObject() { { "decode", new JsonObject() { { "io_id", ioId }, { "commands", commands.ToJsonNode() } } } });
    }

    public BuildNode CreateCanvasBgra32(uint w, uint h, AnyColor color) =>
        CreateCanvas(w, h, color, PixelFormat.Bgra_32);

    public BuildNode CreateCanvasBgr32(uint w, uint h, AnyColor color) =>
        CreateCanvas(w, h, color, PixelFormat.Bgr_32);

    private BuildNode CreateCanvas(uint w, uint h, AnyColor color, PixelFormat format) =>
        BuildNode.StartNode(this,
              //new {create_canvas = new {w, h, color = color.ToImageflowDynamic()
              new JsonObject() {{"create_canvas", new JsonObject()
                  {
                      {"w", w},
                      {"h", h},
                      {"color", color.ToJsonNode()},
                      {"format", format.ToString().ToLowerInvariant()}
                  }
              }});

    [Obsolete("Use a BufferedStreamSource or MemorySource for the source parameter instead")]
    public BuildEndpoint BuildCommandString(IBytesSource source, IOutputDestination dest, string commandString) => BuildCommandString(source, null, dest, null, commandString);
    [Obsolete("Use a BufferedStreamSource or MemorySource for the source parameter instead")]
    public BuildEndpoint BuildCommandString(IBytesSource source, int? sourceIoId, IOutputDestination dest,
        int? destIoId, string commandString)
        => BuildCommandString(source, sourceIoId, dest, destIoId, commandString, null);

    [Obsolete("Use a BufferedStreamSource or MemorySource for the source parameter instead")]
    public BuildEndpoint BuildCommandString(IBytesSource source, IOutputDestination dest, string commandString,
        ICollection<InputWatermark>? watermarks)
        => BuildCommandString(source, null, dest, null, commandString, watermarks);

    [Obsolete("Use a BufferedStreamSource or MemorySource for the source parameter instead")]
    public BuildEndpoint BuildCommandString(IBytesSource source, int? sourceIoId, IOutputDestination dest,
        int? destIoId, string commandString, ICollection<InputWatermark>? watermarks)
    {
        return BuildCommandString(source.ToMemorySource(), sourceIoId, dest, destIoId, commandString, watermarks);
    }

    public BuildEndpoint BuildCommandString(byte[] source, IOutputDestination dest, string commandString) => BuildCommandString(new MemorySource(source), dest, commandString);

    /// <summary>
    /// Modify the input image (source) with the given command string and watermarks and encode to the (dest)
    /// </summary>
    /// <param name="source"></param>
    /// <param name="dest"></param>
    /// <param name="commandString"></param>
    /// <returns></returns>
    ///
    public BuildEndpoint BuildCommandString(IAsyncMemorySource source, IOutputDestination dest, string commandString) => BuildCommandString(source, null, dest, null, commandString);

    public BuildEndpoint BuildCommandString(IAsyncMemorySource source, int? sourceIoId, IOutputDestination dest,
        int? destIoId, string commandString)
        => BuildCommandString(source, sourceIoId, dest, destIoId, commandString, null);

    public BuildEndpoint BuildCommandString(IAsyncMemorySource source, IOutputDestination dest, string commandString,
        ICollection<InputWatermark>? watermarks)
        => BuildCommandString(source, null, dest, null, commandString, watermarks);

    public BuildEndpoint BuildCommandString(IAsyncMemorySource source, int? sourceIoId, IOutputDestination dest, int? destIoId, string commandString, ICollection<InputWatermark>? watermarks)
    {
        sourceIoId = sourceIoId ?? GenerateIoId();
        AddInput(sourceIoId.Value, source);
        destIoId = destIoId ?? GenerateIoId();
        AddOutput(destIoId.Value, dest);

        if (watermarks != null)
        {
            foreach (var w in watermarks)
            {
                if (w.IoId == null && w.Source == null)
                {
                    throw new ArgumentException("InputWatermark instances cannot have both a null IoId and a null Source");
                }

                w.IoId ??= GenerateIoId();
                if (w.Source != null)
                {
                    AddInput(w.IoId.Value, w.Source);
                }
            }
        }

        // dynamic nodeData = new
        // {
        //     command_string = new
        //     {
        //         kind = "ir4",
        //         value = commandString,
        //         decode = sourceIoId,
        //         encode = destIoId,
        //         watermarks = watermarks?.Select(w => w.ToImageflowDynamic()).ToArray()
        //     }
        // };
        var watermarkNodes = watermarks?.Select(w => w.ToJsonNode()).ToArray();
        var nodeData = new JsonObject
        {
            {"command_string", new JsonObject
            {
                {"kind", "ir4"},
                {"value", commandString},
                {"decode", sourceIoId},
                {"encode", destIoId},
                {"watermarks", watermarkNodes != null ? new JsonArray(watermarkNodes) : null}
            }}
        };
        return new BuildEndpoint(this, nodeData, null, null);

    }

    /// <summary>
    /// Complete the job and set execution options
    /// </summary>
    /// <returns></returns>
    public FinishJobBuilder Finish() => new FinishJobBuilder(this, default);

    [Obsolete("Use .Finish().InProcessAsync()")]
    public Task<BuildJobResult> FinishAsync() => Finish().InProcessAsync();

    [Obsolete("Use .Finish().SetCancellationToken(t).InProcessAsync()")]
    public Task<BuildJobResult> FinishAsync(CancellationToken cancellationToken)
        => Finish().SetCancellationToken(cancellationToken).InProcessAsync();

    [Obsolete("Use .Finish().SetCancellationToken(cancellationToken).InSubprocessAsync(imageflowToolPath, outputBufferCapacity)")]
    public Task<BuildJobResult> FinishInSubprocessAsync(CancellationToken cancellationToken,
        string imageflowToolPath, long? outputBufferCapacity = null) =>
        Finish().SetCancellationToken(cancellationToken)
            .InSubprocessAsync(imageflowToolPath, outputBufferCapacity);

    [Obsolete("Use .Finish().SetCancellationToken(cancellationToken).WriteJsonJobAndInputs(deleteFilesOnDispose)")]
    public Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(CancellationToken cancellationToken, bool deleteFilesOnDispose)
        => Finish().SetCancellationToken(cancellationToken).WriteJsonJobAndInputs(deleteFilesOnDispose);

    [Obsolete("Use .Finish().SetCancellationToken(cancellationToken).InProcessAndDisposeAsync()")]
    public Task<BuildJobResult> FinishAndDisposeAsync(CancellationToken cancellationToken)
        => Finish().SetCancellationToken(cancellationToken).InProcessAndDisposeAsync();

    internal JsonObject CreateJsonNodeForFramewiseWithSecurityOptions(SecurityOptions? securityOptions)
    {
        // var message = new
        // {
        //     security = securityOptions?.ToImageflowDynamic(),
        //     framewise = ToFramewise()
        // };
        return new JsonObject()
        {
            ["framewise"] = ToFramewise(),
            ["security"] = securityOptions?.ToJsonNode()
        };
    }

    internal string ToJsonDebug(SecurityOptions? securityOptions = default)
    {
        return CreateJsonNodeForFramewiseWithSecurityOptions(securityOptions).ToJsonString();
    }

    internal async Task<BuildJobResult> FinishAsync(JobExecutionOptions executionOptions, SecurityOptions? securityOptions, CancellationToken cancellationToken)
    {
        var inputByteArrays = await Task.WhenAll(
            _inputs.Select(async pair => new KeyValuePair<int, ReadOnlyMemory<byte>>(pair.Key, await pair.Value.BorrowReadOnlyMemoryAsync(cancellationToken).ConfigureAwait(false)))).ConfigureAwait(false);
        using (var ctx = new JobContext())
        {
            foreach (var pair in inputByteArrays)
            {
                ctx.AddInputBytesPinned(pair.Key, pair.Value, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed);
            }

            foreach (var outId in _outputs.Keys)
            {
                ctx.AddOutputBuffer(outId);
            }

            //TODO: Use a Semaphore to limit concurrency
            var message = CreateJsonNodeForFramewiseWithSecurityOptions(securityOptions);

            var response = executionOptions.OffloadCpuToThreadPool
                ? await Task.Run(() => ctx.InvokeExecute(message), cancellationToken).ConfigureAwait(false)
                : ctx.InvokeExecute(message);

            // TODO: Should we handle failure before copying out the buffers??
            using (response)
            {

                foreach (var pair in _outputs)
                {
                    using var memOwner = ctx.BorrowOutputBufferMemoryAndAddReference(pair.Key);
                    await pair.Value.AdaptiveWriteAllAsync(memOwner.Memory, cancellationToken).ConfigureAwait(false);
                }
                return BuildJobResult.From(response, _outputs);
            }
        }
    }


    // private object BuildJsonWithPlaceholders()
    // {
    //     var inputIo = _inputs.Select(pair =>
    //         new {io_id = pair.Key, direction = "in", io = new {placeholder = (string?) null}});
    //     var outputIo = _outputs.Select(pair =>
    //         new {io_id = pair.Key, direction = "out", io = new {placeholder = (string?) null}});
    //     return new
    //     {
    //         io = inputIo.Concat(outputIo).ToArray(),
    //         framewise = ToFramewise()
    //     };
    // }

    private static ITemporaryFileProvider SystemTempProvider()
    {
        return RuntimeFileLocator.IsUnix ? TemporaryFile.CreateProvider() : TemporaryMemoryFile.CreateProvider();
    }

    class SubprocessFilesystemJob : IPreparedFilesystemJob
    {
        public SubprocessFilesystemJob(ITemporaryFileProvider provider)
        {
            Provider = provider;
        }
        internal ITemporaryFileProvider Provider { get; }
        public string JsonPath { get; set; } = "";
        public IReadOnlyDictionary<int, string> OutputFiles { get; internal set; } = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>());

        internal JsonNode? JobMessage { get; set; }
        internal List<IDisposable> Cleanup { get; } = new List<IDisposable>();
        internal List<KeyValuePair<ITemporaryFile, IOutputDestination>>? Outputs { get; set; }

        internal async Task CopyOutputsToDestinations(CancellationToken token)
        {
            if (Outputs == null)
            {
                return;
            }

            foreach (var pair in Outputs)
            {
                using (var stream = pair.Key.ReadFromBeginning())
                {
                    await pair.Value.CopyFromStreamAsyncInternal(stream, token).ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            foreach (var d in Cleanup)
            {
                d.Dispose();
            }
            Cleanup.Clear();

        }
        ~SubprocessFilesystemJob()
        {
            Dispose();
        }
    }
    private async Task<SubprocessFilesystemJob> PrepareForSubprocessAsync(CancellationToken cancellationToken, SecurityOptions? securityOptions, bool cleanupFiles, long? outputBufferCapacity = null)
    {
        var job = new SubprocessFilesystemJob(cleanupFiles ? SystemTempProvider() : TemporaryFile.CreateProvider());
        try
        {

            var inputFiles = (await Task.WhenAll(_inputs.Select(async pair =>
            {
                var bytes = await pair.Value.BorrowReadOnlyMemoryAsync(cancellationToken).ConfigureAwait(false);

                var file = job.Provider.Create(cleanupFiles, bytes.Length);
                job.Cleanup.Add(file);
                return (io_id: pair.Key, direction: "in",
                    io: new JsonObject { { "file", file.Path } },
                    bytes, bytes.Length, File: file);

            })).ConfigureAwait(false)).ToArray();

            var outputCapacity = outputBufferCapacity ?? inputFiles.Max(v => v.Length) * 2;
            var outputFiles = _outputs.Select(pair =>
            {
                var file = job.Provider.Create(cleanupFiles, outputCapacity);
                job.Cleanup.Add(file);
                return (io_id: pair.Key, direction: "out",
                    io: new JsonObject { { "file", file.Path } },
                    Length: outputCapacity, File: file, Dest: pair.Value);
            }).ToArray();

            foreach (var f in inputFiles)
            {
                using var accessor = f.File.WriteFromBeginning();
                await accessor.WriteMemoryAsync(f.bytes, cancellationToken).ConfigureAwait(false);
            }

            // job.JobMessage = new
            // {
            //     io = inputFiles.Select(v => (object) new {v.io_id, v.direction, v.io})
            //         .Concat(outputFiles.Select(v => (object) new {v.io_id, v.direction, v.io}))
            //         .ToArray(),
            //     builder_config = new
            //     {
            //         security = securityOptions?.ToImageflowDynamic()
            //     },
            //     framewise = ToFramewise()
            // };
            job.JobMessage = new JsonObject
            {
                ["io"] = new JsonArray(inputFiles.Select(v => (JsonNode)new JsonObject
                {
                    ["io_id"] = v.io_id,
                    ["direction"] = v.direction,
                    ["io"] = v.io
                }).Concat(outputFiles.Select(v => new JsonObject
                {
                    ["io_id"] = v.io_id,
                    ["direction"] = v.direction,
                    ["io"] = v.io
                })).ToArray()),
                ["builder_config"] = new JsonObject
                {
                    ["security"] = securityOptions?.ToJsonNode()
                },
                ["framewise"] = ToFramewise()
            };

            var outputFilenames = new Dictionary<int, string>();
            foreach (var f in outputFiles)
            {
                outputFilenames[f.io_id] = f.File.Path;
            }

            job.OutputFiles = new ReadOnlyDictionary<int, string>(outputFilenames);

            job.Outputs = outputFiles
                .Select(f => new KeyValuePair<ITemporaryFile, IOutputDestination>(f.File, f.Dest)).ToList();

            var jsonFile = job.Provider.Create(true, 100000);
            job.Cleanup.Add(jsonFile);
            var stream = jsonFile.WriteFromBeginning();
            // write job.JobMessage to stream using System.Text.Json
            var writer = new Utf8JsonWriter(stream);
            job.JobMessage.WriteTo(writer);
            writer.Flush();
            stream.Flush();
            stream.Dispose();

            job.JsonPath = jsonFile.Path;

            return job;
        }
        catch
        {
            job.Dispose();
            throw;
        }
    }

    internal async Task<BuildJobResult> FinishInSubprocessAsync(SecurityOptions? securityOptions,
        string? imageflowToolPath, long? outputBufferCapacity = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (imageflowToolPath == null)
        {
            imageflowToolPath = RuntimeFileLocator.IsUnix ? "imageflow_tool" : "imageflow_tool.exe";
        }
        if (!File.Exists(imageflowToolPath))
        {
            throw new FileNotFoundException("Cannot find imageflow_tool using path \"" + imageflowToolPath + "\" and currect folder \"" + Directory.GetCurrentDirectory() + "\"");
        }

        using (var job = await PrepareForSubprocessAsync(cancellationToken, securityOptions, true, outputBufferCapacity).ConfigureAwait(false))
        {

            var startInfo = new ProcessStartInfo
            {
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                Arguments = $" v0.1/build --json {job.JsonPath}",
                CreateNoWindow = true,
                FileName = imageflowToolPath
            };

            var results = await ProcessEx.RunAsync(startInfo, cancellationToken).ConfigureAwait(false);

            var output = results.GetBufferedOutputStream();
            var errors = results.GetStandardErrorString();

            if (!string.IsNullOrWhiteSpace(errors) || results.ExitCode != 0)
            {
                if (errors.Contains("InvalidJson"))
                {
                    //throw new ImageflowException(errors + $"\n{JsonConvert.SerializeObject(job.JobMessage)}");
                    throw new ImageflowException(errors + $"\n{job.JobMessage}");
                }
                else
                {
                    throw new ImageflowException(errors);
                }
            }

            await job.CopyOutputsToDestinations(cancellationToken).ConfigureAwait(false);

            var outputMemory = output.GetWrittenMemory();
            return BuildJobResult.From(new MemoryJsonResponse(results.ExitCode, outputMemory), _outputs);
        }
    }

    internal async Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(CancellationToken cancellationToken, SecurityOptions? securityOptions, bool deleteFilesOnDispose)
    {
        return await PrepareForSubprocessAsync(cancellationToken, securityOptions, deleteFilesOnDispose).ConfigureAwait(false);
    }

    private readonly List<BuildItemBase> _nodesCreated = new List<BuildItemBase>(10);

    internal void AddNode(BuildItemBase n)
    {
        AssertReady();

        if (_nodesCreated.Contains(n))
        {
            throw new ImageflowAssertionFailed("Cannot add duplicate node");
        }
        _nodesCreated.Add(n);
        if (n.Canvas != null && !_nodesCreated.Contains(n.Canvas))// || n.Canvas.Builder != this))
        {
            throw new ImageflowAssertionFailed("You cannot use a canvas node from a different ImageJob");
        }
        if (n.Input != null && !_nodesCreated.Contains(n.Input))
        {
            throw new ImageflowAssertionFailed("You cannot use an input node from a different ImageJob");
        }
    }

    private enum EdgeKind
    {
        Canvas,
        Input
    }

    private ICollection<BuildItemBase> CollectUnique() => _nodesCreated;

    private static IEnumerable<Tuple<long, long, EdgeKind>> CollectEdges(ICollection<BuildItemBase> forUniqueNodes)
    {
        var edges = new List<Tuple<long, long, EdgeKind>>(forUniqueNodes.Count);
        foreach (var n in forUniqueNodes)
        {
            if (n.Canvas != null)
            {
                edges.Add(new Tuple<long, long, EdgeKind>(n.Canvas.Uid, n.Uid, EdgeKind.Canvas));
            }
            if (n.Input != null)
            {
                edges.Add(new Tuple<long, long, EdgeKind>(n.Input.Uid, n.Uid, EdgeKind.Input));
            }
        }
        return edges;
    }

    private static long? LowestUid(IEnumerable<BuildItemBase> forNodes) => forNodes.Select(n => n.Uid as long?).Min();

    internal JsonNode ToFramewise()
    {
        var nodes = CollectUnique();
        return ImageJob.ToFramewiseGraph(nodes);
    }

    private static JsonNode ToFramewiseGraph(ICollection<BuildItemBase> uniqueNodes)
    {
        var lowestUid = LowestUid(uniqueNodes) ?? 0;
        var edges = CollectEdges(uniqueNodes)
            .OrderBy(t => t.Item1)
            .ThenBy(t => t.Item2).ToList();
        //var framewiseEdges = edges.Select(t => new
        // {
        //     from = t.Item1 - lowestUid,
        //     to = t.Item2 - lowestUid,
        //     kind = t.Item3.ToString().ToLowerInvariant()
        // }).ToList();
        JsonNode[] framewiseEdges = edges.Select(t => (JsonNode)new JsonObject
        {
            ["from"] = t.Item1 - lowestUid,
            ["to"] = t.Item2 - lowestUid,
            ["kind"] = t.Item3.ToString().ToLowerInvariant()
        }).ToArray();

        var nodes = new JsonObject();
        foreach (var n in uniqueNodes)
        {
            nodes.Add((n.Uid - lowestUid).ToString(CultureInfo.InvariantCulture), n.NodeData);
        }
        // return new
        // {
        //     graph = new
        //     {
        //         edges = framewiseEdges,
        //         nodes = framewiseNodes
        //     }
        // };
        return new JsonObject
        {
            ["graph"] = new JsonObject
            {
                ["edges"] = new JsonArray(framewiseEdges),
                ["nodes"] = nodes
            }
        };
    }

    private void AssertReady()
    {
        ObjectDisposedHelper.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        // Dispose of unmanaged resources.
        Dispose(true);
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var v in _inputs.Values)
            {
                v.Dispose();
            }

            _inputs.Clear();
            foreach (var v in _outputs.Values)
            {
                v.Dispose();
            }

            _outputs.Clear();
        }

        _disposed = true;
    }

    public int GenerateIoId() => _inputs.Keys.Concat(_outputs.Keys).DefaultIfEmpty(-1).Max() + 1;

    /// <summary>
    /// Returns dimensions and format of the provided image stream or byte array
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    [Obsolete("Use GetImageInfoAsync(IMemorySource, DataLifetime) instead; this method is less efficient and lacks clarity on disposing the source.")]
    public static Task<ImageInfo> GetImageInfo(IBytesSource image)
        => GetImageInfo(image, CancellationToken.None);

    /// <summary>
    /// Returns dimensions and format of the provided image stream or byte array
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Obsolete("Use GetImageInfoAsync(IMemorySource, DataLifetime) instead; this method is less efficient and lacks clarity on disposing the source.")]
    public static async Task<ImageInfo> GetImageInfo(IBytesSource image, CancellationToken cancellationToken)
    {
        try
        {
            var inputByteArray = await image.GetBytesAsync(cancellationToken).ConfigureAwait(false);
            using (var ctx = new JobContext())
            {
                ctx.AddInputBytesPinned(0, inputByteArray);
                return ctx.GetImageInfo(0);
            }
        }
        finally
        {
            image.Dispose();
        }
    }

    /// <summary>
    /// Returns dimensions and format of the provided image stream or byte array.
    /// Does NOT dispose the IMemorySource.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="disposeSource"></param>
    /// <returns></returns>
    public static ImageInfo GetImageInfo(IMemorySource image, SourceLifetime disposeSource)
    {
        try
        {
            var inputMemory = image.BorrowReadOnlyMemory();
            using var ctx = new JobContext();
            ctx.AddInputBytesPinned(0, inputMemory, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed);
            return ctx.GetImageInfo(0);
        }
        finally
        {
            if (disposeSource != SourceLifetime.Borrowed)
            {
                image.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns dimensions and format of the provided image stream or memory.
    /// Does not offload processing to a thread pool; will be CPU bound unless IMemorySource is not yet in memory.
    /// Does not dispose the IMemorySource.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="disposeSource"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async ValueTask<ImageInfo> GetImageInfoAsync(IAsyncMemorySource image, SourceLifetime disposeSource, CancellationToken cancellationToken = default)
    {
        try
        {
            var inputMemory = await image.BorrowReadOnlyMemoryAsync(cancellationToken).ConfigureAwait(false);
            if (inputMemory.Length == 0)
            {
                throw new ArgumentException("Input image is empty");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            using var ctx = new JobContext();
            ctx.AddInputBytesPinned(0, inputMemory, MemoryLifetimePromise.MemoryValidUntilAfterJobDisposed);
            return ctx.GetImageInfo(0);
        }
        finally
        {
            if (disposeSource != SourceLifetime.Borrowed)
            {
                image.Dispose();
            }
        }
    }
    /// <summary>
    /// Returns true if it is likely that Imageflow can decode the given image based on the first 12 bytes of the file.
    /// </summary>
    /// <param name="first12Bytes">The first 12 or more bytes of the file</param>
    /// <returns></returns>
    [Obsolete("Bad idea: imageflow may eventually support formats via OS codecs, so this is not predictable. Use Imazen.Common.FileTypeDetection.FileTypeDetector().GuessMimeType(data) with your own allowlist instead.")]
    public static bool CanDecodeBytes(byte[] first12Bytes)
    {
        return MagicBytes.IsDecodable(first12Bytes);
    }

    /// <summary>
    /// Returns a MIME type string such as "image/jpeg" based on the provided first 12 bytes of the file.
    /// Only guaranteed to work for image types Imageflow supports, but support for more file types may be added
    /// later.
    /// </summary>
    /// <param name="first12Bytes">The first 12 or more bytes of the file</param>
    /// <returns></returns>
    [Obsolete("Use new Imazen.Common.FileTypeDetection.FileTypeDetector().GuessMimeType(data) instead")]
    public static string? GetContentTypeForBytes(byte[] first12Bytes)
    {
        return MagicBytes.GetImageContentType(first12Bytes);
    }
}

