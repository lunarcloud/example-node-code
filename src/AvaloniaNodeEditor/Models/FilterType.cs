namespace AvaloniaNodeEditor.Models;

/// <summary>Type of pass filter applied by a <see cref="PassFilterNode"/>.</summary>
public enum FilterType
{
    /// <summary>Passes values at or below the threshold.</summary>
    LowPass,

    /// <summary>Passes values within the mid-band range.</summary>
    MidPass,

    /// <summary>Passes values at or above the threshold.</summary>
    HighPass,
}
