using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeEditor.Model;
using NodeEditor.Mvvm;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that passes or blocks an input float value based on a configurable filter type and threshold.</summary>
public partial class PassFilterNode : NodeViewModel
{
    /// <summary>The float value received at the input pin.</summary>
    [ObservableProperty]
    private double _inputValue;

    /// <summary>The filtered float value emitted at the output pin.</summary>
    [ObservableProperty]
    private double _outputValue;

    /// <summary>The filter type (low-pass, mid-pass, or high-pass).</summary>
    [ObservableProperty]
    private FilterType _filterType = FilterType.LowPass;

    /// <summary>The primary threshold used for low-pass and high-pass filtering.</summary>
    [ObservableProperty]
    private double _threshold;

    /// <summary>The upper threshold used when <see cref="FilterType"/> is <see cref="FilterType.MidPass"/>.</summary>
    [ObservableProperty]
    private double _upperThreshold;

    /// <summary>Initializes a new instance of <see cref="PassFilterNode"/>.</summary>
    public PassFilterNode()
    {
        Name = "Pass Filter";
        Width = 160;
        Height = 90;
        Pins = new ObservableCollection<IPin>
        {
            new PinViewModel { Name = "Input", Alignment = PinAlignment.Left, Parent = this },
            new PinViewModel { Name = "Output", Alignment = PinAlignment.Right, Parent = this },
        };
    }

    /// <summary>Applies the filter to <see cref="InputValue"/> and stores the result in <see cref="OutputValue"/>.</summary>
    /// <returns>The filtered output value, or <c>0</c> when the input is blocked.</returns>
    public double Filter()
    {
        OutputValue = FilterType switch
        {
            FilterType.LowPass => InputValue <= Threshold ? InputValue : 0,
            FilterType.HighPass => InputValue >= Threshold ? InputValue : 0,
            FilterType.MidPass => (InputValue >= Threshold && InputValue <= UpperThreshold) ? InputValue : 0,
            _ => 0,
        };
        return OutputValue;
    }
}
