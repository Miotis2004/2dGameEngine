namespace PlatformerSample.Game.Scripts;

public sealed class PlayerController
{
    public string DisplayName { get; set; } = "Sample Platformer Controller";

    public float MoveSpeed { get; set; } = 7.5f;

    public float JumpImpulse { get; set; } = 12.0f;

    public void Update(float deltaTime)
    {
        // Public-workflow sample hook: read input and drive platformer movement here.
    }
}
