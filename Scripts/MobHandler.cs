using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MobHandler : Node
{

	[Export]
	public PackedScene mobScene_smallFriendly { get; set; }

	[Export]
	public PackedScene mobScene_smallSkittish { get; set; }

	[Export]
	public PackedScene mobScene_medium { get; set; }

	[Export]
	public PackedScene mobScene_large { get; set; }


	public List<Mob> mobs = new List<Mob>();


	public void SpawnRandomMobs(int Amount, Vector3 location, Node3D target, float radius = 30f)
	{
		GD.Print($"MobHandler: Attempting to spawn {Amount} mobs at {location}");
		
		// Check if mob scenes are assigned
		if (mobScene_smallFriendly == null && mobScene_smallSkittish == null && mobScene_medium == null && mobScene_large == null)
		{
			GD.PrintErr("MobHandler: No mob scenes assigned! Please assign mob scene resources in the inspector.");
			return;
		}
		
		// For now, if we only have one small scene, use it for both types
		var hasSmallScene = mobScene_smallFriendly != null || mobScene_smallSkittish != null;
		GD.Print($"MobHandler: Mob scenes - Small: {hasSmallScene}, Medium: {mobScene_medium != null}, Large: {mobScene_large != null}");
		
		if (target == null)
		{
			GD.PrintErr("MobHandler: No target provided for mobs!");
			return;
		}
		
		GD.Print($"MobHandler: Target is {target.Name} at position {target.Position}");
		
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		
		int successfulSpawns = 0;
		
		for (int i = 0; i < Amount; i++)
		{
			var roll = rng.RandiRange(0, 99); // 0-99 instead of 0-100
			var type = MobType.SmallFriendly;
			switch (roll)
			{
				case int n when n < 40:
					type = MobType.SmallFriendly;
					break;
				case int n when n < 80:
					type = MobType.SmallSkittish;
					break;
				case int n when n < 95:
					type = MobType.Medium;
					break;
				default:
					type = MobType.Large;
					break;
			}

			float x = rng.RandfRange(-radius, radius);
			float z = rng.RandfRange(-radius, radius);

			if (SpawnMob(type, location + new Vector3(x, 10, z), target))
			{
				successfulSpawns++;
			}
		}
		
		GD.Print($"MobHandler: Successfully spawned {successfulSpawns}/{Amount} mobs");
	}

	public bool SpawnMob(MobType mobType, Vector3 location, Node3D target)
	{
		GD.Print($"MobHandler: Attempting to spawn {mobType} mob at {location}");
		
		// Add a mob to the scene
		PackedScene mobScene = null;
		switch (mobType)
		{
			case MobType.SmallFriendly:
			case MobType.SmallSkittish:
				// Use the same small scene for both personalities
				mobScene = mobScene_smallFriendly ?? mobScene_smallSkittish ?? mobScene_medium;
				break;
			case MobType.Medium:
				mobScene = mobScene_medium;
				break;
			case MobType.Large:
				mobScene = mobScene_large;
				break;
		}

		if (mobScene == null)
		{
			GD.PrintErr($"MobHandler: No scene assigned for mob type {mobType}. Using medium as fallback.");
			mobScene = mobScene_medium;
			
			if (mobScene == null)
			{
				GD.PrintErr("MobHandler: No mob scenes available at all!");
				return false;
			}
		}

		try
		{
			GD.Print($"MobHandler: Instantiating {mobType} mob from scene {mobScene.ResourcePath}");
			Mob mob = mobScene.Instantiate<Mob>();
			if (mob == null)
			{
				GD.PrintErr($"MobHandler: Failed to instantiate mob scene for {mobType}");
				return false;
			}
			
			GD.Print($"MobHandler: Adding {mobType} mob to scene tree");
			AddChild(mob);
			
			// Find ground position before placing mob
			var groundPos = FindGroundPosition(location);
			GD.Print($"MobHandler: Setting {mobType} mob position to {groundPos}");
			mob.Position = groundPos;
			mob.target = target;
			
			// Set behavior parameters based on mob type
			mob.SetBehaviorParameters(mobType);
			
			// Ensure physics is enabled
			mob.GravityScale = 1.0f;
			mob.CanSleep = false;
			
			mobs.Add(mob);
			
			GD.Print($"MobHandler: Successfully spawned {mobType} mob at {groundPos}. Total mobs: {mobs.Count}");
			return true;
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"MobHandler: Error spawning {mobType} mob: {e.Message}");
			return false;
		}
	}

	private Vector3 FindGroundPosition(Vector3 location)
	{
		// Find a Node3D in the scene to get the 3D world from
		var node3D = GetTree().GetFirstNodeInGroup("terrain") as Node3D;
		if (node3D == null)
		{
			// Try to find any Node3D in the scene
			node3D = GetParent() as Node3D;
			if (node3D == null)
			{
				// Fallback: just return the original location
				return location;
			}
		}
		
		// Raycast down to find ground
		var spaceState = node3D.GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(
			location + Vector3.Up * 20f,
			location + Vector3.Down * 50f
		);
		query.CollideWithAreas = false;
		
		var result = spaceState.IntersectRay(query);
		
		if (result.Count > 0)
		{
			var groundPos = (Vector3)result["position"];
			return groundPos + Vector3.Up * 1f; // Spawn 1 unit above ground
		}
		
		// If no ground found, return original location
		return location;
	}

	// Debug and management methods
	public override void _Process(double delta)
	{
		// Clean up destroyed mobs
		mobs.RemoveAll(mob => !IsInstanceValid(mob));
	}

	public int GetActiveMobCount()
	{
		return mobs.Count(mob => IsInstanceValid(mob));
	}

	public void ClearAllMobs()
	{
		foreach (var mob in mobs)
		{
			if (IsInstanceValid(mob))
			{
				mob.QueueFree();
			}
		}
		mobs.Clear();
	}

	// Get debug info about all mobs
	public void PrintMobStatus()
	{
		GD.Print($"Active mobs: {GetActiveMobCount()}");
		int wandering = 0, flocking = 0, fleeing = 0, investigating = 0;
		
		foreach (var mob in mobs)
		{
			if (IsInstanceValid(mob))
			{
				switch (mob.currentState)
				{
					case MobState.Wandering: wandering++; break;
					case MobState.Flocking: flocking++; break;
					case MobState.Fleeing: fleeing++; break;
					case MobState.Investigating: investigating++; break;
				}
			}
		}
		
		GD.Print($"States - Wandering: {wandering}, Flocking: {flocking}, Fleeing: {fleeing}, Investigating: {investigating}");
	}
}
