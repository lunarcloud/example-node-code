using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeEditor.Model;
using NodeEditor.Mvvm;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that receives and displays an incoming float value.</summary>
public partial class NumberReporterNode : NodeViewModel
{
    /// <summary>The float value received by this node.</summary>
    [ObservableProperty]
    private double _value;

    /// <summary>Initializes a new instance of <see cref="NumberReporterNode"/>.</summary>
    public NumberReporterNode()
    {
        Name = "Number Reporter";
        Width = 160;
        Height = 60;
        Content = Name;
        Pins = new ObservableCollection<IPin>
        {
            new PinViewModel { Name = "Input", Alignment = PinAlignment.Left, Parent = this },
        };
    }
}
