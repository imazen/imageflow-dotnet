using System.Reflection;
using System.Text.Json.Serialization;

using Imageflow.Bindings;
using Imageflow.Fluent;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapGet("/", () => Results.Text("Hello, world!"));

var imageflowApi = app.MapGroup("/imageflow");
imageflowApi.MapGet("/version", () =>
{
    using var c = new JobContext();
    var vi = c.GetVersionInfo();
    return Results.Ok(vi);
});
imageflowApi.MapGet("/resize/width/{width}", async (int width) =>
{
    var resultMemory = await Helpers.SizeIcon(width).ConfigureAwait(false);
    return Results.Bytes(resultMemory, "image/jpeg");
});

// Test basics during startup
using var c = new JobContext();
var _ = c.GetVersionInfo();
var t = Helpers.SizeIcon(10);
var unused = t.Result;

app.Run();

internal static class Helpers
{
    public static async Task<Memory<byte>> SizeIcon(int width)
    {
        var imgBytes = GetResourceBytes("icon.png");
        var job = await new ImageJob()
            .Decode(imgBytes)
            .Constrain(new Constraint((uint)width, 0))
            .EncodeToBytes(new MozJpegEncoder(90)).Finish().InProcessAsync().ConfigureAwait(false);
        var resultBytes = job.First!.TryGetBytes()!.Value;
        return new Memory<byte>(resultBytes.Array, resultBytes.Offset, resultBytes.Count);
    }

    private static byte[] GetResourceBytes(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new ArgumentException($"Resource {resourceName} not found in assembly {assembly.FullName}");
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}


[JsonSerializable(typeof(VersionInfo))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
