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
	public PlayerAction action_AOEAttack = new AOEAttack();

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

	// Mob selection system
	private MobInfoUI mobInfoUI;
	private Mob selectedMob;

	public override void _Ready()
	{
		// Add to player group so other systems can find this node
		AddToGroup("player");
		
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
		
		// Initialize mob info UI
		InitializeMobInfoUI();
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

		if (Input.IsActionJustPressed("aoe_attack") && action_AOEAttack != null)
		{
			GD.Print("AOE attack input detected!");
			action_AOEAttack.DoAction(this);
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

		// Handle mob selection with mouse clicks
		HandleMobSelection();

		// Check if selected mob is still valid
		ValidateSelectedMob();

		// Ensure movementController is initialized before accessing it
		if (movementController != null)
		{
			if (movementController.movementDirection != Vector3.Zero)
			{			
				if(playerState == PlayerState.Crouched)
					PlayAnim("sneak_walking");
				else
					PlayAnim("running", 0.45f);
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
			shape.Height = 0.9f;
			collider.Position = new Vector3(0, 0.45f, 0);
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

	private void InitializeMobInfoUI()
	{
		// Try to find existing MobInfoUI in the scene
		mobInfoUI = GetTree().GetFirstNodeInGroup("mob_info_ui") as MobInfoUI;
		
		// If not found, create one and add it to the current scene
		if (mobInfoUI == null)
		{
			mobInfoUI = new MobInfoUI();
			GetTree().CurrentScene.AddChild(mobInfoUI);
		}
	}

	private void HandleMobSelection()
	{
		// Check for left mouse click
		if (Input.IsActionJustPressed("ui_select") || Input.IsMouseButtonPressed(MouseButton.Left))
		{
			PerformMobRaycast();
		}
		
		// Right click or ESC to deselect
		if (Input.IsActionJustPressed("ui_cancel") || Input.IsMouseButtonPressed(MouseButton.Right))
		{
			DeselectCurrentMob();
		}
	}

	private void PerformMobRaycast()
	{
		if (playerCamera == null) return;

		// Get mouse position
		var mousePos = GetViewport().GetMousePosition();
		
		// Create a ray from camera through mouse position
		var from = playerCamera.ProjectRayOrigin(mousePos);
		var to = from + playerCamera.ProjectRayNormal(mousePos) * 1000; // 1000 units max distance

		// Perform raycast
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		
		var result = spaceState.IntersectRay(query);
		
		if (result.Count > 0)
		{
			var collider = result["collider"].AsGodotObject();
			
			// Check if we hit a mob
			if (collider is Mob mob)
			{
				SelectMob(mob);
			}
			else
			{
				// Check if the collider is a child of a mob (like collision shapes)
				var parentMob = FindMobInParents(collider as Node);
				if (parentMob != null)
				{
					SelectMob(parentMob);
				}
				else
				{
					DeselectCurrentMob();
				}
			}
		}
		else
		{
			DeselectCurrentMob();
		}
	}

	private Mob FindMobInParents(Node node)
	{
		while (node != null)
		{
			if (node is Mob mob)
				return mob;
			node = node.GetParent();
		}
		return null;
	}

	private void ValidateSelectedMob()
	{
		// Check if selected mob is still valid and alive
		if (selectedMob != null && (!GodotObject.IsInstanceValid(selectedMob) || selectedMob.isDead))
		{
			DeselectCurrentMob();
		}
	}

	private void SelectMob(Mob mob)
	{
		// Validate the mob before selection
		if (mob == null || !GodotObject.IsInstanceValid(mob) || mob.isDead)
		{
			DeselectCurrentMob();
			return;
		}
		
		if (selectedMob == mob) return; // Already selected
		
		// Deselect previous mob
		if (selectedMob != null && GodotObject.IsInstanceValid(selectedMob))
		{
			SetMobHighlight(selectedMob, false);
		}
		
		// Select new mob
		selectedMob = mob;
		if (mobInfoUI != null)
		{
			mobInfoUI.SelectMob(mob);
		}
		
		// Highlight the selected mob
		SetMobHighlight(mob, true);
		
		GD.Print($"Selected mob: {mob.name}");
	}

	private void DeselectCurrentMob()
	{
		if (selectedMob != null && GodotObject.IsInstanceValid(selectedMob))
		{
			SetMobHighlight(selectedMob, false);
		}
		selectedMob = null;
		
		if (mobInfoUI != null)
		{
			mobInfoUI.DeselectMob();
		}
	}

	private void SetMobHighlight(Mob mob, bool highlight)
	{
		// Check if mob is still valid before accessing it
		if (mob == null || !GodotObject.IsInstanceValid(mob))
		{
			return;
		}

		// Find the mob's visual representation (MeshInstance3D)
		var meshInstance = mob.FindChild("MeshInstance3D") as MeshInstance3D;
		if (meshInstance == null)
		{
			// Try to find it in children recursively
			meshInstance = FindMeshInstanceRecursive(mob);
		}
		
		if (meshInstance != null)
		{
			if (highlight)
			{
				// Add a highlight effect (e.g., outline or glow)
				// For now, we'll just change the material tint
				var material = meshInstance.GetActiveMaterial(0);
				if (material is StandardMaterial3D stdMat)
				{
					stdMat = stdMat.Duplicate() as StandardMaterial3D;
					stdMat.Emission = new Color(0.2f, 0.2f, 1.0f); // Blue glow
					meshInstance.SetSurfaceOverrideMaterial(0, stdMat);
				}
			}
			else
			{
				// Remove highlight
				meshInstance.SetSurfaceOverrideMaterial(0, null);
			}
		}
	}

	private MeshInstance3D FindMeshInstanceRecursive(Node node)
	{
		if (node is MeshInstance3D meshInstance)
			return meshInstance;
			
		foreach (Node child in node.GetChildren())
		{
			var result = FindMeshInstanceRecursive(child);
			if (result != null)
				return result;
		}
		
		return null;
	}

}
