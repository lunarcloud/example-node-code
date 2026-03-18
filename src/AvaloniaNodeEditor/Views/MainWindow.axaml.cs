using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaNodeEditor.ViewModels;
using NodifyM.Avalonia.Controls;
using Key = Avalonia.Input.Key;
using KeyModifiers = Avalonia.Input.KeyModifiers;

namespace AvaloniaNodeEditor.Views;

public partial class MainWindow : Window
{
    /// <summary>Data format used to pass the node type string through the drag operation.</summary>
    private static readonly DataFormat<string> NodeTypeFormat =
        DataFormat.CreateStringApplicationFormat("NodeType");

    private Point? _galleryDragStart;
    private string? _galleryDragNodeType;

    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to NodifyEditor drag-and-drop events.
        DragDrop.SetAllowDrop(NodeEditorControl, true);
        NodeEditorControl.AddHandler(DragDrop.DragOverEvent, OnEditorDragOver);
        NodeEditorControl.AddHandler(DragDrop.DropEvent, OnEditorDrop);
    }

    // ── Gallery drag-start — wired up when the Gallery control is loaded ───

    /// <summary>
    /// Called when the NodeGallery control is loaded. Wires up pointer events for
    /// drag detection so gallery items can be dragged to the canvas.
    /// </summary>
    private void OnNodeGalleryLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control gallery)
        {
            return;
        }

        // handledEventsToo: true is required because GalleryItem (ListBoxItem) marks
        // PointerPressed as handled during selection before the event bubbles up.
        gallery.AddHandler(InputElement.PointerPressedEvent, OnGalleryPointerPressed,
            RoutingStrategies.Bubble, handledEventsToo: true);
        gallery.AddHandler(InputElement.PointerMovedEvent, OnGalleryPointerMoved,
            RoutingStrategies.Bubble, handledEventsToo: true);
        gallery.AddHandler(InputElement.PointerCaptureLostEvent, OnGalleryPointerCaptureLost);
    }

    private void OnGalleryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _galleryDragStart = e.GetPosition(null);
        // SelectedItem is already updated because GalleryItem (ListBoxItem) marks
        // PointerPressed as handled (completing its own selection) before this fires.
        if (NodeGallery?.SelectedItem is Control selectedItem)
        {
            _galleryDragNodeType = selectedItem.Tag as string;
        }
    }

    private async void OnGalleryPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_galleryDragStart is null || _galleryDragNodeType is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _galleryDragStart = null;
            _galleryDragNodeType = null;
            return;
        }

        // Only start the drag after the pointer has moved a minimum distance.
        var delta = e.GetPosition(null) - _galleryDragStart.Value;
        if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4)
        {
            return;
        }

        var nodeType = _galleryDragNodeType;
        _galleryDragStart = null;
        _galleryDragNodeType = null;

        try
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(NodeTypeFormat, nodeType));
            await DragDrop.DoDragDropAsync(e, dataTransfer, DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Drag-drop failed: {ex.Message}");
        }
    }

    private void OnGalleryPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _galleryDragStart = null;
        _galleryDragNodeType = null;
    }

    // ── NodifyEditor drop ──────────────────────────────────────────────────

    private static void OnEditorDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(NodeTypeFormat)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnEditorDrop(object? sender, DragEventArgs e)
    {
        var nodeType = e.DataTransfer.TryGetValue(NodeTypeFormat);
        if (nodeType is null)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (sender is not NodifyEditor editor)
        {
            return;
        }

        // Convert the drop position (editor-local logical coords) to canvas coords
        // by subtracting the current pan offset (ViewTranslateTransform.X/Y = OffsetX/Y).
        var drop = e.GetPosition(editor);
        var canvasPos = new Point(drop.X - editor.OffsetX, drop.Y - editor.OffsetY);
        vm.AddNodeAt(nodeType, canvasPos);
    }

    // ── Save / Load ────────────────────────────────────────────────────────

    /// <summary>The file type filter for the save/load file dialogs.</summary>
    private static readonly FilePickerFileType JsonFileType = new("JSON Files")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"],
    };

    /// <summary>Saves the current node graph to a JSON file chosen by the user.</summary>
    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var storageProvider = StorageProvider;
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Node Graph",
            DefaultExtension = "json",
            FileTypeChoices = [JsonFileType],
            SuggestedFileName = "node-graph",
        });

        if (file is null)
        {
            return;
        }

        var json = vm.SerializeGraph();
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
    }

    /// <summary>Loads a node graph from a JSON file chosen by the user.</summary>
    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var storageProvider = StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Node Graph",
            AllowMultiple = false,
            FileTypeFilter = [JsonFileType],
        });

        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        vm.DeserializeGraph(json);
    }

    /// <summary>Shows the About dialog.</summary>
    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        await dialog.ShowDialog(this);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            if (e.Key == Key.S)
            {
                OnSaveClick(null, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.O)
            {
                OnLoadClick(null, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
