using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float Acceleration = 10.0f;
    [Export] public float RotationSpeed = 5.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public int Gravity = -10;
    [Export] public Camera3D _camera;
    [Export] public Node3D RotationPivot;

    private Vector2 _mouseDelta;
    private bool _jumpRequested = false;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            _mouseDelta = mouseMotion.Relative;
        }
        else if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                _jumpRequested = true;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Jump with right-click
        if (_jumpRequested && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }
        _jumpRequested = false;

        velocity.Y += (float)(Gravity * delta);

        // Mouse look
        float mouseSensitivity = 0.002f;
        RotationPivot.RotateY(-_mouseDelta.X * mouseSensitivity);
        _camera.RotateX(-_mouseDelta.Y * mouseSensitivity);
        _camera.Rotation = new Vector3(
            Mathf.Clamp(_camera.Rotation.X, -1.2f, 1.2f),
            _camera.Rotation.Y,
            _camera.Rotation.Z
        );
        _mouseDelta = Vector2.Zero;

        // Forward movement with W only
        if (Input.IsActionPressed("forward"))
        {
            Vector3 forward = -RotationPivot.GlobalTransform.Basis.Z;
            forward.Y = 0;
            forward = forward.Normalized();

            Vector3 direction = forward;
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
