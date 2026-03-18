using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AvaloniaNodeEditor.Models;

/// <summary>Data transfer object representing the complete node graph state for serialization.</summary>
public class GraphData
{
    /// <summary>The nodes in the graph.</summary>
    public List<NodeData> Nodes { get; set; } = [];

    /// <summary>The connections between node connectors.</summary>
    public List<ConnectionData> Connections { get; set; } = [];
}

/// <summary>Data transfer object representing a single node for serialization.</summary>
public class NodeData
{
    /// <summary>The node type name (e.g., "Number Producer").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The X coordinate of the node on the canvas.</summary>
    public double X { get; set; }

    /// <summary>The Y coordinate of the node on the canvas.</summary>
    public double Y { get; set; }

    /// <summary>Value property for Number Producer, Number Reporter, and Random Number Generator nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Value { get; set; }

    /// <summary>Input A for Arithmetic Transform nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? InputA { get; set; }

    /// <summary>Input B for Arithmetic Transform nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? InputB { get; set; }

    /// <summary>Result for Arithmetic Transform nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Result { get; set; }

    /// <summary>Operation for Arithmetic Transform nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operation { get; set; }

    /// <summary>Minimum value for Random Number Generator nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MinValue { get; set; }

    /// <summary>Maximum value for Random Number Generator nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxValue { get; set; }

    /// <summary>Input value for Pass Filter nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? InputValue { get; set; }

    /// <summary>Output value for Pass Filter nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? OutputValue { get; set; }

    /// <summary>Filter type for Pass Filter nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilterType { get; set; }

    /// <summary>Threshold for Pass Filter nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Threshold { get; set; }

    /// <summary>Upper threshold for Pass Filter nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? UpperThreshold { get; set; }
}

/// <summary>Data transfer object representing a connection between two connectors for serialization.</summary>
public class ConnectionData
{
    /// <summary>Index of the source node in the nodes list.</summary>
    public int SourceNodeIndex { get; set; }

    /// <summary>True if the source connector is in the node's Outputs collection; false for Inputs.</summary>
    public bool SourceIsOutput { get; set; }

    /// <summary>Index of the source connector within the node's Inputs or Outputs collection.</summary>
    public int SourceConnectorIndex { get; set; }

    /// <summary>Index of the target node in the nodes list.</summary>
    public int TargetNodeIndex { get; set; }

    /// <summary>True if the target connector is in the node's Outputs collection; false for Inputs.</summary>
    public bool TargetIsOutput { get; set; }

    /// <summary>Index of the target connector within the node's Inputs or Outputs collection.</summary>
    public int TargetConnectorIndex { get; set; }
}
