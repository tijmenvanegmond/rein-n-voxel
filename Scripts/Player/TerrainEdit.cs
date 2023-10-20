using Godot;
using System;

public partial class TerrainEdit : Node3D
{
	[Export]
	public Camera3D playerCamera;
	[Export]
	public Node3D cursor;
	[Export]
	public int RayLength = 100;

	private CustomSignals customSignals;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		customSignals = GetNode<CustomSignals>("/root/CustomSignals");
	}

	public override void _PhysicsProcess(double delta)
	{
		//cast ray from mouse
		var mousePosition = GetViewport().GetMousePosition();
		var from = playerCamera.ProjectRayOrigin(mousePosition);
		var to = from + playerCamera.ProjectRayNormal(mousePosition) * RayLength;
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
		{
			cursor.Visible = false;
			GD.Print("No Hit?");
			return;
		}

		var key = (Vector3I)result["position"];
		if (cursor != null)
		{
			cursor.Visible = true;
			cursor.GlobalPosition = key;
		};

		if (Input.IsActionJustPressed("place_terrain"))
		{

			customSignals.EmitSignal(CustomSignals.SignalName.EditTerrain, key, (byte)1);
		}
		else if (Input.IsActionJustPressed("remove_terrain"))
		{
			customSignals.EmitSignal(CustomSignals.SignalName.EditTerrain, key, (byte)0);
		}
	}
}
