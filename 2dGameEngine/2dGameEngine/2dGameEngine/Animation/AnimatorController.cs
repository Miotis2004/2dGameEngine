using System;
using System.Collections.Generic;
using System.Linq;

namespace _2dGameEngine.Animation;

/// <summary>
/// Reusable animation state machine containing parameters, states, transitions, and default state selection.
/// </summary>
public sealed class AnimatorController
{
    private readonly Dictionary<string, AnimatorParameter> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AnimatorState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AnimatorTransition> _transitions = [];

    public AnimatorController(string name, string defaultState, string? assetPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultState);
        Name = name;
        DefaultState = defaultState;
        AssetPath = assetPath;
    }

    public string Name { get; }
    public string DefaultState { get; }
    public string? AssetPath { get; }
    public IReadOnlyDictionary<string, AnimatorParameter> Parameters => _parameters;
    public IReadOnlyDictionary<string, AnimatorState> States => _states;
    public IReadOnlyList<AnimatorTransition> Transitions => _transitions;

    public void AddParameter(AnimatorParameter parameter) => _parameters[parameter.Name] = parameter;
    public void AddState(AnimatorState state) => _states[state.Name] = state;
    public void AddTransition(AnimatorTransition transition) => _transitions.Add(transition);
    public AnimatorState GetState(string name) => _states.TryGetValue(name, out AnimatorState? state) ? state : throw new InvalidOperationException($"Animator state '{name}' was not found.");
    public IEnumerable<AnimatorTransition> GetTransitionsFrom(string stateName) => _transitions.Where(transition => string.Equals(transition.FromState, stateName, StringComparison.OrdinalIgnoreCase));
}

public sealed record AnimatorState(string Name, AnimationClip Clip, float Speed = 1.0f);
public sealed record AnimatorTransition(string FromState, string ToState, TimeSpan ExitTime, AnimatorCondition[] Conditions);
public sealed record AnimatorParameter(string Name, AnimatorParameterType Type, float FloatValue = 0, int IntValue = 0, bool BoolValue = false);
public sealed record AnimatorCondition(string Parameter, AnimatorConditionMode Mode, float Threshold = 0, bool BoolValue = false);
public enum AnimatorParameterType { Float, Int, Bool, Trigger }
public enum AnimatorConditionMode { Equals, NotEquals, Greater, Less, IsTrue, IsFalse, Triggered }
