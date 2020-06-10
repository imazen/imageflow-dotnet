using System;
using System.Drawing;
using System.IO;

namespace Imageflow.Fluent
{
    public class BuildNode :BuildItemBase
    {
        internal static BuildNode StartNode(FluentBuildJob graph, object data) => new BuildNode(graph, data, null, null);
    
        
        public BuildEndpoint Encode(IOutputDestination destination, int ioId, IEncoderPreset encoderPreset)
        {
            Builder.AddOutput(ioId, destination);
            return new BuildEndpoint(Builder,
                new {encode = new {io_id = ioId, preset = encoderPreset?.ToImageflowDynamic()}}, this, null);
        }

        public BuildEndpoint Encode(IOutputDestination destination, IEncoderPreset encoderPreset) =>
            Encode( destination, Builder.GenerateIoId(), encoderPreset);
        

        public BuildEndpoint EncodeToBytes(int ioId, IEncoderPreset encoderPreset) =>
            Encode(new BytesDestination(), ioId, encoderPreset);
        public BuildEndpoint EncodeToBytes(IEncoderPreset encoderPreset) =>
            Encode(new BytesDestination(), encoderPreset);
        
        public BuildEndpoint EncodeToStream(Stream stream, bool disposeStream, int ioId, IEncoderPreset encoderPreset) =>
            Encode(new StreamDestination(stream, disposeStream), ioId, encoderPreset);
        public BuildEndpoint EncodeToStream(Stream stream, bool disposeStream, IEncoderPreset encoderPreset) =>
            Encode(new StreamDestination(stream, disposeStream), encoderPreset);
        
        
        private BuildNode(FluentBuildJob builder,object nodeData, BuildNode inputNode, BuildNode canvasNode) : base(builder, nodeData, inputNode,
            canvasNode){}

        private BuildNode To(object data) => new BuildNode(Builder, data, this, null);
        private BuildNode NodeWithCanvas(BuildNode canvas, object data) => new BuildNode(Builder, data, this, canvas);


        public BuildNode ConstrainWithin(uint? w, uint? h) => To(new { constrain = new {mode="within", w, h } });

    
        public BuildNode ConstrainWithin(uint? w, uint? h, ResampleHints hints)
            => To(new
            {
                constrain = new
                {
                    mode = "within",
                    w,
                    h,
                    hints = hints?.ToImageflowDynamic()

                }
            });

        public BuildNode Constrain(Constraint constraint) => To(new { constrain = constraint.ToImageflowDynamic() });
        public BuildNode Distort(uint w, uint h) => Distort(w, h, null);
        public BuildNode Distort(uint w, uint h, ResampleHints hints)
            => To(new
            {
                resample_2d = new
                {
                    w,
                    h,
                    hints = hints?.ToImageflowDynamic()
                }
            });

        /// <summary>
        /// Crops the image to the given coordinates
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public BuildNode Crop(int x1, int y1, int x2, int y2)
            => To(new
            {
                crop = new
                {
                    x1,
                    y1,
                    x2,
                    y2
                }
            });

        /// <summary>
        /// Region is like a crop command, but you can specify coordinates outside of the image and
        /// thereby add padding. It's like a window. Coordinates are in pixels.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public BuildNode Region(int x1, int y1, int x2, int y2, AnyColor backgroundColor)
            => To(new
            {
                region = new
                {
                    x1,
                    y1,
                    x2,
                    y2,
                    background_color = backgroundColor.ToImageflowDynamic()
                }
            });

        /// <summary>
        /// Region is like a crop command, but you can specify coordinates outside of the image and
        /// thereby add padding. It's like a window.
        /// You can specify a region as a percentage of the image's width and height.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public BuildNode RegionPercent(float x1, float y1, float x2, float y2, AnyColor backgroundColor)
            => To(new
            {
                region_percent = new
                {
                    x1,
                    y1,
                    x2,
                    y2,
                    background_color = backgroundColor.ToImageflowDynamic()
                }
            });
        
         /// <summary>
         ///
         /// </summary>
         /// <param name="threshold">(1..255). determines how much noise/edges to tolerate before cropping
         /// is finalized. 80 is a good starting point.</param>
         /// <param name="percentPadding">determines how much of the image to restore after cropping to
         /// provide some padding. 0.5 (half a percent) is a good starting point.</param>
         /// <returns></returns>
        public BuildNode CropWhitespace(int threshold, float percentPadding)
            => To(new
            {
                crop_whitespace = new
                {
                    threshold,
                    percent_padding = percentPadding
                }
            });

        /// <summary>
        /// Does not honor encoding or decoding parameters. Use FluentBuildJob.BuildCommandString() instead unless
        /// you are actually combining this node with others in a job. 
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        public BuildNode ResizerCommands(string commandString)
            => To(new
            {
                command_string = new
                {
                    kind = "ir4",
                    value = commandString
                }
            });

        public BuildNode FlipVertical() => To(new {flip_v = (string)null});
        public BuildNode FlipHorizontal() => To(new {flip_h = (string)null });
        
        public BuildNode Rotate90() => To(new {rotate_90 = (string)null });
        public BuildNode Rotate180() => To(new {rotate_180 = (string)null });
        public BuildNode Rotate270() => To(new {rotate_270 = (string)null });
        public BuildNode Transpose() => To(new {transpose = (string)null });

        public BuildNode Branch(Func<BuildNode, BuildEndpoint> f)
        {
            f(this);
            return this;
        } 

        /// <summary>
        /// Copies (not composes) the given rectangle from input to canvas.
        /// You cannot copy from a BGRA input to a BGR canvas. 
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="area"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public BuildNode CopyRectTo(BuildNode canvas, Rectangle area, Point to) => NodeWithCanvas(canvas, new
        {
            copy_rect_to_canvas = new
            {
                from_x = area.X,
                from_y = area.Y,
                w = area.Width,
                h = area.Height,
                x = to.X,
                y = to.Y
            }
        });
        
        /// <summary>
        /// Draws the input image to the given rectangle on the canvas, distorting if the aspect ratios differ.
        /// 
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="to"></param>
        /// <param name="hints"></param>
        /// <param name="blend"></param>
        /// <returns></returns>
        public BuildNode DrawImageExactTo(BuildNode canvas, Rectangle to, ResampleHints hints, CompositingMode? blend) => NodeWithCanvas(canvas, new
        {
            draw_image_exact = new
            {
                w = to.Width,
                h = to.Height,
                x = to.X,
                y = to.Y,
                blend = blend?.ToString()?.ToLowerInvariant(),
                hints = hints?.ToImageflowDynamic()
            }
        });
        
        /// <summary>
        /// Fills the given rectangle with the specified color
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public BuildNode FillRectangle(int x1, int y1, int x2, int y2, AnyColor color)
            => To(new
            {
                fill_rect = new
                {
                    x1,
                    y1,
                    x2,
                    y2,
                    color = color.ToImageflowDynamic()
                }
            });
        
        /// <summary>
        /// Adds padding of the given color by enlarging the canvas on the sides specified.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public BuildNode ExpandCanvas(int left, int top, int right, int bottom, AnyColor color)
            => To(new
            {
                expand_canvas = new
                {
                    left,
                    top,
                    right,
                    bottom,
                    color = color.ToImageflowDynamic()
                }
            });
        /// <summary>
        /// This command is not endorsed as it operates in the sRGB space and does not produce perfect results.
        /// </summary>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public BuildNode WhiteBalanceSrgb(int threshold)
            => To(new
            {
                white_balance_histogram_area_threshold_srgb = new
                {
                    threshold
                }
            });
        
        /// <summary>
        /// Set the transparency of the image from 0 (transparent) to 1 (opaque)
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public BuildNode TransparencySrgb(float opacity)
            => To(new
            {
                color_filter_srgb = new
                {
                    alpha = opacity
                }
            });
        
        /// <summary>
        /// Adjust contrast between -1 and 1. 
        /// </summary>
        /// <param name="amount">-1...1</param>
        /// <returns></returns>
        public BuildNode ContrastSrgb(float amount)
            => To(new
            {
                color_filter_srgb = new
                {
                    contrast = amount
                }
            });
        
        /// <summary>
        /// Adjust brightness between -1 and 1. 
        /// </summary>
        /// <param name="amount">-1...1</param>
        /// <returns></returns>
        public BuildNode BrightnessSrgb(float amount)
            => To(new
            {
                color_filter_srgb = new
                {
                    brightness = amount
                }
            });
        
        /// <summary>
        /// Adjust saturation between -1 and 1. 
        /// </summary>
        /// <param name="amount">-1...1</param>
        /// <returns></returns>
        public BuildNode SaturationSrgb(float amount)
            => To(new
            {
                color_filter_srgb = new
                {
                    saturation = amount
                }
            });

        /// <summary>
        /// Apply filters like grayscale, sepia, or inversion in the sRGB color space
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public BuildNode ColorFilterSrgb(ColorFilterSrgb filter)
            => To(new
            {
                color_filter_srgb = filter.ToString().ToLowerInvariant()
            });

        public BuildNode Watermark(IBytesSource source, WatermarkOptions watermark) =>
            Watermark(source, null, watermark);
        
        public BuildNode Watermark(IBytesSource source, int? ioId, WatermarkOptions watermark)
        {
            if (ioId == null)
            {
                ioId = this.Builder.GenerateIoId();
            }
            this.Builder.AddInput(ioId.Value, source);
            return To(new
            {
                watermark = watermark.ToImageflowDynamic(ioId.Value)
            });
            
        }
            
//        public BuildNode Clone() => new BuildNode(NodeData,Input,Canvas,Uid);
//        public BuildNode Branch() => Clone();
        
//        public FluentGraphBuilder Builder() => new FluentGraphBuilder(this);
//        public object ToBuildMessage() => Builder().to_framewise().
    }
}
