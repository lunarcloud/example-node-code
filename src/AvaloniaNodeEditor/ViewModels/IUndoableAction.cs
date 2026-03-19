namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Represents a reversible user action that can be undone and redone.</summary>
public interface IUndoableAction
{
    /// <summary>Reverses the effect of this action.</summary>
    void Undo();

    /// <summary>Re-applies this action after it has been undone.</summary>
    void Redo();
}
