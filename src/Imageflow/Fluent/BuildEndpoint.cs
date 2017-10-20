using System.Threading;
using System.Threading.Tasks;

namespace Imageflow.Fluent
{

  
    public class BuildEndpoint : BuildItemBase
    {
        internal BuildEndpoint(FluentBuildJob builder,object nodeData, BuildNode inputNode, BuildNode canvasNode) : base(builder, nodeData, inputNode,
            canvasNode){}

        public BuildEndpointWithToken Finish() => new BuildEndpointWithToken(Builder, default(CancellationToken));
        public BuildEndpointWithToken FinishWithToken(CancellationToken token) => new BuildEndpointWithToken(Builder, token);

        public BuildEndpointWithToken FinishWithTimeout(int milliseconds)
        {
            var tokenSource = new CancellationTokenSource(milliseconds);
            return FinishWithToken(tokenSource.Token);
        }
        
    }

    public class BuildEndpointWithToken
    {
        private FluentBuildJob builder;
        private CancellationToken token;

        public BuildEndpointWithToken(FluentBuildJob fluentBuildJob, CancellationToken cancellationToken)
        {
            builder = fluentBuildJob;
            token = cancellationToken;
        }


        public Task<BuildJobResult> InProcessAsync() => builder.FinishAsync(token);
        
        public Task<BuildJobResult> InSubprocessAsync(string imageflowToolPath = null) => builder.FinishInSubprocessAsync(token, imageflowToolPath);


        
    }
    
    
    
}
