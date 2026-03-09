using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.Tests.Models;

public class NumberProducerNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new NumberProducerNode();
        Assert.Equal("Number Producer", node.Name);
    }

    [Fact]
    public void Constructor_HasSingleOutputConnector()
    {
        var node = new NumberProducerNode();
        Assert.Empty(node.Inputs);
        Assert.Single(node.Outputs);
        Assert.Equal("Output", node.Outputs[0].Name);
    }

    [Fact]
    public void Value_DefaultsToZero()
    {
        var node = new NumberProducerNode();
        Assert.Equal(0, node.Value);
    }

    [Fact]
    public void Value_CanBeSet()
    {
        var node = new NumberProducerNode();
        node.Value = 42.5;
        Assert.Equal(42.5, node.Value);
    }
}
