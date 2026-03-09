using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaNodeEditor.Models;
using CommunityToolkit.Mvvm.Input;
using NodeEditor.Model;
using NodeEditor.Mvvm;

namespace AvaloniaNodeEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>The node graph editor state including drawing canvas and node templates.</summary>
    public EditorViewModel Editor { get; }

    /// <summary>The active drawing node graph shown in the editor canvas.</summary>
    public IDrawingNode? Drawing => Editor.Drawing;

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel()
    {
        var drawing = new DrawingNodeViewModel
        {
            Name = "Main",
            X = 0,
            Y = 0,
            Width = 900,
            Height = 600,
            Nodes = new ObservableCollection<INode>(),
            Connectors = new ObservableCollection<IConnector>(),
            Settings = new DrawingNodeSettingsViewModel
            {
                EnableSnap = true,
                SnapX = 10,
                SnapY = 10,
                EnableGrid = true,
                GridCellWidth = 20,
                GridCellHeight = 20,
                EnableConnections = true,
                AllowDuplicateConnections = true,
            },
        };

        Editor = new EditorViewModel
        {
            Drawing = drawing,
            Templates = CreateTemplates(),
        };
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

    private static IList<INodeTemplate> CreateTemplates()
    {
        return new ObservableCollection<INodeTemplate>
        {
            new NodeTemplateViewModel
            {
                Title = "Number Producer",
                Template = new NumberProducerNode(),
                Preview = new NumberProducerNode(),
            },
            new NodeTemplateViewModel
            {
                Title = "Number Reporter",
                Template = new NumberReporterNode(),
                Preview = new NumberReporterNode(),
            },
            new NodeTemplateViewModel
            {
                Title = "Arithmetic Transform",
                Template = new ArithmeticTransformNode(),
                Preview = new ArithmeticTransformNode(),
            },
            new NodeTemplateViewModel
            {
                Title = "Random Number Generator",
                Template = new RandomNumberGeneratorNode(),
                Preview = new RandomNumberGeneratorNode(),
            },
            new NodeTemplateViewModel
            {
                Title = "Pass Filter",
                Template = new PassFilterNode(),
                Preview = new PassFilterNode(),
            },
        };
    }
}
