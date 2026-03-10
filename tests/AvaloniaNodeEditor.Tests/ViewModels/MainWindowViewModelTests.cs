using Avalonia;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.Nodes);
        Assert.NotNull(viewModel.Connections);
    }

    [Fact]
    public void Constructor_NodesAndConnectionsAreEmpty()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Empty(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void Constructor_SelectedNodeIsNull()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void ToolboxItems_ContainsAllFiveNodeTypes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.ToolboxItems);
        Assert.Equal(5, viewModel.ToolboxItems.Count);
    }

    [Fact]
    public void ToolboxItems_TitlesMatchExpectedNodes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Contains("Number Producer", viewModel.ToolboxItems);
        Assert.Contains("Number Reporter", viewModel.ToolboxItems);
        Assert.Contains("Arithmetic Transform", viewModel.ToolboxItems);
        Assert.Contains("Random Number Generator", viewModel.ToolboxItems);
        Assert.Contains("Pass Filter", viewModel.ToolboxItems);
    }

    [Fact]
    public void AddNode_AddsNodeToCollection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeCommand.Execute("Number Producer");

        // Assert
        Assert.Single(viewModel.Nodes);
    }

    [Fact]
    public void AddNode_SetsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeCommand.Execute("Number Producer");

        // Assert
        Assert.NotNull(viewModel.SelectedNode);
    }

    [Fact]
    public void AddNode_AddsAllNodeTypes()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        foreach (var item in viewModel.ToolboxItems)
        {
            viewModel.AddNodeCommand.Execute(item);
        }

        // Assert
        Assert.Equal(5, viewModel.Nodes.Count);
    }

    [Fact]
    public void AddNodeAt_PlacesNodeAtSpecifiedPosition()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var expectedPosition = new Point(123, 456);

        // Act
        viewModel.AddNodeAt("Number Producer", expectedPosition);

        // Assert
        Assert.Single(viewModel.Nodes);
        Assert.Equal(expectedPosition, viewModel.Nodes[0].Location);
    }

    [Theory]
    [InlineData("Number Producer")]
    [InlineData("Number Reporter")]
    [InlineData("Arithmetic Transform")]
    [InlineData("Random Number Generator")]
    [InlineData("Pass Filter")]
    public void AddNodeAt_CreatesCorrectNodeType(string nodeType)
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var position = new Point(10, 20);

        // Act
        viewModel.AddNodeAt(nodeType, position);

        // Assert
        Assert.Single(viewModel.Nodes);
        Assert.Equal(nodeType, viewModel.Nodes[0].Name);
    }

    [Fact]
    public void AddNodeAt_SetsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeAt("Pass Filter", new Point(0, 0));

        // Assert
        Assert.NotNull(viewModel.SelectedNode);
        Assert.Equal("Pass Filter", viewModel.SelectedNode.Name);
    }

    [Fact]
    public void ConnectionCompleted_WithSourceTargetTuple_CreatesConnection()
    {
        // Arrange — NodifyEditor fires ConnectionCompletedCommand with (source, target) tuple
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        // Assert
        Assert.Single(viewModel.Connections);
        Assert.Same(source, viewModel.Connections[0].Source);
        Assert.Same(target, viewModel.Connections[0].Target);
    }

    [Fact]
    public void ConnectionCompleted_WithSourceTargetTuple_SetsIsConnected()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        // Assert
        Assert.True(source.IsConnected);
        Assert.True(target.IsConnected);
    }

    [Fact]
    public void ConnectionCompleted_SameSourceAndTarget_DoesNotCreateConnection()
    {
        // Arrange — self-loops should be rejected
        var viewModel = new MainWindowViewModel();
        var connector = new ConnectorViewModel { Name = "out" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((connector, connector));

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void ConnectionCompleted_NullArg_DoesNotThrowOrCreateConnection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.ConnectionCompletedCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void RemoveConnection_RemovesConnectionAndClearsIsConnected()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Single(viewModel.Connections);

        // Act
        viewModel.RemoveConnectionCommand.Execute(viewModel.Connections[0]);

        // Assert
        Assert.Empty(viewModel.Connections);
        Assert.False(source.IsConnected);
        Assert.False(target.IsConnected);
    }
}
