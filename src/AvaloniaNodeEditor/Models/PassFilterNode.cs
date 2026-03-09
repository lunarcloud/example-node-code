using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that passes or blocks an input float value based on a configurable filter type and threshold.</summary>
public partial class PassFilterNode : NodeViewModel
{
    /// <summary>The float value received at the input connector.</summary>
    [ObservableProperty]
    private double _inputValue;

    /// <summary>The filtered float value emitted at the output connector.</summary>
    [ObservableProperty]
    private double _outputValue;

    /// <summary>The filter type (low-pass, mid-pass, or high-pass).</summary>
    [ObservableProperty]
    private FilterType _filterType = FilterType.LowPass;

    /// <summary>The primary threshold used for low-pass and high-pass filtering.</summary>
    [ObservableProperty]
    private double _threshold;

    /// <summary>
    /// The upper threshold used when <see cref="FilterType"/> is <see cref="FilterType.MidPass"/>.
    /// This property is ignored for <see cref="FilterType.LowPass"/> and <see cref="FilterType.HighPass"/>.
    /// </summary>
    [ObservableProperty]
    private double _upperThreshold;

    /// <summary>Initializes a new instance of <see cref="PassFilterNode"/>.</summary>
    public PassFilterNode()
    {
        Name = "Pass Filter";
        Inputs.Add(new ConnectorViewModel { Name = "Input" });
        Outputs.Add(new ConnectorViewModel { Name = "Output" });
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
