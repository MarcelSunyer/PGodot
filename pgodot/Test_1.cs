using Godot;
using System;

public partial class Test_1 : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Helloworld");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
