using Godot;

public partial class Mob : RigidBody3D
{

	[Export]
	public Node3D target;
	[Export]
	public Area3D visionArea;
	[Export]
	public float power { get; private set; } = 5f;
	[Export]
	private float maxSpeed = 4f;
	public TerrainManager terrainManager;

	void OnBodyEntered(PhysicsBody3D body)
	{
		
		
	}

		void OnBodyExited(PhysicsBody3D body)
	{
		
		
	}

	public override void _Process(double delta)
	{
		if (target == null)
		{
			GD.Print("Mob: target is null");
			return;
		}

		var bodies = visionArea.GetOverlappingBodies();

		float mobRepulsionDistanceSq = 4f;

		foreach (var body in bodies)
		{
			if(body == this)
				continue;
			if (body is Mob)
			{
				var distSq = body.Position.DistanceSquaredTo(Position);
				if(distSq < mobRepulsionDistanceSq)
				{
					Vector3 direction = ( Position -body.Position).Normalized();
					if(this.LinearVelocity.Length() < maxSpeed)
						ApplyCentralImpulse(direction * power * .5f * (float)delta);
				}
				break;
			}

			if (body is MovementController)
			{
				Vector3 direction = ( body.Position - Position).Normalized();
				if(this.LinearVelocity.Length() < maxSpeed)				
					ApplyCentralImpulse(direction* power * (float)delta);
			}
		}

		
	}

	
}
