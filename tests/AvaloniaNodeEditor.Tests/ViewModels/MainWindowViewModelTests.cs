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
        Assert.Equal(nodeType, viewModel.Nodes[0].NodeType);
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
        Assert.Equal("Pass Filter", viewModel.SelectedNode.NodeType);
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
    public void NodeViewModel_Location_ClampsNegativeXToZero()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(50, 50));
        var node = viewModel.Nodes[0];

        // Act — NodifyEditor can move a node to a negative x position when panning
        node.Location = new Point(-30, 50);

        // Assert — x must be clamped to 0; y is unchanged
        Assert.Equal(new Point(0, 50), node.Location);
    }

    [Fact]
    public void NodeViewModel_Location_ClampsNegativeYToZero()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Reporter", new Point(50, 50));
        var node = viewModel.Nodes[0];

        // Act
        node.Location = new Point(50, -20);

        // Assert — y must be clamped to 0; x is unchanged
        Assert.Equal(new Point(50, 0), node.Location);
    }

    [Fact]
    public void NodeViewModel_Location_ClampsNegativeBothToZero()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Arithmetic Transform", new Point(50, 50));
        var node = viewModel.Nodes[0];

        // Act
        node.Location = new Point(-100, -200);

        // Assert
        Assert.Equal(new Point(0, 0), node.Location);
    }

    [Fact]
    public void AddNodeAt_WithNegativePosition_ClampsToOrigin()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act — simulate a drop at negative canvas coordinates
        viewModel.AddNodeAt("Pass Filter", new Point(-50, -80));

        // Assert — the node must be placed at the origin, not off-canvas
        Assert.Equal(new Point(0, 0), viewModel.Nodes[0].Location);
    }

    [Fact]
    public void AddNodeAt_WithPartiallyNegativePosition_ClampsOnlyNegativeAxis()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeAt("Number Producer", new Point(-10, 120));

        // Assert — only x is clamped; y is preserved
        Assert.Equal(new Point(0, 120), viewModel.Nodes[0].Location);
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
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];

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
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];

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
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        var connector = viewModel.Nodes[0].Outputs[0];

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
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Single(viewModel.Connections);

        // Act
        viewModel.RemoveConnectionCommand.Execute(viewModel.Connections[0]);

        // Assert
        Assert.Empty(viewModel.Connections);
        Assert.False(source.IsConnected);
        Assert.False(target.IsConnected);
    }

    [Fact]
    public void ConnectionCompleted_InputToOutput_SwapsAndCreatesConnection()
    {
        // Arrange — user drags from an input to an output; endpoints should be swapped
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var output = viewModel.Nodes[0].Outputs[0];
        var input = viewModel.Nodes[1].Inputs[0];

        // Act — pass input as source, output as target (backwards)
        viewModel.ConnectionCompletedCommand.Execute((input, output));

        // Assert — connection is created with output as Source, input as Target
        Assert.Single(viewModel.Connections);
        Assert.Same(output, viewModel.Connections[0].Source);
        Assert.Same(input, viewModel.Connections[0].Target);
    }

    [Fact]
    public void ConnectionCompleted_OutputToOutput_DoesNotCreateConnection()
    {
        // Arrange — connecting two outputs should be rejected
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Random Number Generator", new Point(100, 200));
        var output1 = viewModel.Nodes[0].Outputs[0];
        var output2 = viewModel.Nodes[1].Outputs[0];

        // Act
        viewModel.ConnectionCompletedCommand.Execute((output1, output2));

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void ConnectionCompleted_InputToInput_DoesNotCreateConnection()
    {
        // Arrange — connecting two inputs should be rejected
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Reporter", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var input1 = viewModel.Nodes[0].Inputs[0];
        var input2 = viewModel.Nodes[1].Inputs[0];

        // Act
        viewModel.ConnectionCompletedCommand.Execute((input1, input2));

        // Assert
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void DisconnectConnector_RemovesAllConnectionsOnConnector()
    {
        // Arrange — one output connected to two inputs
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        viewModel.AddNodeAt("Arithmetic Transform", new Point(200, 200));
        var output = viewModel.Nodes[0].Outputs[0];
        var input1 = viewModel.Nodes[1].Inputs[0];
        var input2 = viewModel.Nodes[2].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((output, input1));
        viewModel.ConnectionCompletedCommand.Execute((output, input2));
        Assert.Equal(2, viewModel.Connections.Count);

        // Act — disconnect the output connector
        viewModel.DisconnectConnectorCommand.Execute(output);

        // Assert — all connections removed and IsConnected cleared
        Assert.Empty(viewModel.Connections);
        Assert.False(output.IsConnected);
        Assert.False(input1.IsConnected);
        Assert.False(input2.IsConnected);
    }

    [Fact]
    public void DisconnectConnector_NullArg_DoesNotThrow()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act & Assert — should not throw
        viewModel.DisconnectConnectorCommand.Execute(null);
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void ConnectionCompleted_InputAlreadyConnected_RejectsSecondConnection()
    {
        // Arrange — one input can only accept a single connection
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Producer", new Point(100, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(200, 200));
        var output1 = viewModel.Nodes[0].Outputs[0];
        var output2 = viewModel.Nodes[1].Outputs[0];
        var input = viewModel.Nodes[2].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((output1, input));
        Assert.Single(viewModel.Connections);

        // Act — try to connect a second output to the same input
        viewModel.ConnectionCompletedCommand.Execute((output2, input));

        // Assert — second connection is rejected; only the first remains
        Assert.Single(viewModel.Connections);
        Assert.Same(output1, viewModel.Connections[0].Source);
    }

    [Fact]
    public void ConnectionCompleted_OutputToMultipleInputs_AllowsMultipleConnections()
    {
        // Arrange — one output can feed many inputs
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        viewModel.AddNodeAt("Arithmetic Transform", new Point(200, 200));
        var output = viewModel.Nodes[0].Outputs[0];
        var input1 = viewModel.Nodes[1].Inputs[0];
        var input2 = viewModel.Nodes[2].Inputs[0];

        // Act
        viewModel.ConnectionCompletedCommand.Execute((output, input1));
        viewModel.ConnectionCompletedCommand.Execute((output, input2));

        // Assert — both connections succeed
        Assert.Equal(2, viewModel.Connections.Count);
    }

    // ── NewGraph ────────────────────────────────────────────────────────────

    [Fact]
    public void NewGraph_ClearsNodesConnectionsAndSelection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.GraphName = "Test Graph";
        viewModel.GraphVersion = "1.0";
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
        Assert.Equal(string.Empty, viewModel.GraphName);
        Assert.Equal(string.Empty, viewModel.GraphVersion);
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
        Assert.Contains("layout", json);
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
        // Layout is stored separately from node content
        Assert.Contains("layout", json);
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
        Assert.Equal("Number Producer", viewModel2.Nodes[0].NodeType);
        Assert.Equal("Number Producer 1", viewModel2.Nodes[0].Name);
        Assert.Equal(new Point(100, 200), viewModel2.Nodes[0].Location);
        Assert.Equal("Number Reporter", viewModel2.Nodes[1].NodeType);
        Assert.Equal("Number Reporter 1", viewModel2.Nodes[1].Name);
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
        Assert.Equal(nodeType, viewModel2.Nodes[0].NodeType);
        Assert.Equal($"{nodeType} 1", viewModel2.Nodes[0].Name);
        Assert.Equal(new Point(50, 75), viewModel2.Nodes[0].Location);
    }

    // ── Unique node names ───────────────────────────────────────────────────

    [Fact]
    public void AddNodeAt_GeneratesUniqueDefaultName()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        viewModel.AddNodeAt("Number Producer", new Point(10, 10));

        // Assert
        Assert.Equal("Number Producer 1", viewModel.Nodes[0].Name);
        Assert.Equal("Number Producer 2", viewModel.Nodes[1].Name);
    }

    [Fact]
    public void AddNodeAt_SkipsExistingIndexes()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        viewModel.AddNodeAt("Number Producer", new Point(10, 10));

        // Rename "Number Producer 2" to "Number Producer 1" to create a gap
        viewModel.Nodes[0].Name = "Number Producer 99";

        // Act — adding another should skip 99 and use 1
        viewModel.AddNodeAt("Number Producer", new Point(20, 20));

        // Assert
        Assert.Equal("Number Producer 1", viewModel.Nodes[2].Name);
    }

    [Fact]
    public void ValidateAllNodeNames_FlagsDuplicates()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        viewModel.AddNodeAt("Number Producer", new Point(10, 10));

        // Act — set both nodes to the same name
        viewModel.Nodes[1].Name = "Number Producer 1";

        // Assert — both should have name errors
        Assert.True(viewModel.Nodes[0].HasNameError);
        Assert.True(viewModel.Nodes[1].HasNameError);
    }

    [Fact]
    public void ValidateAllNodeNames_ClearsErrorOnUniqueName()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        viewModel.AddNodeAt("Number Producer", new Point(10, 10));

        // Force a duplicate
        viewModel.Nodes[1].Name = "Number Producer 1";
        Assert.True(viewModel.Nodes[0].HasNameError);

        // Act — fix the duplicate
        viewModel.Nodes[1].Name = "Number Producer 2";

        // Assert
        Assert.False(viewModel.Nodes[0].HasNameError);
        Assert.False(viewModel.Nodes[1].HasNameError);
    }

    // ── Graph name and version ──────────────────────────────────────────────

    [Fact]
    public void GraphNameAndVersion_DefaultsToEmpty()
    {
        var viewModel = new MainWindowViewModel();
        Assert.Equal(string.Empty, viewModel.GraphName);
        Assert.Equal(string.Empty, viewModel.GraphVersion);
    }

    [Fact]
    public void SerializeDeserialize_PreservesGraphNameAndVersion()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.GraphName = "My System";
        viewModel.GraphVersion = "2.1.0";
        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Equal("My System", viewModel2.GraphName);
        Assert.Equal("2.1.0", viewModel2.GraphVersion);
    }

    [Fact]
    public void SerializeGraph_EmptyNameAndVersion_OmitsFromJson()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        var json = viewModel.SerializeGraph();

        // Assert — empty name/version should not appear in JSON
        Assert.DoesNotContain("\"name\"", json);
        Assert.DoesNotContain("\"version\"", json);
    }

    // ── Layout separation ───────────────────────────────────────────────────

    [Fact]
    public void SerializeGraph_StoresLayoutSeparately()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(150, 250));
        var nodeName = viewModel.Nodes[0].Name;

        // Act
        var json = viewModel.SerializeGraph();

        // Assert — layout section contains the node's position keyed by name
        Assert.Contains("\"layout\"", json);
        Assert.Contains(nodeName, json);
    }

    // ── Name-based connections ──────────────────────────────────────────────

    [Fact]
    public void SerializeGraph_ConnectionsUseNameBasedFormat()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        // Act
        var json = viewModel.SerializeGraph();

        // Assert — connections use "from" and "to" with "NodeName.ConnectorName" format
        Assert.Contains("\"from\"", json);
        Assert.Contains("\"to\"", json);
        Assert.Contains("Number Producer 1.Output", json);
        Assert.Contains("Number Reporter 1.Input", json);
    }

    [Fact]
    public void DeserializeGraph_RestoresNameBasedConnections()
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

    // ── IsPropertiesPanelVisible ─────────────────────────────────────────────

    [Fact]
    public void IsPropertiesPanelVisible_DefaultsToFalse()
    {
        var viewModel = new MainWindowViewModel();
        Assert.False(viewModel.IsPropertiesPanelVisible);
    }

    [Fact]
    public void IsPropertiesPanelVisible_CanBeSetToTrue()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.IsPropertiesPanelVisible = true;
        Assert.True(viewModel.IsPropertiesPanelVisible);
    }

    // ── CopyNode ─────────────────────────────────────────────────────────────

    [Fact]
    public void CopyNodeCommand_CannotExecute_WhenNoNodeSelected()
    {
        var viewModel = new MainWindowViewModel();
        Assert.False(viewModel.CopyNodeCommand.CanExecute(null));
    }

    [Fact]
    public void CopyNodeCommand_CanExecute_WhenNodeSelected()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        Assert.True(viewModel.CopyNodeCommand.CanExecute(null));
    }

    // ── PasteNode ────────────────────────────────────────────────────────────

    [Fact]
    public void PasteNodeCommand_CannotExecute_BeforeCopy()
    {
        var viewModel = new MainWindowViewModel();
        Assert.False(viewModel.PasteNodeCommand.CanExecute(null));
    }

    [Fact]
    public void PasteNode_AfterCopy_CreatesNewNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.CopyNodeCommand.Execute(null);

        // Act
        viewModel.PasteNodeCommand.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.Nodes.Count);
    }

    [Fact]
    public void PasteNode_CreatesNodeOfSameType()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Arithmetic Transform", new Point(10, 20));
        viewModel.CopyNodeCommand.Execute(null);

        // Act
        viewModel.PasteNodeCommand.Execute(null);

        // Assert
        Assert.Equal("Arithmetic Transform", viewModel.Nodes[1].NodeType);
    }

    [Fact]
    public void PasteNode_PlacesNodeAtOffset()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(100, 200));
        viewModel.CopyNodeCommand.Execute(null);

        // Act
        viewModel.PasteNodeCommand.Execute(null);

        // Assert — pasted node is placed 30px below-right of original
        Assert.Equal(new Point(130, 230), viewModel.Nodes[1].Location);
    }

    [Fact]
    public void PasteNode_GeneratesUniqueName()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        viewModel.CopyNodeCommand.Execute(null);

        // Act
        viewModel.PasteNodeCommand.Execute(null);

        // Assert
        Assert.NotEqual(viewModel.Nodes[0].Name, viewModel.Nodes[1].Name);
    }

    [Fact]
    public void PasteNode_CopiesNumberProducerValue()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        ((NumberProducerNode)viewModel.Nodes[0]).Value = 42.5;
        viewModel.CopyNodeCommand.Execute(null);

        // Act
        viewModel.PasteNodeCommand.Execute(null);

        // Assert
        var pasted = Assert.IsType<NumberProducerNode>(viewModel.Nodes[1]);
        Assert.Equal(42.5, pasted.Value);
    }

    [Fact]
    public void PasteNodeCommand_CanExecute_AfterCopy()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));

        // Act
        viewModel.CopyNodeCommand.Execute(null);

        // Assert
        Assert.True(viewModel.PasteNodeCommand.CanExecute(null));
    }

    // ── DeleteNode ───────────────────────────────────────────────────────────

    [Fact]
    public void DeleteNodeCommand_CannotExecute_WhenNoNodeSelected()
    {
        var viewModel = new MainWindowViewModel();
        Assert.False(viewModel.DeleteNodeCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteNode_RemovesSelectedNodeFromCanvas()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        Assert.Single(viewModel.Nodes);

        // Act
        viewModel.DeleteNodeCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.Nodes);
    }

    [Fact]
    public void DeleteNode_ClearsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        Assert.NotNull(viewModel.SelectedNode);

        // Act
        viewModel.DeleteNodeCommand.Execute(null);

        // Assert
        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void DeleteNode_RemovesAttachedConnections()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Single(viewModel.Connections);

        // Select the producer and delete it
        viewModel.Nodes[0].IsSelected = true;

        // Act
        viewModel.DeleteNodeCommand.Execute(null);

        // Assert
        Assert.Single(viewModel.Nodes);
        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void DeleteNode_UpdatesIsConnectedOnRemainingConnectors()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var reporter = viewModel.Nodes[1];
        var target = reporter.Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.True(target.IsConnected);

        // Select producer and delete
        viewModel.Nodes[0].IsSelected = true;
        viewModel.DeleteNodeCommand.Execute(null);

        // Assert — reporter's input connector is no longer connected
        Assert.False(target.IsConnected);
    }

    // ── Tabs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_CreatesDefaultTab()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert — one tab named "Main" is created and set as active
        Assert.Single(viewModel.Tabs);
        Assert.Equal("Main", viewModel.Tabs[0].Name);
        Assert.NotNull(viewModel.ActiveTab);
        Assert.Same(viewModel.Tabs[0], viewModel.ActiveTab);
    }

    [Fact]
    public void AddTab_AddsNewTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        Assert.Equal(2, viewModel.Tabs.Count);
    }

    [Fact]
    public void AddTab_SwitchesToNewTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        Assert.Same(viewModel.Tabs[1], viewModel.ActiveTab);
    }

    [Fact]
    public void AddTab_NewTabEntersRenameMode()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        Assert.True(viewModel.Tabs[1].IsEditing);
    }

    [Fact]
    public void AddTabAfter_NewTabEntersRenameMode()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabAfterCommand.Execute(viewModel.Tabs[0]);
        Assert.True(viewModel.Tabs[1].IsEditing);
    }

    [Fact]
    public void AddTab_GeneratesUniqueName()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        Assert.Equal("Tab 1", viewModel.Tabs[1].Name);
    }

    [Fact]
    public void AddTabAfter_InsertsAfterTargetTab()
    {
        // Arrange — two tabs: Main (index 0) and Tab 1 (index 1)
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null); // Creates Tab 1
        var mainTab = viewModel.Tabs[0];
        var tab1 = viewModel.Tabs[1];

        // Act — insert a new tab after Main
        viewModel.AddTabAfterCommand.Execute(mainTab);

        // Assert — Main, new Tab 2, Tab 1
        Assert.Equal(3, viewModel.Tabs.Count);
        Assert.Same(mainTab, viewModel.Tabs[0]);
        Assert.Equal("Tab 2", viewModel.Tabs[1].Name); // New tab is "Tab 2" (Tab 1 is taken)
        Assert.Same(tab1, viewModel.Tabs[2]);
    }

    [Fact]
    public void DeleteTab_RemovesTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        var tab2 = viewModel.Tabs[1];

        viewModel.DeleteTabCommand.Execute(tab2);

        Assert.Single(viewModel.Tabs);
        Assert.DoesNotContain(tab2, viewModel.Tabs);
    }

    [Fact]
    public void DeleteTab_LastTab_DoesNotRemove()
    {
        // The last remaining tab must never be deleted.
        var viewModel = new MainWindowViewModel();
        Assert.Single(viewModel.Tabs);

        viewModel.DeleteTabCommand.Execute(viewModel.Tabs[0]);

        Assert.Single(viewModel.Tabs);
    }

    [Fact]
    public void DeleteTab_ActiveTab_SwitchesToAdjacentTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null); // Tab 2
        viewModel.AddTabCommand.Execute(null); // Tab 3
        var tab2 = viewModel.Tabs[1]; // currently active after last AddTab

        // Make Tab 2 active then delete it
        viewModel.ActiveTab = tab2;
        viewModel.DeleteTabCommand.Execute(tab2);

        // Should have switched to the adjacent (now index 1) tab
        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.NotSame(tab2, viewModel.ActiveTab);
    }

    [Fact]
    public void Tabs_NodesAreIsolated()
    {
        // Nodes added to one tab must not appear in another.
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));

        viewModel.AddTabCommand.Execute(null); // Switch to new empty tab

        Assert.Empty(viewModel.Nodes);
        Assert.Single(viewModel.Tabs[0].Nodes);
    }

    [Fact]
    public void SwitchingTabs_UpdatesNodes()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddTabCommand.Execute(null); // Tab 2 — empty

        Assert.Empty(viewModel.Nodes);

        viewModel.ActiveTab = viewModel.Tabs[0]; // Back to Tab 1

        Assert.Single(viewModel.Nodes);
    }

    [Fact]
    public void SwitchingTabs_ClearsSelectedNode()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        Assert.NotNull(viewModel.SelectedNode);

        viewModel.AddTabCommand.Execute(null); // Switch to new tab

        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void SerializeDeserialize_MultipleTabs_PreservesAllTabs()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20)); // Tab 1
        viewModel.AddTabCommand.Execute(null); // Tab 2
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200)); // Tab 2

        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Equal(2, viewModel2.Tabs.Count);
        Assert.Single(viewModel2.Tabs[0].Nodes);
        Assert.Equal("Number Producer", viewModel2.Tabs[0].Nodes[0].NodeType);
        Assert.Single(viewModel2.Tabs[1].Nodes);
        Assert.Equal("Number Reporter", viewModel2.Tabs[1].Nodes[0].NodeType);
    }

    [Fact]
    public void SerializeDeserialize_MultipleTabs_PreservesTabNames()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.Tabs[0].Name = "My First Tab";
        viewModel.AddTabCommand.Execute(null);
        viewModel.Tabs[1].Name = "My Second Tab";

        var json = viewModel.SerializeGraph();

        // Act
        var viewModel2 = new MainWindowViewModel();
        viewModel2.DeserializeGraph(json);

        // Assert
        Assert.Equal("My First Tab", viewModel2.Tabs[0].Name);
        Assert.Equal("My Second Tab", viewModel2.Tabs[1].Name);
    }

    [Fact]
    public void DeserializeGraph_LegacyFormat_LoadsIntoSingleTab()
    {
        // A JSON file produced before the tabs feature should load into a single tab.
        const string legacyJson = """
            {
              "nodes": [
                { "name": "Number Producer 1", "type": "Number Producer" }
              ],
              "layout": {
                "Number Producer 1": { "x": 50, "y": 75 }
              },
              "connections": []
            }
            """;

        var viewModel = new MainWindowViewModel();
        viewModel.DeserializeGraph(legacyJson);

        Assert.Single(viewModel.Tabs);
        Assert.Single(viewModel.Nodes);
        Assert.Equal("Number Producer", viewModel.Nodes[0].NodeType);
        Assert.Equal(new Point(50, 75), viewModel.Nodes[0].Location);
    }

    [Fact]
    public void NewGraph_ResetsToSingleTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        viewModel.AddTabCommand.Execute(null);
        Assert.Equal(3, viewModel.Tabs.Count);

        viewModel.NewGraphCommand.Execute(null);

        Assert.Single(viewModel.Tabs);
        Assert.Equal("Main", viewModel.Tabs[0].Name);
    }

    // ── MoveNodeToTab ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveNodeToTabCommand_CannotExecute_WhenNoNodeSelected()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];

        Assert.False(viewModel.MoveNodeToTabCommand.CanExecute(targetTab));
    }

    [Fact]
    public void MoveNodeToTabCommand_CannotExecute_WhenTargetIsSameTab()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));

        Assert.False(viewModel.MoveNodeToTabCommand.CanExecute(viewModel.ActiveTab));
    }

    [Fact]
    public void MoveNodeToTab_RemovesNodeFromSourceTab()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        var node = viewModel.Nodes[0];
        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = viewModel.Tabs[0];
        viewModel.SelectedNode = node;

        // Act
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Assert
        Assert.Empty(viewModel.Tabs[0].Nodes);
    }

    [Fact]
    public void MoveNodeToTab_AddsNodeToTargetTab()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        var node = viewModel.Nodes[0];
        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = viewModel.Tabs[0];
        viewModel.SelectedNode = node;

        // Act
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Assert
        Assert.Single(targetTab.Nodes);
        Assert.Same(node, targetTab.Nodes[0]);
    }

    [Fact]
    public void MoveNodeToTab_ClearsSelectedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        var node = viewModel.Nodes[0];
        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = viewModel.Tabs[0];
        viewModel.SelectedNode = node;

        // Act
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Assert
        Assert.Null(viewModel.SelectedNode);
    }

    [Fact]
    public void MoveNodeToTab_RemovesConnectionsAttachedToMovedNode()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));
        Assert.Single(viewModel.Connections);

        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = viewModel.Tabs[0];

        // Select the producer and move it.
        viewModel.SelectedNode = viewModel.Nodes[0];

        // Act
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Assert — connection was removed from source tab
        Assert.Empty(viewModel.Tabs[0].Connections);
    }

    [Fact]
    public void MoveNodeToTab_UpdatesIsConnectedOnRemainingConnectors()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var reporterInput = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, reporterInput));
        Assert.True(reporterInput.IsConnected);

        viewModel.AddTabCommand.Execute(null);
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = viewModel.Tabs[0];
        viewModel.SelectedNode = viewModel.Nodes[0];

        // Act
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Assert — reporter's input connector is no longer marked connected
        Assert.False(reporterInput.IsConnected);
    }

    [Fact]
    public void MoveNodeToTab_UndoRestoresNodeToSourceTab()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        var node = viewModel.Nodes[0];
        viewModel.AddTabCommand.Execute(null);
        var sourceTab = viewModel.Tabs[0];
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = sourceTab;
        viewModel.SelectedNode = node;
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Act
        viewModel.UndoCommand.Execute(null);

        // Assert
        Assert.Contains(node, sourceTab.Nodes);
        Assert.Empty(targetTab.Nodes);
    }

    [Fact]
    public void MoveNodeToTab_UndoRestoresRemovedConnections()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(10, 20));
        viewModel.AddNodeAt("Number Reporter", new Point(100, 200));
        var source = viewModel.Nodes[0].Outputs[0];
        var target = viewModel.Nodes[1].Inputs[0];
        viewModel.ConnectionCompletedCommand.Execute((source, target));

        viewModel.AddTabCommand.Execute(null);
        var sourceTab = viewModel.Tabs[0];
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = sourceTab;
        viewModel.SelectedNode = viewModel.Nodes[0];
        viewModel.MoveNodeToTabCommand.Execute(targetTab);

        // Act
        viewModel.UndoCommand.Execute(null);

        // Assert — connection is restored in source tab
        Assert.Single(sourceTab.Connections);
        Assert.True(target.IsConnected);
    }

    [Fact]
    public void MoveNodeToTab_RedoReappliesMoveAfterUndo()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.AddNodeAt("Number Producer", new Point(0, 0));
        var node = viewModel.Nodes[0];
        viewModel.AddTabCommand.Execute(null);
        var sourceTab = viewModel.Tabs[0];
        var targetTab = viewModel.Tabs[1];
        viewModel.ActiveTab = sourceTab;
        viewModel.SelectedNode = node;
        viewModel.MoveNodeToTabCommand.Execute(targetTab);
        viewModel.UndoCommand.Execute(null);

        // Act
        viewModel.RedoCommand.Execute(null);

        // Assert
        Assert.Empty(sourceTab.Nodes);
        Assert.Single(targetTab.Nodes);
        Assert.Same(node, targetTab.Nodes[0]);
    }
}
