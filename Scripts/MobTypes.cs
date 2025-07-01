using Godot;

public enum MobState
{
	Wandering,
	Flocking,
	Chasing,
	Fleeing,
	Investigating,
	Idle,
	Seeking,       // Looking for food
	Eating,        // Consuming food
	Combat,        // In combat mode
	Attacking,     // Currently attacking
	Dead           // Mob has died
}

public enum MobPersonality
{
	Friendly,    // Approaches player, flocks with player
	Skittish,    // Runs from player, flocks with other skittish mobs
	Neutral,     // Default behavior for medium/large mobs
	Aggressive   // Hunts other mobs, causes fear
}

public enum MobType
{
	SmallFriendly,  // Approaches and flocks with player
	SmallSkittish,  // Runs away from player and flocks together
	Medium,
	Large,
	Predator,       // Dangerous mob that hunts others
	HostileSmall,   // Small hostile creatures
	HostileMedium,  // Medium hostile creatures
	HostileLarge,   // Large hostile creatures
	DefenderFriendly, // Protects friendly mobs and player
	DefenderNeutral,  // Protects neutral mobs from hostiles
	DefenderWild      // Protects wild mobs from predators
}
