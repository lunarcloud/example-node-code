using System;
using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Base class for all node view models in the node graph.</summary>
public abstract partial class NodeViewModel : ObservableObject
{
    /// <summary>The type identifier for this node (e.g. "Number Producer").</summary>
    public abstract string NodeType { get; }

    /// <summary>The unique user-editable name of this node.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Indicates whether this node has a duplicate name conflict with another node.</summary>
    [ObservableProperty]
    private bool _hasNameError;

    /// <summary>The position of the node on the editor canvas. Coordinates are clamped to non-negative values.</summary>
    [ObservableProperty]
    private Point _location;

    /// <summary>Clamps the location so that neither X nor Y can be negative.</summary>
    partial void OnLocationChanged(Point value)
    {
        // Clamp each axis independently; only reassign if something changed.
        // The generated setter's equality guard ensures the second assignment (when
        // clamping is needed) does not trigger a third call, so there is no infinite loop.
        var clampedX = Math.Max(0, value.X);
        var clampedY = Math.Max(0, value.Y);
        if (clampedX != value.X || clampedY != value.Y)
            Location = new Point(clampedX, clampedY);
    }

    /// <summary>True when this node is currently selected in the editor.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>True when this node's name is currently being edited inline.</summary>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>The input connectors for this node.</summary>
    public ObservableCollection<ConnectorViewModel> Inputs { get; } = [];

    /// <summary>The output connectors for this node.</summary>
    public ObservableCollection<ConnectorViewModel> Outputs { get; } = [];
}
