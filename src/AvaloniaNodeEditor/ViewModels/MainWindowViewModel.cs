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
    // ── Fallback empty collections used when ActiveTab is transiently null ──
    private static readonly ObservableCollection<NodeViewModel> _fallbackNodes = [];
    private static readonly ObservableCollection<ConnectionViewModel> _fallbackConnections = [];

    /// <summary>The ordered collection of tabs visible in the tab bar.</summary>
    public ObservableCollection<TabViewModel> Tabs { get; } = [];

    /// <summary>The currently active tab whose nodes and connections are shown in the editor.</summary>
    [ObservableProperty]
    private TabViewModel? _activeTab;

    /// <summary>
    /// The nodes displayed in the editor canvas — delegates to the active tab's node collection.
    /// Raises <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> when
    /// <see cref="ActiveTab"/> changes.
    /// </summary>
    public ObservableCollection<NodeViewModel> Nodes => ActiveTab?.Nodes ?? _fallbackNodes;

    /// <summary>
    /// The connections between node connectors — delegates to the active tab's connection collection.
    /// Raises <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> when
    /// <see cref="ActiveTab"/> changes.
    /// </summary>
    public ObservableCollection<ConnectionViewModel> Connections => ActiveTab?.Connections ?? _fallbackConnections;

    /// <summary>Called by the toolkit whenever <see cref="ActiveTab"/> changes.</summary>
    partial void OnActiveTabChanged(TabViewModel? oldValue, TabViewModel? newValue)
    {
        // Clear selection when switching tabs so the properties panel does not show stale data.
        SelectedNode = null;

        // Notify bindings that the Nodes and Connections references have changed.
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Connections));

        CopyNodeCommand.NotifyCanExecuteChanged();
        DeleteNodeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The currently selected node, shown in the properties panel.</summary>
    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    /// <summary>The name of the graph/system.</summary>
    [ObservableProperty]
    private string _graphName = string.Empty;

    /// <summary>The version of the graph/system.</summary>
    [ObservableProperty]
    private string _graphVersion = string.Empty;

    /// <summary>Whether the node properties panel is currently visible in the UI.</summary>
    [ObservableProperty]
    private bool _isPropertiesPanelVisible;

    /// <summary>The node types available in the toolbox.</summary>
    public IReadOnlyList<string> ToolboxItems { get; } =
    [
        "Number Producer",
        "Number Reporter",
        "Arithmetic Transform",
        "Random Number Generator",
        "Pass Filter",
    ];

    /// <summary>The node currently held in the in-memory clipboard for paste operations.</summary>
    private NodeViewModel? _clipboardNode;

    /// <summary>Initialises the view model with a single default tab.</summary>
    public MainWindowViewModel()
    {
        var defaultTab = new TabViewModel("Tab 1");
        Tabs.Add(defaultTab);
        ActiveTab = defaultTab;
    }

    /// <summary>Notifies copy and delete commands when the selected node changes.</summary>
    partial void OnSelectedNodeChanged(NodeViewModel? value)
    {
        CopyNodeCommand.NotifyCanExecuteChanged();
        DeleteNodeCommand.NotifyCanExecuteChanged();
    }

    // ── Tab management ────────────────────────────────────────────────────────

    /// <summary>Adds a new tab at the end of the tab bar and activates it.</summary>
    [RelayCommand]
    private void AddTab()
    {
        var tab = new TabViewModel(GenerateUniqueTabName());
        Tabs.Add(tab);
        ActiveTab = tab;
    }

    /// <summary>Inserts a new tab immediately after <paramref name="targetTab"/> and activates it.</summary>
    [RelayCommand]
    private void AddTabAfter(TabViewModel? targetTab)
    {
        var tab = new TabViewModel(GenerateUniqueTabName());
        if (targetTab is not null)
        {
            var index = Tabs.IndexOf(targetTab);
            if (index >= 0)
            {
                Tabs.Insert(index + 1, tab);
            }
            else
            {
                Tabs.Add(tab);
            }
        }
        else
        {
            Tabs.Add(tab);
        }

        ActiveTab = tab;
    }

    /// <summary>Removes <paramref name="tab"/> from the tab bar.  The last remaining tab cannot be deleted.</summary>
    [RelayCommand]
    private void DeleteTab(TabViewModel? tab)
    {
        if (tab is null || Tabs.Count <= 1)
        {
            return;
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (ActiveTab == tab)
        {
            ActiveTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
    }

    /// <summary>Generates a unique tab name of the form "Tab N".</summary>
    private string GenerateUniqueTabName()
    {
        int index = 1;
        while (Tabs.Any(t => t.Name == $"Tab {index}"))
        {
            index++;
        }

        return $"Tab {index}";
    }

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

        // Each input connector accepts at most one connection.
        if (Connections.Any(c => c.Target == target))
        {
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

    /// <summary>Copies the currently selected node to the in-memory clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanCopyNode))]
    private void CopyNode()
    {
        _clipboardNode = SelectedNode;
        PasteNodeCommand.NotifyCanExecuteChanged();
    }

    private bool CanCopyNode() => SelectedNode is not null;

    /// <summary>Pastes the clipboard node as a new node, placed 30 px below-right of the source.</summary>
    [RelayCommand(CanExecute = nameof(CanPasteNode))]
    private void PasteNode()
    {
        if (_clipboardNode is null)
        {
            return;
        }

        var offset = new Point(_clipboardNode.Location.X + 30, _clipboardNode.Location.Y + 30);
        var node = CloneNode(_clipboardNode, offset);
        if (node is null)
        {
            return;
        }

        node.Name = GenerateUniqueName(node.NodeType);
        TrackNodeSelection(node);
        TrackNodeName(node);
        Nodes.Add(node);
        SelectedNode = node;
    }

    private bool CanPasteNode() => _clipboardNode is not null;

    /// <summary>Deletes the currently selected node and all of its connections from the canvas.</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteNode))]
    private void DeleteNode()
    {
        if (SelectedNode is null)
        {
            return;
        }

        var node = SelectedNode;
        var toRemove = Connections
            .Where(c => node.Inputs.Contains(c.Target) || node.Outputs.Contains(c.Source))
            .ToList();

        foreach (var conn in toRemove)
        {
            Connections.Remove(conn);
            UpdateIsConnected(conn.Source);
            UpdateIsConnected(conn.Target);
        }

        Nodes.Remove(node);
        SelectedNode = null;
    }

    private bool CanDeleteNode() => SelectedNode is not null;

    /// <summary>Creates a copy of <paramref name="source"/> at <paramref name="position"/>, preserving all type-specific properties.</summary>
    private static NodeViewModel? CloneNode(NodeViewModel source, Point position) =>
        source switch
        {
            NumberProducerNode np => new NumberProducerNode { Location = position, Value = np.Value },
            NumberReporterNode nr => new NumberReporterNode { Location = position, Value = nr.Value },
            ArithmeticTransformNode at => new ArithmeticTransformNode
            {
                Location = position,
                InputA = at.InputA,
                InputB = at.InputB,
                Operation = at.Operation,
            },
            RandomNumberGeneratorNode rng => new RandomNumberGeneratorNode
            {
                Location = position,
                MinValue = rng.MinValue,
                MaxValue = rng.MaxValue,
            },
            PassFilterNode pf => new PassFilterNode
            {
                Location = position,
                FilterType = pf.FilterType,
                Threshold = pf.Threshold,
                UpperThreshold = pf.UpperThreshold,
            },
            _ => null,
        };

    /// <summary>Clears all nodes and connections from the graph and resets to a single default tab.</summary>
    [RelayCommand]
    private void NewGraph()
    {
        // Create and register the new tab before clearing so ActiveTab is never null.
        var newTab = new TabViewModel("Tab 1");
        Tabs.Add(newTab);
        ActiveTab = newTab;

        // Remove every tab except the freshly added one.
        for (var i = Tabs.Count - 2; i >= 0; i--)
        {
            Tabs.RemoveAt(i);
        }

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

    /// <summary>Serializes the current node graph (all tabs) to a JSON string.</summary>
    /// <returns>A JSON string representing the full graph state including all tabs.</returns>
    public string SerializeGraph()
    {
        var data = new GraphData
        {
            Name = string.IsNullOrWhiteSpace(GraphName) ? null : GraphName,
            Version = string.IsNullOrWhiteSpace(GraphVersion) ? null : GraphVersion,
            Tabs = [],
        };

        foreach (var tab in Tabs)
        {
            data.Tabs.Add(SerializeTab(tab));
        }

        return JsonSerializer.Serialize(data, GraphJsonContext.Default.GraphData);
    }

    /// <summary>Converts a single <see cref="TabViewModel"/> into its serialization DTO.</summary>
    private static TabData SerializeTab(TabViewModel tab)
    {
        var tabData = new TabData { Title = tab.Name };

        foreach (var node in tab.Nodes)
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

            tabData.Nodes.Add(nodeData);

            // Store layout separately so visual position is decoupled from node content.
            tabData.Layout[node.Name] = new LayoutData
            {
                X = node.Location.X,
                Y = node.Location.Y,
            };
        }

        foreach (var connection in tab.Connections)
        {
            var (sourceNodeName, sourceConnectorName) = FindConnectorNameInNodes(tab.Nodes, connection.Source);
            var (targetNodeName, targetConnectorName) = FindConnectorNameInNodes(tab.Nodes, connection.Target);

            if (sourceNodeName is null || targetNodeName is null)
            {
                continue;
            }

            tabData.Connections.Add(new ConnectionData
            {
                From = $"{sourceNodeName}.{sourceConnectorName}",
                To = $"{targetNodeName}.{targetConnectorName}",
            });
        }

        return tabData;
    }

    /// <summary>Deserializes a JSON string and restores the node graph state (all tabs).</summary>
    /// <param name="json">A JSON string previously produced by <see cref="SerializeGraph"/>.</param>
    public void DeserializeGraph(string json)
    {
        var data = JsonSerializer.Deserialize(json, GraphJsonContext.Default.GraphData)
            ?? throw new JsonException("Failed to deserialize graph data.");

        // Keep ActiveTab non-null during the reset by staging the first restored tab.
        SelectedNode = null;
        GraphName = data.Name ?? string.Empty;
        GraphVersion = data.Version ?? string.Empty;

        if (data.Tabs is { Count: > 0 })
        {
            // Multi-tab format
            var restoredTabs = new List<TabViewModel>();
            foreach (var tabData in data.Tabs)
            {
                var tab = new TabViewModel(tabData.Title);
                RestoreTabContent(tab, tabData.Nodes, tabData.Layout, tabData.Connections);
                restoredTabs.Add(tab);
            }

            ReplaceAllTabs(restoredTabs);
        }
        else
        {
            // Legacy single-tab format (files saved before the tabs feature).
            var tab = new TabViewModel("Tab 1");
            RestoreTabContent(tab, data.Nodes, data.Layout, data.Connections);
            ReplaceAllTabs([tab]);
        }
    }

    /// <summary>
    /// Replaces the entire <see cref="Tabs"/> collection with <paramref name="newTabs"/>,
    /// keeping <see cref="ActiveTab"/> valid at all times.
    /// </summary>
    private void ReplaceAllTabs(IReadOnlyList<TabViewModel> newTabs)
    {
        // Activate the first new tab before clearing the old ones so ActiveTab is never null.
        var first = newTabs[0];
        Tabs.Add(first);
        ActiveTab = first;

        // Remove all previously existing tabs (everything before the last appended entry).
        for (var i = Tabs.Count - 2; i >= 0; i--)
        {
            Tabs.RemoveAt(i);
        }

        // Append any remaining new tabs.
        for (var i = 1; i < newTabs.Count; i++)
        {
            Tabs.Add(newTabs[i]);
        }
    }

    /// <summary>Restores nodes and connections from serialization DTOs into a <see cref="TabViewModel"/>.</summary>
    private void RestoreTabContent(
        TabViewModel tab,
        IReadOnlyList<NodeData> nodes,
        IReadOnlyDictionary<string, LayoutData> layout,
        IReadOnlyList<ConnectionData> connections)
    {
        foreach (var nodeData in nodes)
        {
            var position = layout.TryGetValue(nodeData.Name, out var layoutData)
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

            TrackNodeSelection(node);
            TrackNodeName(node);

            tab.Nodes.Add(node);
        }

        foreach (var connectionData in connections)
        {
            var source = ResolveConnectorInNodes(tab.Nodes, connectionData.From);
            var target = ResolveConnectorInNodes(tab.Nodes, connectionData.To);

            if (source is null || target is null)
            {
                continue;
            }

            tab.Connections.Add(new ConnectionViewModel(source, target));
            source.IsConnected = true;
            target.IsConnected = true;
        }

        ValidateNodeNamesInCollection(tab.Nodes);
    }

    /// <summary>Generates a unique default name for a new node of the given type within the active tab.</summary>
    internal string GenerateUniqueName(string nodeType)
    {
        int index = 1;
        while (Nodes.Any(n => n.Name == $"{nodeType} {index}"))
        {
            index++;
        }

        return $"{nodeType} {index}";
    }

    /// <summary>Validates all node names in the active tab for uniqueness and sets <see cref="NodeViewModel.HasNameError"/> accordingly.</summary>
    internal void ValidateAllNodeNames() => ValidateNodeNamesInCollection(Nodes);

    /// <summary>Validates node names in <paramref name="nodes"/> for uniqueness.</summary>
    private static void ValidateNodeNamesInCollection(IEnumerable<NodeViewModel> nodes)
    {
        var nameGroups = nodes.GroupBy(n => n.Name);
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

    /// <summary>Finds the node name and connector name for a given connector view model in the active tab.</summary>
    private (string? nodeName, string? connectorName) FindConnectorName(ConnectorViewModel connector) =>
        FindConnectorNameInNodes(Nodes, connector);

    /// <summary>Finds the node name and connector name for a given connector in an arbitrary node collection.</summary>
    private static (string? nodeName, string? connectorName) FindConnectorNameInNodes(
        IEnumerable<NodeViewModel> nodes, ConnectorViewModel connector)
    {
        foreach (var node in nodes)
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

    /// <summary>Resolves a connector from a "NodeName.ConnectorName" string in the active tab.</summary>
    private ConnectorViewModel? ResolveConnector(string endpoint) =>
        ResolveConnectorInNodes(Nodes, endpoint);

    /// <summary>Resolves a connector from a "NodeName.ConnectorName" string in an arbitrary node collection.</summary>
    private static ConnectorViewModel? ResolveConnectorInNodes(
        IEnumerable<NodeViewModel> nodes, string endpoint)
    {
        var dotIndex = endpoint.LastIndexOf('.');
        if (dotIndex < 0)
        {
            return null;
        }

        var nodeName = endpoint[..dotIndex];
        var connectorName = endpoint[(dotIndex + 1)..];

        var node = nodes.FirstOrDefault(n => n.Name == nodeName);
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

