using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AvaloniaNodeEditor.Models;

/// <summary>Source-generated JSON serialization context for graph data types.</summary>
[JsonSerializable(typeof(GraphData))]
[JsonSerializable(typeof(TabData))]
[JsonSerializable(typeof(List<TabData>))]
[JsonSerializable(typeof(Dictionary<string, LayoutData>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GraphJsonContext : JsonSerializerContext
{
}
