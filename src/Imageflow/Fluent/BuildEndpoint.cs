using System.Threading;

namespace Imageflow.Fluent
{

    /// <summary>
    /// Represents an endpoint in the operations graph, such as an Encode node. No more nodes can be chained to this one.
    /// Only allows executing the job.
    /// </summary>
    public class BuildEndpoint : BuildItemBase
    {
        internal BuildEndpoint(FluentBuildJob builder,object nodeData, BuildNode inputNode, BuildNode canvasNode) : base(builder, nodeData, inputNode,
            canvasNode){}

        public BuildEndpointWithToken Finish() => new BuildEndpointWithToken(Builder, default);
        public BuildEndpointWithToken FinishWithToken(CancellationToken token) => new BuildEndpointWithToken(Builder, token);

        public BuildEndpointWithToken FinishWithTimeout(int milliseconds)
        {
            using (var tokenSource = new CancellationTokenSource(milliseconds))
            {
                return FinishWithToken(tokenSource.Token);
            }
        }
        
    }
    
    
    
}
