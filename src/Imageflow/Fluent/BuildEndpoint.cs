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
        private readonly FluentBuildJob _builder;
        private readonly CancellationToken _token;

        public BuildEndpointWithToken(FluentBuildJob fluentBuildJob, CancellationToken cancellationToken)
        {
            _builder = fluentBuildJob;
            _token = cancellationToken;
        }


        public Task<BuildJobResult> InProcessAsync() => _builder.FinishAsync(_token);
        
        public Task<BuildJobResult> InSubprocessAsync(string imageflowToolPath = null) => _builder.FinishInSubprocessAsync(_token, imageflowToolPath);

        /// <summary>
        /// Returns a prepared job that can be executed with `imageflow_tool --json [job.JsonPath]`. Supporting input/output files are also created.
        /// If deleteFilesOnDispose is true, then the files will be deleted when the job is disposed. 
        /// </summary>
        /// <returns></returns>
        public Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(bool deleteFilesOnDispose) =>
            _builder.WriteJsonJobAndInputs(_token, deleteFilesOnDispose);
    }
    
    
    
}
