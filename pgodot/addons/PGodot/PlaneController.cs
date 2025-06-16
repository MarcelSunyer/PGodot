using Godot;
using System;

public partial class PlaneController : CharacterBody3D
{
    // Movement parameters
    [Export] public float ForwardSpeed = 50.0f;
    [Export] public float RotationSpeed = 1.5f;
    [Export] public float PitchSpeed = 1.0f;
    [Export] public float RollSpeed = 2.0f;
    [Export] public float MaxPitchAngle = 0.5f; // In radians
    [Export] public float MaxRollAngle = 0.8f;  // In radians

    // Current movement values
    private float yawInput = 0.0f;
    private float pitchInput = 0.0f;

    public override void _Ready()
    {
        // Initialize any necessary components here
    }

    public override void _Process(double delta)
    {
        float deltaFloat = (float)delta;

        // Get input
        yawInput = Input.GetActionStrength("right") - Input.GetActionStrength("left");
        pitchInput = Input.GetActionStrength("down") - Input.GetActionStrength("up");

        // Calculate rotations
        float yaw = yawInput * RotationSpeed * deltaFloat;
        float pitch = pitchInput * PitchSpeed * deltaFloat;
        float roll = -yawInput * RollSpeed;

        // Apply rotations with limits
        RotateY(yaw);
        RotateX(Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle));

        // Smooth roll (banking) effect
        Quaternion currentRot = Quaternion.FromEuler(Rotation);
        Quaternion targetRot = Quaternion.FromEuler(new Vector3(
            Rotation.X,
            Rotation.Y,
            Mathf.Clamp(roll, -MaxRollAngle, MaxRollAngle)
        ));

        Rotation = currentRot.Slerp(targetRot, 5.0f * deltaFloat).GetEuler();

        // Always move forward
        Vector3 forward = -GlobalTransform.Basis.Z.Normalized(); // Forward is negative Z in Godot
        Velocity = forward * ForwardSpeed;

        // Move the plane
        MoveAndSlide();
    }
}