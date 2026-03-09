using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeEditor.Model;
using NodeEditor.Mvvm;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that produces a constant float value on its output pin.</summary>
public partial class NumberProducerNode : NodeViewModel
{
    /// <summary>The float value produced by this node.</summary>
    [ObservableProperty]
    private double _value;

    /// <summary>Initializes a new instance of <see cref="NumberProducerNode"/>.</summary>
    public NumberProducerNode()
    {
        Name = "Number Producer";
        Width = 160;
        Height = 60;
        Content = this;
        Pins = new ObservableCollection<IPin>
        {
            new PinViewModel { Name = "Output", Alignment = PinAlignment.Right, Parent = this },
        };
    }
}
