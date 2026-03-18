using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaNodeEditor.Views;

/// <summary>A simple modal dialog that shows application version and attribution information.</summary>
public partial class AboutDialog : Window
{
    /// <summary>Initializes a new instance of <see cref="AboutDialog"/>.</summary>
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
