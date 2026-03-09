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
}
