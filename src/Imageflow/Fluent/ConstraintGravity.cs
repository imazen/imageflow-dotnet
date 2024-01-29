using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    public class ConstraintGravity
    {
        /// <summary>
        /// Centers the gravity (the default)
        /// </summary>
        public ConstraintGravity()
        {
            XPercent = 50;
            YPercent = 50;
        }
        /// <summary>
        /// Aligns the watermark so xPercent of free space is on the left and yPercent of free space is on the top
        /// </summary>
        /// <param name="xPercent"></param>
        /// <param name="yPercent"></param>
        public ConstraintGravity(float xPercent, float yPercent)
        {
            XPercent = xPercent;
            YPercent = yPercent;
        }

        public float XPercent { get; }
        public float YPercent { get; }

        [Obsolete("Use ToJsonNode() instead")]
        public object ToImageflowDynamic()
        {
            return new
            {
                percentage = new {
                    x = XPercent,
                    y = YPercent
                }

            };
        }

        public JsonNode ToJsonNode()
        {
            var obj = new JsonObject
            { { "percentage", new JsonObject()
            {
                {"x", XPercent},
                {"y", YPercent}
            } } };
            return obj;
        }
    }

}
