using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaNodeEditor.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>The collection of nodes displayed in the editor canvas.</summary>
    public ObservableCollection<NodeViewModel> Nodes { get; } = [];

    /// <summary>The collection of connections between node connectors.</summary>
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    /// <summary>The currently selected node, shown in the properties panel.</summary>
    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    /// <summary>The name of the graph/system.</summary>
    [ObservableProperty]
    private string _graphName = string.Empty;

    /// <summary>The version of the graph/system.</summary>
    [ObservableProperty]
    private string _graphVersion = string.Empty;

    /// <summary>The node types available in the toolbox.</summary>
    public IReadOnlyList<string> ToolboxItems { get; } =
    [
        "Number Producer",
        "Number Reporter",
        "Arithmetic Transform",
        "Random Number Generator",
        "Pass Filter",
    ];

    /// <summary>Adds a new node of the given type to the canvas at a default staggered position.</summary>
    [RelayCommand]
    private void AddNode(string? nodeType)
    {
        if (nodeType is null)
        {
            return;
        }

        // Stagger new nodes so they do not overlap
        var offset = new Point(60 + (Nodes.Count * 30 % 300), 60 + (Nodes.Count * 20 % 200));
        AddNodeAt(nodeType, offset);
    }

    /// <summary>Adds a new node of the given type to the canvas at the specified canvas position.</summary>
    /// <param name="nodeType">The display name of the node type to create (must match a <see cref="ToolboxItems"/> entry).</param>
    /// <param name="canvasPosition">The position in canvas coordinates where the node will be placed.</param>
    public void AddNodeAt(string nodeType, Point canvasPosition)
    {
        NodeViewModel node = nodeType switch
        {
            "Number Producer" => new NumberProducerNode { Location = canvasPosition },
            "Number Reporter" => new NumberReporterNode { Location = canvasPosition },
            "Arithmetic Transform" => new ArithmeticTransformNode { Location = canvasPosition },
            "Random Number Generator" => new RandomNumberGeneratorNode { Location = canvasPosition },
            "Pass Filter" => new PassFilterNode { Location = canvasPosition },
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null),
        };

        node.Name = GenerateUniqueName(nodeType);

        // Track selection and name changes.
        TrackNodeSelection(node);
        TrackNodeName(node);

        Nodes.Add(node);
        SelectedNode = node;
    }

    /// <summary>Called by the editor when the user finishes dragging a connection to a target connector.
    /// <para>The editor passes a <c>(source, target)</c> tuple as the argument.
    /// Only output-to-input connections are allowed. If the user drags from an input to an
    /// output the endpoints are automatically swapped.</para></summary>
    [RelayCommand]
    private void ConnectionCompleted(object? args)
    {
        if (args is not (ConnectorViewModel source, object targetObj) || targetObj is not ConnectorViewModel target)
        {
            return;
        }

        if (source == target)
        {
            return;
        }

        // Determine whether each endpoint is an input or output in a single pass.
        bool sourceIsOutput = IsOutputConnector(source);
        bool sourceIsInput = !sourceIsOutput && IsInputConnector(source);
        bool targetIsInput = IsInputConnector(target);
        bool targetIsOutput = !targetIsInput && IsOutputConnector(target);

        if (sourceIsOutput && targetIsInput)
        {
            // Normal direction — output → input
        }
        else if (sourceIsInput && targetIsOutput)
        {
            // User dragged backwards — swap so the connection is output → input
            (source, target) = (target, source);
        }
        else
        {
            // Both are outputs, both are inputs, or not found on any node
            return;
        }

        Connections.Add(new ConnectionViewModel(source, target));
        source.IsConnected = true;
        target.IsConnected = true;
    }

    /// <summary>Removes a connection from the graph.</summary>
    [RelayCommand]
    private void RemoveConnection(object? connection)
    {
        if (connection is not ConnectionViewModel conn)
        {
            return;
        }

        Connections.Remove(conn);

        // Re-evaluate IsConnected for both endpoints
        UpdateIsConnected(conn.Source);
        UpdateIsConnected(conn.Target);
    }

    /// <summary>Removes all connections attached to a given connector.</summary>
    [RelayCommand]
    private void DisconnectConnector(object? connector)
    {
        if (connector is not ConnectorViewModel conn)
        {
            return;
        }

        var toRemove = Connections.Where(c => c.Source == conn || c.Target == conn).ToList();
        foreach (var connection in toRemove)
        {
            Connections.Remove(connection);
            UpdateIsConnected(connection.Source);
            UpdateIsConnected(connection.Target);
        }
    }

    /// <summary>Clears all nodes and connections from the graph.</summary>
    [RelayCommand]
    private void NewGraph()
    {
        Connections.Clear();
        Nodes.Clear();
        SelectedNode = null;
        GraphName = string.Empty;
        GraphVersion = string.Empty;
    }

    /// <summary>Quits the application.</summary>
    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>Serializes the current node graph to a JSON string.</summary>
    /// <returns>A JSON string representing the graph state.</returns>
    public string SerializeGraph()
    {
        var data = new GraphData
        {
            Name = string.IsNullOrWhiteSpace(GraphName) ? null : GraphName,
            Version = string.IsNullOrWhiteSpace(GraphVersion) ? null : GraphVersion,
        };

        foreach (var node in Nodes)
        {
            var nodeData = new NodeData
            {
                Name = node.Name,
                Type = node.NodeType,
            };

            switch (node)
            {
                case NumberProducerNode np:
                    nodeData.Value = np.Value;
                    break;
                case NumberReporterNode nr:
                    nodeData.Value = nr.Value;
                    break;
                case ArithmeticTransformNode at:
                    nodeData.InputA = at.InputA;
                    nodeData.InputB = at.InputB;
                    nodeData.Result = at.Result;
                    nodeData.Operation = at.Operation.ToString();
                    break;
                case RandomNumberGeneratorNode rng:
                    nodeData.MinValue = rng.MinValue;
                    nodeData.MaxValue = rng.MaxValue;
                    nodeData.Value = rng.Value;
                    break;
                case PassFilterNode pf:
                    nodeData.InputValue = pf.InputValue;
                    nodeData.OutputValue = pf.OutputValue;
                    nodeData.FilterType = pf.FilterType.ToString();
                    nodeData.Threshold = pf.Threshold;
                    nodeData.UpperThreshold = pf.UpperThreshold;
                    break;
            }

            data.Nodes.Add(nodeData);

            // Store layout separately
            data.Layout[node.Name] = new LayoutData
            {
                X = node.Location.X,
                Y = node.Location.Y,
            };
        }

        foreach (var connection in Connections)
        {
            var (sourceNodeName, sourceConnectorName) = FindConnectorName(connection.Source);
            var (targetNodeName, targetConnectorName) = FindConnectorName(connection.Target);

            if (sourceNodeName is null || targetNodeName is null)
            {
                continue;
            }

            data.Connections.Add(new ConnectionData
            {
                From = $"{sourceNodeName}.{sourceConnectorName}",
                To = $"{targetNodeName}.{targetConnectorName}",
            });
        }

        return JsonSerializer.Serialize(data, GraphJsonContext.Default.GraphData);
    }

    /// <summary>Deserializes a JSON string and restores the node graph state.</summary>
    /// <param name="json">A JSON string previously produced by <see cref="SerializeGraph"/>.</param>
    public void DeserializeGraph(string json)
    {
        var data = JsonSerializer.Deserialize(json, GraphJsonContext.Default.GraphData)
            ?? throw new JsonException("Failed to deserialize graph data.");

        // Clear current state
        Connections.Clear();
        Nodes.Clear();
        SelectedNode = null;

        // Restore graph-level properties
        GraphName = data.Name ?? string.Empty;
        GraphVersion = data.Version ?? string.Empty;

        // Restore nodes
        foreach (var nodeData in data.Nodes)
        {
            // Resolve layout position from the layout dictionary
            var position = data.Layout.TryGetValue(nodeData.Name, out var layoutData)
                ? new Point(layoutData.X, layoutData.Y)
                : default;

            NodeViewModel node = nodeData.Type switch
            {
                "Number Producer" => CreateNumberProducer(nodeData, position),
                "Number Reporter" => CreateNumberReporter(nodeData, position),
                "Arithmetic Transform" => CreateArithmeticTransform(nodeData, position),
                "Random Number Generator" => CreateRandomNumberGenerator(nodeData, position),
                "Pass Filter" => CreatePassFilter(nodeData, position),
                _ => throw new JsonException($"Unknown node type: {nodeData.Type}"),
            };

            node.Name = nodeData.Name;

            // Track selection and name changes.
            TrackNodeSelection(node);
            TrackNodeName(node);

            Nodes.Add(node);
        }

        // Restore connections
        foreach (var connectionData in data.Connections)
        {
            var source = ResolveConnector(connectionData.From);
            var target = ResolveConnector(connectionData.To);

            if (source is null || target is null)
            {
                continue;
            }

            Connections.Add(new ConnectionViewModel(source, target));
            source.IsConnected = true;
            target.IsConnected = true;
        }

        ValidateAllNodeNames();
    }

    /// <summary>Generates a unique default name for a new node of the given type.</summary>
    internal string GenerateUniqueName(string nodeType)
    {
        int index = 1;
        while (Nodes.Any(n => n.Name == $"{nodeType} {index}"))
        {
            index++;
        }

        return $"{nodeType} {index}";
    }

    /// <summary>Validates all node names for uniqueness and sets <see cref="NodeViewModel.HasNameError"/> accordingly.</summary>
    internal void ValidateAllNodeNames()
    {
        var nameGroups = Nodes.GroupBy(n => n.Name);
        foreach (var group in nameGroups)
        {
            bool hasDuplicate = group.Count() > 1;
            foreach (var node in group)
            {
                node.HasNameError = hasDuplicate;
            }
        }
    }

    private static NumberProducerNode CreateNumberProducer(NodeData data, Point position)
    {
        return new NumberProducerNode
        {
            Location = position,
            Value = data.Value ?? 0,
        };
    }

    private static NumberReporterNode CreateNumberReporter(NodeData data, Point position)
    {
        return new NumberReporterNode
        {
            Location = position,
            Value = data.Value ?? 0,
        };
    }

    private static ArithmeticTransformNode CreateArithmeticTransform(NodeData data, Point position)
    {
        return new ArithmeticTransformNode
        {
            Location = position,
            InputA = data.InputA ?? 0,
            InputB = data.InputB ?? 0,
            Result = data.Result ?? 0,
            Operation = Enum.TryParse<ArithmeticOperation>(data.Operation, out var op)
                ? op
                : ArithmeticOperation.Add,
        };
    }

    private static RandomNumberGeneratorNode CreateRandomNumberGenerator(NodeData data, Point position)
    {
        return new RandomNumberGeneratorNode
        {
            Location = position,
            MinValue = data.MinValue ?? 0,
            MaxValue = data.MaxValue ?? 1.0,
            Value = data.Value ?? 0,
        };
    }

    private static PassFilterNode CreatePassFilter(NodeData data, Point position)
    {
        return new PassFilterNode
        {
            Location = position,
            InputValue = data.InputValue ?? 0,
            OutputValue = data.OutputValue ?? 0,
            FilterType = Enum.TryParse<FilterType>(data.FilterType, out var ft)
                ? ft
                : FilterType.LowPass,
            Threshold = data.Threshold ?? 0,
            UpperThreshold = data.UpperThreshold ?? 0,
        };
    }

    /// <summary>Finds the node name and connector name for a given connector view model.</summary>
    private (string? nodeName, string? connectorName) FindConnectorName(ConnectorViewModel connector)
    {
        foreach (var node in Nodes)
        {
            foreach (var output in node.Outputs)
            {
                if (output == connector)
                {
                    return (node.Name, output.Name);
                }
            }

            foreach (var input in node.Inputs)
            {
                if (input == connector)
                {
                    return (node.Name, input.Name);
                }
            }
        }

        return (null, null);
    }

    /// <summary>Resolves a connector from a "NodeName.ConnectorName" string.</summary>
    private ConnectorViewModel? ResolveConnector(string endpoint)
    {
        var dotIndex = endpoint.LastIndexOf('.');
        if (dotIndex < 0)
        {
            return null;
        }

        var nodeName = endpoint[..dotIndex];
        var connectorName = endpoint[(dotIndex + 1)..];

        var node = Nodes.FirstOrDefault(n => n.Name == nodeName);
        if (node is null)
        {
            return null;
        }

        return node.Outputs.FirstOrDefault(c => c.Name == connectorName)
            ?? node.Inputs.FirstOrDefault(c => c.Name == connectorName);
    }

    /// <summary>Subscribes to <paramref name="node"/>'s PropertyChanged so the properties panel
    /// stays in sync with the selected node.</summary>
    private void TrackNodeSelection(NodeViewModel node)
    {
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.IsSelected))
            {
                if (node.IsSelected)
                {
                    SelectedNode = node;
                }
                else if (SelectedNode == node)
                {
                    SelectedNode = null;
                }
            }
        };
    }

    /// <summary>Subscribes to <paramref name="node"/>'s PropertyChanged to revalidate
    /// name uniqueness when the node name changes.</summary>
    private void TrackNodeName(NodeViewModel node)
    {
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.Name))
            {
                ValidateAllNodeNames();
            }
        };
    }

    private void UpdateIsConnected(ConnectorViewModel connector)
    {
        connector.IsConnected = Connections.Any(
            c => c.Source == connector || c.Target == connector);
    }

    /// <summary>Returns true when <paramref name="connector"/> belongs to any node's <see cref="NodeViewModel.Outputs"/> collection.</summary>
    internal bool IsOutputConnector(ConnectorViewModel connector) =>
        Nodes.Any(n => n.Outputs.Contains(connector));

    /// <summary>Returns true when <paramref name="connector"/> belongs to any node's <see cref="NodeViewModel.Inputs"/> collection.</summary>
    internal bool IsInputConnector(ConnectorViewModel connector) =>
        Nodes.Any(n => n.Inputs.Contains(connector));
}

