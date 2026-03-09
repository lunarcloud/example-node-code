using AvaloniaNodeEditor.Models;
using NodeEditor.Model;

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
    public void Constructor_HasSingleOutputPin()
    {
        var node = new NumberProducerNode();
        Assert.NotNull(node.Pins);
        Assert.Single(node.Pins);
        Assert.Equal("Output", node.Pins[0].Name);
        Assert.Equal(PinAlignment.Right, node.Pins[0].Alignment);
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
