using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AvaloniaNodeEditor.Models;

/// <summary>Data transfer object representing the complete node graph state for serialization.</summary>
public class GraphData
{
    /// <summary>The name of the graph/system.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>The version of the graph/system.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>
    /// The tabs in the graph editor.  When present this supersedes the legacy
    /// <see cref="Nodes" />, <see cref="Layout" /> and <see cref="Connections" /> fields.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TabData>? Tabs { get; set; }

    /// <summary>Legacy single-tab nodes list.  Used only when <see cref="Tabs" /> is absent.</summary>
    public List<NodeData> Nodes { get; set; } = [];

    /// <summary>Legacy single-tab layout dictionary.  Used only when <see cref="Tabs" /> is absent.</summary>
    public Dictionary<string, LayoutData> Layout { get; set; } = [];

    /// <summary>Legacy single-tab connections list.  Used only when <see cref="Tabs" /> is absent.</summary>
    public List<ConnectionData> Connections { get; set; } = [];
}

/// <summary>Data transfer object representing a single graph tab for serialization.</summary>
public class TabData
{
    /// <summary>The display title of this tab.</summary>
    public string Title { get; set; } = "Tab 1";

    /// <summary>The nodes in this tab's graph.</summary>
    public List<NodeData> Nodes { get; set; } = [];

    /// <summary>The visual layout positions of nodes, keyed by node name.</summary>
    public Dictionary<string, LayoutData> Layout { get; set; } = [];

    /// <summary>The connections between node connectors in this tab's graph.</summary>
    public List<ConnectionData> Connections { get; set; } = [];
}

/// <summary>Data transfer object representing the visual position of a node for serialization.</summary>
public class LayoutData
{
    /// <summary>The X coordinate of the node on the canvas.</summary>
    public double X { get; set; }

    /// <summary>The Y coordinate of the node on the canvas.</summary>
    public double Y { get; set; }
}

/// <summary>Data transfer object representing a single node for serialization.</summary>
public class NodeData
{
    /// <summary>The unique name of this node instance.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The node type name (e.g., "Number Producer").</summary>
    public string Type { get; set; } = string.Empty;

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

    /// <summary>Pair identifier for Tab Pass nodes (shared between the two sibling nodes).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PairId { get; set; }

    /// <summary>The shared display label for Tab Pass node pairs.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PairLabel { get; set; }

    /// <summary>Connector slot definitions for Tab Pass nodes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SlotData>? Slots { get; set; }
}

/// <summary>Data transfer object representing a connector slot in a Tab Pass node.</summary>
public class SlotData
{
    /// <summary>The display name of the connector slot.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this slot is an input on its owner node.</summary>
    public bool IsInput { get; set; }
}

/// <summary>Data transfer object representing a connection between two node connectors for serialization.</summary>
public class ConnectionData
{
    /// <summary>The source endpoint in "NodeName.ConnectorName" format.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>The target endpoint in "NodeName.ConnectorName" format.</summary>
    public string To { get; set; } = string.Empty;
}
