using System;
using System.Collections.Generic;

namespace _2dGameEngine.Animation;

/// <summary>
/// Describes a named marker fired while an animation clip or timeline is playing.
/// </summary>
public sealed record AnimationEvent(string Name, TimeSpan Time, IReadOnlyDictionary<string, string>? Arguments = null);
