namespace Imageflow.Fluent
{
    public record struct PerformanceDetailsNode
    {

        public string Name { get; internal init; }
        public long WallMicroseconds { get; internal init; }
    }
}
