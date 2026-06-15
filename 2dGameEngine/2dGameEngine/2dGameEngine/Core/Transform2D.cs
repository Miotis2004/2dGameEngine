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
