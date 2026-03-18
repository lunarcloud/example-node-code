using System.Text.Json.Serialization;

namespace AvaloniaNodeEditor.Models;

/// <summary>Source-generated JSON serialization context for graph data types.</summary>
[JsonSerializable(typeof(GraphData))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GraphJsonContext : JsonSerializerContext
{
}
