using System.Threading;
using System.Threading.Tasks;

namespace Imageflow.Fluent
{
    /// <summary>
    /// Allows job execution in a fluent way
    /// </summary>
    public class BuildEndpointWithToken
    {
        private readonly ImageJob _builder;
        private readonly CancellationToken _token;

        public BuildEndpointWithToken(ImageJob ImageJob, CancellationToken cancellationToken)
        {
            _builder = ImageJob;
            _token = cancellationToken;
        }


        public Task<BuildJobResult> InProcessAsync() => _builder.FinishAsync(_token);

        public Task<BuildJobResult> InSubprocessAsync(string imageflowToolPath = null) =>
            _builder.FinishInSubprocessAsync(_token, imageflowToolPath);

        /// <summary>
        /// Returns a prepared job that can be executed with `imageflow_tool --json [job.JsonPath]`. Supporting input/output files are also created.
        /// If deleteFilesOnDispose is true, then the files will be deleted when the job is disposed. 
        /// </summary>
        /// <returns></returns>
        public Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(bool deleteFilesOnDispose) =>
            _builder.WriteJsonJobAndInputs(_token, deleteFilesOnDispose);
    }


}