using NodeEditor.Model;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia Node Editor!";
    
    public IDrawingNode? Drawing { get; set; }
}
