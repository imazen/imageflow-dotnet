using System.Text.Json;
using System.Text.Json.Nodes;
using Imageflow.CodeGen;

// ─── Configuration ───
var outputDir = args.Length > 0 ? args[0] : Path.Combine(
    Directory.GetCurrentDirectory(), "..", "Imageflow", "Generated");

Console.WriteLine($"Imageflow CodeGen — generating to {Path.GetFullPath(outputDir)}");

// ─── Load schemas from native library ───
Console.WriteLine("Loading schemas from native imageflow library...");

var schemas = SchemaLoader.LoadFromNativeLib();

Console.WriteLine($"  V3 node defs: {schemas.NodeDefs?.Count ?? 0}");
Console.WriteLine($"  V3 QS key groups: {schemas.QsKeyGroups?.Count ?? 0}");

// ─── Generate V3 files ───
Console.WriteLine("Generating V3 types...");
Directory.CreateDirectory(Path.Combine(outputDir, "V3"));

var v3Gen = new V3Generator(schemas);
var files = v3Gen.GenerateAll();

foreach (var (filename, content) in files)
{
    var path = Path.Combine(outputDir, "V3", filename);
    File.WriteAllText(path, content);
    Console.WriteLine($"  wrote {filename} ({content.Length} bytes)");
}

Console.WriteLine($"Done. Generated {files.Count} files.");
