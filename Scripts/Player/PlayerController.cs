using Godot;

public partial class PlayerController : Node3D
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
		// If movementController is not assigned, try to find it as a child node
		if (movementController == null)
		{
			movementController = FindChild("MovementController") as MovementController;
			if (movementController == null)
			{
				GD.PrintErr("PlayerController: MovementController not found! Please assign it in the inspector or ensure it exists as a child node.");
			}
		}
		
		SetChildParams();
	}

	private void SetChildParams()
	{
		var cameraContoller = FindChild("CameraPivot") as CameraController;
		cameraContoller.playerCamera = playerCamera;

		var terrainEdit = FindChild("TerrainEdit") as TerrainEdit;
		terrainEdit.playerCamera = playerCamera;
	}

	private void PlayAnim(string animName, float speed = 1f)
	{
		animationPlayer.SpeedScale = speed;
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

		// Screenshot functionality
		if (Input.IsActionJustPressed("take_screenshot"))
		{
			TakeScreenshot();
		}

		// Ensure movementController is initialized before accessing it
		if (movementController != null)
		{
			if (movementController.movementDirection != Vector3.Zero)
			{			
				if(playerState == PlayerState.Crouched)
					PlayAnim("sneak_walking");
				else
					PlayAnim("running", .45f);
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
		}

		base._Process(delta);
	}


	public void ToggleCrouch()
	{
		// Ensure movementController is initialized before toggling crouch
		if (movementController == null)
		{
			return;
		}
		
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

	private void TakeScreenshot()
	{
		// Find the screenshot manager in the scene
		var screenshotManager = GetTree().GetFirstNodeInGroup("screenshot_manager");
		if (screenshotManager == null)
		{
			screenshotManager = GetTree().CurrentScene.FindChild("ScreenshotManager");
		}
		
		if (screenshotManager != null && screenshotManager.HasMethod("TakeScreenshot"))
		{
			screenshotManager.Call("TakeScreenshot");
		}
		else
		{
			GD.Print("Screenshot manager not found - taking manual screenshot");
			// Fallback: take screenshot directly
			var viewport = GetViewport();
			var image = viewport.GetTexture().GetImage();
			var timestamp = Time.GetDatetimeStringFromSystem().Replace(":", "-").Replace(" ", "_");
			var filename = $"Screenshots/player_screenshot_{timestamp}.png";
			image.SavePng(filename);
			GD.Print($"Screenshot saved: {filename}");
		}
	}

}
