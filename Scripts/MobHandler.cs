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

	[Export]
	public PackedScene mobScene_predator { get; set; }


	public List<Mob> mobs = new List<Mob>();


	public void SpawnRandomMobs(int Amount, Vector3 location, Node3D target, float radius = 30f)
	{
		GD.Print($"MobHandler: Attempting to spawn {Amount} mobs at {location}");
		
		// Check if mob scenes are assigned
		if (mobScene_smallFriendly == null && mobScene_smallSkittish == null && mobScene_medium == null && mobScene_large == null && mobScene_predator == null)
		{
			GD.PrintErr("MobHandler: No mob scenes assigned! Please assign mob scene resources in the inspector.");
			return;
		}
		
		// For now, if we only have one small scene, use it for both types
		var hasSmallScene = mobScene_smallFriendly != null || mobScene_smallSkittish != null;
		GD.Print($"MobHandler: Mob scenes - Small: {hasSmallScene}, Medium: {mobScene_medium != null}, Large: {mobScene_large != null}, Predator: {mobScene_predator != null}");
		
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
			var roll = rng.RandiRange(0, 99); // 0-99
			var type = MobType.SmallFriendly;
			switch (roll)
			{
				case int n when n < 22: // Reduced slightly to make room for defenders
					type = MobType.SmallFriendly;
					break;
				case int n when n < 44: // Reduced slightly
					type = MobType.SmallSkittish;
					break;
				case int n when n < 62: // Reduced to make room
					type = MobType.Medium;
					break;
				case int n when n < 75:
					type = MobType.Large;
					break;
				case int n when n < 77: // Reduced from 4% to 2% (86-82 = 4% -> 77-75 = 2%)
					type = MobType.Predator; // Reduced predator spawn rate
					break;
				case int n when n < 82:
					type = MobType.HostileSmall; // 5% chance (reduced from 6%)
					break;
				case int n when n < 86:
					type = MobType.HostileMedium; // 4% chance (reduced from 5%)
					break;
				case int n when n < 89:
					type = MobType.HostileLarge; // 3% chance (same)
					break;
				case int n when n < 93:
					type = MobType.DefenderFriendly; // 4% chance - protects friendly faction
					break;
				case int n when n < 96:
					type = MobType.DefenderNeutral; // 3% chance - protects neutral faction
					break;
				default:
					type = MobType.DefenderWild; // 4% chance - protects wild faction
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
				mobScene = mobScene_smallFriendly ?? mobScene_medium;
				break;
			case MobType.SmallSkittish:
				mobScene = mobScene_smallSkittish ?? mobScene_medium;
				break;
			case MobType.Medium:
				mobScene = mobScene_medium;
				break;
			case MobType.Large:
				mobScene = mobScene_large;
				break;
			case MobType.Predator:
				mobScene = mobScene_predator;
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
			
			// Assign personality and faction based on mob type
			AssignPersonalityAndFaction(mob, mobType);
			
			// Re-initialize combat with the new personality and faction
			mob.combatComponent.Initialize(mobType, mob.personality);
			
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
		
		// Process all mobs using thread manager (will use single-thread when multithreading disabled)
		if (mobs.Count > 0)
		{
			MobThreadManager.Instance.ProcessMobsSync(delta);
		}
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

	public override void _Ready()
	{
		// Initialize the thread manager
		MobThreadManager.Instance.StartProcessing();
		
		GD.Print("MobHandler initialized with threading support");
	}

	private void AssignPersonalityAndFaction(Mob mob, MobType mobType)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		
		// Assign personality and faction based on mob type
		switch (mobType)
		{
			case MobType.SmallFriendly:
				mob.personality = MobPersonality.Friendly;
				mob.faction = MobFaction.Player; // Allied with player
				break;
				
			case MobType.SmallSkittish:
				mob.personality = MobPersonality.Skittish;
				mob.faction = MobFaction.Wild; // Natural, avoid conflict
				break;
				
			case MobType.Medium:
				// Medium mobs have varied personalities
				var roll = rng.RandiRange(0, 99);
				if (roll < 40)
				{
					mob.personality = MobPersonality.Neutral;
					mob.faction = MobFaction.Neutral;
				}
				else if (roll < 70)
				{
					mob.personality = MobPersonality.Friendly;
					mob.faction = MobFaction.Wild;
				}
				else
				{
					mob.personality = MobPersonality.Skittish;
					mob.faction = MobFaction.Wild;
				}
				break;
				
			case MobType.Large:
				// Large mobs are usually territorial but not aggressive
				if (rng.RandiRange(0, 99) < 70)
				{
					mob.personality = MobPersonality.Neutral;
					mob.faction = MobFaction.Wild;
				}
				else
				{
					mob.personality = MobPersonality.Aggressive;
					mob.faction = MobFaction.Wild;
				}
				break;
				
			case MobType.Predator:
				// Predators are always aggressive and hostile
				mob.personality = MobPersonality.Aggressive;
				mob.faction = MobFaction.Hostile; // Hostile to everyone
				mob.name = "Predator"; // Give them a distinct name
				break;
				
			case MobType.HostileSmall:
				// Small hostile creatures - aggressive and hostile
				mob.personality = MobPersonality.Aggressive;
				mob.faction = MobFaction.Hostile;
				mob.name = "Hostile Runt";
				break;
				
			case MobType.HostileMedium:
				// Medium hostile creatures - aggressive and hostile
				mob.personality = MobPersonality.Aggressive;
				mob.faction = MobFaction.Hostile;
				mob.name = "Hostile Warrior";
				break;
				
			case MobType.HostileLarge:
				// Large hostile creatures - aggressive and hostile
				mob.personality = MobPersonality.Aggressive;
				mob.faction = MobFaction.Hostile;
				mob.name = "Hostile Brute";
				break;
				
			case MobType.DefenderFriendly:
				// Defenders of friendly faction - protective but friendly
				mob.personality = MobPersonality.Friendly;
				mob.faction = MobFaction.Player;
				mob.name = "Guardian";
				break;
				
			case MobType.DefenderNeutral:
				// Defenders of neutral faction - protective but neutral
				mob.personality = MobPersonality.Neutral;
				mob.faction = MobFaction.Neutral;
				mob.name = "Sentinel";
				break;
				
			case MobType.DefenderWild:
				// Defenders of wild faction - protective of nature
				mob.personality = MobPersonality.Neutral;
				mob.faction = MobFaction.Wild;
				mob.name = "Warden";
				break;
		}
		
		GD.Print($"MobHandler: Assigned {mob.personality} personality and {mob.faction} faction to {mobType} mob");
	}
}
