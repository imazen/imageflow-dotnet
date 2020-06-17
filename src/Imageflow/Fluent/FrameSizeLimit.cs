namespace Imageflow.Fluent
{
    public struct FrameSizeLimit
    {
        public FrameSizeLimit(uint maxWidth, uint maxHeight, float maxMegapixels)
        {
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            MaxMegapixels = maxMegapixels;
        }
       public uint MaxWidth { get; }
       public uint MaxHeight { get; }
       public float MaxMegapixels { get; }

       internal object ToImageflowDynamic()
       {
           return new
           {
               w = MaxWidth,
               h = MaxHeight,
               megapixels = MaxMegapixels
           };
       }
    }
}