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
}
