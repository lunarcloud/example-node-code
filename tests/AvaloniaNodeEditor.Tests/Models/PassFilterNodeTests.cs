using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.Tests.Models;

public class PassFilterNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new PassFilterNode();
        Assert.Equal("Pass Filter", node.Name);
        Assert.Equal("Pass Filter", node.NodeType);
    }

    [Fact]
    public void Constructor_HasInputAndOutputConnectors()
    {
        var node = new PassFilterNode();
        Assert.Single(node.Inputs);
        Assert.Single(node.Outputs);
        Assert.Equal("Input", node.Inputs[0].Name);
        Assert.Equal("Output", node.Outputs[0].Name);
    }

    [Fact]
    public void Constructor_DefaultFilterTypeIsLowPass()
    {
        var node = new PassFilterNode();
        Assert.Equal(FilterType.LowPass, node.FilterType);
    }

    [Theory]
    [InlineData(3.0, 5.0, FilterType.LowPass, 3.0)]
    [InlineData(7.0, 5.0, FilterType.LowPass, 0.0)]
    [InlineData(5.0, 5.0, FilterType.LowPass, 5.0)]
    public void Filter_LowPass_PassesBelowThreshold(double input, double threshold, FilterType type, double expected)
    {
        var node = new PassFilterNode { InputValue = input, Threshold = threshold, FilterType = type };
        var result = node.Filter();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(7.0, 5.0, FilterType.HighPass, 7.0)]
    [InlineData(3.0, 5.0, FilterType.HighPass, 0.0)]
    [InlineData(5.0, 5.0, FilterType.HighPass, 5.0)]
    public void Filter_HighPass_PassesAboveThreshold(double input, double threshold, FilterType type, double expected)
    {
        var node = new PassFilterNode { InputValue = input, Threshold = threshold, FilterType = type };
        var result = node.Filter();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5.0, 3.0, 7.0, FilterType.MidPass, 5.0)]
    [InlineData(2.0, 3.0, 7.0, FilterType.MidPass, 0.0)]
    [InlineData(8.0, 3.0, 7.0, FilterType.MidPass, 0.0)]
    [InlineData(3.0, 3.0, 7.0, FilterType.MidPass, 3.0)]
    [InlineData(7.0, 3.0, 7.0, FilterType.MidPass, 7.0)]
    public void Filter_MidPass_PassesWithinRange(double input, double low, double high, FilterType type, double expected)
    {
        var node = new PassFilterNode { InputValue = input, Threshold = low, UpperThreshold = high, FilterType = type };
        var result = node.Filter();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Filter_UpdatesOutputValueProperty()
    {
        var node = new PassFilterNode { InputValue = 3.0, Threshold = 5.0, FilterType = FilterType.LowPass };
        node.Filter();
        Assert.Equal(3.0, node.OutputValue);
    }
}
