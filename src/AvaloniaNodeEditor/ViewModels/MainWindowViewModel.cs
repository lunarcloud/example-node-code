using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    // ── Undo/redo infrastructure ───────────────────────────────────────────────
    private readonly UndoRedoManager _undoRedoManager = new();

    /// <summary>Whether there are actions available to undo.</summary>
    public bool CanUndo => _undoRedoManager.CanUndo;

    /// <summary>Whether there are actions available to redo.</summary>
    public bool CanRedo => _undoRedoManager.CanRedo;

    /// <summary>Undoes the most recent reversible action.</summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => _undoRedoManager.Undo();

    /// <summary>Redoes the most recently undone action.</summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => _undoRedoManager.Redo();

    // ── Node-rename tracking ───────────────────────────────────────────────────
    /// <summary>
    /// Maps each node to the name it had when the current rename session began.
    /// Populated by the <see cref="PropertyChanging"/> subscription and committed when
    /// the node is deselected (see <see cref="OnSelectedNodeChanged"/>).
    /// </summary>
    private readonly Dictionary<NodeViewModel, string> _nodeRenameOldNames = [];

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
        "Tab Pass",
    ];

    /// <summary>The node currently held in the in-memory clipboard for paste operations.</summary>
    private NodeViewModel? _clipboardNode;

    /// <summary>Initializes the view model with a single default tab.</summary>
    public MainWindowViewModel()
    {
        var defaultTab = new TabViewModel("Main");
        Tabs.Add(defaultTab);
        ActiveTab = defaultTab;

        // Forward UndoRedoManager's CanUndo/CanRedo changes so command CanExecute re-evaluates.
        _undoRedoManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UndoRedoManager.CanUndo))
            {
                OnPropertyChanged(nameof(CanUndo));
                UndoCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(UndoRedoManager.CanRedo))
            {
                OnPropertyChanged(nameof(CanRedo));
                RedoCommand.NotifyCanExecuteChanged();
            }
        };
    }

    /// <summary>Notifies copy and delete commands when the selected node changes.
    /// Also commits any in-progress node rename when the selection moves away.</summary>
    partial void OnSelectedNodeChanged(NodeViewModel? oldValue, NodeViewModel? newValue)
    {
        CommitPendingRename(oldValue);
        CopyNodeCommand.NotifyCanExecuteChanged();
        DeleteNodeCommand.NotifyCanExecuteChanged();
        MoveNodeToTabCommand.NotifyCanExecuteChanged();
    }

    // ── Tab management ────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks the tab that was most recently created by an add-tab command so the view
    /// can suppress recording the initial auto-rename as a separate undo action.
    /// </summary>
    internal TabViewModel? NewlyCreatedTab { get; private set; }

    /// <summary>Adds a new tab at the end of the tab bar, activates it, and starts inline rename.</summary>
    [RelayCommand]
    private void AddTab()
    {
        var previousActive = ActiveTab;
        var tab = new TabViewModel(GenerateUniqueTabName());
        var insertIndex = Tabs.Count;
        Tabs.Add(tab);
        ActiveTab = tab;
        NewlyCreatedTab = tab;
        tab.IsEditing = true;
        _undoRedoManager.Record(new AddTabAction(this, tab, insertIndex, previousActive));
    }

    /// <summary>Inserts a new tab immediately after <paramref name="targetTab"/>, activates it, and starts inline rename.</summary>
    [RelayCommand]
    private void AddTabAfter(TabViewModel? targetTab)
    {
        var previousActive = ActiveTab;
        var tab = new TabViewModel(GenerateUniqueTabName());
        int insertIndex;
        if (targetTab is not null)
        {
            var index = Tabs.IndexOf(targetTab);
            if (index >= 0)
            {
                insertIndex = index + 1;
                Tabs.Insert(insertIndex, tab);
            }
            else
            {
                insertIndex = Tabs.Count;
                Tabs.Add(tab);
            }
        }
        else
        {
            insertIndex = Tabs.Count;
            Tabs.Add(tab);
        }

        ActiveTab = tab;
        NewlyCreatedTab = tab;
        tab.IsEditing = true;
        _undoRedoManager.Record(new AddTabAction(this, tab, insertIndex, previousActive));
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
        var wasActive = ActiveTab == tab;
        Tabs.Remove(tab);

        TabViewModel? activatedTab = null;
        if (wasActive)
        {
            activatedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            ActiveTab = activatedTab;
        }

        _undoRedoManager.Record(new DeleteTabAction(this, tab, index, activatedTab));
    }

    /// <summary>Records a tab rename for undo/redo.  The view calls this after the user commits the rename.</summary>
    internal void RecordTabRename(TabViewModel tab, string oldName, string newName)
    {
        if (oldName == newName)
        {
            return;
        }

        _undoRedoManager.Record(new RenameTabAction(tab, oldName, newName));
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
        if (nodeType == "Tab Pass")
        {
            AddTabPassPairAt(canvasPosition);
            return;
        }

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

        var tab = ActiveTab!;
        Nodes.Add(node);
        SelectedNode = node;

        _undoRedoManager.Record(new AddNodeAction(this, tab, node));
    }

    /// <summary>Creates a linked Tab-Pass pair at the specified position and records the action for undo/redo.</summary>
    private void AddTabPassPairAt(Point canvasPosition)
    {
        var pairId = Guid.NewGuid();

        var node1 = new TabPassNode { Location = canvasPosition, PairId = pairId, PairIndex = 1 };
        var node2 = new TabPassNode
        {
            Location = new Point(canvasPosition.X + 220, canvasPosition.Y),
            PairId = pairId,
            PairIndex = 2,
        };

        // Link the pair before setting PairLabel so the name-sync fires on both nodes.
        node1.Pair = node2;
        node2.Pair = node1;

        // Setting PairLabel on node1 propagates to node2 and auto-sets Name on both nodes
        // to "{PairLabel}.1" and "{PairLabel}.2" respectively.
        var baseLabel = GenerateUniquePairLabel("Tab Pass");
        node1.PairLabel = baseLabel;

        // Add default slots: node1 gets an input and an output; node2 receives the mirrors.
        node1.AddConnectorSlot("In 1", isInput: true);
        node1.AddConnectorSlot("Out 1", isInput: false);

        TrackNodeSelection(node1);
        TrackNodeName(node1);
        TrackNodeSelection(node2);
        TrackNodeName(node2);

        var tab = ActiveTab!;
        Nodes.Add(node1);
        Nodes.Add(node2);
        SelectedNode = node1;

        _undoRedoManager.Record(new AddTabPassPairAction(this, tab, node1, node2));
    }

    /// <summary>Adds an input slot to the given Tab-Pass node and records the action for undo/redo.</summary>
    [RelayCommand]
    private void AddTabPassInputSlot(TabPassNode? node)
    {
        if (node is null)
        {
            return;
        }

        var name = node.GenerateNextInputSlotName();
        node.AddConnectorSlot(name, isInput: true);
        _undoRedoManager.Record(new AddTabPassSlotAction(this, node, name, isInput: true));
    }

    /// <summary>Adds an output slot to the given Tab-Pass node and records the action for undo/redo.</summary>
    [RelayCommand]
    private void AddTabPassOutputSlot(TabPassNode? node)
    {
        if (node is null)
        {
            return;
        }

        var name = node.GenerateNextOutputSlotName();
        node.AddConnectorSlot(name, isInput: false);
        _undoRedoManager.Record(new AddTabPassSlotAction(this, node, name, isInput: false));
    }

    /// <summary>Removes a slot from a Tab-Pass node, cleaning up any attached connections, and records the action for undo/redo.</summary>
    [RelayCommand]
    private void RemoveTabPassSlot(TabPassConnectorSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        // Locate the owning node across all tabs.
        var node = Tabs.SelectMany(t => t.Nodes)
                       .OfType<TabPassNode>()
                       .FirstOrDefault(n => n.ConnectorSlots.Contains(slot));
        if (node is null)
        {
            return;
        }

        RemoveTabPassSlotInternal(node, slot);
    }

    /// <summary>Internal slot-removal logic used by both the command and undo/redo actions.</summary>
    internal void RemoveTabPassSlotInternal(TabPassNode node, TabPassConnectorSlot slot)
    {
        var slotName = slot.Name;
        var isInput = slot.IsInput;

        RemoveTabPassSlotCore(node, slot);

        _undoRedoManager.Record(new RemoveTabPassSlotAction(this, node, slotName, isInput));
    }

    /// <summary>
    /// Physical slot removal used directly by undo/redo actions.
    /// Does NOT record on the undo stack.
    /// </summary>
    internal void RemoveTabPassSlotCore(TabPassNode node, TabPassConnectorSlot slot)
    {
        var (removed, pairRemoved) = node.RemoveConnectorSlot(slot);

        if (removed is not null)
        {
            RemoveConnectionsForConnectorInAllTabs(removed);
        }

        if (pairRemoved is not null)
        {
            RemoveConnectionsForConnectorInAllTabs(pairRemoved);
        }
    }

    /// <summary>Removes all connections referencing <paramref name="connector"/> from every tab.</summary>
    private void RemoveConnectionsForConnectorInAllTabs(ConnectorViewModel connector)
    {
        foreach (var tab in Tabs)
        {
            var toRemove = tab.Connections
                .Where(c => c.Source == connector || c.Target == connector)
                .ToList();

            foreach (var conn in toRemove)
            {
                tab.Connections.Remove(conn);
                var other = conn.Source == connector ? conn.Target : conn.Source;
                other.IsConnected = tab.Connections.Any(c => c.Source == other || c.Target == other);
            }

            connector.IsConnected = false;
        }
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

        var connection = new ConnectionViewModel(source, target);
        Connections.Add(connection);
        source.IsConnected = true;
        target.IsConnected = true;

        _undoRedoManager.Record(new AddConnectionAction(ActiveTab!, connection));
    }

    /// <summary>Removes a connection from the graph.</summary>
    [RelayCommand]
    private void RemoveConnection(object? connection)
    {
        if (connection is not ConnectionViewModel conn)
        {
            return;
        }

        var tab = ActiveTab!;
        Connections.Remove(conn);

        // Re-evaluate IsConnected for both endpoints
        UpdateIsConnected(conn.Source);
        UpdateIsConnected(conn.Target);

        _undoRedoManager.Record(new RemoveConnectionAction(tab, conn));
    }

    /// <summary>Removes all connections attached to a given connector.</summary>
    [RelayCommand]
    private void DisconnectConnector(object? connector)
    {
        if (connector is not ConnectorViewModel conn)
        {
            return;
        }

        var tab = ActiveTab!;
        var toRemove = Connections.Where(c => c.Source == conn || c.Target == conn).ToList();
        foreach (var connection in toRemove)
        {
            Connections.Remove(connection);
            UpdateIsConnected(connection.Source);
            UpdateIsConnected(connection.Target);
        }

        if (toRemove.Count > 0)
        {
            _undoRedoManager.Record(new DisconnectConnectorAction(tab, toRemove));
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

        var tab = ActiveTab!;
        Nodes.Add(node);
        SelectedNode = node;

        _undoRedoManager.Record(new AddNodeAction(this, tab, node));
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

        // Commit any in-progress rename before deleting the node.
        CommitPendingRename(SelectedNode);

        var node = SelectedNode;
        var tab = ActiveTab!;
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

        _undoRedoManager.Record(new DeleteNodeAction(this, tab, node, toRemove));
    }

    private bool CanDeleteNode() => SelectedNode is not null;

    /// <summary>Moves the currently selected node to <paramref name="targetTab"/>, removing all of its connections.</summary>
    [RelayCommand(CanExecute = nameof(CanMoveNodeToTab))]
    private void MoveNodeToTab(TabViewModel? targetTab)
    {
        if (SelectedNode is null || targetTab is null || ActiveTab is null || targetTab == ActiveTab)
        {
            return;
        }

        CommitPendingRename(SelectedNode);

        var node = SelectedNode;
        var sourceTab = ActiveTab;

        // Remove all connections involving this node from the source tab.
        var toRemove = sourceTab.Connections
            .Where(c => node.Inputs.Contains(c.Target) || node.Outputs.Contains(c.Source))
            .ToList();

        foreach (var conn in toRemove)
        {
            sourceTab.Connections.Remove(conn);
            UpdateIsConnected(conn.Source);
            UpdateIsConnected(conn.Target);
        }

        sourceTab.Nodes.Remove(node);
        SelectedNode = null;
        targetTab.Nodes.Add(node);

        _undoRedoManager.Record(new MoveNodeToTabAction(this, node, sourceTab, targetTab, toRemove));

        ValidateNodeNamesInTab(sourceTab);
        ValidateNodeNamesInTab(targetTab);
    }

    private bool CanMoveNodeToTab(TabViewModel? targetTab) =>
        SelectedNode is not null && targetTab is not null && targetTab != ActiveTab;

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
            TabPassNode tp => CloneTabPassNode(tp, position),
            _ => null,
        };

    /// <summary>
    /// Creates a standalone (unpaired) copy of a <see cref="TabPassNode"/> at <paramref name="position"/>.
    /// The clone receives a fresh <see cref="TabPassNode.PairId"/> and has no <see cref="TabPassNode.Pair"/> link.
    /// <see cref="TabPassNode.PairLabel"/> is intentionally not copied so the clone does not produce a
    /// name collision with the source node.
    /// </summary>
    private static TabPassNode CloneTabPassNode(TabPassNode source, Point position)
    {
        var clone = new TabPassNode { Location = position };
        foreach (var slot in source.ConnectorSlots)
        {
            clone.AddConnectorSlotInternal(slot.Name, slot.IsInput);
        }

        return clone;
    }

    /// <summary>Clears all nodes and connections from the graph and resets to a single default tab.</summary>
    [RelayCommand]
    private void NewGraph()
    {
        // Create and register the new tab before clearing so ActiveTab is never null.
        var newTab = new TabViewModel("Main");
        Tabs.Add(newTab);
        ActiveTab = newTab;

        // Remove every tab except the freshly added one.
        for (var i = Tabs.Count - 2; i >= 0; i--)
        {
            Tabs.RemoveAt(i);
        }

        GraphName = string.Empty;
        GraphVersion = string.Empty;

        // A new graph has no history.
        _undoRedoManager.Clear();
        _nodeRenameOldNames.Clear();
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
                case TabPassNode tp:
                    nodeData.PairId = tp.PairId.ToString();
                    nodeData.PairLabel = string.IsNullOrEmpty(tp.PairLabel) ? null : tp.PairLabel;
                    nodeData.PairIndex = tp.PairIndex;
                    nodeData.Slots = tp.ConnectorSlots
                        .Select(s => new SlotData { Name = s.Name, IsInput = s.IsInput })
                        .ToList();
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

        // Link Tab-Pass pairs across all tabs by shared PairId.
        LinkTabPassPairs();

        // A freshly loaded graph has no history.
        _undoRedoManager.Clear();
        _nodeRenameOldNames.Clear();
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
                "Tab Pass" => CreateTabPassNode(nodeData, position),
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

    /// <summary>
    /// Generates a unique pair label for a Tab-Pass pair so that neither
    /// <c>{label}.1</c> nor <c>{label}.2</c> conflicts with an existing node name across all tabs.
    /// </summary>
    private string GenerateUniquePairLabel(string nodeType)
    {
        var allNames = Tabs.SelectMany(t => t.Nodes).Select(n => n.Name).ToHashSet();
        int index = 1;
        while (allNames.Contains($"{nodeType} {index}.1") || allNames.Contains($"{nodeType} {index}.2"))
        {
            index++;
        }

        return $"{nodeType} {index}";
    }

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

    private static TabPassNode CreateTabPassNode(NodeData data, Point position)
    {
        var node = new TabPassNode { Location = position };

        if (Guid.TryParse(data.PairId, out var pairId))
        {
            node.PairId = pairId;
        }

        if (data.PairIndex > 0)
        {
            node.PairIndex = data.PairIndex;
        }

        if (!string.IsNullOrEmpty(data.PairLabel))
        {
            node.PairLabel = data.PairLabel;
        }

        if (data.Slots is not null)
        {
            foreach (var slotData in data.Slots)
            {
                node.AddConnectorSlotInternal(slotData.Name, slotData.IsInput);
            }
        }

        return node;
    }

    /// <summary>
    /// Scans all tabs for <see cref="TabPassNode"/> instances and links siblings that share the same
    /// <see cref="TabPassNode.PairId"/>.  Called once after all tabs have been restored from JSON.
    /// </summary>
    private void LinkTabPassPairs()
    {
        var allNodes = Tabs.SelectMany(t => t.Nodes).OfType<TabPassNode>().ToList();

        foreach (var group in allNodes.GroupBy(n => n.PairId))
        {
            var pair = group.ToList();
            if (pair.Count == 2)
            {
                pair[0].Pair = pair[1];
                pair[1].Pair = pair[0];
            }
        }
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

    /// <summary>Subscribes to <paramref name="node"/>'s property events to:
    /// <list type="bullet">
    ///   <item>Capture the pre-edit name for undo via <see cref="PropertyChanging"/>.</item>
    ///   <item>Revalidate name uniqueness via <see cref="PropertyChanged"/>.</item>
    /// </list></summary>
    private void TrackNodeName(NodeViewModel node)
    {
        node.PropertyChanging += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.Name))
            {
                // Capture the old name only once per rename session (first keystroke).
                _nodeRenameOldNames.TryAdd(node, node.Name);
            }
        };

        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.Name))
            {
                ValidateAllNodeNames();
            }
        };
    }

    /// <summary>
    /// If <paramref name="node"/> has an uncommitted rename, records a <see cref="RenameNodeAction"/>
    /// on the undo stack (if the name actually changed) and clears the tracked old name.
    /// </summary>
    private void CommitPendingRename(NodeViewModel? node)
    {
        if (node is null)
        {
            return;
        }

        if (!_nodeRenameOldNames.Remove(node, out var oldName))
        {
            return;
        }

        if (oldName != node.Name)
        {
            _undoRedoManager.Record(new RenameNodeAction(this, node, oldName, node.Name));
        }
    }

    // ── Node-drag tracking ────────────────────────────────────────────────────

    /// <summary>Saved node locations captured at the start of a drag gesture.</summary>
    private Dictionary<NodeViewModel, Point>? _preDragLocations;

    /// <summary>
    /// Called by the view when a left-button press on a node (or the canvas) starts a potential drag.
    /// Saves the current location of every node in the active tab so a move can be undone.
    /// </summary>
    internal void BeginNodeDrag()
    {
        if (ActiveTab is null)
        {
            return;
        }

        _preDragLocations = ActiveTab.Nodes.ToDictionary(n => n, n => n.Location);
    }

    /// <summary>
    /// Called by the view when the left button is released after a potential drag.
    /// Compares current node positions against the saved pre-drag positions; if any node
    /// moved, records a <see cref="MoveNodesAction"/> on the undo stack.
    /// </summary>
    internal void EndNodeDrag()
    {
        if (_preDragLocations is null)
        {
            return;
        }

        var moves = new List<(NodeViewModel Node, Point OldLocation, Point NewLocation)>();

        if (ActiveTab is not null)
        {
            foreach (var node in ActiveTab.Nodes)
            {
                if (_preDragLocations.TryGetValue(node, out var oldPos) && oldPos != node.Location)
                {
                    moves.Add((node, oldPos, node.Location));
                }
            }
        }

        _preDragLocations = null;

        if (moves.Count > 0)
        {
            _undoRedoManager.Record(new MoveNodesAction(moves));
        }
    }

    // ── Validation helpers ────────────────────────────────────────────────────

    /// <summary>Validates all node names in the active tab for uniqueness.</summary>
    internal void ValidateAllNodeNames() => ValidateNodeNamesInCollection(Nodes);

    /// <summary>Validates node names in <paramref name="tab"/> for uniqueness.</summary>
    internal void ValidateNodeNamesInTab(TabViewModel tab) => ValidateNodeNamesInCollection(tab.Nodes);

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

