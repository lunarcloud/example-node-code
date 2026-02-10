using AvaloniaNodeEditor.ViewModels;
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
        Assert.Equal("Welcome to Avalonia Node Editor!", viewModel.Greeting);
    }

    [Fact]
    public void Drawing_CanBeSet()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.Drawing = null;

        // Assert
        Assert.Null(viewModel.Drawing);
    }

    [Fact]
    public void Drawing_DefaultValue_IsNull()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Null(viewModel.Drawing);
    }
}
