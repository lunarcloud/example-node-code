using AvaloniaNodeEditor.ViewModels;
using NodeEditor.Model;
using NodeEditor.Mvvm;
using Xunit;

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
        Assert.NotNull(viewModel.Editor);
    }

    [Fact]
    public void Constructor_InitializesEditor()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.Editor);
    }

    [Fact]
    public void Drawing_ReturnsEditorDrawing()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.Drawing);
        Assert.Same(viewModel.Editor.Drawing, viewModel.Drawing);
    }

    [Fact]
    public void Editor_Templates_ContainsAllFiveNodeTypes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.Editor.Templates);
        Assert.Equal(5, viewModel.Editor.Templates.Count);
    }

    [Fact]
    public void Editor_Templates_TitlesMatchExpectedNodes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();
        Assert.NotNull(viewModel.Editor.Templates);
        var titles = viewModel.Editor.Templates.Select(t => t.Title).ToList();

        // Assert
        Assert.Contains("Number Producer", titles);
        Assert.Contains("Number Reporter", titles);
        Assert.Contains("Arithmetic Transform", titles);
        Assert.Contains("Random Number Generator", titles);
        Assert.Contains("Pass Filter", titles);
    }

    [Fact]
    public void Editor_Drawing_IsDrawingNodeViewModel()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.IsType<DrawingNodeViewModel>(viewModel.Editor.Drawing);
    }
}
