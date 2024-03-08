using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Imageflow.Fluent;

public class PerformanceDetails
{
    internal PerformanceDetails(JsonNode? perf)
    {
        var obj = perf?.AsObject();
        if (obj == null)
        {
            return;
        }

        // foreach (var f in perf.frames)
        // {
        //     frames.Add(new PerformanceDetailsFrame(f));
        // }
        if (obj.TryGetPropertyValue("frames", out var framesValue))
        {
            foreach (var f in framesValue?.AsArray() ?? [])
            {
                _frames.Add(new PerformanceDetailsFrame(f));
            }
        }
    }
    private readonly List<PerformanceDetailsFrame> _frames = new List<PerformanceDetailsFrame>();
    public ICollection<PerformanceDetailsFrame> Frames => _frames;

    public string GetFirstFrameSummary()
    {
        var sb = new StringBuilder();
        if (Frames.Count > 1)
        {
            sb.Append(string.Format(CultureInfo.InvariantCulture,"First of {0} frames: ", Frames.Count));
        }
        else if (Frames.Count == 0)
        {
            sb.Append("No frames found");
        }

        foreach (var n in Frames.First().Nodes)
        {
            sb.Append(n.Name);
            sb.Append('(');
            sb.Append((n.WallMicroseconds / 1000.0).ToString( "0.####",CultureInfo.InvariantCulture));
            sb.Append("ms) ");
        }

        return sb.ToString();
    }
}
