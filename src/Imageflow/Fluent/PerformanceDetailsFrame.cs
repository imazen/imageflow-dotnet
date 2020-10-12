using System.Collections.Generic;

namespace Imageflow.Fluent
{
    public class PerformanceDetailsFrame
    {
        internal PerformanceDetailsFrame(dynamic frame)
        {
            foreach (var n in frame.nodes)
            {
                nodes.Add(new PerformanceDetailsNode()
                {
                    Name = n.name,
                    WallMicroseconds = n.wall_microseconds
                });
            }
        }

        private List<PerformanceDetailsNode> nodes = new List<PerformanceDetailsNode>();

        public ICollection<PerformanceDetailsNode> Nodes => nodes;
    }
}