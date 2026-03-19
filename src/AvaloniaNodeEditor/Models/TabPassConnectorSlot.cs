using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.Models;

/// <summary>Defines a named connector slot on a <see cref="TabPassNode"/>, specifying its direction relative to that node.</summary>
public partial class TabPassConnectorSlot : ObservableObject
{
    /// <summary>The display name of this connector slot.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// When <c>true</c>, this slot appears as an input on its owner node and as an output on the paired node.
    /// When <c>false</c>, this slot appears as an output on its owner node and as an input on the paired node.
    /// </summary>
    public bool IsInput { get; set; }

    /// <summary>Returns a human-readable direction label ("Input" or "Output").</summary>
    public string DirectionLabel => IsInput ? "Input" : "Output";
}
