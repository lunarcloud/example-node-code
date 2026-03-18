using AvaloniaNodeEditor.Models;

namespace AvaloniaNodeEditor.Tests.Models;

public class NumberReporterNodeTests
{
    [Fact]
    public void Constructor_SetsExpectedName()
    {
        var node = new NumberReporterNode();
        Assert.Equal("Number Reporter", node.Name);
        Assert.Equal("Number Reporter", node.NodeType);
    }

    [Fact]
    public void Constructor_HasSingleInputConnector()
    {
        var node = new NumberReporterNode();
        Assert.Single(node.Inputs);
        Assert.Empty(node.Outputs);
        Assert.Equal("Input", node.Inputs[0].Name);
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
