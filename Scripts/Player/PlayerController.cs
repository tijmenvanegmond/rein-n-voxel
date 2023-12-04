using Godot;

public partial class PlayerController : Node
{
	public enum PlayerState
	{
		Normal,
		Crouched
	}

	public PlayerState playerState = PlayerState.Normal;
	[Export]
	CollisionShape3D collider;
	[Export]
	AnimationPlayer animationPlayer;
	Camera3D _playerCamera;
	public PlayerAction action_MovementAbility = new Blink();

	[Export]
	public Camera3D playerCamera
	{
		get => _playerCamera;
		set
		{
			_playerCamera = value;
			SetChildParams();
		}
	}

	[Export]
	public MovementController movementController { get; set; }

	public override void _Ready()
	{
		SetChildParams();
	}

	private void SetChildParams()
	{
		var cameraContoller = FindChild("CameraPivot") as CameraController;
		cameraContoller.playerCamera = playerCamera;

		var terrainEdit = FindChild("TerrainEdit") as TerrainEdit;
		terrainEdit.playerCamera = playerCamera;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("movement_abilty") && action_MovementAbility != null)
		{
			action_MovementAbility.DoAction(this);
		}

		if (Input.IsActionJustPressed("crouch"))
		{
			ToggleCrouch();
		}

		if (movementController.movementDirection != Vector3.Zero)
		{
			if (animationPlayer.CurrentAnimation != "running")
				animationPlayer.Play("running");
		}
		else
		{
			if (animationPlayer.CurrentAnimation != "idle")
				animationPlayer.Play("idle");
		}

		base._Process(delta);
	}


	public void ToggleCrouch()
	{
		if (playerState == PlayerState.Normal)
		{
			playerState = PlayerState.Crouched;
			CapsuleShape3D shape = collider.Shape as CapsuleShape3D;
			shape.Height = .9f;
			movementController.Crouch();
		}
		else if (playerState == PlayerState.Crouched)
		{
			playerState = PlayerState.Normal;
			CapsuleShape3D shape = collider.Shape as CapsuleShape3D;
			shape.Height = 1.8f;
			movementController.UnCrouch();
		}
	}

}
