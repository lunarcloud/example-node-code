using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.Tests.Models;

public class ArithmeticTransformNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new ArithmeticTransformNode();
        Assert.Equal("Arithmetic Transform", node.Name);
        Assert.Equal("Arithmetic Transform", node.NodeType);
    }

    [Fact]
    public void Constructor_HasTwoInputsAndOneOutput()
    {
        var node = new ArithmeticTransformNode();
        Assert.Equal(2, node.Inputs.Count);
        Assert.Single(node.Outputs);
        Assert.Equal("A", node.Inputs[0].Name);
        Assert.Equal("B", node.Inputs[1].Name);
        Assert.Equal("Result", node.Outputs[0].Name);
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
