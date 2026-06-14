namespace _2dGameEngine.Animation;

/// <summary>
/// Implement on components that need callbacks from animation clips or animator states.
/// </summary>
public interface IAnimationEventReceiver
{
    void OnAnimationEvent(AnimationEvent animationEvent);
}
