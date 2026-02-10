using CommunityToolkit.Mvvm.ComponentModel;
using NodeEditor.Model;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia Node Editor!";

    [ObservableProperty]
    private IDrawingNode? _drawing;
}
