using System.Text.Json;
using System.Text.Json.Nodes;
using Imageflow.Bindings;

namespace Imageflow.CodeGen;

/// <summary>
/// Schemas loaded from the native imageflow library.
/// </summary>
public record LoadedSchemas
{
    /// <summary>V3 node definitions keyed by node ID (from $defs).</summary>
    public Dictionary<string, JsonObject>? NodeDefs { get; init; }
    /// <summary>V3 querystring key groups keyed by node ID.</summary>
    public Dictionary<string, JsonObject>? QsKeyGroups { get; init; }
    /// <summary>V3 querystring JSON Schema.</summary>
    public JsonObject? QsSchema { get; init; }
    /// <summary>V1 OpenAPI schema (raw JSON string).</summary>
    public string? V1OpenApi { get; init; }
}

/// <summary>
/// Loads schemas from the native imageflow library via JSON endpoints.
/// </summary>
public static class SchemaLoader
{
    public static LoadedSchemas LoadFromNativeLib()
    {
        using var ctx = new JobContext();

        var nodeDefs = CallEndpoint(ctx, "v3/schema/nodes");
        var qsKeys = CallEndpoint(ctx, "v3/schema/querystring/keys");
        var qsSchema = CallEndpoint(ctx, "v3/schema/querystring");
        var v1OpenApi = CallEndpointRaw(ctx, "v1/schema/openapi/latest/get");

        // Extract $defs from the node schemas
        Dictionary<string, JsonObject>? defs = null;
        if (nodeDefs?["$defs"] is JsonObject defsObj)
        {
            defs = new Dictionary<string, JsonObject>();
            foreach (var (key, value) in defsObj)
            {
                if (value is JsonObject obj)
                    defs[key] = obj;
            }
        }

        // Extract nodes from QS keys
        Dictionary<string, JsonObject>? groups = null;
        if (qsKeys?["nodes"] is JsonObject nodesObj)
        {
            groups = new Dictionary<string, JsonObject>();
            foreach (var (key, value) in nodesObj)
            {
                if (value is JsonObject obj)
                    groups[key] = obj;
            }
        }

        return new LoadedSchemas
        {
            NodeDefs = defs,
            QsKeyGroups = groups,
            QsSchema = qsSchema?.AsObject(),
            V1OpenApi = v1OpenApi,
        };
    }

    /// <summary>
    /// Load schemas from JSON files on disk (fallback when native lib unavailable).
    /// </summary>
    public static LoadedSchemas LoadFromFiles(string schemaDir)
    {
        var nodesPath = Path.Combine(schemaDir, "v3_nodes.json");
        var qsKeysPath = Path.Combine(schemaDir, "v3_qs_keys.json");
        var qsSchemaPath = Path.Combine(schemaDir, "v3_qs_schema.json");

        JsonObject? nodes = File.Exists(nodesPath)
            ? JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(nodesPath))
            : null;
        JsonObject? qsKeys = File.Exists(qsKeysPath)
            ? JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(qsKeysPath))
            : null;
        JsonObject? qsSchema = File.Exists(qsSchemaPath)
            ? JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(qsSchemaPath))
            : null;

        Dictionary<string, JsonObject>? defs = null;
        if (nodes?["$defs"] is JsonObject defsObj)
        {
            defs = new Dictionary<string, JsonObject>();
            foreach (var (key, value) in defsObj)
            {
                if (value is JsonObject obj)
                    defs[key] = obj;
            }
        }

        Dictionary<string, JsonObject>? groups = null;
        if (qsKeys?["nodes"] is JsonObject nodesKvObj)
        {
            groups = new Dictionary<string, JsonObject>();
            foreach (var (key, value) in nodesKvObj)
            {
                if (value is JsonObject obj)
                    groups[key] = obj;
            }
        }

        return new LoadedSchemas
        {
            NodeDefs = defs,
            QsKeyGroups = groups,
            QsSchema = qsSchema,
        };
    }

#pragma warning disable CS0618 // GetStream is deprecated but it's the only way to read raw JSON
    private static string ReadResponse(IJsonResponseProvider response)
    {
        using var stream = response.GetStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
#pragma warning restore CS0618

    private static JsonNode? CallEndpoint(JobContext ctx, string endpoint)
    {
        try
        {
            using var response = ctx.SendJsonBytes(endpoint, "{}"u8.ToArray());
            var json = ReadResponse(response);
            // The response is wrapped: {"code":200,"success":true,"message":"OK","data":{...}}
            var wrapper = JsonSerializer.Deserialize<JsonObject>(json);
            if (wrapper?["data"] is JsonNode data)
                return data;
            // Some endpoints return data directly
            return JsonSerializer.Deserialize<JsonNode>(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Warning: endpoint '{endpoint}' failed: {ex.Message}");
            return null;
        }
    }

    private static string? CallEndpointRaw(JobContext ctx, string endpoint)
    {
        try
        {
            using var response = ctx.SendJsonBytes(endpoint, "{}"u8.ToArray());
            return ReadResponse(response);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Warning: endpoint '{endpoint}' failed: {ex.Message}");
            return null;
        }
    }
}
