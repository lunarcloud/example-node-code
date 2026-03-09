namespace AvaloniaNodeEditor.Models;

/// <summary>Arithmetic operation performed by an <see cref="ArithmeticTransformNode"/>.</summary>
public enum ArithmeticOperation
{
    /// <summary>Adds inputs A and B.</summary>
    Add,

    /// <summary>Subtracts input B from input A.</summary>
    Subtract,

    /// <summary>Multiplies inputs A and B.</summary>
    Multiply,

    /// <summary>Divides input A by input B.</summary>
    Divide,
}
