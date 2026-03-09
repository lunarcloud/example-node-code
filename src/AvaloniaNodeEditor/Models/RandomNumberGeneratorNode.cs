using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeEditor.Model;
using NodeEditor.Mvvm;

namespace AvaloniaNodeEditor.Models;

/// <summary>A node that generates a random float value within a configurable range.</summary>
public partial class RandomNumberGeneratorNode : NodeViewModel
{
    private static readonly Random _random = Random.Shared;

    /// <summary>The lower bound of the generated value (inclusive).</summary>
    [ObservableProperty]
    private double _minValue;

    /// <summary>The upper bound of the generated value (exclusive).</summary>
    [ObservableProperty]
    private double _maxValue = 1.0;

    /// <summary>The most recently generated float value.</summary>
    [ObservableProperty]
    private double _value;

    /// <summary>Initializes a new instance of <see cref="RandomNumberGeneratorNode"/>.</summary>
    public RandomNumberGeneratorNode()
    {
        Name = "Random Number Generator";
        Width = 200;
        Height = 90;
        Content = this;
        Pins = new ObservableCollection<IPin>
        {
            new PinViewModel { Name = "Output", Alignment = PinAlignment.Right, Parent = this },
        };
    }

    /// <summary>Generates a new random value within [<see cref="MinValue"/>, <see cref="MaxValue"/>].</summary>
    /// <returns>A random double value in the configured range.</returns>
    public double Generate()
    {
        Value = MinValue + (_random.NextDouble() * (MaxValue - MinValue));
        return Value;
    }
}
