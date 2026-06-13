using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace _2dGameEngine.Input;

/// <summary>
/// Tracks keyboard and mouse input across engine frames.
/// </summary>
public sealed class InputState
{
    private readonly HashSet<Keys> _currentKeys = [];
    private readonly HashSet<Keys> _previousKeys = [];
    private readonly HashSet<MouseButtons> _currentMouseButtons = [];
    private readonly HashSet<MouseButtons> _previousMouseButtons = [];
    private Point _previousMousePosition;

    /// <summary>
    /// Gets the latest mouse position in viewport/client coordinates.
    /// </summary>
    public Point MousePosition { get; private set; }

    /// <summary>
    /// Gets the mouse movement that occurred since the previous engine frame.
    /// </summary>
    public Point MouseDelta => new(MousePosition.X - _previousMousePosition.X, MousePosition.Y - _previousMousePosition.Y);

    /// <summary>
    /// Gets the accumulated mouse wheel delta for the current engine frame.
    /// </summary>
    public int MouseWheelDelta { get; private set; }

    /// <summary>
    /// Returns whether the specified key is currently held down.
    /// </summary>
    public bool IsKeyDown(Keys key) => _currentKeys.Contains(key);

    /// <summary>
    /// Returns whether the specified key transitioned from up to down this frame.
    /// </summary>
    public bool WasKeyPressed(Keys key) => _currentKeys.Contains(key) && !_previousKeys.Contains(key);

    /// <summary>
    /// Returns whether the specified key transitioned from down to up this frame.
    /// </summary>
    public bool WasKeyReleased(Keys key) => !_currentKeys.Contains(key) && _previousKeys.Contains(key);

    /// <summary>
    /// Returns whether the specified mouse button is currently held down.
    /// </summary>
    public bool IsMouseButtonDown(MouseButtons button) => _currentMouseButtons.Contains(button);

    /// <summary>
    /// Returns whether the specified mouse button transitioned from up to down this frame.
    /// </summary>
    public bool WasMouseButtonPressed(MouseButtons button) => _currentMouseButtons.Contains(button) && !_previousMouseButtons.Contains(button);

    /// <summary>
    /// Returns whether the specified mouse button transitioned from down to up this frame.
    /// </summary>
    public bool WasMouseButtonReleased(MouseButtons button) => !_currentMouseButtons.Contains(button) && _previousMouseButtons.Contains(button);

    /// <summary>
    /// Records that a key is currently down.
    /// </summary>
    public void SetKeyDown(Keys key) => _currentKeys.Add(key);

    /// <summary>
    /// Records that a key is currently up.
    /// </summary>
    public void SetKeyUp(Keys key) => _currentKeys.Remove(key);

    /// <summary>
    /// Records that a mouse button is currently down.
    /// </summary>
    public void SetMouseButtonDown(MouseButtons button) => _currentMouseButtons.Add(button);

    /// <summary>
    /// Records that a mouse button is currently up.
    /// </summary>
    public void SetMouseButtonUp(MouseButtons button) => _currentMouseButtons.Remove(button);

    /// <summary>
    /// Records the current mouse position in viewport/client coordinates.
    /// </summary>
    public void SetMousePosition(Point position) => MousePosition = position;

    /// <summary>
    /// Adds wheel movement to the current frame accumulator.
    /// </summary>
    public void AddMouseWheelDelta(int delta) => MouseWheelDelta += delta;

    /// <summary>
    /// Advances transient input state after frame processing has completed.
    /// </summary>
    internal void AdvanceFrame()
    {
        _previousKeys.Clear();
        _previousKeys.UnionWith(_currentKeys);
        _previousMouseButtons.Clear();
        _previousMouseButtons.UnionWith(_currentMouseButtons);
        _previousMousePosition = MousePosition;
        MouseWheelDelta = 0;
    }
}
