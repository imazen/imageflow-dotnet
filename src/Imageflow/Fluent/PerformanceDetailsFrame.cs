using System.Text.Json.Nodes;
using Imageflow.Bindings;

namespace Imageflow.Fluent
{
    public class PerformanceDetailsFrame
    {
        internal PerformanceDetailsFrame(JsonNode? frame)
        {
            if (frame == null) return;
            var obj = frame.AsObject();
            // foreach (var n in frame.nodes)
            // {
            //     _nodes.Add(new PerformanceDetailsNode()
            //     {
            //         Name = n.name,
            //         WallMicroseconds = n.wall_microseconds
            //     });
            // }
            if (obj.TryGetPropertyValue("nodes", out var nodesValue))
            {
                foreach (var n in nodesValue?.AsArray() ?? [])
                {
                    if (n == null) continue;
                    var name = n.AsObject().TryGetPropertyValue("name", out var nameValue)
                            ? nameValue?.GetValue<string>()
                            : throw new ImageflowAssertionFailed("PerformanceDetailsFrame node name is null");
                        
                    var microseconds = n.AsObject().TryGetPropertyValue("wall_microseconds", out var microsecondsValue)
                        ? microsecondsValue?.GetValue<long>()
                        : throw new ImageflowAssertionFailed("PerformanceDetailsFrame node wall_microseconds is null");
                    _nodes.Add(new PerformanceDetailsNode()
                    {
                        Name = name!,
                        WallMicroseconds = microseconds!.Value
                    });
                }
            }
        }

        private readonly List<PerformanceDetailsNode> _nodes = new List<PerformanceDetailsNode>();

        public ICollection<PerformanceDetailsNode> Nodes => _nodes;
    }
}