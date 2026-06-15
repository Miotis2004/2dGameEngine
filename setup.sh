#!/bin/bash
cat << 'INNER_EOF' > 2dGameEngine/2dGameEngine/2dGameEngine/Vector2Converter.cs
using System;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace _2dGameEngine;

public class Vector2Converter : ExpandableObjectConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string))
            return true;
        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            string[] parts = str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, culture, out float x) && float.TryParse(parts[1], NumberStyles.Float, culture, out float y))
            {
                return new Vector2(x, y);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Vector2 v)
        {
            return $"{v.X}, {v.Y}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

    public override object CreateInstance(ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues)
    {
        return new Vector2((float)(propertyValues["X"] ?? 0f), (float)(propertyValues["Y"] ?? 0f));
    }
}
INNER_EOF

sed -i 's/public sealed class Transform2D/public sealed class Transform2D : System.ComponentModel.INotifyPropertyChanged/' 2dGameEngine/2dGameEngine/2dGameEngine/Core/Transform2D.cs
cat << 'INNER_EOF' > 2dGameEngine/2dGameEngine/2dGameEngine/Core/Transform2D.cs.new
using System.ComponentModel;
using System.Numerics;

namespace _2dGameEngine.Core;

/// <summary>
/// Describes an entity's position, rotation, and scale in two-dimensional space.
/// </summary>
public sealed class Transform2D : INotifyPropertyChanged
{
    private Vector2 _position;
    private float _rotation;
    private Vector2 _scale = Vector2.One;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the entity position in world space.
    /// </summary>
    [TypeConverter(typeof(Vector2Converter))]
    public Vector2 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the entity rotation in radians.
    /// </summary>
    public float Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation != value)
            {
                _rotation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rotation)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the entity scale.
    /// </summary>
    [TypeConverter(typeof(Vector2Converter))]
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            if (_scale != value)
            {
                _scale = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Scale)));
            }
        }
    }
}
INNER_EOF
mv 2dGameEngine/2dGameEngine/2dGameEngine/Core/Transform2D.cs.new 2dGameEngine/2dGameEngine/2dGameEngine/Core/Transform2D.cs
