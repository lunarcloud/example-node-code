using System;
using System.Collections.Generic;

namespace AvaloniaNodeEditor.Models;

/// <summary>Provides a static list of all <see cref="FilterType"/> values for XAML ComboBox bindings.</summary>
public static class FilterTypeValues
{
    /// <summary>All available filter types.</summary>
    public static IReadOnlyList<FilterType> All { get; } =
        Enum.GetValues<FilterType>();
}
