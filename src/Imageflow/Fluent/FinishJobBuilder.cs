namespace Imageflow.Fluent;

/// <summary>
/// Allows job execution in a fluent way
/// </summary>
public class FinishJobBuilder
{
    private readonly ImageJob _builder;
    private CancellationToken _token;
    private CancellationTokenSource? _tokenSource;
    private SecurityOptions? _security;

    internal FinishJobBuilder(ImageJob imageJob, CancellationToken cancellationToken)
    {
        _builder = imageJob;
        _token = cancellationToken;
    }

    public FinishJobBuilder WithSecurityOptions(SecurityOptions securityOptions)
    {
        _security = securityOptions;
        return this;
    }
    public FinishJobBuilder SetSecurityOptions(SecurityOptions securityOptions)
    {
        _security = securityOptions;
        return this;
    }

    /// <summary>
    /// Replaces the cancellation token
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public FinishJobBuilder WithCancellationToken(CancellationToken token)
    {
        _token = token;
        return this;
    }
    /// <summary>
    /// Replaces the cancellation token
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public FinishJobBuilder SetCancellationToken(CancellationToken token)
    {
        _token = token;
        return this;
    }

    /// <summary>
    /// Replaces the CancellationToken with a timeout
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <returns></returns>
    public FinishJobBuilder WithCancellationTimeout(int milliseconds)
    {
        _tokenSource = new CancellationTokenSource(milliseconds);
        return WithCancellationToken(_tokenSource.Token);
    }
    /// <summary>
    /// Replaces the CancellationToken with a timeout
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <returns></returns>
    public FinishJobBuilder SetCancellationTimeout(int milliseconds)
    {
        _tokenSource = new CancellationTokenSource(milliseconds);
        return WithCancellationToken(_tokenSource.Token);
    }

    public Task<BuildJobResult> InProcessAsync() => _builder.FinishAsync(new JobExecutionOptions(), _security, _token);

    public Task<BuildJobResult> InSubprocessAsync(string? imageflowToolPath = null, long? outputBufferCapacity = null) =>
        _builder.FinishInSubprocessAsync(_security, imageflowToolPath, outputBufferCapacity, _token);

    /// <summary>
    /// Returns a prepared job that can be executed with `imageflow_tool --json [job.JsonPath]`. Supporting input/output files are also created.
    /// If deleteFilesOnDispose is true, then the files will be deleted when the job is disposed. 
    /// </summary>
    /// <returns></returns>
    public Task<IPreparedFilesystemJob> WriteJsonJobAndInputs(bool deleteFilesOnDispose) =>
        _builder.WriteJsonJobAndInputs(_token, _security, deleteFilesOnDispose);

    internal string ToJsonDebug(SecurityOptions? securityOptions = null) => _builder.ToJsonDebug(securityOptions);

    public async Task<BuildJobResult> InProcessAndDisposeAsync()
    {
        BuildJobResult r;
        try
        {
            r = await InProcessAsync().ConfigureAwait(false);
        }
        finally
        {
            _builder.Dispose();
        }

        return r;
    }

}
