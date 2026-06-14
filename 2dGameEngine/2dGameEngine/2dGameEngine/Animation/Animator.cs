using System;
using System.Collections.Generic;
using System.Linq;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;

namespace _2dGameEngine.Animation;

/// <summary>
/// Runtime component that evaluates an <see cref="AnimatorController"/> state machine and applies sprite animation to the entity.
/// </summary>
public sealed class Animator : Component
{
    private readonly Dictionary<string, AnimatorParameterValue> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private TimeSpan _stateElapsed;
    private string _currentState;

    public Animator(AnimatorController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        Controller = controller;
        _currentState = controller.DefaultState;
        foreach (AnimatorParameter parameter in controller.Parameters.Values)
        {
            _parameters[parameter.Name] = AnimatorParameterValue.FromParameter(parameter);
        }
    }

    public AnimatorController Controller { get; }
    public string CurrentStateName => _currentState;
    public float PlaybackSpeed { get; set; } = 1.0f;

    public void SetFloat(string name, float value) => _parameters[name] = new AnimatorParameterValue(AnimatorParameterType.Float, value);
    public void SetInt(string name, int value) => _parameters[name] = new AnimatorParameterValue(AnimatorParameterType.Int, value);
    public void SetBool(string name, bool value) => _parameters[name] = new AnimatorParameterValue(AnimatorParameterType.Bool, BoolValue: value);
    public void SetTrigger(string name) => _parameters[name] = new AnimatorParameterValue(AnimatorParameterType.Trigger, BoolValue: true);

    public override void Update(Time time)
    {
        AnimatorState state = Controller.GetState(_currentState);
        TimeSpan previous = _stateElapsed;
        _stateElapsed += TimeSpan.FromTicks((long)(time.DeltaTime.Ticks * PlaybackSpeed * state.Speed));
        DispatchEvents(state.Clip, previous, _stateElapsed);
        ApplyFrame(state.Clip.GetFrameAt(_stateElapsed));
        EvaluateTransitions(state);
    }

    public void Play(string stateName)
    {
        Controller.GetState(stateName);
        _currentState = stateName;
        _stateElapsed = TimeSpan.Zero;
        ApplyFrame(Controller.GetState(_currentState).Clip.GetFrameAt(_stateElapsed));
    }

    protected override void OnAttached() => ApplyFrame(Controller.GetState(_currentState).Clip.GetFrameAt(TimeSpan.Zero));

    private void EvaluateTransitions(AnimatorState state)
    {
        foreach (AnimatorTransition transition in Controller.GetTransitionsFrom(state.Name))
        {
            if (_stateElapsed < transition.ExitTime || !transition.Conditions.All(IsMet)) continue;
            Play(transition.ToState);
            foreach (AnimatorCondition condition in transition.Conditions.Where(condition => condition.Mode == AnimatorConditionMode.Triggered))
            {
                _parameters[condition.Parameter] = new AnimatorParameterValue(AnimatorParameterType.Trigger, BoolValue: false);
            }
            break;
        }
    }

    private bool IsMet(AnimatorCondition condition)
    {
        if (!_parameters.TryGetValue(condition.Parameter, out AnimatorParameterValue value)) return false;
        return condition.Mode switch
        {
            AnimatorConditionMode.Equals => value.Number == condition.Threshold,
            AnimatorConditionMode.NotEquals => value.Number != condition.Threshold,
            AnimatorConditionMode.Greater => value.Number > condition.Threshold,
            AnimatorConditionMode.Less => value.Number < condition.Threshold,
            AnimatorConditionMode.IsTrue => value.BoolValue,
            AnimatorConditionMode.IsFalse => !value.BoolValue,
            AnimatorConditionMode.Triggered => value.BoolValue,
            _ => false,
        };
    }

    private void ApplyFrame(AnimationFrame frame)
    {
        if (Entity?.GetComponent<SpriteRenderer>() is { } sprite) sprite.Frame = frame.SpriteFrame;
    }

    private void DispatchEvents(AnimationClip clip, TimeSpan previous, TimeSpan elapsed)
    {
        if (Entity is null || clip.Events.Count == 0) return;
        TimeSpan start = clip.IsLooping && clip.Duration > TimeSpan.Zero ? TimeSpan.FromTicks(previous.Ticks % clip.Duration.Ticks) : previous;
        TimeSpan end = clip.IsLooping && clip.Duration > TimeSpan.Zero ? TimeSpan.FromTicks(elapsed.Ticks % clip.Duration.Ticks) : elapsed;
        foreach (AnimationEvent animationEvent in clip.Events.Where(animationEvent => start <= end ? animationEvent.Time > start && animationEvent.Time <= end : animationEvent.Time > start || animationEvent.Time <= end))
        {
            foreach (IAnimationEventReceiver receiver in Entity.Components.OfType<IAnimationEventReceiver>()) receiver.OnAnimationEvent(animationEvent);
        }
    }

    private readonly record struct AnimatorParameterValue(AnimatorParameterType Type, float Number = 0, bool BoolValue = false)
    {
        public static AnimatorParameterValue FromParameter(AnimatorParameter parameter) => parameter.Type switch
        {
            AnimatorParameterType.Bool or AnimatorParameterType.Trigger => new AnimatorParameterValue(parameter.Type, BoolValue: parameter.BoolValue),
            AnimatorParameterType.Int => new AnimatorParameterValue(parameter.Type, parameter.IntValue),
            _ => new AnimatorParameterValue(parameter.Type, parameter.FloatValue),
        };
    }
}
