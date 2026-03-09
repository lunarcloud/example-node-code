using System;
using System.Collections.Generic;

namespace AvaloniaNodeEditor.Models;

/// <summary>Provides a static list of all <see cref="ArithmeticOperation"/> values for XAML ComboBox bindings.</summary>
public static class ArithmeticOperationValues
{
    /// <summary>All available arithmetic operations.</summary>
    public static IReadOnlyList<ArithmeticOperation> All { get; } =
        Enum.GetValues<ArithmeticOperation>();
}
