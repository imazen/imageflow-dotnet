using System.Text;

namespace Imageflow.Fluent
{
    public class PerformanceDetails
    {
        internal PerformanceDetails(dynamic perf)
        {
            foreach (var f in perf.frames)
            {
                frames.Add(new PerformanceDetailsFrame(f));
            } 
        }
        private List<PerformanceDetailsFrame> frames = new List<PerformanceDetailsFrame>();
        public ICollection<PerformanceDetailsFrame> Frames => frames;

        public string GetFirstFrameSummary()
        {
            var sb = new StringBuilder();
            if (Frames.Count > 1)
            {
                sb.Append($"First of {Frames.Count} frames: ");
            }else if (Frames.Count == 0)
            {
                sb.Append("No frames found");
            }

            foreach (var n in Frames.First().Nodes)
            {
                sb.Append(n.Name);
                sb.Append("(");
                sb.Append((n.WallMicroseconds / 1000.0).ToString("0.####"));
                sb.Append("ms) ");
            }

            return sb.ToString();
        }
    }
}