using Avalonia;
using AvaloniaNodeEditor.Models;
using AvaloniaNodeEditor.ViewModels;

namespace AvaloniaNodeEditor.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.Nodes);
        Assert.NotNull(viewModel.Connections);
    }

    [Fact]
    public void Constructor_NodesAndConnectionsAreEmpty()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Empty(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void Constructor_SelectedNodeIsNull()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void ToolboxItems_ContainsAllFiveNodeTypes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.ToolboxItems);
        Assert.Equal(5, viewModel.ToolboxItems.Count);
    }

    [Fact]
    public void ToolboxItems_TitlesMatchExpectedNodes()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Contains("Number Producer", viewModel.ToolboxItems);
        Assert.Contains("Number Reporter", viewModel.ToolboxItems);
        Assert.Contains("Arithmetic Transform", viewModel.ToolboxItems);
        Assert.Contains("Random Number Generator", viewModel.ToolboxItems);
        Assert.Contains("Pass Filter", viewModel.ToolboxItems);
    }

    [Fact]
    public void AddNode_AddsNodeToCollection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeCommand.Execute("Number Producer");

        // Assert
        Assert.Single(viewModel.Nodes);
    }

    [Fact]
    public void AddNode_SetsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeCommand.Execute("Number Producer");

        // Assert
        Assert.NotNull(viewModel.SelectedNode);
    }

    [Fact]
    public void AddNode_AddsAllNodeTypes()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        foreach (var item in viewModel.ToolboxItems)
        {
            viewModel.AddNodeCommand.Execute(item);
        }

        // Assert
        Assert.Equal(5, viewModel.Nodes.Count);
    }

    [Fact]
    public void AddNodeAt_PlacesNodeAtSpecifiedPosition()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var expectedPosition = new Point(123, 456);

        // Act
        viewModel.AddNodeAt("Number Producer", expectedPosition);

        // Assert
        Assert.Single(viewModel.Nodes);
        Assert.Equal(expectedPosition, viewModel.Nodes[0].Location);
    }

    [Theory]
    [InlineData("Number Producer")]
    [InlineData("Number Reporter")]
    [InlineData("Arithmetic Transform")]
    [InlineData("Random Number Generator")]
    [InlineData("Pass Filter")]
    public void AddNodeAt_CreatesCorrectNodeType(string nodeType)
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var position = new Point(10, 20);

        // Act
        viewModel.AddNodeAt(nodeType, position);

        // Assert
        Assert.Single(viewModel.Nodes);
        Assert.Equal(nodeType, viewModel.Nodes[0].Name);
    }

    [Fact]
    public void AddNodeAt_SetsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeAt("Pass Filter", new Point(0, 0));

        // Assert
        Assert.NotNull(viewModel.SelectedNode);
        Assert.Equal("Pass Filter", viewModel.SelectedNode.Name);
    }

    [Fact]
    public void NodeViewModel_Location_IsMutable()
    {
        // Arrange — NodifyEditor.NodeDragging sets node.Location directly on the control;
        // the TwoWay binding propagates that back to the ViewModel.Location.
        // This test verifies the ViewModel's Location setter is observable and writable.
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        var node = viewModel.Nodes[0];
        bool propertyChangedFired = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.Location))
                propertyChangedFired = true;
        };

        // Act — simulate what the TwoWay binding does when NodifyEditor moves a node
        node.Location = new Point(99, 88);

        // Assert
        Assert.Equal(new Point(99, 88), node.Location);
        Assert.True(propertyChangedFired);
    }

    [Fact]
    public void NodeViewModel_IsSelected_IsMutable()
    {
        // Arrange — NodifyEditor.SelectItem sets node.IsSelected directly on the control;
        // the TwoWay binding propagates that back to the ViewModel.IsSelected.
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Reporter", new Point(0, 0));
        var node = viewModel.Nodes[0];
        bool propertyChangedFired = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.IsSelected))
                propertyChangedFired = true;
        };

        // Act — simulate what the TwoWay binding does when the node is selected
        node.IsSelected = !node.IsSelected;

        // Assert
        Assert.True(propertyChangedFired);
    }

    [Fact]
    public void ConnectionCompleted_WithSourceTargetTuple_CreatesConnection()
    {
        // Arrange — NodifyEditor fires ConnectionCompletedCommand with (source, target) tuple
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        // Assert
        Assert.Single(viewModel.Connections);
        Assert.Same(source, viewModel.Connections[0].Source);
        Assert.Same(target, viewModel.Connections[0].Target);
    }

    [Fact]
    public void ConnectionCompleted_WithSourceTargetTuple_SetsIsConnected()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        // Assert
        Assert.True(source.IsConnected);
        Assert.True(target.IsConnected);
    }

    [Fact]
    public void ConnectionCompleted_SameSourceAndTarget_DoesNotCreateConnection()
    {
        // Arrange — self-loops should be rejected
        var viewModel = new MainWindowViewModel();
        var connector = new ConnectorViewModel { Name = "out" };

        // Act
        viewModel.ConnectionCompletedCommand.Execute((connector, connector));

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void ConnectionCompleted_NullArg_DoesNotThrowOrCreateConnection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.ConnectionCompletedCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void RemoveConnection_RemovesConnectionAndClearsIsConnected()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var source = new ConnectorViewModel { Name = "out" };
        var target = new ConnectorViewModel { Name = "in" };
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Single(viewModel.Connections);

        // Act
        viewModel.RemoveConnectionCommand.Execute(viewModel.Connections[0]);

        // Assert
        Assert.Empty(viewModel.Connections);
        Assert.False(source.IsConnected);
        Assert.False(target.IsConnected);
    }

    // ── NewGraph ────────────────────────────────────────────────────────────

    [Fact]
    public void NewGraph_ClearsNodesConnectionsAndSelection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Equal(2, viewModel.Nodes.Count);
        Assert.Single(viewModel.Connections);
        Assert.NotNull(viewModel.SelectedNode);

        // Act
        viewModel.NewGraphCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void NewGraph_OnEmptyGraph_DoesNotThrow()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act & Assert – should not throw
        viewModel.NewGraphCommand.Execute(null);
        Assert.Empty(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
    }

    // ── Serialize / Deserialize ─────────────────────────────────────────────

    [Fact]
    public void SerializeGraph_EmptyGraph_ReturnsValidJson()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        var json = viewModel.SerializeGraph();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("nodes", json);
        Assert.Contains("connections", json);
    }

    [Fact]
    public void SerializeGraph_WithNodes_IncludesNodeData()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(100, 200));

        // Act
        var json = viewModel.SerializeGraph();

        // Assert
        Assert.Contains("Number Producer", json);
        Assert.Contains("100", json);
        Assert.Contains("200", json);
    }

    [Fact]
    public void DeserializeGraph_RestoresNodes()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(100, 200));
        viewModel.AddNodeAt("Number Reporter", new Point(300, 400));
        var json = viewModel.SerializeGraph();

        // Act – deserialize into a fresh view model
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Equal(2, viewModel2.Nodes.Count);
        Assert.Equal("Number Producer", viewModel2.Nodes[0].Name);
        Assert.Equal(new Point(100, 200), viewModel2.Nodes[0].Location);
        Assert.Equal("Number Reporter", viewModel2.Nodes[1].Name);
        Assert.Equal(new Point(300, 400), viewModel2.Nodes[1].Location);
    }

    [Fact]
    public void DeserializeGraph_RestoresConnections()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Single(viewModel2.Connections);
        Assert.True(viewModel2.Nodes[0].Outputs[0].IsConnected);
        Assert.True(viewModel2.Nodes[1].Inputs[0].IsConnected);
    }

    [Fact]
    public void DeserializeGraph_ClearsExistingState()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        Assert.Equal(2, viewModel.Nodes.Count);

        // Deserialize an empty graph
        var emptyJson = new MainWindowViewModel().SerializeGraph();

        // Act
        viewModel.DeserializeGraph(emptyJson);

        // Assert
        Assert.Empty(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void SerializeDeserialize_NumberProducerNode_PreservesValue()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        ((NumberProducerNode)viewModel.Nodes[0]).Value = 42.5;
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        var restored = Assert.IsType<NumberProducerNode>(viewModel2.Nodes[0]);
        Assert.Equal(42.5, restored.Value);
    }

    [Fact]
    public void SerializeDeserialize_ArithmeticTransformNode_PreservesProperties()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Arithmetic Transform", new Point(0, 0));
        var node = (ArithmeticTransformNode)viewModel.Nodes[0];
        node.InputA = 10;
        node.InputB = 5;
        node.Operation = ArithmeticOperation.Multiply;
        node.Compute();
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        var restored = Assert.IsType<ArithmeticTransformNode>(viewModel2.Nodes[0]);
        Assert.Equal(10, restored.InputA);
        Assert.Equal(5, restored.InputB);
        Assert.Equal(ArithmeticOperation.Multiply, restored.Operation);
        Assert.Equal(50, restored.Result);
    }

    [Fact]
    public void SerializeDeserialize_RandomNumberGeneratorNode_PreservesProperties()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Random Number Generator", new Point(0, 0));
        var node = (RandomNumberGeneratorNode)viewModel.Nodes[0];
        node.MinValue = 5;
        node.MaxValue = 10;
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        var restored = Assert.IsType<RandomNumberGeneratorNode>(viewModel2.Nodes[0]);
        Assert.Equal(5, restored.MinValue);
        Assert.Equal(10, restored.MaxValue);
    }

    [Fact]
    public void SerializeDeserialize_PassFilterNode_PreservesProperties()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Pass Filter", new Point(0, 0));
        var node = (PassFilterNode)viewModel.Nodes[0];
        node.FilterType = FilterType.MidPass;
        node.Threshold = 1.5;
        node.UpperThreshold = 8.5;
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        var restored = Assert.IsType<PassFilterNode>(viewModel2.Nodes[0]);
        Assert.Equal(FilterType.MidPass, restored.FilterType);
        Assert.Equal(1.5, restored.Threshold);
        Assert.Equal(8.5, restored.UpperThreshold);
    }

    [Theory]
    [InlineData("Number Producer")]
    [InlineData("Number Reporter")]
    [InlineData("Arithmetic Transform")]
    [InlineData("Random Number Generator")]
    [InlineData("Pass Filter")]
    public void SerializeDeserialize_AllNodeTypes_RoundTrip(string nodeType)
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt(nodeType, new Point(50, 75));
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Single(viewModel2.Nodes);
        Assert.Equal(nodeType, viewModel2.Nodes[0].Name);
        Assert.Equal(new Point(50, 75), viewModel2.Nodes[0].Location);
    }
}
