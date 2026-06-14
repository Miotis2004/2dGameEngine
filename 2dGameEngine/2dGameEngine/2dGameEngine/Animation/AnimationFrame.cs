using System;
using _2dGameEngine.Content;

namespace _2dGameEngine.Animation;

/// <summary>
/// Describes a single sprite frame and how long it should be displayed by an animation clip.
/// </summary>
public sealed record AnimationFrame(SpriteFrame SpriteFrame, TimeSpan Duration);
