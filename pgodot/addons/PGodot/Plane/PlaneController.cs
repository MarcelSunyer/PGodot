using Godot;
using System;

public partial class PlaneController : CharacterBody3D
{
    // Speed parameters
    [Export] public float MaxSpeed = 80.0f; // meters per second
    [Export] public float MinSpeed = 20.0f;
    [Export] public float Acceleration = 15.5f;
    [Export] public float Deceleration = 10.5f;
    [Export] public float CurrentSpeed = 50.0f;

    // Rotation parameters (in degrees)
    [Export] public float YawSpeed = 30.0f;
    [Export] public float PitchSpeed = 30.0f;
    [Export] public float RollSpeed = 45.0f;

    // Node references
    private Node3D _prop;
    private Node3D _planeMesh;

    private Vector2 _turnInput = Vector2.Zero;

    public override void _Ready()
    {
        // Convert degrees to radians
        PitchSpeed = Mathf.DegToRad(PitchSpeed);
        YawSpeed = Mathf.DegToRad(YawSpeed);
        RollSpeed = Mathf.DegToRad(RollSpeed);

        // Get node references
        _prop = GetNode<Node3D>("Plane2/Plane/propellor");
        _planeMesh = GetNode<Node3D>("Plane2");
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaFloat = (float)delta;

        // Get input - CORRECCIÓN PRINCIPAL: Asignamos a _turnInput
        _turnInput = Input.GetVector("left", "right", "down", "up");
        float roll = Input.GetAxis("roll_left", "roll_right");

        // Adjust speed
        if (_turnInput.Y > 0 && CurrentSpeed < MaxSpeed)
        {
            CurrentSpeed += Acceleration * deltaFloat;
        }
        else if (_turnInput.Y < 0 && CurrentSpeed > MinSpeed)
        {
            CurrentSpeed -= Deceleration * deltaFloat;
        }

        // Movement
        Velocity = -GlobalTransform.Basis.Z * CurrentSpeed;
        MoveAndSlide();

        // Rotation
        Vector3 turnDir = new Vector3(-_turnInput.Y, -_turnInput.X, -roll);
        ApplyRotation(turnDir, deltaFloat);

        // Propeller animation
        SpinPropeller(deltaFloat);
    }

    private void ApplyRotation(Vector3 vector, float delta)
    {
        // Apply rotations
        Rotate(GlobalTransform.Basis.Z, vector.Z * RollSpeed * delta);
        Rotate(GlobalTransform.Basis.X, vector.X * PitchSpeed * delta);
        Rotate(GlobalTransform.Basis.Y, vector.Y * YawSpeed * delta);

        // Lean the mesh (banking effect)
        if (vector.Y < 0)
        {
            _planeMesh.Rotation = new Vector3(
                _planeMesh.Rotation.X,
                _planeMesh.Rotation.Y,
                Mathf.LerpAngle(_planeMesh.Rotation.Z, Mathf.DegToRad(-45) * -vector.Y, delta)
            );
        }
        else if (vector.Y > 0)
        {
            _planeMesh.Rotation = new Vector3(
                _planeMesh.Rotation.X,
                _planeMesh.Rotation.Y,
                Mathf.LerpAngle(_planeMesh.Rotation.Z, Mathf.DegToRad(45) * vector.Y, delta)
            );
        }
        else
        {
            _planeMesh.Rotation = new Vector3(
                _planeMesh.Rotation.X,
                _planeMesh.Rotation.Y,
                Mathf.LerpAngle(_planeMesh.Rotation.Z, 0, delta)
            );
        }
    }

    private void SpinPropeller(float delta)
    {
        float speedRatio = CurrentSpeed / MaxSpeed;
        _prop.RotateZ(150 * delta * speedRatio);

        if (_prop.Rotation.Z > Mathf.Tau)
        {
            _prop.Rotation = new Vector3(
                _prop.Rotation.X,
                _prop.Rotation.Y,
                _prop.Rotation.Z - Mathf.Tau
            );
        }
    }
}