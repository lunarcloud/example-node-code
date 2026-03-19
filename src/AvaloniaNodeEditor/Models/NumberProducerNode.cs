using AvaloniaNodeEditor.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that produces a constant float value on its output connector.</summary>
public partial class NumberProducerNode : NodeViewModel
{
    /// <inheritdoc />
    public override string NodeType => "Number Producer";

    /// <summary>The float value produced by this node.</summary>
    [ObservableProperty]
    private double _value;

    /// <summary>Initializes a new instance of <see cref="NumberProducerNode"/>.</summary>
    public NumberProducerNode()
    {
        Name = "Number Producer";
        Outputs.Add(new ConnectorViewModel { Name = "Output" });
    }
}
