using System;
using System.Collections.Generic;
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
    public class FluentBuildJob: IDisposable
    {
        private bool _disposed;
        private readonly Dictionary<int, IBytesSource> _inputs = new Dictionary<int, IBytesSource>(2);
        private readonly Dictionary<int, IOutputDestination> _outputs = new Dictionary<int, IOutputDestination>(2);


        private void AddInput(int ioId, IBytesSource source)
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
        
        

        public  Task<BuildJobResult> FinishAsync() => FinishAsync(default(CancellationToken));
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
                
                var response = ctx.Execute(new
                {
                    framewise = ToFramewise()
                });

                const int bufferSize = 81920;
                var buffer = new byte[bufferSize];
                
                foreach (var pair in _outputs)
                {
                    using (var str = ctx.GetOutputBuffer(pair.Key))
                    {
                        await pair.Value.RequestCapacityAsync((int)str.Length);
                        int bytesRead;
                        while ((bytesRead = await str.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await pair.Value.WriteAsync(new ArraySegment<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }

                        await pair.Value.FlushAsync(cancellationToken);
                    }
                }
                
                return BuildJobResult.From(response, _outputs);
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

        private static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

        
        private static ITemporaryFileProvider SystemTempProvider()
        {
            return IsUnix ? TemporaryFile.CreateProvider() : TemporaryMemoryFile.CreateProvider();
        }
        public  Task<BuildJobResult> FinishInSubprocessAsync(CancellationToken cancellationToken, string imageflowToolPath) => FinishInSubprocessAsync(cancellationToken, SystemTempProvider(), imageflowToolPath);

        private async Task<BuildJobResult> FinishInSubprocessAsync(CancellationToken cancellationToken, ITemporaryFileProvider provider,
            string imageflowToolPath, long? outputBufferCapacity = null)
        {
            if (imageflowToolPath == null)
            {
                imageflowToolPath = IsUnix ? "imageflow_tool" : "imageflow_tool.exe";
            }
            if (!File.Exists(imageflowToolPath))
            {
                throw new FileNotFoundException("Cannot find imageflow_tool using path \"" + imageflowToolPath + "\" and currect folder \"" + Directory.GetCurrentDirectory() + "\"");
            }
            var cleanup = new List<IDisposable>();
            try
            {
                var inputFiles = (await Task.WhenAll(_inputs.Select(async pair =>
                {
                    var bytes = await pair.Value.GetBytesAsync(cancellationToken);

                    var file = provider.Create(bytes.Count);
                    cleanup.Add(file);
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
                    var file = provider.Create(outputCapacity);
                    cleanup.Add(file);
                    return new {io_id = pair.Key, direction = "out", io = new {file = file.Path}, Length = outputCapacity, File = file, Dest = pair.Value};
                }).ToArray();


                var build = new
                {
                    io = inputFiles.Select(v => (object)new {v.io_id, v.direction, v.io}).Concat(outputFiles.Select(v => (object)new {v.io_id, v.direction, v.io}))
                        .ToArray(),
                    framewise = ToFramewise()
                };

                var jsonFile = provider.Create(100000);
                cleanup.Add(jsonFile);
                using (var writer = new StreamWriter(jsonFile.WriteFromBeginning(), new UTF8Encoding(false)))
                {
                    JsonSerializer.Create().Serialize(writer, build);
                    writer.Flush(); //Required or no bytes appear
                }

                foreach (var f in inputFiles)
                {
                    using (var accessor = f.File.WriteFromBeginning())
                    {
                        await accessor.WriteAsync(f.bytes.Array, f.bytes.Offset, f.bytes.Count, cancellationToken);
                    }
                }

                var startInfo = new ProcessStartInfo
                {
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    Arguments = $" v0.1/build --json {jsonFile.Path}",
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
                        throw new ImageflowException(errors + $"\n{JsonConvert.SerializeObject(build)}");
                    }
                    else
                    {
                        throw new ImageflowException(errors);
                    }
                }
                
                
                foreach (var f in outputFiles)
                {
                    using (var stream = f.File.ReadFromBeginning())
                    {
                        await f.Dest.CopyFromStreamAsync(stream, cancellationToken);
                    }
                }
                
                
                return BuildJobResult.From(new MemoryStreamJsonProvider(output), _outputs);

            }
            finally
            {
                foreach(var d in cleanup)
                    d.Dispose();
            }
           
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
            if (nodes.All(n => n.Canvas == null) && nodes.Count(n => n.Canvas == null && n.Input == null) == 1)
            {
                return new {steps = nodes.OrderBy(n => n.Uid).Select(n => n.NodeData).ToList()};
            }
            else
            {
                return ToFramewiseGraph(nodes);
            }
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
            if (_disposed) return;
            
            _disposed = true;
            foreach (var v in _inputs.Values)
                v.Dispose();
            _inputs.Clear();
            foreach (var v in _outputs.Values)
                v.Dispose();
            _outputs.Clear();
        }

        public int GenerateIoId() =>_inputs.Keys.Concat(_outputs.Keys).DefaultIfEmpty(-1).Max() + 1;
        
    }
}
