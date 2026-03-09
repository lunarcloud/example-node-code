namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Represents a directed connection from one connector to another in the node graph.</summary>
public class ConnectionViewModel
{
    /// <summary>The connector that is the source of the connection.</summary>
    public ConnectorViewModel Source { get; }

    /// <summary>The connector that is the target of the connection.</summary>
    public ConnectorViewModel Target { get; }

    /// <summary>Initializes a new instance of <see cref="ConnectionViewModel"/>.</summary>
    /// <param name="source">The source connector.</param>
    /// <param name="target">The target connector.</param>
    public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
    {
        Source = source;
        Target = target;
    }
}
