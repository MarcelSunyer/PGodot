using Godot;
using System;
using System.Diagnostics;

public partial class Node3d_Test : Node3D
{
    [Export]
    public MeshInstance3D PlaneInstance { get; set; }

    [Export]
    public float Speed = 5.0f;

    public override void _Ready()
    {
        // Si quieres hacer algo al iniciar, pero el PlaneInstance ya vendrá asignado desde el editor.
    }

    public override void _Process(double delta)
    {
        if (PlaneInstance == null)
            return; // No hacer nada si no se asignó

        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("ui_up"))    // W
            direction.Z -= 1;
        if (Input.IsActionPressed("ui_down"))  // S
            direction.Z += 1;
        if (Input.IsActionPressed("ui_left"))  // A
            direction.X -= 1;
            direction.X +=1;

        Debug.Print( direction.X.ToString());

        direction = direction.Normalized();

        PlaneInstance.Position += direction * Speed * (float)delta;
    }
}
