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

var imageflowApi = app.MapGroup("/imageflow");
imageflowApi.MapGet("/version", () =>
{
    using var c = new JobContext();
    var vi = c.GetVersionInfo();
    
    return Results.Ok(vi);
});
imageflowApi.MapGet("/resize/width/{width}", async(int width) =>
{
    var imgBytes = Helpers.GetResourceBytes("icon.png");
    var job = await new Imageflow.Fluent.ImageJob()
        .Decode(imgBytes)
        .Constrain(new Constraint((uint)width, 0))
        .EncodeToBytes(new Imageflow.Fluent.MozJpegEncoder(90)).Finish().InProcessAsync();
    var resultBytes = job.First!.TryGetBytes()!.Value;
    var resultMemory = new Memory<byte>(resultBytes.Array, resultBytes.Offset, resultBytes.Count);
    return Results.Bytes(resultMemory, "image/jpeg");
});

app.Run();

internal static class Helpers
{
    public static byte[] GetResourceBytes(string resourceName)
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
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}