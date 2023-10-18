using Godot;
using System;

public partial class TerrainEdit : Node3D
{
	[Export]
	public Camera3D camera3D;
	[Export]
	public Node3D cursor;
	[Export]
	public int RayLength = 100;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{ }

    public override void _PhysicsProcess(double delta)
    {
		if (Input.IsActionJustPressed("place_terrain"))
        {

         	var mousePosition = GetViewport().GetMousePosition();
			var from = camera3D.ProjectRayOrigin(mousePosition);
        	var to = from + camera3D.ProjectRayNormal(mousePosition) * RayLength;
			var spaceState = GetWorld3D().DirectSpaceState;
    		// use global coordinates, not local to node
			var query = PhysicsRayQueryParameters3D.Create(from, to);
    		var result = spaceState.IntersectRay(query);
			if (result.Count > 0)
    			GD.Print("Hit at point: ", result["position"]); //["position", "normal", "collider_id", "collider", "shape", "rid"]
			else
				GD.Print("No Hit?");
		}

		if (Input.IsActionJustPressed("remove_terrain"))
        {
			//remove terrain
		}
    }
}
