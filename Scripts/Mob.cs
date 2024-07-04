using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Godot;

public partial class Mob : RigidBody3D
{
	[Export]
	public String name = "Mob";
	[Export]
	public Node3D target;
	[Export]
	public Area3D visionArea;
	[Export]
	public float power { get; private set; } = 5f;
	[Export]
	private float maxSpeed = 4f;
	public TerrainManager terrainManager;

	public override void _Process(double delta)
	{
		if (target == null)
		{
			GD.Print("Mob: target is null");
			return;
		}

		var bodies = visionArea.GetOverlappingBodies();

		float mobRepulsionDistanceSq = 8f;
		float mobAttractionDistanceSq = 16f;


		var directions = new List<Vector3>();

		var bodies7 = bodies.OrderBy(x => x.Position.DistanceSquaredTo(Position)).Take(7);

		foreach (var body in bodies7)
		{
			if (body == this)
				continue;
			else if (body is Mob)
			{
				if (this.LinearVelocity.Length() > maxSpeed)
				{
					break;
				}

				var distSq = body.Position.DistanceSquaredTo(Position);
				if (distSq < mobRepulsionDistanceSq)
				{
					//repulsion
					directions.Add((Position - body.Position).Normalized()*.5f);
				} 
				else
				{
					//attraction
					directions.Add((body.Position - Position).Normalized()*.2f);
				}
				
					//flock
					directions.Add((body as Mob).LinearVelocity.Normalized()*.5f);	
			}
			else if (body is PlayerController)
			{
				//look at player
				GD.Print("Mob: Player found");
				this.LookAt(body.GlobalTransform.Origin, Vector3.Up);
			}
		}

		if(!directions.Any()){
			return;
		}

		Vector3 direction = Vector3.Zero;//(directions.Aggregate((a, x) => { return a + x; }) / directions.Count()).Normalized();

		foreach (var v in directions)
		{
			direction += v;
		}

		direction = direction / directions.Count();

		ApplyCentralImpulse(direction * power * (float)delta);


	}


}
