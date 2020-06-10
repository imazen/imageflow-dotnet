using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Imageflow.Bindings;
using Imageflow.Net.IO;
using Newtonsoft.Json;

namespace Imageflow.Fluent
{
    public partial class FluentBuildJob: IDisposable
    {
        private bool _disposed;
        private readonly Dictionary<int, IBytesSource> _inputs = new Dictionary<int, IBytesSource>(2);
        private readonly Dictionary<int, IOutputDestination> _outputs = new Dictionary<int, IOutputDestination>(2);


        internal void AddInput(int ioId, IBytesSource source)
        {
            if (_inputs.ContainsKey(ioId) || _outputs.ContainsKey(ioId))
                throw new ArgumentException("ioId", $"ioId {ioId} has already been assigned");
            _inputs.Add(ioId, source);
        }
        internal void AddOutput(int ioId, IOutputDestination destination)
        {
            if (_inputs.ContainsKey(ioId) || _outputs.ContainsKey(ioId))
                throw new ArgumentException("ioId", $"ioId {ioId} has already been assigned");
            _outputs.Add(ioId, destination);
        }
       
        public BuildNode Decode(IBytesSource source, int ioId, DecodeCommands commands)
        {
            AddInput(ioId, source);
            if (commands == null)
            {
                return BuildNode.StartNode(this,
                    new
                    {
                        decode = new
                        {
                            io_id = ioId
                        }
                    });
            }
            return BuildNode.StartNode(this,
                new
                {
                    decode = new
                    {
                        io_id = ioId,
                        commands = commands.ToImageflowDynamic()
                    }
                });
        }
        
        public BuildNode Decode(IBytesSource source, int ioId) => Decode(source, ioId, null);
        public BuildNode Decode(IBytesSource source) => Decode( source, GenerateIoId());
        public BuildNode Decode(ArraySegment<byte> source) => Decode( new BytesSource(source), GenerateIoId());
        public BuildNode Decode(byte[] source) => Decode( new BytesSource(source), GenerateIoId());
        public BuildNode Decode(Stream source, bool disposeStream) => Decode( new StreamSource(source, disposeStream), GenerateIoId());
        public BuildNode Decode(ArraySegment<byte> source, int ioId) => Decode( new BytesSource(source), ioId);
        public BuildNode Decode(byte[] source, int ioId) => Decode( new BytesSource(source), ioId);
        public BuildNode Decode(Stream source, bool disposeStream, int ioId) => Decode( new StreamSource(source, disposeStream), ioId);

        public BuildEndpoint BuildCommandString(byte[] source, IOutputDestination dest, string commandString) => BuildCommandString(new BytesSource(source), dest, commandString);

        public BuildEndpoint BuildCommandString(IBytesSource source, IOutputDestination dest, string commandString) => BuildCommandString(source, null, dest, null, commandString);


        public BuildEndpoint BuildCommandString(IBytesSource source, int? sourceIoId, IOutputDestination dest,
            int? destIoId, string commandString)
            => BuildCommandString(source, sourceIoId, dest, destIoId, commandString, null);

        public BuildEndpoint BuildCommandString(IBytesSource source, IOutputDestination dest, string commandString,
            ICollection<InputWatermark> watermarks)
            => BuildCommandString(source, null, dest, null, commandString, watermarks);
        
        public BuildEndpoint BuildCommandString(IBytesSource source, int? sourceIoId, IOutputDestination dest, int? destIoId, string commandString, ICollection<InputWatermark> watermarks)
        {
            sourceIoId = sourceIoId ?? GenerateIoId();
            AddInput(sourceIoId.Value, source);
            destIoId = destIoId ?? GenerateIoId();
            AddOutput(destIoId.Value, dest);
            
            if (watermarks != null)
            {
                foreach (var w in watermarks)
                {
                    if (w.IoId == null && w.Source == null) throw new ArgumentException("InputWatermark instances cannot have both a null IoId and a null Source");
                    if (w.IoId == null) w.IoId = this.GenerateIoId();
                    if (w.Source != null) AddInput(w.IoId.Value, w.Source);
                }
            }
            
            dynamic nodeData = new
            {
                command_string = new
                {
                    kind = "ir4",
                    value = commandString,
                    decode = sourceIoId,
                    encode = destIoId,
                    watermarks = watermarks?.Select(w => w.ToImageflowDynamic()).ToArray()
                }
            };
            return new BuildEndpoint(this, nodeData, null, null);
            
        }

        public  Task<BuildJobResult> FinishAsync() => FinishAsync(default);
        public async Task<BuildJobResult> FinishAsync(CancellationToken cancellationToken)
        {
            var inputByteArrays = await Task.WhenAll(_inputs.Select( async pair => new KeyValuePair<int, ArraySegment<byte>>(pair.Key, await pair.Value.GetBytesAsync(cancellationToken))));
            using (var ctx = new JobContext())
            {
                foreach (var pair in inputByteArrays)
                    ctx.AddInputBytesPinned(pair.Key, pair.Value);

                foreach (var outId in _outputs.Keys)
                {
                    ctx.AddOutputBuffer(outId);
                }
                //TODO: Use a Semaphore to limit concurrency; and move work to threadpool

                using (var response = ctx.Execute(new
                {
                    framewise = ToFramewise()
                }))
                {

                    foreach (var pair in _outputs)
                    {
                        using (var stream = ctx.GetOutputBuffer(pair.Key))
                        {
                            await pair.Value.CopyFromStreamAsync(stream, cancellationToken);
                        }
                    }
                    return BuildJobResult.From(response, _outputs);
                }
            }
        }

        private class MemoryStreamJsonProvider : IJsonResponseProvider
        {
            private readonly MemoryStream _ms;
            public MemoryStreamJsonProvider(MemoryStream ms) => _ms = ms;
            public void Dispose() => _ms.Dispose();
            public Stream GetStream() => _ms;
        }


        private object BuildJsonWithPlaceholders()
        {
            var inputIo = _inputs.Select(pair =>
                new {io_id = pair.Key, direction = "in", io = new {placeholder = (string) null}});
            var outputIo = _outputs.Select(pair =>
                new {io_id = pair.Key, direction = "out", io = new {placeholder = (string) null}});
            return new
            {
                io = inputIo.Concat(outputIo).ToArray(),
                framewise = ToFramewise()
            };
        }

        private static ITemporaryFileProvider SystemTempProvider()
        {
            return RuntimeFileLocator.IsUnix ? TemporaryFile.CreateProvider() : TemporaryMemoryFile.CreateProvider();
        }
        
    
        class SubprocessFilesystemJob: IPreparedFilesystemJob
        {
            public string JsonPath { get; set; }
            public IReadOnlyDictionary<int, string> OutputFiles { get; internal set; }
            internal ITemporaryFileProvider Provider { get; set; }
            internal object JobMessage { get; set; }
            internal List<IDisposable> Cleanup { get; } = new List<IDisposable>();
            internal List<KeyValuePair<ITemporaryFile, IOutputDestination>> Outputs { get; set; }

            internal async Task CopyOutputsToDestinations(CancellationToken token)
            {
                foreach (var pair in Outputs)
                {
                    using (var stream = pair.Key.ReadFromBeginning())
                    {
                        await pair.Value.CopyFromStreamAsync(stream, token);
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
        private async Task<SubprocessFilesystemJob> PrepareForSubprocessAsync(CancellationToken cancellationToken, bool cleanupFiles, long? outputBufferCapacity = null)
        {
            var job = new SubprocessFilesystemJob { Provider = cleanupFiles ? SystemTempProvider() : TemporaryFile.CreateProvider()};
            try
            {

                var inputFiles = (await Task.WhenAll(_inputs.Select(async pair =>
                {
                    var bytes = await pair.Value.GetBytesAsync(cancellationToken);

                    var file = job.Provider.Create(cleanupFiles, bytes.Count);
                    job.Cleanup.Add(file);
                    return new
                    {
                        io_id = pair.Key,
                        direction = "in",
                        io = new {file = file.Path},
                        bytes,
                        Length = bytes.Count,
                        File = file
                    };
                }))).ToArray();
                
                var outputCapacity = outputBufferCapacity ?? inputFiles.Max(v => v.Length) * 2;
                var outputFiles = _outputs.Select(pair =>
                {
                    var file = job.Provider.Create(cleanupFiles, outputCapacity);
                    job.Cleanup.Add(file);
                    return new
                    {
                        io_id = pair.Key,
                        direction = "out",
                        io = new {file = file.Path},
                        Length = outputCapacity,
                        File = file,
                        Dest = pair.Value
                    };
                }).ToArray();
                
                foreach (var f in inputFiles)
                {
                    using (var accessor = f.File.WriteFromBeginning())
                    {
                        await accessor.WriteAsync(f.bytes.Array, f.bytes.Offset, f.bytes.Count, cancellationToken);
                    }
                }

                job.JobMessage = new
                {
                    io = inputFiles.Select(v => (object) new {v.io_id, v.direction, v.io})
                        .Concat(outputFiles.Select(v => (object) new {v.io_id, v.direction, v.io}))
                        .ToArray(),
                    framewise = ToFramewise()
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
                using (var writer = new StreamWriter(jsonFile.WriteFromBeginning(), new UTF8Encoding(false)))
                {
                    JsonSerializer.Create().Serialize(writer, job.JobMessage);
                    writer.Flush(); //Required or no bytes appear
                }

                job.JsonPath = jsonFile.Path;
                    
                return job;
            }
            catch
            {
                job.Dispose();
                throw; 
            }
        }
        
        public async Task<BuildJobResult> FinishInSubprocessAsync(CancellationToken cancellationToken,
            string imageflowToolPath, long? outputBufferCapacity = null)
        {
            if (imageflowToolPath == null)
            {
                imageflowToolPath = RuntimeFileLocator.IsUnix ? "imageflow_tool" : "imageflow_tool.exe";
            }
            if (!File.Exists(imageflowToolPath))
            {
                throw new FileNotFoundException("Cannot find imageflow_tool using path \"" + imageflowToolPath + "\" and currect folder \"" + Directory.GetCurrentDirectory() + "\"");
            }

            using (var job = await PrepareForSubprocessAsync(cancellationToken, true, outputBufferCapacity))
            {
                
                var startInfo = new ProcessStartInfo
                {
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    Arguments = $" v0.1/build --json {job.JsonPath}",
                    CreateNoWindow = true,
                    FileName = imageflowToolPath
                };

                var results = await ProcessEx.RunAsync(startInfo, cancellationToken);


                var output = results.GetBufferedOutputStream();
                var errors = results.GetStandardErrorString();

                if (!string.IsNullOrWhiteSpace(errors) || results.ExitCode != 0)
                {
                    if (errors.Contains("InvalidJson"))
                    {
                        throw new ImageflowException(errors + $"\n{JsonConvert.SerializeObject(job.JobMessage)}");
                    }
                    else
                    {
                        throw new ImageflowException(errors);
                    }
                }

                await job.CopyOutputsToDestinations(cancellationToken);

                using (var jsonProvider = new MemoryStreamJsonProvider(output)) {
                    return BuildJobResult.From(jsonProvider, _outputs);
                }
            }
        }
        
        public async Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(CancellationToken cancellationToken, bool deleteFilesOnDispose)
        {
            return await PrepareForSubprocessAsync(cancellationToken, deleteFilesOnDispose);
        }
        
        
        public async Task<BuildJobResult> FinishAndDisposeAsync(CancellationToken cancellationToken)
        {
            var r = await FinishAsync(cancellationToken);
            Dispose();
            return r;
        }
        
        private readonly HashSet<BuildItemBase> _nodesCreated = new HashSet<BuildItemBase>();
       
        public BuildNode CreateCanvasBgra32(uint w, uint h, AnyColor color) =>
            CreateCanvas(w, h, color, PixelFormat.Bgra_32);
        
        public BuildNode CreateCanvasBgr32(uint w, uint h, AnyColor color) =>
            CreateCanvas(w, h, color, PixelFormat.Bgr_32);
        
        private BuildNode CreateCanvas(uint w, uint h, AnyColor color, PixelFormat format) => 
            BuildNode.StartNode(this, new {create_canvas = new {w, h, color, format = format.ToString().ToLowerInvariant()}});

        
        
        internal void AddNode(BuildItemBase n)
        {
            AssertReady();
            
            if (!_nodesCreated.Add(n))
            {
                throw new ImageflowAssertionFailed("Cannot add duplicate node");
            }
            if (n.Canvas != null && !_nodesCreated.Contains(n.Canvas))// || n.Canvas.Builder != this))
            {
                throw new ImageflowAssertionFailed("You cannot use a canvas node from a different FluentBuildJob");
            }
            if (n.Input != null &&  !_nodesCreated.Contains(n.Input))
            {
                throw new ImageflowAssertionFailed("You cannot use an input node from a different FluentBuildJob");
            }
        }


        private enum EdgeKind
        {
            Canvas,
            Input
        }

        private ICollection<BuildItemBase> CollectUnique() => _nodesCreated;

        private static IEnumerable<Tuple<long, long, EdgeKind>> CollectEdges(ICollection<BuildItemBase> forUniqueNodes){
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

        internal object ToFramewise()
        {
            var nodes = CollectUnique();
            return ToFramewiseGraph(nodes);
        }

        private object ToFramewiseGraph(ICollection<BuildItemBase> uniqueNodes)
        {
            var lowestUid = LowestUid(uniqueNodes) ?? 0;
            var edges = CollectEdges(uniqueNodes);
            var framewiseEdges = edges.Select(t => new
            {
                from = t.Item1 - lowestUid,
                to = t.Item2 - lowestUid,
                kind = t.Item3.ToString().ToLowerInvariant()
            }).ToList();
            var framewiseNodes = new Dictionary<string, object>(_nodesCreated.Count);
            foreach (var n in uniqueNodes)
            {
                framewiseNodes.Add((n.Uid - lowestUid).ToString(), n.NodeData );
            }
            return new
            {
                graph = new
                {
                    edges = framewiseEdges,
                    nodes = framewiseNodes
                }
            };
        }

        private void AssertReady()
        {
            if (_disposed) throw new ObjectDisposedException("FluentBuildJob");
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
            if (_disposed) return;

            if (disposing)
            {
                foreach (var v in _inputs.Values)
                    v.Dispose();
                _inputs.Clear();
                foreach (var v in _outputs.Values)
                    v.Dispose();
                _outputs.Clear();
            }

            _disposed = true;
        }

        public int GenerateIoId() =>_inputs.Keys.Concat(_outputs.Keys).DefaultIfEmpty(-1).Max() + 1;
        
    }
}
