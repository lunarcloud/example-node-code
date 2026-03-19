using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNodeEditor.ViewModels;

/// <summary>Manages bounded undo and redo stacks for reversible user actions.</summary>
public partial class UndoRedoManager : ObservableObject
{
    private const int MaxStackSize = 100;
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    /// <summary>Whether there are actions available to undo.</summary>
    [ObservableProperty]
    private bool _canUndo;

    /// <summary>Whether there are actions available to redo.</summary>
    [ObservableProperty]
    private bool _canRedo;

    /// <summary>Records a completed action onto the undo stack and clears the redo stack.</summary>
    public void Record(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();

        if (_undoStack.Count > MaxStackSize)
        {
            TrimStack(_undoStack, MaxStackSize);
        }

        UpdateFlags();
    }

    /// <summary>Undoes the most recent action.</summary>
    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        UpdateFlags();
    }

    /// <summary>Redoes the most recently undone action.</summary>
    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);
        UpdateFlags();
    }

    /// <summary>Clears both the undo and redo stacks.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateFlags();
    }

    private void UpdateFlags()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    /// <summary>Keeps only the <paramref name="maxSize"/> most-recently-pushed entries in <paramref name="stack"/>.</summary>
    private static void TrimStack(Stack<IUndoableAction> stack, int maxSize)
    {
        var items = stack.ToArray(); // items[0] = top (most recent)
        stack.Clear();

        // Re-push from the oldest-kept entry (index maxSize-1) up to the newest (index 0).
        for (var i = maxSize - 1; i >= 0; i--)
        {
            stack.Push(items[i]);
        }
    }
}
