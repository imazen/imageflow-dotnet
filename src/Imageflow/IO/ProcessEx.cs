using System.Diagnostics;
using System.Text;

using Microsoft.IO;

namespace Imageflow.IO;

internal sealed
    class ProcessResults : IDisposable
{
    public ProcessResults(Process process, string[] standardOutput, string[] standardError)
    {
        Process = process;
        ExitCode = process.ExitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    public Process Process { get; }
    public int ExitCode { get; }
    public string[] StandardOutput { get; }
    public string[] StandardError { get; }
    public void Dispose() { Process.Dispose(); }

    public MemoryStream GetBufferedOutputStream() => GetStreamForStrings(StandardOutput);
    public MemoryStream GetBufferedErrorStream() => GetStreamForStrings(StandardError);

    public string GetStandardOutputString() => string.Join("", StandardOutput);
    public string GetStandardErrorString() => string.Join("", StandardError);

    private static readonly RecyclableMemoryStreamManager Mgr = new RecyclableMemoryStreamManager();
    static MemoryStream GetStreamForStrings(string[] strings)
    {
        // Initial alloc is optimized for ascii
        var stream = Mgr.GetStream("stdout bytes", strings.Sum(s => s.Length));
        var streamWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true);
        foreach (var str in strings)
        {
            streamWriter.Write(str);
        }
        streamWriter.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}

internal static class ProcessEx
{

    public static Task<ProcessResults> RunAsync(string fileName)
        => RunAsync(new ProcessStartInfo(fileName));

    public static Task<ProcessResults> RunAsync(string fileName, string arguments)
        => RunAsync(new ProcessStartInfo(fileName, arguments));

    public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo)
        => RunAsync(processStartInfo, CancellationToken.None);

    public static async Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken)
    {
        // force some settings in the start info so we can capture the output
        processStartInfo.UseShellExecute = false;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        var tcs = new TaskCompletionSource<ProcessResults>();

        var standardOutput = new List<string>();
        var standardError = new List<string>();

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        var standardOutputResults = new TaskCompletionSource<string[]>();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                standardOutput.Add(args.Data);
            }
            else
            {
                standardOutputResults.SetResult(standardOutput.ToArray());
            }
        };

        var standardErrorResults = new TaskCompletionSource<string[]>();
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                standardError.Add(args.Data);
            }
            else
            {
                standardErrorResults.SetResult(standardError.ToArray());
            }
        };

        process.Exited += (sender, args) =>
        {
            // Since the Exited event can happen asynchronously to the output and error events, 
            // we use the task results for stdout/stderr to ensure they both closed
            tcs.TrySetResult(new ProcessResults(process, standardOutputResults.Task.Result, standardErrorResults.Task.Result));
        };

        using (cancellationToken.Register(
            callback: () =>
            {
                tcs.TrySetCanceled();
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (InvalidOperationException) { }
            }))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (process.Start() == false)
            {
                tcs.TrySetException(new InvalidOperationException("Failed to start process"));
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
