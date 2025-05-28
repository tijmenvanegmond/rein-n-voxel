using Godot;

public enum MobState
{
	Wandering,
	Flocking,
	Chasing,
	Fleeing,
	Investigating,
	Idle
}

public enum MobPersonality
{
	Friendly,    // Approaches player, flocks with player
	Skittish,    // Runs from player, flocks with other skittish mobs
	Neutral      // Default behavior for medium/large mobs
}

public enum MobType
{
	SmallFriendly,  // Approaches and flocks with player
	SmallSkittish,  // Runs away from player and flocks together
	Medium,
	Large
}
