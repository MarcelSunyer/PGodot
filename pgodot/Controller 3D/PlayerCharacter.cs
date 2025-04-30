using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
    [Export]
    public float Speed = 5.0f;
    [Export]
    public float Acceleration = 10.0f;
    [Export]
    public float RotationSpeed = 5.0f;
    [Export]
    public float JumpVelocity = 4.5f;

    [Export]
    public int Gravity = -10;

    private Camera3D _camera;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
    }
    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        velocity.Y += (float)(Gravity * delta);

        Vector2 input = new Vector2(Input.GetActionRawStrength("right") - Input.GetActionRawStrength("left"), Input.GetActionRawStrength("back") - Input.GetActionRawStrength("forward")).Normalized();

        if (input.Length() > 0.01f)
        {
            Vector3 forward = _camera.GlobalTransform.Basis.Z;
            Vector3 right = _camera.GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            Vector3 direction = (right * input.X + forward * input.Y).Normalized();

            // Set horizontal movement
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;

            float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
            Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, targetYaw, RotationSpeed * (float)delta), Rotation.Z);
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X, 0, Acceleration * (float)delta);
            velocity.Z = Mathf.Lerp(velocity.Z, 0, Acceleration * (float)delta);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
