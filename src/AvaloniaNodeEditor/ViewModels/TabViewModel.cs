using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Represents a single tab page in the graph editor, containing its own set of nodes and connections.</summary>
public partial class TabViewModel : ViewModelBase
{
    /// <summary>The display name of this tab.</summary>
    [ObservableProperty]
    private string _name;

    /// <summary>Whether the tab name is currently being edited inline.</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>The collection of nodes on this tab's canvas.</summary>
    public ObservableCollection<NodeViewModel> Nodes { get; } = [];

    /// <summary>The collection of connections between nodes on this tab's canvas.</summary>
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    /// <summary>Initializes a new tab with the given display name.</summary>
    public TabViewModel(string name)
    {
        _name = name;
    }
}
