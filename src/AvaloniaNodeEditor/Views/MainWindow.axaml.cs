using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    // ── Middle-mouse-button panning state ──────────────────────────────────
    private bool _isMiddleMousePanning;
    private Point _middlePanStartPoint;
    private double _middlePanStartOffsetX;
    private double _middlePanStartOffsetY;

    // ── Node-drag state ─────────────────────────────────────────────────────
    private bool _leftButtonDownOnInteractiveElement;

    // ── Tab-rename state ────────────────────────────────────────────────────
    private string? _tabRenameOldName;
    private TabViewModel? _tabBeingRenamed;

    // ── Node context-menu target ─────────────────────────────────────────────
    // Captured in the tunnel right-click handler so that context-menu item clicks
    // can reliably restore SelectedNode even if NodifyEditor deselects nodes after
    // the pointer press.
    private NodeViewModel? _contextMenuTargetNode;

    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to NodifyEditor drag-and-drop events.
        DragDrop.SetAllowDrop(NodeEditorControl, true);
        NodeEditorControl.AddHandler(DragDrop.DragOverEvent, OnEditorDragOver);
        NodeEditorControl.AddHandler(DragDrop.DropEvent, OnEditorDrop);

        // Tunnel handler fires before the NodifyEditor's own OnPointerPressed so we can:
        //   • Intercept bare left-clicks on the empty canvas to prevent the built-in
        //     left-button panning, and
        //   • Capture the middle mouse button to start our custom pan gesture.
        NodeEditorControl.AddHandler(
            InputElement.PointerPressedEvent,
            OnEditorPointerPressedTunnel,
            RoutingStrategies.Tunnel);

        // Bubble handlers (with handledEventsToo: true) track middle-mouse move/release
        // even after NodifyEditor marks the event as handled.
        NodeEditorControl.AddHandler(
            InputElement.PointerMovedEvent,
            OnEditorPointerMovedMiddle,
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        NodeEditorControl.AddHandler(
            InputElement.PointerReleasedEvent,
            OnEditorPointerReleasedMiddle,
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        // Bubble handler for left-button release: finalizes node-drag for undo recording.
        NodeEditorControl.AddHandler(
            InputElement.PointerReleasedEvent,
            OnEditorPointerReleasedLeft,
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        // Keep the negative-space overlay in sync with the viewport pan offset.
        NodeEditorControl.GetObservable(NodifyEditor.OffsetXProperty)
            .Subscribe(_ => UpdateNegativeSpaceOverlay());
        NodeEditorControl.GetObservable(NodifyEditor.OffsetYProperty)
            .Subscribe(_ => UpdateNegativeSpaceOverlay());
        NodeEditorControl.SizeChanged += (_, _) => UpdateNegativeSpaceOverlay();
        NegativeSpaceCanvas.SizeChanged += (_, _) => UpdateNegativeSpaceOverlay();
    }

    // ── Negative-space overlay ─────────────────────────────────────────────

    /// <summary>
    /// Repositions the two overlay rectangles so they cover exactly the visible
    /// portion of the canvas where X &lt; 0 or Y &lt; 0.
    /// </summary>
    private void UpdateNegativeSpaceOverlay()
    {
        var w = NegativeSpaceCanvas.Bounds.Width;
        var h = NegativeSpaceCanvas.Bounds.Height;

        // clampedOffsetX/Y: how many screen pixels the canvas origin is from the
        // top-left corner of the editor.  When OffsetX is positive the canvas has
        // been panned right, so the area to the LEFT of position OffsetX (i.e.
        // screen x ∈ [0, OffsetX]) corresponds to canvas x < 0 (negative space).
        // When OffsetX ≤ 0 the origin is off-screen and no negative X area is visible.
        var clampedOffsetX = Math.Max(0, NodeEditorControl.OffsetX);
        var clampedOffsetY = Math.Max(0, NodeEditorControl.OffsetY);

        // Left vertical strip: negative X area (canvas x < 0 is visible to the left of ox).
        NegSpaceLeft.IsVisible = clampedOffsetX > 0;
        Canvas.SetLeft(NegSpaceLeft, 0);
        Canvas.SetTop(NegSpaceLeft, 0);
        NegSpaceLeft.Width = clampedOffsetX;
        NegSpaceLeft.Height = h;

        // Top horizontal strip: negative Y area (canvas y < 0 is visible above oy).
        // Starts at clampedOffsetX so the top-left corner is not painted twice.
        NegSpaceTop.IsVisible = clampedOffsetY > 0;
        Canvas.SetLeft(NegSpaceTop, clampedOffsetX);
        Canvas.SetTop(NegSpaceTop, 0);
        NegSpaceTop.Width = Math.Max(0, w - clampedOffsetX);
        NegSpaceTop.Height = clampedOffsetY;
    }

    // ── Middle-mouse panning ───────────────────────────────────────────────

    /// <summary>
    /// Tunnel-phase handler: runs before NodifyEditor's own OnPointerPressed.
    /// <list type="bullet">
    ///   <item>Middle button → start our custom pan gesture and mark handled so
    ///     NodifyEditor never sees the press.</item>
    ///   <item>Left button on empty canvas (no Shift, no Alt) → mark handled so
    ///     NodifyEditor cannot start its built-in left-button pan.</item>
    /// </list>
    /// </summary>
    private void OnEditorPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(NodeEditorControl);
        var props = point.Properties;

        if (props.IsMiddleButtonPressed)
        {
            _isMiddleMousePanning = true;
            _middlePanStartPoint = point.Position;
            _middlePanStartOffsetX = NodeEditorControl.OffsetX;
            _middlePanStartOffsetY = NodeEditorControl.OffsetY;
            e.Pointer.Capture(NodeEditorControl);
            e.Handled = true;
            return;
        }

        // Right-click: capture the right-clicked node in _contextMenuTargetNode so each
        // context-menu item click can restore SelectedNode before executing its command,
        // even if NodifyEditor deselects all nodes while processing the right-click.
        if (props.IsRightButtonPressed)
        {
            _contextMenuTargetNode = GetNodeViewModelFromSource(e.Source);
            // Do NOT mark as handled — the event must still reach the ContextMenu.
            return;
        }

        // Block left-button panning on empty canvas.
        // Allow: clicks on nodes/connectors/connections (they handle their own interaction),
        //        Shift+left (selection rectangle), Alt+left (connection removal).
        if (props.IsLeftButtonPressed
            && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            if (IsSourceOnInteractiveElement(e))
            {
                // A node (or connector/connection) was pressed — capture pre-drag locations.
                _leftButtonDownOnInteractiveElement = true;
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.BeginNodeDrag();
                }
            }
            else
            {
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Walks the visual tree from <paramref name="source"/> upward looking for a
    /// <see cref="BaseNode"/> whose DataContext is a <see cref="NodeViewModel"/>.
    /// Returns the <see cref="NodeViewModel"/> if found, <c>null</c> otherwise.
    /// </summary>
    private static NodeViewModel? GetNodeViewModelFromSource(object? source)
    {
        var visual = source as Visual;
        while (visual is not null)
        {
            if (visual is BaseNode nodeControl && nodeControl.DataContext is NodeViewModel nodeVm)
            {
                return nodeVm;
            }

            visual = visual.GetVisualParent() as Visual;
        }

        return null;
    }

    private void OnEditorPointerMovedMiddle(object? sender, PointerEventArgs e)
    {
        if (!_isMiddleMousePanning)
        {
            return;
        }

        if (!e.GetCurrentPoint(NodeEditorControl).Properties.IsMiddleButtonPressed)
        {
            _isMiddleMousePanning = false;
            e.Pointer.Capture(null);
            return;
        }

        var delta = e.GetCurrentPoint(NodeEditorControl).Position - _middlePanStartPoint;
        var newOffsetX = _middlePanStartOffsetX + delta.X;
        var newOffsetY = _middlePanStartOffsetY + delta.Y;

        // Update both the stored offset and the visual translate transform,
        // mirroring what NodifyEditor itself does in its OnPointerMoved handler.
        NodeEditorControl.OffsetX = newOffsetX;
        NodeEditorControl.ViewTranslateTransform.X = newOffsetX;
        NodeEditorControl.OffsetY = newOffsetY;
        NodeEditorControl.ViewTranslateTransform.Y = newOffsetY;

        e.Handled = true;
    }

    private void OnEditorPointerReleasedMiddle(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isMiddleMousePanning)
        {
            return;
        }

        if (e.GetCurrentPoint(NodeEditorControl).Properties.PointerUpdateKind
            == PointerUpdateKind.MiddleButtonReleased)
        {
            _isMiddleMousePanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnEditorPointerReleasedLeft(object? sender, PointerReleasedEventArgs e)
    {
        if (!_leftButtonDownOnInteractiveElement)
        {
            return;
        }

        if (e.GetCurrentPoint(NodeEditorControl).Properties.PointerUpdateKind
            == PointerUpdateKind.LeftButtonReleased)
        {
            _leftButtonDownOnInteractiveElement = false;
            if (DataContext is MainWindowViewModel vm)
            {
                vm.EndNodeDrag();
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the pointer event originated on a node, connector,
    /// or connection — i.e. any interactive canvas element that handles its own
    /// pointer events and should not be suppressed.
    /// </summary>
    private static bool IsSourceOnInteractiveElement(PointerEventArgs e)
    {
        var visual = e.Source as Visual;
        while (visual != null)
        {
            if (visual is BaseNode or Connector or BaseConnection)
            {
                return true;
            }

            visual = visual.GetVisualParent() as Visual;
        }

        return false;
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
        // Use sender (the gallery that registered the handler) to support multiple galleries.
        if (sender is ListBox gallery && gallery.SelectedItem is Control selectedItem)
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

    // ── Tab bar ──────────────────────────────────────────────────────────────

    /// <summary>Adds a new tab at the end of the tab bar.</summary>
    private void OnAddTabClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.AddTabCommand.Execute(null);
            FocusActiveTabRenameBox();
        }
    }

    // ── Node context menu ────────────────────────────────────────────────────

    /// <summary>
    /// Fired just before the node context menu opens. Selects the right-clicked node and
    /// populates the dynamic "Move To Tab" submenu with the current tab list.
    /// </summary>
    private void OnNodeContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Resolve the right-clicked node: prefer the field captured in the tunnel handler
        // (set before NodifyEditor can deselect nodes), with PlacementTarget as a fallback.
        var node = _contextMenuTargetNode
            ?? (contextMenu.PlacementTarget?.DataContext as NodeViewModel);

        if (node is not null)
        {
            // Keep _contextMenuTargetNode up to date so click handlers can reference it.
            _contextMenuTargetNode = node;

            // Re-select the node so commands have a valid target and the selection indicator
            // is visible again (NodifyEditor may have cleared IsSelected on right-click).
            node.IsSelected = true;
            vm.SelectedNode = node;
        }

        // Rebuild the "Move To Tab" submenu from the current tab list.
        var moveToTabItem = contextMenu.Items
            .OfType<MenuItem>()
            .FirstOrDefault(m => m.Header is "Move To Tab");

        if (moveToTabItem is null)
        {
            return;
        }

        var tabItems = vm.Tabs.Select(tab =>
        {
            var item = new MenuItem
            {
                Header = tab.Name,
                Tag = tab,
                IsEnabled = tab != vm.ActiveTab,
            };
            item.Click += OnNodeMoveToTabItemClick;
            return item;
        }).ToList();

        moveToTabItem.ItemsSource = tabItems;
    }

    /// <summary>
    /// Restores <see cref="MainWindowViewModel.SelectedNode"/> from <see cref="_contextMenuTargetNode"/>
    /// and ensures the node's <see cref="NodeViewModel.IsSelected"/> flag is set, counteracting any
    /// deselection NodifyEditor applied while processing the right-click press.
    /// </summary>
    private void RestoreContextMenuTargetSelection(MainWindowViewModel vm)
    {
        if (_contextMenuTargetNode is null)
        {
            return;
        }

        _contextMenuTargetNode.IsSelected = true;
        vm.SelectedNode = _contextMenuTargetNode;
    }

    /// <summary>Context menu: shows the properties panel and focuses the Name field for the right-clicked node.</summary>
    private void OnNodeContextMenuRename(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        RestoreContextMenuTargetSelection(vm);
        vm.IsPropertiesPanelVisible = true;

        Dispatcher.UIThread.Post(() =>
        {
            FocusNodeNameTextBox();
        });
    }

    /// <summary>Context menu: deletes the right-clicked node.</summary>
    private void OnNodeContextMenuDelete(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        RestoreContextMenuTargetSelection(vm);
        vm.DeleteNodeCommand.Execute(null);
    }

    /// <summary>Context menu: moves the right-clicked node to the tab identified by the menu item's Tag.</summary>
    private void OnNodeMoveToTabItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: TabViewModel targetTab } || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        RestoreContextMenuTargetSelection(vm);
        vm.MoveNodeToTabCommand.Execute(targetTab);
    }

    /// <summary>Context menu: shows the properties panel for the right-clicked node.</summary>
    private void OnNodeContextMenuProperties(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        RestoreContextMenuTargetSelection(vm);
        vm.IsPropertiesPanelVisible = true;
    }

    /// <summary>
    /// Focuses and selects-all in the Name TextBox inside the node properties content panel.
    /// Posts the action so the UI has time to render the panel before focusing.
    /// </summary>
    private void FocusNodeNameTextBox()
    {
        var textBox = NodePropertiesContent?
            .GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(tb => tb.IsVisible && tb.Name == "NodeNameTextBox");

        if (textBox is not null)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    /// <summary>Context menu: inserts a new tab to the right of the right-clicked tab.</summary>
    private void OnTabContextMenuNewTab(object? sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab && DataContext is MainWindowViewModel vm)
        {
            vm.AddTabAfterCommand.Execute(tab);
            FocusActiveTabRenameBox();
        }
    }

    /// <summary>Context menu: enters inline-rename mode for the right-clicked tab.</summary>
    private void OnTabContextMenuRename(object? sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
        {
            _tabBeingRenamed = tab;
            _tabRenameOldName = tab.Name;
            tab.IsEditing = true;
            FocusActiveTabRenameBox();
        }
    }

    /// <summary>Context menu: deletes the right-clicked tab.</summary>
    private void OnTabContextMenuDelete(object? sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab && DataContext is MainWindowViewModel vm)
        {
            vm.DeleteTabCommand.Execute(tab);
        }
    }

    /// <summary>
    /// Resolves the <see cref="TabViewModel"/> associated with a context menu item.
    /// The DataContext of a <see cref="MenuItem"/> inside a <see cref="ContextMenu"/> is
    /// inherited from the <see cref="ContextMenu"/>'s PlacementTarget, which in this case
    /// is the <see cref="Panel"/> defined in the tab item DataTemplate.
    /// </summary>
    private static TabViewModel? GetTabFromContextMenuItem(object? sender)
    {
        if (sender is not MenuItem menuItem)
        {
            return null;
        }

        // DataContext is normally propagated from the PlacementTarget automatically.
        if (menuItem.DataContext is TabViewModel tab)
        {
            return tab;
        }

        // Fallback: walk up to the ContextMenu and read PlacementTarget.DataContext.
        var parent = menuItem.Parent;
        while (parent is not null)
        {
            if (parent is ContextMenu { PlacementTarget.DataContext: TabViewModel tabFromPlacement })
            {
                return tabFromPlacement;
            }

            parent = (parent as StyledElement)?.Parent;
        }

        return null;
    }

    /// <summary>
    /// Posts a deferred action to focus and select-all the rename <see cref="TextBox"/>
    /// of whichever tab currently has <see cref="TabViewModel.IsEditing"/> set.
    /// The post is required so Avalonia has time to show the TextBox before focus is set.
    /// </summary>
    private void FocusActiveTabRenameBox()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var textBox = TabListBox?.GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(tb => tb.IsVisible && tb.DataContext is TabViewModel { IsEditing: true });
            if (textBox is not null)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        });
    }

    /// <summary>Commits the inline tab rename when the editing TextBox loses focus.</summary>
    private void OnTabTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: TabViewModel tab })
        {
            CommitTabRename(tab);
            tab.IsEditing = false;
        }
    }

    /// <summary>Commits (Enter) or reverts (Escape) the inline tab rename via keyboard.</summary>
    private void OnTabTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: TabViewModel tab })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            CommitTabRename(tab);
            tab.IsEditing = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Revert to the name from before editing started.
            if (_tabBeingRenamed == tab && _tabRenameOldName is not null)
            {
                tab.Name = _tabRenameOldName;
            }

            _tabBeingRenamed = null;
            _tabRenameOldName = null;
            tab.IsEditing = false;
            e.Handled = true;
        }
    }

    /// <summary>Records the tab rename for undo/redo if the name actually changed and the tab was not just created.</summary>
    private void CommitTabRename(TabViewModel tab)
    {
        if (_tabBeingRenamed != tab || _tabRenameOldName is null)
        {
            return;
        }

        var oldName = _tabRenameOldName;
        _tabBeingRenamed = null;
        _tabRenameOldName = null;

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Suppress recording the very first rename of a newly created tab — the AddTab
        // undo action already covers removing the entire tab.
        if (vm.NewlyCreatedTab == tab)
        {
            return;
        }

        vm.RecordTabRename(tab, oldName, tab.Name);
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
