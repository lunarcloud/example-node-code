using AvaloniaNodeEditor.Models;
using NodeEditor.Model;

namespace AvaloniaNodeEditor.Tests.Models;

public class ArithmeticTransformNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new ArithmeticTransformNode();
        Assert.Equal("Arithmetic Transform", node.Name);
    }

    [Fact]
    public void Constructor_HasTwoInputsAndOneOutputPin()
    {
        var node = new ArithmeticTransformNode();
        Assert.NotNull(node.Pins);
        Assert.Equal(3, node.Pins.Count);
        Assert.Equal(PinAlignment.Left, node.Pins[0].Alignment);
        Assert.Equal(PinAlignment.Left, node.Pins[1].Alignment);
        Assert.Equal(PinAlignment.Right, node.Pins[2].Alignment);
    }

    [Fact]
    public void Constructor_DefaultOperationIsAdd()
    {
        var node = new ArithmeticTransformNode();
        Assert.Equal(ArithmeticOperation.Add, node.Operation);
    }

    [Theory]
    [InlineData(3, 4, ArithmeticOperation.Add, 7)]
    [InlineData(10, 4, ArithmeticOperation.Subtract, 6)]
    [InlineData(3, 4, ArithmeticOperation.Multiply, 12)]
    [InlineData(12, 4, ArithmeticOperation.Divide, 3)]
    public void Compute_ReturnsExpectedResult(double a, double b, ArithmeticOperation op, double expected)
    {
        var node = new ArithmeticTransformNode { InputA = a, InputB = b, Operation = op };
        var result = node.Compute();
        Assert.Equal(expected, result);
        Assert.Equal(expected, node.Result);
    }

    [Fact]
    public void Compute_DivideByZero_ReturnsNaN()
    {
        var node = new ArithmeticTransformNode { InputA = 5, InputB = 0, Operation = ArithmeticOperation.Divide };
        var result = node.Compute();
        Assert.True(double.IsNaN(result));
    }
}
