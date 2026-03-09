using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaNodeEditor.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodifyM.Avalonia.Events;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ConnectorViewModel? _pendingSource;

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

    /// <summary>Adds a new node of the given type to the canvas at a default position.</summary>
    [RelayCommand]
    private void AddNode(string? nodeType)
    {
        if (nodeType is null)
        {
            return;
        }

        // Stagger new nodes so they do not overlap
        var offset = new Point(60 + (Nodes.Count * 30 % 300), 60 + (Nodes.Count * 20 % 200));

        NodeViewModel node = nodeType switch
        {
            "Number Producer" => new NumberProducerNode { Location = offset },
            "Number Reporter" => new NumberReporterNode { Location = offset },
            "Arithmetic Transform" => new ArithmeticTransformNode { Location = offset },
            "Random Number Generator" => new RandomNumberGeneratorNode { Location = offset },
            "Pass Filter" => new PassFilterNode { Location = offset },
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

    /// <summary>Called by the editor when the user starts dragging a new connection from a connector.</summary>
    [RelayCommand]
    private void ConnectionStarted(object? source)
    {
        _pendingSource = source as ConnectorViewModel;
    }

    /// <summary>Called by the editor when the user finishes dragging a connection to a target connector.</summary>
    [RelayCommand]
    private void ConnectionCompleted(object? args)
    {
        ConnectorViewModel? target = args switch
        {
            ConnectorViewModel cv => cv,
            PendingConnectionEventArgs ea => ea.TargetConnector as ConnectorViewModel,
            _ => null,
        };

        if (_pendingSource is not null && target is not null && _pendingSource != target)
        {
            Connections.Add(new ConnectionViewModel(_pendingSource, target));
            _pendingSource.IsConnected = true;
            target.IsConnected = true;
        }

        _pendingSource = null;
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

