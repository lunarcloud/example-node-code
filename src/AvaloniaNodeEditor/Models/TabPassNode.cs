using System;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Models;

/// <summary>
/// A pair of linked nodes that allow signal flow to continue across tabs.
/// Creating one Tab-Pass node automatically creates its sibling; the sibling mirrors
/// every connector with the opposite direction (input ↔ output).
/// </summary>
public partial class TabPassNode : NodeViewModel
{
    /// <inheritdoc />
    public override string NodeType => "Tab Pass";

    /// <summary>The pair identifier shared by this node and its sibling. Persisted to JSON.</summary>
    public Guid PairId { get; set; } = Guid.NewGuid();

    /// <summary>Transient reference to the sibling node. Not serialized.</summary>
    public TabPassNode? Pair { get; set; }

    /// <summary>The ordered list of connector slot definitions for this node.</summary>
    public ObservableCollection<TabPassConnectorSlot> ConnectorSlots { get; } = [];

    /// <summary>Initializes a new instance of <see cref="TabPassNode"/> with no connector slots.</summary>
    public TabPassNode()
    {
        Name = "Tab Pass";
    }

    /// <summary>
    /// Adds a connector slot to this node and — when a <see cref="Pair"/> is linked —
    /// adds the mirrored slot to the sibling node.
    /// </summary>
    /// <param name="name">Display name for the slot.</param>
    /// <param name="isInput"><c>true</c> to create an input here (output on the sibling); <c>false</c> for the reverse.</param>
    public void AddConnectorSlot(string name, bool isInput)
    {
        AddConnectorSlotInternal(name, isInput);
        Pair?.AddConnectorSlotInternal(name, !isInput);
    }

    /// <summary>Adds a connector slot without propagating to the paired node. Used by the pair-sync path and deserialization.</summary>
    internal void AddConnectorSlotInternal(string name, bool isInput)
    {
        var slot = new TabPassConnectorSlot { Name = name, IsInput = isInput };
        ConnectorSlots.Add(slot);
        if (isInput)
            Inputs.Add(new ConnectorViewModel { Name = name });
        else
            Outputs.Add(new ConnectorViewModel { Name = name });
    }

    /// <summary>
    /// Removes a connector slot from this node and from the sibling node.
    /// Returns the connector that was removed from this node and the connector removed from the sibling,
    /// so the caller can clean up any attached connections.
    /// </summary>
    public (ConnectorViewModel? Removed, ConnectorViewModel? PairRemoved) RemoveConnectorSlot(
        TabPassConnectorSlot slot)
    {
        var removed = RemoveConnectorSlotInternal(slot.Name, slot.IsInput);
        ConnectorViewModel? pairRemoved = null;
        if (Pair is not null)
            pairRemoved = Pair.RemoveConnectorSlotInternal(slot.Name, !slot.IsInput);
        return (removed, pairRemoved);
    }

    /// <summary>Removes a connector slot without propagating to the paired node.</summary>
    internal ConnectorViewModel? RemoveConnectorSlotInternal(string name, bool isInput)
    {
        var slot = ConnectorSlots.FirstOrDefault(s => s.Name == name && s.IsInput == isInput);
        if (slot is not null)
            ConnectorSlots.Remove(slot);

        if (isInput)
        {
            var connector = Inputs.FirstOrDefault(c => c.Name == name);
            if (connector is not null)
            {
                Inputs.Remove(connector);
                return connector;
            }
        }
        else
        {
            var connector = Outputs.FirstOrDefault(c => c.Name == name);
            if (connector is not null)
            {
                Outputs.Remove(connector);
                return connector;
            }
        }

        return null;
    }

    /// <summary>Returns a generated name for the next input slot (e.g., "In 1", "In 2", …).</summary>
    public string GenerateNextInputSlotName()
    {
        int index = 1;
        while (ConnectorSlots.Any(s => s.Name == $"In {index}"))
            index++;
        return $"In {index}";
    }

    /// <summary>Returns a generated name for the next output slot (e.g., "Out 1", "Out 2", …).</summary>
    public string GenerateNextOutputSlotName()
    {
        int index = 1;
        while (ConnectorSlots.Any(s => s.Name == $"Out {index}"))
            index++;
        return $"Out {index}";
    }
}
