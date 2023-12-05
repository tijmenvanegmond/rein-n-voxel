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

	private void PlayAnim(string animName)
	{
		if (animationPlayer.CurrentAnimation != animName)
			animationPlayer.Play(animName);
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
			if(playerState == PlayerState.Crouched)
				PlayAnim("sneak_walking");
			else
				PlayAnim("running");
		}
		else
		{
			if(playerState == PlayerState.Crouched)
				PlayAnim("sneak_idle");
			else
				PlayAnim("idle");
		}

		if(!movementController.IsOnFloor()){
			PlayAnim("falling");
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
			collider.Position = new Vector3(0, .45f, 0);
			movementController.Crouch();
		}
		else if (playerState == PlayerState.Crouched)
		{
			playerState = PlayerState.Normal;
			CapsuleShape3D shape = collider.Shape as CapsuleShape3D;
			shape.Height = 1.8f;
			collider.Position = new Vector3(0, .9f, 0);
			movementController.UnCrouch();
		}
	}

}
