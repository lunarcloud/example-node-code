using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that receives and displays an incoming float value.</summary>
public partial class NumberReporterNode : NodeViewModel
{
    /// <inheritdoc />
    public override string NodeType => "Number Reporter";

    /// <summary>The float value received by this node.</summary>
    [ObservableProperty]
    private double _value;

    /// <summary>Initializes a new instance of <see cref="NumberReporterNode"/>.</summary>
    public NumberReporterNode()
    {
        Name = "Number Reporter";
        Inputs.Add(new ConnectorViewModel { Name = "Input" });
    }
}
