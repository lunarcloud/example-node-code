using AvaloniaNodeEditor.Models;
using NodeEditor.Model;

namespace AvaloniaNodeEditor.Tests.Models;

public class NumberReporterNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new NumberReporterNode();
        Assert.Equal("Number Reporter", node.Name);
    }

    [Fact]
    public void Constructor_HasSingleInputPin()
    {
        var node = new NumberReporterNode();
        Assert.NotNull(node.Pins);
        Assert.Single(node.Pins);
        Assert.Equal("Input", node.Pins[0].Name);
        Assert.Equal(PinAlignment.Left, node.Pins[0].Alignment);
    }

    [Fact]
    public void Value_DefaultsToZero()
    {
        var node = new NumberReporterNode();
        Assert.Equal(0, node.Value);
    }

    [Fact]
    public void Value_CanBeSet()
    {
        var node = new NumberReporterNode();
        node.Value = -7.3;
        Assert.Equal(-7.3, node.Value);
    }
}
