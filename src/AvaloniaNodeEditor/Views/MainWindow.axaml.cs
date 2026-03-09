using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaNodeEditor.ViewModels;
using NodifyM.Avalonia.Controls;

namespace AvaloniaNodeEditor.Views;

public partial class MainWindow : Window
{
    /// <summary>Data format used to pass the node type string through the drag operation.</summary>
    private static readonly DataFormat<string> NodeTypeFormat =
        DataFormat.CreateStringApplicationFormat("NodeType");

    private Point? _toolboxDragStart;
    private string? _toolboxDragNodeType;

    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to toolbox pointer events for drag detection.
        // handledEventsToo: true is required because ListBoxItem marks PointerPressed
        // as handled during selection, so we must opt-in to see already-handled events.
        ToolboxList.AddHandler(InputElement.PointerPressedEvent, OnToolboxPointerPressed,
            RoutingStrategies.Bubble, handledEventsToo: true);
        ToolboxList.AddHandler(InputElement.PointerMovedEvent, OnToolboxPointerMoved,
            RoutingStrategies.Bubble, handledEventsToo: true);
        ToolboxList.AddHandler(InputElement.PointerCaptureLostEvent, OnToolboxPointerCaptureLost);

        // Subscribe to NodifyEditor drag-and-drop events.
        DragDrop.SetAllowDrop(NodeEditorControl, true);
        NodeEditorControl.AddHandler(DragDrop.DragOverEvent, OnEditorDragOver);
        NodeEditorControl.AddHandler(DragDrop.DropEvent, OnEditorDrop);
    }

    // ── Toolbox drag-start ─────────────────────────────────────────────────

    private void OnToolboxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _toolboxDragStart = e.GetPosition(null);
        // SelectedItem is already updated because ListBoxItem marks PointerPressed as
        // handled (completing its own selection logic) before this handler fires.
        _toolboxDragNodeType = ToolboxList.SelectedItem as string;
    }

    private async void OnToolboxPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_toolboxDragStart is null || _toolboxDragNodeType is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _toolboxDragStart = null;
            _toolboxDragNodeType = null;
            return;
        }

        // Only start the drag after the pointer has moved a minimum distance.
        var delta = e.GetPosition(null) - _toolboxDragStart.Value;
        if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4)
        {
            return;
        }

        var nodeType = _toolboxDragNodeType;
        _toolboxDragStart = null;
        _toolboxDragNodeType = null;

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

    private void OnToolboxPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _toolboxDragStart = null;
        _toolboxDragNodeType = null;
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

    // ── Toolbox double-click (fallback) ────────────────────────────────────

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
