using AvaloniaNodeEditor.Models;
using NodeEditor.Model;

namespace AvaloniaNodeEditor.Tests.Models;

public class RandomNumberGeneratorNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new RandomNumberGeneratorNode();
        Assert.Equal("Random Number Generator", node.Name);
    }

    [Fact]
    public void Constructor_HasSingleOutputPin()
    {
        var node = new RandomNumberGeneratorNode();
        Assert.NotNull(node.Pins);
        Assert.Single(node.Pins);
        Assert.Equal("Output", node.Pins[0].Name);
        Assert.Equal(PinAlignment.Right, node.Pins[0].Alignment);
    }

    [Fact]
    public void Constructor_DefaultRangeIsZeroToOne()
    {
        var node = new RandomNumberGeneratorNode();
        Assert.Equal(0, node.MinValue);
        Assert.Equal(1.0, node.MaxValue);
    }

    [Fact]
    public void Generate_ReturnsValueWithinRange()
    {
        var node = new RandomNumberGeneratorNode { MinValue = 5.0, MaxValue = 10.0 };
        for (var i = 0; i < 20; i++)
        {
            var value = node.Generate();
            Assert.InRange(value, 5.0, 10.0);
        }
    }

    [Fact]
    public void Generate_UpdatesValueProperty()
    {
        var node = new RandomNumberGeneratorNode { MinValue = 0, MaxValue = 1 };
        var value = node.Generate();
        Assert.Equal(value, node.Value);
    }
}
