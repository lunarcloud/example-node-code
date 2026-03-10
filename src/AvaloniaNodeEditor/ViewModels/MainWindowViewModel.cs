using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaNodeEditor.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>The collection of nodes displayed in the editor canvas.</summary>
    public ObservableCollection<NodeViewModel> Nodes { get; } = [];

    /// <summary>The collection of connections between node connectors.</summary>
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    /// <summary>The currently selected node, shown in the properties panel.</summary>
    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    /// <summary>The node types available in the toolbox.</summary>
    public IReadOnlyList<string> ToolboxItems { get; } =
    [
        "Number Producer",
        "Number Reporter",
        "Arithmetic Transform",
        "Random Number Generator",
        "Pass Filter",
    ];

    /// <summary>Adds a new node of the given type to the canvas at a default staggered position.</summary>
    [RelayCommand]
    private void AddNode(string? nodeType)
    {
        if (nodeType is null)
        {
            return;
        }

        // Stagger new nodes so they do not overlap
        var offset = new Point(60 + (Nodes.Count * 30 % 300), 60 + (Nodes.Count * 20 % 200));
        AddNodeAt(nodeType, offset);
    }

    /// <summary>Adds a new node of the given type to the canvas at the specified canvas position.</summary>
    /// <param name="nodeType">The display name of the node type to create (must match a <see cref="ToolboxItems"/> entry).</param>
    /// <param name="canvasPosition">The position in canvas coordinates where the node will be placed.</param>
    public void AddNodeAt(string nodeType, Point canvasPosition)
    {
        NodeViewModel node = nodeType switch
        {
            "Number Producer" => new NumberProducerNode { Location = canvasPosition },
            "Number Reporter" => new NumberReporterNode { Location = canvasPosition },
            "Arithmetic Transform" => new ArithmeticTransformNode { Location = canvasPosition },
            "Random Number Generator" => new RandomNumberGeneratorNode { Location = canvasPosition },
            "Pass Filter" => new PassFilterNode { Location = canvasPosition },
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null),
        };

        // Track selection changes so the properties panel stays in sync.
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.IsSelected))
            {
                if (node.IsSelected)
                {
                    SelectedNode = node;
                }
                else if (SelectedNode == node)
                {
                    SelectedNode = null;
                }
            }
        };

        Nodes.Add(node);
        SelectedNode = node;
    }

    /// <summary>Called by the editor when the user finishes dragging a connection to a target connector.
    /// <para>The editor passes a <c>(source, target)</c> tuple as the argument.</para></summary>
    [RelayCommand]
    private void ConnectionCompleted(object? args)
    {
        if (args is not (ConnectorViewModel source, object targetObj) || targetObj is not ConnectorViewModel target)
        {
            return;
        }

        if (source != target)
        {
            Connections.Add(new ConnectionViewModel(source, target));
            source.IsConnected = true;
            target.IsConnected = true;
        }
    }

    /// <summary>Removes a connection from the graph.</summary>
    [RelayCommand]
    private void RemoveConnection(object? connection)
    {
        if (connection is not ConnectionViewModel conn)
        {
            return;
        }

        Connections.Remove(conn);

        // Re-evaluate IsConnected for both endpoints
        UpdateIsConnected(conn.Source);
        UpdateIsConnected(conn.Target);
    }

    /// <summary>Quits the application.</summary>
    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void UpdateIsConnected(ConnectorViewModel connector)
    {
        connector.IsConnected = Connections.Any(
            c => c.Source == connector || c.Target == connector);
    }
}

