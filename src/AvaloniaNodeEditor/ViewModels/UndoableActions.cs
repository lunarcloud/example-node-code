using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.ViewModels;

// ── Helper ────────────────────────────────────────────────────────────────────

/// <summary>Shared helper used by connection-manipulating actions.</summary>
file static class ConnectorHelper
{
    internal static void UpdateIsConnected(TabViewModel tab, ConnectorViewModel connector)
        => connector.IsConnected = tab.Connections.Any(c => c.Source == connector || c.Target == connector);
}

// ── Node actions ──────────────────────────────────────────────────────────────

/// <summary>Records the creation of a new node (via add or paste).</summary>
internal sealed class AddNodeAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabViewModel _tab;
    private readonly NodeViewModel _node;

    public AddNodeAction(MainWindowViewModel vm, TabViewModel tab, NodeViewModel node)
    {
        _vm = vm;
        _tab = tab;
        _node = node;
    }

    public void Undo()
    {
        var toRemove = _tab.Connections
            .Where(c => _node.Inputs.Contains(c.Target) || _node.Outputs.Contains(c.Source))
            .ToList();

        foreach (var conn in toRemove)
        {
            _tab.Connections.Remove(conn);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Source);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Target);
        }

        _tab.Nodes.Remove(_node);

        if (_vm.SelectedNode == _node)
        {
            _vm.SelectedNode = null;
        }

        _vm.ValidateNodeNamesInTab(_tab);
    }

    public void Redo()
    {
        _tab.Nodes.Add(_node);
        _vm.SelectedNode = _node;
        _vm.ValidateNodeNamesInTab(_tab);
    }
}

/// <summary>Records the deletion of a node and its connections.</summary>
internal sealed class DeleteNodeAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabViewModel _tab;
    private readonly NodeViewModel _node;
    private readonly IReadOnlyList<ConnectionViewModel> _removedConnections;

    public DeleteNodeAction(
        MainWindowViewModel vm,
        TabViewModel tab,
        NodeViewModel node,
        IReadOnlyList<ConnectionViewModel> removedConnections)
    {
        _vm = vm;
        _tab = tab;
        _node = node;
        _removedConnections = removedConnections;
    }

    public void Undo()
    {
        _tab.Nodes.Add(_node);

        foreach (var conn in _removedConnections)
        {
            _tab.Connections.Add(conn);
            conn.Source.IsConnected = true;
            conn.Target.IsConnected = true;
        }

        _vm.SelectedNode = _node;
        _vm.ValidateNodeNamesInTab(_tab);
    }

    public void Redo()
    {
        foreach (var conn in _removedConnections)
        {
            _tab.Connections.Remove(conn);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Source);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Target);
        }

        _tab.Nodes.Remove(_node);

        if (_vm.SelectedNode == _node)
        {
            _vm.SelectedNode = null;
        }

        _vm.ValidateNodeNamesInTab(_tab);
    }
}

/// <summary>Records the dragged movement of one or more nodes.</summary>
internal sealed class MoveNodesAction : IUndoableAction
{
    private readonly IReadOnlyList<(NodeViewModel Node, Point OldLocation, Point NewLocation)> _moves;

    public MoveNodesAction(IReadOnlyList<(NodeViewModel Node, Point OldLocation, Point NewLocation)> moves)
    {
        _moves = moves;
    }

    public void Undo()
    {
        foreach (var (node, oldLoc, _) in _moves)
        {
            node.Location = oldLoc;
        }
    }

    public void Redo()
    {
        foreach (var (node, _, newLoc) in _moves)
        {
            node.Location = newLoc;
        }
    }
}

/// <summary>Records renaming a node.</summary>
internal sealed class RenameNodeAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly NodeViewModel _node;
    private readonly string _oldName;
    private readonly string _newName;

    public RenameNodeAction(MainWindowViewModel vm, NodeViewModel node, string oldName, string newName)
    {
        _vm = vm;
        _node = node;
        _oldName = oldName;
        _newName = newName;
    }

    public void Undo()
    {
        _node.Name = _oldName;
        _vm.ValidateAllNodeNames();
    }

    public void Redo()
    {
        _node.Name = _newName;
        _vm.ValidateAllNodeNames();
    }
}

// ── Connection actions ────────────────────────────────────────────────────────

/// <summary>Records the creation of a new connection.</summary>
internal sealed class AddConnectionAction : IUndoableAction
{
    private readonly TabViewModel _tab;
    private readonly ConnectionViewModel _connection;

    public AddConnectionAction(TabViewModel tab, ConnectionViewModel connection)
    {
        _tab = tab;
        _connection = connection;
    }

    public void Undo()
    {
        _tab.Connections.Remove(_connection);
        ConnectorHelper.UpdateIsConnected(_tab, _connection.Source);
        ConnectorHelper.UpdateIsConnected(_tab, _connection.Target);
    }

    public void Redo()
    {
        _tab.Connections.Add(_connection);
        _connection.Source.IsConnected = true;
        _connection.Target.IsConnected = true;
    }
}

/// <summary>Records the removal of a single connection.</summary>
internal sealed class RemoveConnectionAction : IUndoableAction
{
    private readonly TabViewModel _tab;
    private readonly ConnectionViewModel _connection;

    public RemoveConnectionAction(TabViewModel tab, ConnectionViewModel connection)
    {
        _tab = tab;
        _connection = connection;
    }

    public void Undo()
    {
        _tab.Connections.Add(_connection);
        _connection.Source.IsConnected = true;
        _connection.Target.IsConnected = true;
    }

    public void Redo()
    {
        _tab.Connections.Remove(_connection);
        ConnectorHelper.UpdateIsConnected(_tab, _connection.Source);
        ConnectorHelper.UpdateIsConnected(_tab, _connection.Target);
    }
}

/// <summary>Records disconnecting all connections from a single connector.</summary>
internal sealed class DisconnectConnectorAction : IUndoableAction
{
    private readonly TabViewModel _tab;
    private readonly IReadOnlyList<ConnectionViewModel> _removedConnections;

    public DisconnectConnectorAction(TabViewModel tab, IReadOnlyList<ConnectionViewModel> removedConnections)
    {
        _tab = tab;
        _removedConnections = removedConnections;
    }

    public void Undo()
    {
        foreach (var conn in _removedConnections)
        {
            _tab.Connections.Add(conn);
            conn.Source.IsConnected = true;
            conn.Target.IsConnected = true;
        }
    }

    public void Redo()
    {
        foreach (var conn in _removedConnections)
        {
            _tab.Connections.Remove(conn);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Source);
            ConnectorHelper.UpdateIsConnected(_tab, conn.Target);
        }
    }
}

// ── Move-node-to-tab action ───────────────────────────────────────────────────

/// <summary>Records moving a node from one tab to another, including removal of its cross-tab connections.</summary>
internal sealed class MoveNodeToTabAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly NodeViewModel _node;
    private readonly TabViewModel _sourceTab;
    private readonly TabViewModel _targetTab;
    private readonly IReadOnlyList<ConnectionViewModel> _removedConnections;

    public MoveNodeToTabAction(
        MainWindowViewModel vm,
        NodeViewModel node,
        TabViewModel sourceTab,
        TabViewModel targetTab,
        IReadOnlyList<ConnectionViewModel> removedConnections)
    {
        _vm = vm;
        _node = node;
        _sourceTab = sourceTab;
        _targetTab = targetTab;
        _removedConnections = removedConnections;
    }

    public void Undo()
    {
        // Move the node back to the source tab.
        _targetTab.Nodes.Remove(_node);
        _sourceTab.Nodes.Add(_node);

        // Restore the connections that were removed when the node was moved.
        foreach (var conn in _removedConnections)
        {
            _sourceTab.Connections.Add(conn);
            conn.Source.IsConnected = true;
            conn.Target.IsConnected = true;
        }

        if (_vm.ActiveTab == _sourceTab)
        {
            _vm.SelectedNode = _node;
        }
        else if (_vm.ActiveTab == _targetTab && _vm.SelectedNode == _node)
        {
            _vm.SelectedNode = null;
        }

        _vm.ValidateNodeNamesInTab(_sourceTab);
        _vm.ValidateNodeNamesInTab(_targetTab);
    }

    public void Redo()
    {
        // Remove connections from the source tab.
        foreach (var conn in _removedConnections)
        {
            _sourceTab.Connections.Remove(conn);
            ConnectorHelper.UpdateIsConnected(_sourceTab, conn.Source);
            ConnectorHelper.UpdateIsConnected(_sourceTab, conn.Target);
        }

        _sourceTab.Nodes.Remove(_node);

        if (_vm.ActiveTab == _sourceTab && _vm.SelectedNode == _node)
        {
            _vm.SelectedNode = null;
        }

        _targetTab.Nodes.Add(_node);
        _vm.ValidateNodeNamesInTab(_sourceTab);
        _vm.ValidateNodeNamesInTab(_targetTab);
    }
}

// ── Tab actions ───────────────────────────────────────────────────────────────

/// <summary>Records the addition of a new tab.</summary>
internal sealed class AddTabAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabViewModel _tab;
    private readonly int _insertIndex;
    private readonly TabViewModel? _previousActiveTab;

    public AddTabAction(MainWindowViewModel vm, TabViewModel tab, int insertIndex, TabViewModel? previousActiveTab)
    {
        _vm = vm;
        _tab = tab;
        _insertIndex = insertIndex;
        _previousActiveTab = previousActiveTab;
    }

    public void Undo()
    {
        if (_vm.Tabs.Count <= 1)
        {
            return;
        }

        _vm.Tabs.Remove(_tab);

        if (_vm.ActiveTab == _tab)
        {
            _vm.ActiveTab = _previousActiveTab is not null && _vm.Tabs.Contains(_previousActiveTab)
                ? _previousActiveTab
                : _vm.Tabs[Math.Max(0, Math.Min(_insertIndex, _vm.Tabs.Count - 1))];
        }
    }

    public void Redo()
    {
        var insertAt = Math.Min(_insertIndex, _vm.Tabs.Count);
        _vm.Tabs.Insert(insertAt, _tab);
        _vm.ActiveTab = _tab;
    }
}

/// <summary>Records the deletion of an existing tab.</summary>
internal sealed class DeleteTabAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabViewModel _deletedTab;
    private readonly int _originalIndex;
    private readonly TabViewModel? _activatedTab;

    public DeleteTabAction(
        MainWindowViewModel vm,
        TabViewModel deletedTab,
        int originalIndex,
        TabViewModel? activatedTab)
    {
        _vm = vm;
        _deletedTab = deletedTab;
        _originalIndex = originalIndex;
        _activatedTab = activatedTab;
    }

    public void Undo()
    {
        var insertAt = Math.Min(_originalIndex, _vm.Tabs.Count);
        _vm.Tabs.Insert(insertAt, _deletedTab);
        _vm.ActiveTab = _deletedTab;
    }

    public void Redo()
    {
        if (_vm.Tabs.Count <= 1)
        {
            return;
        }

        var index = _vm.Tabs.IndexOf(_deletedTab);
        _vm.Tabs.Remove(_deletedTab);

        if (_vm.ActiveTab == _deletedTab)
        {
            _vm.ActiveTab = _activatedTab is not null && _vm.Tabs.Contains(_activatedTab)
                ? _activatedTab
                : _vm.Tabs[Math.Max(0, Math.Min(index, _vm.Tabs.Count - 1))];
        }
    }
}

/// <summary>Records renaming a tab.</summary>
internal sealed class RenameTabAction : IUndoableAction
{
    private readonly TabViewModel _tab;
    private readonly string _oldName;
    private readonly string _newName;

    public RenameTabAction(TabViewModel tab, string oldName, string newName)
    {
        _tab = tab;
        _oldName = oldName;
        _newName = newName;
    }

    public void Undo() => _tab.Name = _oldName;

    public void Redo() => _tab.Name = _newName;
}

// ── Tab-Pass node actions ─────────────────────────────────────────────────────

/// <summary>Records the creation of a linked Tab-Pass pair (two nodes added simultaneously).</summary>
internal sealed class AddTabPassPairAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabViewModel _tab;
    private readonly TabPassNode _node1;
    private readonly TabPassNode _node2;

    public AddTabPassPairAction(
        MainWindowViewModel vm, TabViewModel tab, TabPassNode node1, TabPassNode node2)
    {
        _vm = vm;
        _tab = tab;
        _node1 = node1;
        _node2 = node2;
    }

    public void Undo()
    {
        foreach (var node in new[] { _node1, _node2 })
        {
            var toRemove = _tab.Connections
                .Where(c => node.Inputs.Contains(c.Target) || node.Outputs.Contains(c.Source))
                .ToList();

            foreach (var conn in toRemove)
            {
                _tab.Connections.Remove(conn);
                ConnectorHelper.UpdateIsConnected(_tab, conn.Source);
                ConnectorHelper.UpdateIsConnected(_tab, conn.Target);
            }

            _tab.Nodes.Remove(node);
        }

        if (_vm.SelectedNode == _node1 || _vm.SelectedNode == _node2)
            _vm.SelectedNode = null;

        _vm.ValidateNodeNamesInTab(_tab);
    }

    public void Redo()
    {
        _tab.Nodes.Add(_node1);
        _tab.Nodes.Add(_node2);
        _vm.SelectedNode = _node1;
        _vm.ValidateNodeNamesInTab(_tab);
    }
}

/// <summary>Records adding a connector slot to a Tab-Pass node (and its mirrored slot on the sibling).</summary>
internal sealed class AddTabPassSlotAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabPassNode _node;
    private readonly string _slotName;
    private readonly bool _isInput;

    public AddTabPassSlotAction(
        MainWindowViewModel vm, TabPassNode node, string slotName, bool isInput)
    {
        _vm = vm;
        _node = node;
        _slotName = slotName;
        _isInput = isInput;
    }

    public void Undo()
    {
        var slot = _node.ConnectorSlots.FirstOrDefault(s => s.Name == _slotName && s.IsInput == _isInput);
        if (slot is not null)
            _vm.RemoveTabPassSlotCore(_node, slot);
    }

    public void Redo() => _node.AddConnectorSlot(_slotName, _isInput);
}

/// <summary>Records removing a connector slot from a Tab-Pass node (and its mirrored slot on the sibling).</summary>
internal sealed class RemoveTabPassSlotAction : IUndoableAction
{
    private readonly MainWindowViewModel _vm;
    private readonly TabPassNode _node;
    private readonly string _slotName;
    private readonly bool _isInput;

    public RemoveTabPassSlotAction(
        MainWindowViewModel vm, TabPassNode node, string slotName, bool isInput)
    {
        _vm = vm;
        _node = node;
        _slotName = slotName;
        _isInput = isInput;
    }

    public void Undo() => _node.AddConnectorSlot(_slotName, _isInput);

    public void Redo()
    {
        var slot = _node.ConnectorSlots.FirstOrDefault(s => s.Name == _slotName && s.IsInput == _isInput);
        if (slot is not null)
            _vm.RemoveTabPassSlotCore(_node, slot);
    }
}
