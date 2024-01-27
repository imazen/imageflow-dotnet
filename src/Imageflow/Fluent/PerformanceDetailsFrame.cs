namespace Imageflow.Fluent
{
    public class PerformanceDetailsFrame
    {
        internal PerformanceDetailsFrame(dynamic frame)
        {
            foreach (var n in frame.nodes)
            {
                _nodes.Add(new PerformanceDetailsNode()
                {
                    Name = n.name,
                    WallMicroseconds = n.wall_microseconds
                });
            }
        }

        private readonly List<PerformanceDetailsNode> _nodes = new List<PerformanceDetailsNode>();

        public ICollection<PerformanceDetailsNode> Nodes => _nodes;
    }
}