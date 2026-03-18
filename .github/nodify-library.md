# NodifyM.Avalonia Library Reference

This document describes how the **NodifyM.Avalonia** library is used in this project so that
agents do not need to inspect the NuGet package or its source code to understand the
integration.

**Package**: `NodifyM.Avalonia` **Version**: `1.1.9`
**Namespace**: `NodifyM.Avalonia.Controls` (XAML alias `nodify`)

---

## Key Controls

### `NodifyEditor`

The root canvas control. It owns the list of node items and the list of drawn connections.

| Property / Binding | Type | Purpose |
| --- | --- | --- |
| `ItemsSource` | `ObservableCollection<NodeViewModel>` | Nodes displayed on the canvas |
| `Connections` | `ObservableCollection<ConnectionViewModel>` | Drawn connections |
| `ConnectionCompletedCommand` | `ICommand` | Fired on drag completion (see [Commands](#commands)) |
| `RemoveConnectionCommand` | `ICommand` | Fired on Alt+Click of a connection line |
| `OffsetX` / `OffsetY` | `double` | Current pan offset; subtract from drop point for canvas coords |

**Important**: Wrap `NodifyEditor` in a `<Border ClipToBounds="True">` that is assigned to
its Grid column. Setting `ClipToBounds` directly on the editor is insufficient — the internal
`Canvas` ContentPresenter bleeds outside the column and covers adjacent panels.

### `Node`

Represents a single node box on the canvas.

| Property / Binding | Type | Notes |
| --- | --- | --- |
| `Header` | `string` | Title shown at the top of the node box |
| `Location` | `Point` | **Must use `Mode=TwoWay`** — editor writes back drag positions |
| `IsSelected` | `bool` | **Must use `Mode=TwoWay`** — propagates multi-select back to ViewModel |
| `Input` | `IEnumerable` | Collection of input-side connector view models |
| `Output` | `IEnumerable` | Collection of output-side connector view models |
| `InputConnectorTemplate` | `DataTemplate` | Displayed for each item in `Input` |
| `OutputConnectorTemplate` | `DataTemplate` | Displayed for each item in `Output` |

### `Connector`

Represents a single socket (dot) on a node. Bind inside a `DataTemplate` for each entry
in `Input` / `Output`.

| Property / Binding | Type | Notes |
| --- | --- | --- |
| `IsConnected` | `bool` | Highlights the connector when at least one connection is attached |
| `Anchor` | `Point` | **Must use `Mode=TwoWay`** — library writes screen-space center back |

This project uses a single `ConnectorTemplate` (defined in `Window.Resources`) for both
input and output sides:

```xml
<DataTemplate x:Key="ConnectorTemplate" x:DataType="vm:ConnectorViewModel">
    <nodify:Connector IsConnected="{Binding IsConnected}"
                      Anchor="{Binding Anchor, Mode=TwoWay}" />
</DataTemplate>
```

### `Connection`

Draws a bezier curve between two anchor points. Bind inside `NodifyEditor.ConnectionTemplate`.

```xml
<nodify:NodifyEditor.ConnectionTemplate>
    <DataTemplate>
        <nodify:Connection Source="{Binding Source.Anchor}"
                           Target="{Binding Target.Anchor}" />
    </DataTemplate>
</nodify:NodifyEditor.ConnectionTemplate>
```

The `Source` and `Target` properties are `Point` values sourced from `ConnectorViewModel.Anchor`.

### `PendingConnection`

Renders the in-progress "rubber-band" line while the user drags a new connection.
Set `AllowOnlyConnectors="True"` so that dragging can only be started from a connector dot,
not from the node header or body.

```xml
<nodify:NodifyEditor.PendingConnectionTemplate>
    <DataTemplate>
        <nodify:PendingConnection AllowOnlyConnectors="True" />
    </DataTemplate>
</nodify:NodifyEditor.PendingConnectionTemplate>
```

---

## Theme Setup

Include the library's style resource in `App.axaml`. No separate resource dictionary is needed.

```xml
<StyleInclude Source="avares://NodifyM.Avalonia/Styles/ControlStyles.axaml" />
```

The library does **not** provide `SystemControlBackgroundChromeMediumLowBrush` or similar
Fluent shell brushes. Define your own panel brushes in `App.axaml`:

```xml
<SolidColorBrush x:Key="PanelBackgroundBrush" Color="#F3F3F3"/>  <!-- light -->
<SolidColorBrush x:Key="PanelBorderBrush"     Color="#CCCCCC"/>  <!-- light -->
```

(See `App.axaml` for the dark-mode variants.)

---

## Commands

### `ConnectionCompletedCommand`

Fired once when the user releases a drag onto a target connector.
The command parameter is a `ValueTuple<object, object>` where:

- `Item1` (`source`) — the `ConnectorViewModel` the drag started from
- `Item2` (`target`) — the `ConnectorViewModel` where the drag ended

**Do not** also bind `PendingConnection.StartedCommand` / `.CompletedCommand`. Doing so
causes double-invocation and creates duplicate connections.

Typical handler pattern:

```csharp
[RelayCommand]
private void ConnectionCompleted(object? args)
{
    if (args is not (ConnectorViewModel source, object targetObj)
        || targetObj is not ConnectorViewModel target
        || source == target)
    {
        return;
    }

    Connections.Add(new ConnectionViewModel(source, target));
    source.IsConnected = true;
    target.IsConnected = true;
}
```

### `RemoveConnectionCommand`

Fired when the user Alt+Clicks a rendered connection line. The parameter is the
`ConnectionViewModel` for that connection. After removing it, recalculate
`IsConnected` on both endpoint connectors.

---

## Drag-and-Drop (Toolbox → Canvas)

Avalonia 11.x uses the `DataTransfer` API (not the deprecated `DataObject`):

```csharp
private static readonly DataFormat<string> NodeTypeFormat =
    DataFormat.CreateStringApplicationFormat("NodeType");

// Start drag — called from PointerEventArgs e (OnToolboxPointerMoved handler)
var dataTransfer = new DataTransfer();
dataTransfer.Add(DataTransferItem.Create(NodeTypeFormat, nodeType));
await DragDrop.DoDragDropAsync(e, dataTransfer, DragDropEffects.Copy);

// Handle drop — e is DragEventArgs from the NodifyEditor DragDrop.DropEvent handler
// Convert editor-local coords to canvas coords
var drop = e.GetPosition(editor);
var canvasPos = new Point(drop.X - editor.OffsetX, drop.Y - editor.OffsetY);
```

`DragEventArgs.DataTransfer` is the current API property;
`DragEventArgs.Data` is the deprecated one.

---

## ViewModel Contracts

The library does not impose base classes, but the following observable properties are
required by the XAML bindings above.

### Node ViewModel (`NodeViewModel`)

| Property | Type | Binding |
| --- | --- | --- |
| `Location` | `Point` | TwoWay → `Node.Location` |
| `IsSelected` | `bool` | TwoWay → `Node.IsSelected` |
| `Inputs` | `ObservableCollection<ConnectorViewModel>` | → `Node.Input` |
| `Outputs` | `ObservableCollection<ConnectorViewModel>` | → `Node.Output` |

### Connector ViewModel (`ConnectorViewModel`)

| Property | Type | Binding |
| --- | --- | --- |
| `Anchor` | `Point` | TwoWay → `Connector.Anchor` |
| `IsConnected` | `bool` | → `Connector.IsConnected` |

### Connection ViewModel (`ConnectionViewModel`)

| Property | Type | Binding |
| --- | --- | --- |
| `Source` | `ConnectorViewModel` | → `Connection.Source` (via `.Anchor`) |
| `Target` | `ConnectorViewModel` | → `Connection.Target` (via `.Anchor`) |

---

## Known Pitfalls

| Pitfall | Fix |
| --- | --- |
| Nodes do not move when dragged | Add `Mode=TwoWay` to `Node.Location` binding |
| Canvas bleeds over adjacent panels | Wrap `NodifyEditor` in `<Border ClipToBounds="True">` |
| Connections created twice | Bind only `ConnectionCompletedCommand`; drop `PendingConnection` commands |
| Drop position wrong after panning | Subtract `editor.OffsetX` / `editor.OffsetY` from the drop point |
| Anchor points are always (0,0) | Add `Mode=TwoWay` to `Connector.Anchor` binding |
| `IsSelected` not reflected in ViewModel | Add `Mode=TwoWay` to `Node.IsSelected` binding |
