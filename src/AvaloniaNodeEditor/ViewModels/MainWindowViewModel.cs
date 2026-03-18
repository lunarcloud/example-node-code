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

        // Track selection changes so the properties panel stays in sync.
        TrackNodeSelection(node);

        Nodes.Add(node);
        SelectedNode = node;
    }

    /// <summary>Called by the editor when the user finishes dragging a connection to a target connector.
    /// <para>The editor passes a <c>(source, target)</c> tuple as the argument.</para></summary>
    [RelayCommand]
    private void ConnectionCompleted(object? args)
    {
        if (args is not (ConnectorViewModel source, object targetObj) || targetObj is not ConnectorViewModel target)
        {
            return;
        }

        if (source != target)
        {
            Connections.Add(new ConnectionViewModel(source, target));
            source.IsConnected = true;
            target.IsConnected = true;
        }
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

    /// <summary>Clears all nodes and connections from the graph.</summary>
    [RelayCommand]
    private void NewGraph()
    {
        Connections.Clear();
        Nodes.Clear();
        SelectedNode = null;
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
        var data = new GraphData();

        foreach (var node in Nodes)
        {
            var nodeData = new NodeData
            {
                Type = node.Name,
                X = node.Location.X,
                Y = node.Location.Y,
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
        }

        foreach (var connection in Connections)
        {
            var (sourceNodeIndex, sourceIsOutput, sourceConnectorIndex) = FindConnector(connection.Source);
            var (targetNodeIndex, targetIsOutput, targetConnectorIndex) = FindConnector(connection.Target);

            if (sourceNodeIndex < 0 || targetNodeIndex < 0)
            {
                continue;
            }

            data.Connections.Add(new ConnectionData
            {
                SourceNodeIndex = sourceNodeIndex,
                SourceIsOutput = sourceIsOutput,
                SourceConnectorIndex = sourceConnectorIndex,
                TargetNodeIndex = targetNodeIndex,
                TargetIsOutput = targetIsOutput,
                TargetConnectorIndex = targetConnectorIndex,
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

        // Restore nodes
        foreach (var nodeData in data.Nodes)
        {
            var position = new Point(nodeData.X, nodeData.Y);
            NodeViewModel node = nodeData.Type switch
            {
                "Number Producer" => CreateNumberProducer(nodeData, position),
                "Number Reporter" => CreateNumberReporter(nodeData, position),
                "Arithmetic Transform" => CreateArithmeticTransform(nodeData, position),
                "Random Number Generator" => CreateRandomNumberGenerator(nodeData, position),
                "Pass Filter" => CreatePassFilter(nodeData, position),
                _ => throw new JsonException($"Unknown node type: {nodeData.Type}"),
            };

            // Track selection changes so the properties panel stays in sync.
            TrackNodeSelection(node);

            Nodes.Add(node);
        }

        // Restore connections
        foreach (var connectionData in data.Connections)
        {
            if (connectionData.SourceNodeIndex < 0 || connectionData.SourceNodeIndex >= Nodes.Count ||
                connectionData.TargetNodeIndex < 0 || connectionData.TargetNodeIndex >= Nodes.Count)
            {
                continue;
            }

            var sourceNode = Nodes[connectionData.SourceNodeIndex];
            var targetNode = Nodes[connectionData.TargetNodeIndex];

            var sourceConnectors = connectionData.SourceIsOutput ? sourceNode.Outputs : sourceNode.Inputs;
            var targetConnectors = connectionData.TargetIsOutput ? targetNode.Outputs : targetNode.Inputs;

            if (connectionData.SourceConnectorIndex < 0 || connectionData.SourceConnectorIndex >= sourceConnectors.Count ||
                connectionData.TargetConnectorIndex < 0 || connectionData.TargetConnectorIndex >= targetConnectors.Count)
            {
                continue;
            }

            var source = sourceConnectors[connectionData.SourceConnectorIndex];
            var target = targetConnectors[connectionData.TargetConnectorIndex];

            Connections.Add(new ConnectionViewModel(source, target));
            source.IsConnected = true;
            target.IsConnected = true;
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

    private (int nodeIndex, bool isOutput, int connectorIndex) FindConnector(ConnectorViewModel connector)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            var outputIdx = Nodes[i].Outputs.IndexOf(connector);
            if (outputIdx >= 0)
            {
                return (i, true, outputIdx);
            }

            var inputIdx = Nodes[i].Inputs.IndexOf(connector);
            if (inputIdx >= 0)
            {
                return (i, false, inputIdx);
            }
        }

        return (-1, false, -1);
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

    private void UpdateIsConnected(ConnectorViewModel connector)
    {
        connector.IsConnected = Connections.Any(
            c => c.Source == connector || c.Target == connector);
    }
}

