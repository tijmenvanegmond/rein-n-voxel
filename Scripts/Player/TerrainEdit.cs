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
		if (Input.IsActionJustPressed("place_terrain"))
		{
			var mousePosition = GetViewport().GetMousePosition();
			var from = playerCamera.ProjectRayOrigin(mousePosition);
			var to = from + playerCamera.ProjectRayNormal(mousePosition) * RayLength;
			var spaceState = GetWorld3D().DirectSpaceState;
			// use global coordinates, not local to node
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			var result = spaceState.IntersectRay(query);
			if (result.Count > 0)
			{
				GD.Print("Hit at point: ", result["position"]); //["position", "normal", "collider_id", "collider", "shape", "rid"]
				var key = (Vector3I)result["position"];
				customSignals.EmitSignal(CustomSignals.SignalName.EditTerrain, key, (byte)1);
			}
			else
				GD.Print("No Hit?");
		}

		if (Input.IsActionJustPressed("remove_terrain"))
		{
			var mousePosition = GetViewport().GetMousePosition();
			var from = playerCamera.ProjectRayOrigin(mousePosition);
			var to = from + playerCamera.ProjectRayNormal(mousePosition) * RayLength;
			var spaceState = GetWorld3D().DirectSpaceState;
			// use global coordinates, not local to node
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			var result = spaceState.IntersectRay(query);
			if (result.Count > 0)
			{
				GD.Print("Hit at point: ", result["position"]); //["position", "normal", "collider_id", "collider", "shape", "rid"]
				var key = (Vector3I)result["position"];
				customSignals.EmitSignal(CustomSignals.SignalName.EditTerrain, key, (byte)0);
			}
			else
				GD.Print("No Hit?");
		}
	}
}
