using System.Text.Json.Nodes;

namespace Imageflow.Fluent
{
    public readonly struct FrameSizeLimit(uint maxWidth, uint maxHeight, float maxMegapixels)
    {
        public uint MaxWidth { get; } = maxWidth;
       public uint MaxHeight { get; } = maxHeight;
       public float MaxMegapixels { get; } = maxMegapixels;

       internal object ToImageflowDynamic()
       {
           return new
           {
               w = MaxWidth,
               h = MaxHeight,
               megapixels = MaxMegapixels
           };
       }

       internal JsonNode ToJsonNode()
       {
           var node = new JsonObject
           {
               { "w", MaxWidth },
               { "h", MaxHeight },
               { "megapixels", MaxMegapixels }
           };
           return node;
              
       }
    }
}