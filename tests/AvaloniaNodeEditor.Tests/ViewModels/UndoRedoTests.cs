using Avalonia;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Tests.ViewModels;

public class UndoRedoTests
{
    // ── UndoRedoManager unit tests ────────────────────────────────────────────

    [Fact]
    public void UndoRedoManager_InitialState_CanUndoAndCanRedoAreFalse()
    {
        var manager = new UndoRedoManager();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_AfterRecord_CanUndoIsTrue()
    {
        var manager = new UndoRedoManager();
        manager.Record(new NopAction());

        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_AfterUndo_CanRedoIsTrue()
    {
        var manager = new UndoRedoManager();
        manager.Record(new NopAction());
        manager.Undo();

        Assert.False(manager.CanUndo);
        Assert.True(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_AfterRedo_CanUndoIsTrue()
    {
        var manager = new UndoRedoManager();
        manager.Record(new NopAction());
        manager.Undo();
        manager.Redo();

        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_RecordClearsRedoStack()
    {
        var manager = new UndoRedoManager();
        manager.Record(new NopAction());
        manager.Undo(); // CanRedo = true

        manager.Record(new NopAction()); // should clear redo stack

        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_Clear_ResetsState()
    {
        var manager = new UndoRedoManager();
        manager.Record(new NopAction());
        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void UndoRedoManager_UndoOnEmpty_DoesNotThrow()
    {
        var manager = new UndoRedoManager();
        var ex = Record.Exception(() => manager.Undo());
        Assert.Null(ex);
    }

    [Fact]
    public void UndoRedoManager_RedoOnEmpty_DoesNotThrow()
    {
        var manager = new UndoRedoManager();
        var ex = Record.Exception(() => manager.Redo());
        Assert.Null(ex);
    }

    [Fact]
    public void UndoRedoManager_UndoCallsUndoOnAction()
    {
        var manager = new UndoRedoManager();
        var action = new TrackingAction();
        manager.Record(action);
        manager.Undo();

        Assert.Equal(1, action.UndoCount);
        Assert.Equal(0, action.RedoCount);
    }

    [Fact]
    public void UndoRedoManager_RedoCallsRedoOnAction()
    {
        var manager = new UndoRedoManager();
        var action = new TrackingAction();
        manager.Record(action);
        manager.Undo();
        manager.Redo();

        Assert.Equal(1, action.UndoCount);
        Assert.Equal(1, action.RedoCount);
    }

    // ── Add-node undo/redo ────────────────────────────────────────────────────

    [Fact]
    public void UndoAddNode_RemovesNodeFromTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        Assert.Single(vm.Nodes);

        vm.UndoCommand.Execute(null);

        Assert.Empty(vm.Nodes);
    }

    [Fact]
    public void RedoAddNode_ReAddsNodeToTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        vm.UndoCommand.Execute(null);
        Assert.Empty(vm.Nodes);

        vm.RedoCommand.Execute(null);

        Assert.Single(vm.Nodes);
    }

    [Fact]
    public void UndoAddNode_ClearsSelectedNode()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        Assert.NotNull(vm.SelectedNode);

        vm.UndoCommand.Execute(null);

        Assert.Null(vm.SelectedNode);
    }

    [Fact]
    public void UndoAddNode_AlsoRemovesConnectionsOnNode()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));
        Assert.Single(vm.Connections);

        // Undo the Number Reporter add — should also remove the connection.
        vm.UndoCommand.Execute(null); // undo add connection
        vm.UndoCommand.Execute(null); // undo add number reporter

        Assert.Single(vm.Nodes);
        Assert.Empty(vm.Connections);
    }

    // ── Delete-node undo/redo ─────────────────────────────────────────────────

    [Fact]
    public void UndoDeleteNode_ReAddsNode()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.Nodes[0].IsSelected = true;
        vm.DeleteNodeCommand.Execute(null);
        Assert.Empty(vm.Nodes);

        vm.UndoCommand.Execute(null);

        Assert.Single(vm.Nodes);
    }

    [Fact]
    public void UndoDeleteNode_ReAddsConnections()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));

        // Delete the producer (removes the connection too).
        vm.Nodes[0].IsSelected = true;
        vm.DeleteNodeCommand.Execute(null);
        Assert.Empty(vm.Connections);

        vm.UndoCommand.Execute(null);

        Assert.Single(vm.Connections);
        Assert.True(src.IsConnected);
        Assert.True(dst.IsConnected);
    }

    [Fact]
    public void RedoDeleteNode_RemovesNodeAgain()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.Nodes[0].IsSelected = true;
        vm.DeleteNodeCommand.Execute(null);
        vm.UndoCommand.Execute(null); // restore
        Assert.Single(vm.Nodes);

        vm.RedoCommand.Execute(null);

        Assert.Empty(vm.Nodes);
    }

    // ── Move-node undo/redo ───────────────────────────────────────────────────

    [Fact]
    public void UndoMoveNode_RestoresOriginalLocation()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 20));
        var node = vm.Nodes[0];

        vm.BeginNodeDrag();
        node.Location = new Point(100, 200);
        vm.EndNodeDrag();

        // MoveNodesAction is on top of the stack (pushed after AddNodeAction); first undo reverts the move.
        vm.UndoCommand.Execute(null);
        Assert.Equal(new Point(10, 20), node.Location);
    }

    [Fact]
    public void RedoMoveNode_ReappliesMove()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 20));
        var node = vm.Nodes[0];

        vm.BeginNodeDrag();
        node.Location = new Point(100, 200);
        vm.EndNodeDrag();

        vm.UndoCommand.Execute(null); // undo move
        Assert.Equal(new Point(10, 20), node.Location);

        vm.RedoCommand.Execute(null); // redo move
        Assert.Equal(new Point(100, 200), node.Location);
    }

    [Fact]
    public void EndNodeDrag_WithNoMove_DoesNotRecord()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 20));
        Assert.True(vm.CanUndo); // add-node is on the stack

        var undoCountBefore = vm.CanUndo;
        vm.BeginNodeDrag();
        vm.EndNodeDrag(); // no move happened

        // Stack depth didn't increase — only the AddNode action is there.
        Assert.True(vm.CanUndo);
        vm.UndoCommand.Execute(null); // undo add-node
        Assert.False(vm.CanUndo);
    }

    // ── Connection undo/redo ──────────────────────────────────────────────────

    [Fact]
    public void UndoAddConnection_RemovesConnection()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));
        Assert.Single(vm.Connections);

        vm.UndoCommand.Execute(null);

        Assert.Empty(vm.Connections);
        Assert.False(src.IsConnected);
        Assert.False(dst.IsConnected);
    }

    [Fact]
    public void RedoAddConnection_ReAddsConnection()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));
        vm.UndoCommand.Execute(null);
        Assert.Empty(vm.Connections);

        vm.RedoCommand.Execute(null);

        Assert.Single(vm.Connections);
        Assert.True(src.IsConnected);
        Assert.True(dst.IsConnected);
    }

    [Fact]
    public void UndoRemoveConnection_ReAddsConnection()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));
        var conn = vm.Connections[0];
        vm.RemoveConnectionCommand.Execute(conn);
        Assert.Empty(vm.Connections);

        vm.UndoCommand.Execute(null);

        Assert.Single(vm.Connections);
        Assert.True(src.IsConnected);
        Assert.True(dst.IsConnected);
    }

    [Fact]
    public void UndoDisconnectConnector_ReAddsAllConnections()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        vm.AddNodeAt("Number Reporter", new Point(100, 100));
        var src = vm.Nodes[0].Outputs[0];
        var dst = vm.Nodes[1].Inputs[0];
        vm.ConnectionCompletedCommand.Execute((src, dst));
        vm.DisconnectConnectorCommand.Execute(src);
        Assert.Empty(vm.Connections);

        vm.UndoCommand.Execute(null);

        Assert.Single(vm.Connections);
        Assert.True(src.IsConnected);
        Assert.True(dst.IsConnected);
    }

    // ── Tab undo/redo ─────────────────────────────────────────────────────────

    [Fact]
    public void UndoAddTab_RemovesTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddTabCommand.Execute(null);
        Assert.Equal(2, vm.Tabs.Count);

        vm.UndoCommand.Execute(null);

        Assert.Single(vm.Tabs);
    }

    [Fact]
    public void UndoAddTab_SwitchesBackToPreviousTab()
    {
        var vm = new MainWindowViewModel();
        var mainTab = vm.Tabs[0];
        vm.AddTabCommand.Execute(null);
        Assert.NotSame(mainTab, vm.ActiveTab);

        vm.UndoCommand.Execute(null);

        Assert.Same(mainTab, vm.ActiveTab);
    }

    [Fact]
    public void RedoAddTab_ReAddsTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddTabCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        Assert.Single(vm.Tabs);

        vm.RedoCommand.Execute(null);

        Assert.Equal(2, vm.Tabs.Count);
    }

    [Fact]
    public void UndoDeleteTab_ReAddsTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddTabCommand.Execute(null);
        var tab2 = vm.Tabs[1];
        vm.DeleteTabCommand.Execute(tab2);
        Assert.Single(vm.Tabs);

        vm.UndoCommand.Execute(null);

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Contains(tab2, vm.Tabs);
    }

    [Fact]
    public void UndoDeleteTab_SwitchesBackToDeletedTab()
    {
        var vm = new MainWindowViewModel();
        vm.AddTabCommand.Execute(null);
        var tab2 = vm.Tabs[1];
        vm.ActiveTab = tab2;
        vm.DeleteTabCommand.Execute(tab2);

        vm.UndoCommand.Execute(null);

        Assert.Same(tab2, vm.ActiveTab);
    }

    [Fact]
    public void RedoDeleteTab_RemovesTabAgain()
    {
        var vm = new MainWindowViewModel();
        vm.AddTabCommand.Execute(null);
        var tab2 = vm.Tabs[1];
        vm.DeleteTabCommand.Execute(tab2);
        vm.UndoCommand.Execute(null);
        Assert.Equal(2, vm.Tabs.Count);

        vm.RedoCommand.Execute(null);

        Assert.Single(vm.Tabs);
    }

    // ── Tab-rename undo/redo ──────────────────────────────────────────────────

    [Fact]
    public void UndoRenameTab_RestoresOldName()
    {
        var vm = new MainWindowViewModel();
        var tab = vm.Tabs[0];
        tab.Name = "Renamed";
        vm.RecordTabRename(tab, "Main", "Renamed");

        vm.UndoCommand.Execute(null);

        Assert.Equal("Main", tab.Name);
    }

    [Fact]
    public void RedoRenameTab_ReappliesNewName()
    {
        var vm = new MainWindowViewModel();
        var tab = vm.Tabs[0];
        tab.Name = "Renamed";
        vm.RecordTabRename(tab, "Main", "Renamed");

        vm.UndoCommand.Execute(null);
        Assert.Equal("Main", tab.Name);

        vm.RedoCommand.Execute(null);
        Assert.Equal("Renamed", tab.Name);
    }

    // ── Node-rename undo/redo ─────────────────────────────────────────────────

    [Fact]
    public void UndoRenameNode_RestoresOldName()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        var node = vm.Nodes[0];
        var originalName = node.Name;

        // Simulate a rename: select, change name, deselect.
        node.IsSelected = true;
        node.Name = "My Producer";
        vm.SelectedNode = null; // triggers CommitPendingRename

        vm.UndoCommand.Execute(null); // undo rename

        Assert.Equal(originalName, node.Name);
    }

    [Fact]
    public void RedoRenameNode_ReappliesNewName()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeAt("Number Producer", new Point(10, 10));
        var node = vm.Nodes[0];

        node.IsSelected = true;
        node.Name = "My Producer";
        vm.SelectedNode = null;

        vm.UndoCommand.Execute(null); // undo rename
        vm.RedoCommand.Execute(null); // redo rename

        Assert.Equal("My Producer", node.Name);
    }

    // ── NewGraph / DeserializeGraph clear the stack ───────────────────────────

    [Fact]
    public void NewGraph_ClearsUndoStack()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        Assert.True(vm.CanUndo);

        vm.NewGraphCommand.Execute(null);

        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void DeserializeGraph_ClearsUndoStack()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        Assert.True(vm.CanUndo);

        var json = new MainWindowViewModel().SerializeGraph(); // empty graph JSON
        vm.DeserializeGraph(json);

        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    // ── CanUndo / CanRedo properties ──────────────────────────────────────────

    [Fact]
    public void CanUndo_IsFalseOnFreshViewModel()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.CanUndo);
    }

    [Fact]
    public void CanRedo_IsFalseOnFreshViewModel()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void CanUndo_BecomesTrueAfterAddNode()
    {
        var vm = new MainWindowViewModel();
        vm.AddNodeCommand.Execute("Number Producer");
        Assert.True(vm.CanUndo);
    }
}

// ── Test helpers ──────────────────────────────────────────────────────────────

/// <summary>An action that does nothing — used to test the manager's stack logic.</summary>
file sealed class NopAction : IUndoableAction
{
    public void Undo() { }
    public void Redo() { }
}

/// <summary>An action that counts Undo/Redo calls.</summary>
file sealed class TrackingAction : IUndoableAction
{
    public int UndoCount { get; private set; }
    public int RedoCount { get; private set; }

    public void Undo() => UndoCount++;
    public void Redo() => RedoCount++;
}
