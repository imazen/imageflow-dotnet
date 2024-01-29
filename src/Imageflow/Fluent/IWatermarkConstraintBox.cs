using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    public interface IWatermarkConstraintBox
    {
        [Obsolete("Use ToJsonNode() instead")]
        object ToImageflowDynamic();
        JsonNode ToJsonNode();
    }
}