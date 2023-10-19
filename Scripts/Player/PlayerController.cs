using Godot;
using System;

public partial class PlayerController : Node
{
	[Export]
    public Camera3D playerCamera;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var camCont = FindChild("CameraPivot") as CameraController;
		camCont.playerCamera = playerCamera;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
