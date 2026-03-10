using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that applies an arithmetic operation to two input float values and produces a result.</summary>
public partial class ArithmeticTransformNode : NodeViewModel
{
    /// <summary>The first input operand.</summary>
    [ObservableProperty]
    private double _inputA;

    /// <summary>The second input operand.</summary>
    [ObservableProperty]
    private double _inputB;

    /// <summary>The result of the arithmetic operation.</summary>
    [ObservableProperty]
    private double _result;

    /// <summary>The arithmetic operation to apply.</summary>
    [ObservableProperty]
    private ArithmeticOperation _operation = ArithmeticOperation.Add;

    /// <summary>Initializes a new instance of <see cref="ArithmeticTransformNode"/>.</summary>
    public ArithmeticTransformNode()
    {
        Name = "Arithmetic Transform";
        Inputs.Add(new ConnectorViewModel { Name = "A" });
        Inputs.Add(new ConnectorViewModel { Name = "B" });
        Outputs.Add(new ConnectorViewModel { Name = "Result" });
    }

    /// <summary>Computes the result from <see cref="InputA"/>, <see cref="InputB"/>, and <see cref="Operation"/>.</summary>
    /// <returns>The computed result, or <see cref="double.NaN"/> when dividing by zero.</returns>
    public double Compute()
    {
        Result = Operation switch
        {
            ArithmeticOperation.Add => InputA + InputB,
            ArithmeticOperation.Subtract => InputA - InputB,
            ArithmeticOperation.Multiply => InputA * InputB,
            ArithmeticOperation.Divide when InputB == 0 => double.NaN,
            ArithmeticOperation.Divide => InputA / InputB,
            _ => double.NaN,
        };
        return Result;
    }
}
