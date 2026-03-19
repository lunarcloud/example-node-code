using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaNodeEditor.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

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

    /// <summary>
    /// The shared display label shown in the node header on the canvas.
    /// Both nodes of the pair share the same <see cref="PairLabel"/>; changes propagate to the sibling.
    /// Changing <see cref="PairLabel"/> also auto-updates the underlying <see cref="NodeViewModel.Name"/>
    /// to <c>{PairLabel}.{PairIndex}</c> on both nodes.
    /// </summary>
    [ObservableProperty]
    private string _pairLabel = string.Empty;

    /// <inheritdoc />
    public override string DisplayName => string.IsNullOrEmpty(PairLabel) ? Name : PairLabel;

    /// <summary>
    /// The 1-based index of this node within the pair (1 or 2).
    /// Used to generate the unique internal <see cref="NodeViewModel.Name"/> as
    /// <c>{PairLabel}.{PairIndex}</c> when <see cref="PairLabel"/> changes.
    /// </summary>
    public int PairIndex { get; set; } = 1;

    /// <summary>Prevents recursive label sync when the paired node's label is being updated.</summary>
    private bool _isSyncingPairLabel;

    /// <summary>Called whenever <see cref="PairLabel"/> changes; propagates to the sibling and updates both nodes' names.</summary>
    partial void OnPairLabelChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));

        // Auto-set this node's internal Name to "{PairLabel}.{PairIndex}".
        // When PairLabel is cleared, revert to the generic "Tab Pass {PairIndex}" fallback.
        Name = string.IsNullOrEmpty(value)
            ? $"Tab Pass {PairIndex}"
            : $"{value}.{PairIndex}";

        if (_isSyncingPairLabel || Pair is null || Pair.PairLabel == value)
            return;

        Pair._isSyncingPairLabel = true;
        try
        {
            Pair.PairLabel = value;
        }
        finally
        {
            Pair._isSyncingPairLabel = false;
        }
    }

    /// <summary>The pair identifier shared by this node and its sibling. Persisted to JSON.</summary>
    public Guid PairId { get; set; } = Guid.NewGuid();

    /// <summary>Transient reference to the sibling node. Not serialized.</summary>
    public TabPassNode? Pair { get; set; }

    /// <summary>The ordered list of connector slot definitions for this node.</summary>
    public ObservableCollection<TabPassConnectorSlot> ConnectorSlots { get; } = [];

    /// <summary>Tracks old slot names captured by PropertyChanging, keyed by slot instance.</summary>
    private readonly Dictionary<TabPassConnectorSlot, string> _pendingSlotRenames = [];

    /// <summary>Prevents recursive sync when this node's slots are being updated by the pair.</summary>
    private bool _isSyncingSlotName;

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

        // Subscribe to name changes so the slot label and the paired node stay in sync.
        slot.PropertyChanging += OnSlotPropertyChanging;
        slot.PropertyChanged += OnSlotPropertyChanged;

        ConnectorSlots.Add(slot);
        if (isInput)
        {
            Inputs.Add(new ConnectorViewModel { Name = name });
        }
        else
        {
            Outputs.Add(new ConnectorViewModel { Name = name });
        }
    }

    private void OnSlotPropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(TabPassConnectorSlot.Name) && sender is TabPassConnectorSlot slot)
        {
            _pendingSlotRenames.TryAdd(slot, slot.Name);
        }
    }

    private void OnSlotPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabPassConnectorSlot.Name) && sender is TabPassConnectorSlot slot)
        {
            SyncSlotNameChange(slot);
        }
    }

    /// <summary>
    /// Propagates a slot name change to the matching <see cref="ConnectorViewModel"/> on this node
    /// and to the mirrored slot (and its connector) on the <see cref="Pair"/>.
    /// </summary>
    private void SyncSlotNameChange(TabPassConnectorSlot changedSlot)
    {
        // Always consume the pending rename entry even when suppressing recursive sync.
        if (!_pendingSlotRenames.Remove(changedSlot, out var oldName))
        {
            return;
        }

        if (_isSyncingSlotName)
        {
            return;
        }

        if (oldName == changedSlot.Name)
        {
            return;
        }

        var newName = changedSlot.Name;

        // Update the ConnectorViewModel label on this node.
        var ownConnectors = changedSlot.IsInput ? Inputs : Outputs;
        var ownConnector = ownConnectors.FirstOrDefault(c => c.Name == oldName);
        if (ownConnector is not null)
        {
            ownConnector.Name = newName;
        }

        // Propagate to the paired node (mirrored direction, same name).
        if (Pair is not null)
        {
            Pair._isSyncingSlotName = true;
            try
            {
                var pairSlot = Pair.ConnectorSlots.FirstOrDefault(
                    s => s.Name == oldName && s.IsInput != changedSlot.IsInput);
                if (pairSlot is not null)
                {
                    pairSlot.Name = newName;
                }

                var pairConnectors = changedSlot.IsInput ? Pair.Outputs : Pair.Inputs;
                var pairConnector = pairConnectors.FirstOrDefault(c => c.Name == oldName);
                if (pairConnector is not null)
                {
                    pairConnector.Name = newName;
                }
            }
            finally
            {
                Pair._isSyncingSlotName = false;
            }
        }
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
        {
            pairRemoved = Pair.RemoveConnectorSlotInternal(slot.Name, !slot.IsInput);
        }

        return (removed, pairRemoved);
    }

    /// <summary>Removes a connector slot without propagating to the paired node.</summary>
    internal ConnectorViewModel? RemoveConnectorSlotInternal(string name, bool isInput)
    {
        var slot = ConnectorSlots.FirstOrDefault(s => s.Name == name && s.IsInput == isInput);
        if (slot is not null)
        {
            slot.PropertyChanging -= OnSlotPropertyChanging;
            slot.PropertyChanged -= OnSlotPropertyChanged;
            _pendingSlotRenames.Remove(slot);
            ConnectorSlots.Remove(slot);
        }

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
        {
            index++;
        }

        return $"In {index}";
    }

    /// <summary>Returns a generated name for the next output slot (e.g., "Out 1", "Out 2", …).</summary>
    public string GenerateNextOutputSlotName()
    {
        int index = 1;
        while (ConnectorSlots.Any(s => s.Name == $"Out {index}"))
        {
            index++;
        }

        return $"Out {index}";
    }
}

