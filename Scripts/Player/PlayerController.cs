using Godot;
using System;

public partial class PlayerController : Node
{
	Camera3D _playerCamera;
	[Export]
    public Camera3D playerCamera  
	{
		get => _playerCamera;
        set
        {
			_playerCamera = value;
        	var cameraContoller = FindChild("CameraPivot") as CameraController;
			cameraContoller.playerCamera = value;
        }
	}

	[Export]
    public MovementController movementController {get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var cameraContoller = FindChild("CameraPivot") as CameraController;
		cameraContoller.playerCamera = playerCamera;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
