using System.Collections.Generic;
using Godot;

public enum MobType
{
	Small,
	Medium,
	Large
}

public partial class MobHandler : Node
{

	[Export]
	public PackedScene mobScene_small { get; set; }

	[Export]
	public PackedScene mobScene_medium { get; set; }

	[Export]
	public PackedScene mobScene_large { get; set; }


	public List<Mob> mobs = new List<Mob>();


	public void SpawnRandomMobs(int Amount, Vector3 location, Node3D target, float radius = 30f)
	{
		var rng = new RandomNumberGenerator();
		for (int i = 0; i < Amount; i++)
		{
			var roll = rng.RandiRange(0, 100);
			var type = MobType.Small;
			switch (roll)
			{
				case int n when n < 90:
					type = MobType.Small;
					break;
				case int n when n < 97:
					type = MobType.Medium;
					break;
				case int n when n < 100:
					type = MobType.Large;
					break;
			}

			float x = rng.RandfRange(-radius, radius);
			float z = rng.RandfRange(-radius, radius);

			SpawnMob(type, location + new Vector3(x, 10, z), target);
		}
	}

	public void SpawnMob(MobType mobType, Vector3 location, Node3D target)
	{
		// Add a mob to the scene
		PackedScene mobScene = mobScene_medium;
		switch (mobType)
		{
			case MobType.Small:
				mobScene = mobScene_small;
				break;
			case MobType.Medium:
				mobScene = mobScene_medium;
				break;
			case MobType.Large:
				mobScene = mobScene_large;
				break;
		}

		Mob mob = mobScene.Instantiate<Mob>();
		AddChild(mob);
		mob.Position = location;
		mob.target = target;
	}
}
