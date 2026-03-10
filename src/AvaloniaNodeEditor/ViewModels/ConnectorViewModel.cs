using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Represents a single input or output connector on a node.</summary>
public partial class ConnectorViewModel : ObservableObject
{
    /// <summary>The display name of the connector.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>True when at least one connection is attached to this connector.</summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>The canvas anchor point of this connector, updated by the Connector control via two-way binding.</summary>
    [ObservableProperty]
    private Point _anchor;
}
