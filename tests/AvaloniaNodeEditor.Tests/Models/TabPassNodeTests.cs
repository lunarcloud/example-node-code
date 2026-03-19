using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.Tests.Models;

public class TabPassNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedNameAndType()
    {
        var node = new TabPassNode();
        Assert.Equal("Tab Pass", node.Name);
        Assert.Equal("Tab Pass", node.NodeType);
    }

    [Fact]
    public void Constructor_HasNoPairAndNoSlots()
    {
        var node = new TabPassNode();
        Assert.Null(node.Pair);
        Assert.Empty(node.ConnectorSlots);
        Assert.Empty(node.Inputs);
        Assert.Empty(node.Outputs);
    }

    [Fact]
    public void Constructor_PairIdIsUnique()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        Assert.NotEqual(node1.PairId, node2.PairId);
    }

    [Fact]
    public void AddConnectorSlot_Input_AddsToInputsAndSlots()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("In 1", isInput: true);

        Assert.Single(node.ConnectorSlots);
        Assert.Single(node.Inputs);
        Assert.Empty(node.Outputs);
        Assert.Equal("In 1", node.Inputs[0].Name);
        Assert.True(node.ConnectorSlots[0].IsInput);
    }

    [Fact]
    public void AddConnectorSlot_Output_AddsToOutputsAndSlots()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("Out 1", isInput: false);

        Assert.Single(node.ConnectorSlots);
        Assert.Empty(node.Inputs);
        Assert.Single(node.Outputs);
        Assert.Equal("Out 1", node.Outputs[0].Name);
        Assert.False(node.ConnectorSlots[0].IsInput);
    }

    [Fact]
    public void AddConnectorSlot_WithPair_SyncsMirroredSlotToPair()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;

        node1.AddConnectorSlot("In 1", isInput: true);

        // node1: input "In 1"
        Assert.Single(node1.Inputs);
        Assert.Empty(node1.Outputs);

        // node2 (pair): output "In 1" (mirrored)
        Assert.Empty(node2.Inputs);
        Assert.Single(node2.Outputs);
        Assert.Equal("In 1", node2.Outputs[0].Name);
        Assert.False(node2.ConnectorSlots[0].IsInput);
    }

    [Fact]
    public void AddConnectorSlot_OutputWithPair_SyncsMirroredInputToPair()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;

        node1.AddConnectorSlot("Out 1", isInput: false);

        // node1: output "Out 1"
        Assert.Single(node1.Outputs);
        Assert.Empty(node1.Inputs);

        // node2 (pair): input "Out 1" (mirrored)
        Assert.Single(node2.Inputs);
        Assert.Empty(node2.Outputs);
        Assert.Equal("Out 1", node2.Inputs[0].Name);
        Assert.True(node2.ConnectorSlots[0].IsInput);
    }

    [Fact]
    public void RemoveConnectorSlot_RemovesFromNodeAndPair()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;

        node1.AddConnectorSlot("In 1", isInput: true);

        var slotToRemove = node1.ConnectorSlots[0];
        var (removed, pairRemoved) = node1.RemoveConnectorSlot(slotToRemove);

        Assert.NotNull(removed);
        Assert.NotNull(pairRemoved);
        Assert.Empty(node1.ConnectorSlots);
        Assert.Empty(node1.Inputs);
        Assert.Empty(node2.ConnectorSlots);
        Assert.Empty(node2.Outputs);
    }

    [Fact]
    public void RemoveConnectorSlot_WithNoPair_OnlyRemovesFromSelf()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("In 1", isInput: true);

        var slotToRemove = node.ConnectorSlots[0];
        var (removed, pairRemoved) = node.RemoveConnectorSlot(slotToRemove);

        Assert.NotNull(removed);
        Assert.Null(pairRemoved);
        Assert.Empty(node.ConnectorSlots);
        Assert.Empty(node.Inputs);
    }

    [Fact]
    public void GenerateNextInputSlotName_StartsAtIn1()
    {
        var node = new TabPassNode();
        Assert.Equal("In 1", node.GenerateNextInputSlotName());
    }

    [Fact]
    public void GenerateNextInputSlotName_IncrementsWhenConflict()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("In 1", isInput: true);
        Assert.Equal("In 2", node.GenerateNextInputSlotName());
    }

    [Fact]
    public void GenerateNextOutputSlotName_StartsAtOut1()
    {
        var node = new TabPassNode();
        Assert.Equal("Out 1", node.GenerateNextOutputSlotName());
    }

    [Fact]
    public void GenerateNextOutputSlotName_IncrementsWhenConflict()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("Out 1", isInput: false);
        Assert.Equal("Out 2", node.GenerateNextOutputSlotName());
    }

    [Fact]
    public void TabPassConnectorSlot_DirectionLabel_IsInputReturnsInput()
    {
        var slot = new TabPassConnectorSlot { Name = "In 1", IsInput = true };
        Assert.Equal("Input", slot.DirectionLabel);
    }

    [Fact]
    public void TabPassConnectorSlot_DirectionLabel_IsOutputReturnsOutput()
    {
        var slot = new TabPassConnectorSlot { Name = "Out 1", IsInput = false };
        Assert.Equal("Output", slot.DirectionLabel);
    }

    [Fact]
    public void RenameSlot_UpdatesConnectorViewModelOnSameNode()
    {
        var node = new TabPassNode();
        node.AddConnectorSlotInternal("In 1", isInput: true);

        node.ConnectorSlots[0].Name = "My Signal";

        Assert.Equal("My Signal", node.Inputs[0].Name);
    }

    [Fact]
    public void RenameSlot_SyncsNameToPairedNode()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;
        node1.AddConnectorSlot("In 1", isInput: true);

        // Rename the input slot on node1
        node1.ConnectorSlots[0].Name = "Renamed";

        // node2's mirrored output slot should also be renamed
        Assert.Equal("Renamed", node2.ConnectorSlots[0].Name);
    }

    [Fact]
    public void RenameSlot_SyncsConnectorViewModelOnPairedNode()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;
        node1.AddConnectorSlot("Out 1", isInput: false);

        // Rename the output slot on node1
        node1.ConnectorSlots[0].Name = "Data Out";

        // node2's mirrored input ConnectorViewModel should also be renamed
        Assert.Equal("Data Out", node2.Inputs[0].Name);
    }

    [Fact]
    public void RenameSlot_DoesNotCauseInfiniteRecursion()
    {
        var node1 = new TabPassNode();
        var node2 = new TabPassNode();
        node1.Pair = node2;
        node2.Pair = node1;
        node1.AddConnectorSlot("In 1", isInput: true);

        // This should complete without a StackOverflowException
        node1.ConnectorSlots[0].Name = "Safe Rename";

        Assert.Equal("Safe Rename", node1.ConnectorSlots[0].Name);
        Assert.Equal("Safe Rename", node2.ConnectorSlots[0].Name);
    }

    // ── PairLabel / DisplayName / PairIndex tests ─────────────────────────────────────────

    [Fact]
    public void DisplayName_WhenPairLabelEmpty_ReturnsFallbackName()
    {
        var node = new TabPassNode();
        node.Name = "Tab Pass 1";
        Assert.Equal("Tab Pass 1", node.DisplayName);
    }

    [Fact]
    public void DisplayName_WhenPairLabelSet_ReturnsPairLabel()
    {
        var node = new TabPassNode();
        node.PairLabel = "My Signal";
        Assert.Equal("My Signal", node.DisplayName);
    }

    [Fact]
    public void PairLabel_AutoSetsNameWithPairIndex()
    {
        var node = new TabPassNode { PairIndex = 1 };
        node.PairLabel = "My Signal";
        Assert.Equal("My Signal.1", node.Name);
    }

    [Fact]
    public void PairLabel_AutoSetsNameWithPairIndex2()
    {
        var node = new TabPassNode { PairIndex = 2 };
        node.PairLabel = "My Signal";
        Assert.Equal("My Signal.2", node.Name);
    }

    [Fact]
    public void PairLabel_SyncsToPairedNode()
    {
        var node1 = new TabPassNode { PairIndex = 1 };
        var node2 = new TabPassNode { PairIndex = 2 };
        node1.Pair = node2;
        node2.Pair = node1;

        node1.PairLabel = "My Signal";

        Assert.Equal("My Signal", node2.PairLabel);
    }

    [Fact]
    public void PairLabel_SyncsNamesOnBothNodesWithCorrectIndex()
    {
        var node1 = new TabPassNode { PairIndex = 1 };
        var node2 = new TabPassNode { PairIndex = 2 };
        node1.Pair = node2;
        node2.Pair = node1;

        node1.PairLabel = "Signal";

        Assert.Equal("Signal.1", node1.Name);
        Assert.Equal("Signal.2", node2.Name);
    }

    [Fact]
    public void PairLabel_SyncsToPairedNodeInReverse()
    {
        var node1 = new TabPassNode { PairIndex = 1 };
        var node2 = new TabPassNode { PairIndex = 2 };
        node1.Pair = node2;
        node2.Pair = node1;

        node2.PairLabel = "From Pair 2";

        Assert.Equal("From Pair 2", node1.PairLabel);
    }

    [Fact]
    public void PairLabel_DoesNotCauseInfiniteRecursion()
    {
        var node1 = new TabPassNode { PairIndex = 1 };
        var node2 = new TabPassNode { PairIndex = 2 };
        node1.Pair = node2;
        node2.Pair = node1;

        // Renaming from node1 should not stack-overflow.
        node1.PairLabel = "Stable";

        Assert.Equal("Stable", node1.PairLabel);
        Assert.Equal("Stable", node2.PairLabel);
    }

    [Fact]
    public void PairLabel_SyncUpdatesDisplayNameOnBothNodes()
    {
        var node1 = new TabPassNode { PairIndex = 1 };
        var node2 = new TabPassNode { PairIndex = 2 };
        node1.Pair = node2;
        node2.Pair = node1;

        node1.PairLabel = "Shared Label";

        Assert.Equal("Shared Label", node1.DisplayName);
        Assert.Equal("Shared Label", node2.DisplayName);
    }

    [Fact]
    public void PairLabel_ClearReverts_NameToFallback()
    {
        var node = new TabPassNode { PairIndex = 1 };
        node.PairLabel = "My Signal";
        Assert.Equal("My Signal.1", node.Name);

        node.PairLabel = string.Empty;

        Assert.Equal("Tab Pass 1", node.Name);
    }
}
