using System;
using System.Threading;

namespace Imageflow.Fluent
{

    /// <summary>
    /// Represents an endpoint in the operations graph, such as an Encode node. No more nodes can be chained to this one.
    /// Only allows executing the job.
    /// </summary>
    public class BuildEndpoint : BuildItemBase
    {
        internal BuildEndpoint(ImageJob builder,object nodeData, BuildNode inputNode, BuildNode canvasNode) : base(builder, nodeData, inputNode,
            canvasNode){}
        
        public FinishJobBuilder Finish() => new FinishJobBuilder(Builder, default);
        
        [Obsolete("Use Finish().WithCancellationToken")]
        public FinishJobBuilder FinishWithToken(CancellationToken token) => new FinishJobBuilder(Builder, token);

        [Obsolete("Use Finish().WithCancellationTimeout")]
        public FinishJobBuilder FinishWithTimeout(int milliseconds)
        {
            using (var tokenSource = new CancellationTokenSource(milliseconds))
            {
                return FinishWithToken(tokenSource.Token);
            }
        }
        
    }
    
    
    
}
