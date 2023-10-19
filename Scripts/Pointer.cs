using Godot;
using System;

public partial class Pointer : Node3D
{

    [Export]
    public Node3D Target { get; set; }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		this.LookAt(Target.GlobalPosition);
	}
}
