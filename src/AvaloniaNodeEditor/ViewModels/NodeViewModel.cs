using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Base class for all node view models in the node graph.</summary>
public abstract partial class NodeViewModel : ObservableObject
{
    /// <summary>The display name of the node.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>The position of the node on the editor canvas.</summary>
    [ObservableProperty]
    private Point _location;

    /// <summary>True when this node is currently selected in the editor.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>The input connectors for this node.</summary>
    public ObservableCollection<ConnectorViewModel> Inputs { get; } = [];

    /// <summary>The output connectors for this node.</summary>
    public ObservableCollection<ConnectorViewModel> Outputs { get; } = [];
}
