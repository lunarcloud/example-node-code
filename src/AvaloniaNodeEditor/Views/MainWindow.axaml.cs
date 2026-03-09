using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Adds the double-clicked toolbox entry as a new node on the canvas.</summary>
    private void OnToolboxDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: string nodeType } &&
            DataContext is MainWindowViewModel vm)
        {
            vm.AddNodeCommand.Execute(nodeType);
        }
    }
}
