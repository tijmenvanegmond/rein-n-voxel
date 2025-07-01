using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum MobFaction
{
	Player,     // Allied with player
	Neutral,    // Independent, no strong allegiances
	Wild,       // Natural creatures, avoid player
	Hostile     // Aggressive towards player (future use)
}

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
	[Export]
	public float detectionRange = 15f;
	[Export]
	public float fleeDistance = 5f;
	[Export]
	public float investigateDistance = 8f;	[Export]
	public float wanderRadius = 20f;	public MobState currentState = MobState.Wandering;
	public MobPersonality personality = MobPersonality.Neutral;
	public MobFaction faction = MobFaction.Neutral;
	public MobType mobType = MobType.Medium; // Default mob type
	private Vector3 wanderTarget;
	private float stateTimer = 0f;
	private Node3D lastInterestPoint;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public TerrainManager terrainManager;

	// Behavior components
	private MobMovement movementComponent;
	private MobBehaviorStates behaviorStatesComponent;
	private MobPersonalityBehavior personalityComponent;
	private MobTerrainInteraction terrainComponent;
	private MobAppearance appearanceComponent;
	
	// New complex systems
	public MobHealth healthComponent { get; private set; }
	public MobFood foodComponent { get; private set; }
	public MobCombat combatComponent { get; private set; }
	private MobHealthBar healthBar;
	
	// Additional properties
	public bool isDead => healthComponent?.isDead ?? false;
	private Timer despawnTimer;

	public override void _Ready()
	{
		rng.Randomize();
		
		// Initialize behavior components
		movementComponent = new MobMovement(this);
		behaviorStatesComponent = new MobBehaviorStates(this);
		personalityComponent = new MobPersonalityBehavior(this);
		terrainComponent = new MobTerrainInteraction(this);
		appearanceComponent = new MobAppearance(this);
		
		// Initialize new complex systems
		healthComponent = new MobHealth(this);
		foodComponent = new MobFood(this);
		combatComponent = new MobCombat(this);
		
		// Initialize despawn timer
		despawnTimer = new Timer();
		despawnTimer.WaitTime = 10.0f; // 10 seconds
		despawnTimer.OneShot = true;
		despawnTimer.Timeout += OnDespawnTimeout;
		AddChild(despawnTimer);
		
		// Wire up events
		SetupEventHandlers();
		
		SetRandomWanderTarget();
		
		// Ensure physics properties are set
		GravityScale = 1.0f;
		CanSleep = false;
		
		// Find terrain manager
		terrainManager = terrainComponent.FindTerrainManager();
		
		// Set initial appearance based on personality
		SetPersonalityAppearance();
		
		// Register with thread manager
		MobThreadManager.Instance.RegisterMob(this);
		
		// Create health bar
		CreateHealthBar();
	}
	
	private void CreateHealthBar()
	{
		var healthBarScene = GD.Load<PackedScene>("res://Scenes/mob_health_bar.tscn");
		if (healthBarScene != null)
		{
			healthBar = healthBarScene.Instantiate<MobHealthBar>();
			if (healthBar != null)
			{
				// Add to a UI layer (we'll need to find the main scene's UI layer)
				var mainScene = GetTree().CurrentScene;
				if (mainScene != null)
				{
					// Look for a UI layer or create one
					var uiLayer = mainScene.GetNodeOrNull<CanvasLayer>("UILayer");
					if (uiLayer == null)
					{
						uiLayer = new CanvasLayer();
						uiLayer.Name = "UILayer";
						uiLayer.Layer = 100; // High layer so it appears on top
						mainScene.AddChild(uiLayer);
					}
					
					uiLayer.AddChild(healthBar);
					healthBar.SetTarget(this);
					
					// Update with current values
					healthBar.UpdateHealth(healthComponent.currentHealth, healthComponent.maxHealth);
					healthBar.UpdateHunger(healthComponent.currentHunger, healthComponent.maxHunger);
				}
			}
		}
	}
	
	private void SetupEventHandlers()
	{
		// Connect health events
		healthComponent.OnDamageTaken += OnDamageTakenHandler;
		healthComponent.OnDeath += OnDeathHandler;
		healthComponent.OnHealthChanged += OnHealthChangedHandler;
		healthComponent.OnHungerChanged += OnHungerChangedHandler;
		
		// Connect combat events
		combatComponent.OnAttackPerformed += OnAttackPerformedHandler;
		combatComponent.OnThreatLevelChanged += OnThreatLevelChangedHandler;
		combatComponent.OnCombatStateChanged += OnCombatStateChangedHandler;
		
		// Connect food events
		foodComponent.OnFoodEaten += OnFoodEatenHandler;
	}
	
	private void OnDamageTakenHandler(float damage, DamageType damageType, Node3D source)
	{
		// When taking damage, add the source as a threat if it's not an ally
		if (source != null && source != this && damageType == DamageType.Combat)
		{
			if (source is Mob sourceMob && !IsAlliedWith(sourceMob))
			{
				combatComponent.AddThreat(sourceMob, damage * 2f); // Double threat for being attacked
				GD.Print($"{name} was attacked by {sourceMob.name} for {damage} damage - adding as threat!");
			}
			else if (source is PlayerController && faction == MobFaction.Hostile)
			{
				combatComponent.AddThreat(source, damage * 2f);
				GD.Print($"{name} was attacked by player for {damage} damage - adding as threat!");
			}
		}
	}
	
	private void OnDeathHandler()
	{
		GD.Print($"{name} has died!");
		// Clean up health bar
		if (healthBar != null)
		{
			healthBar.QueueFree();
		}
		
		// Start despawn timer
		if (despawnTimer != null)
		{
			GD.Print($"Starting despawn timer for {name} (10 seconds)");
			despawnTimer.Start();
		}
	}
	
	private void OnHealthChangedHandler(float currentHealth, float maxHealth)
	{
		if (healthBar != null)
		{
			healthBar.UpdateHealth(currentHealth, maxHealth);
		}
	}
	
	private void OnHungerChangedHandler(float currentHunger, float maxHunger)
	{
		if (healthBar != null)
		{
			healthBar.UpdateHunger(currentHunger, maxHunger);
		}
	}
	
	private void OnAttackPerformedHandler(Node3D target, float damage)
	{
		GD.Print($"{name} attacked {target.Name} for {damage} damage!");
	}
	
	private void OnThreatLevelChangedHandler(float newThreatLevel)
	{
		if (newThreatLevel > 10f)
		{
			GD.Print($"{name} threat level increased to {newThreatLevel:F1}");
		}
	}
	
	private void OnCombatStateChangedHandler(bool inCombat)
	{
		if (inCombat)
		{
			GD.Print($"{name} entered combat!");
		}
		// Remove spam: else { GD.Print($"{name} exited combat."); }
	}
	
	private void OnFoodEatenHandler(FoodType foodType, float nutritionGained)
	{
		GD.Print($"{name} ate {foodType} and gained {nutritionGained:F1} nutrition");
	}

	// ...existing code...
	
	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return; // Don't process dead mobs
		
		stateTimer += (float)delta;
		
		if (target == null)
		{
			return;
		}

		// Update new complex systems
		healthComponent.UpdateHealth(delta);
		foodComponent.UpdateEating(delta);
		combatComponent.UpdateCombat(delta);
		
		// Aggressive mobs actively look for targets
		ScanForThreatsAndTargets(delta);

		// Traditional single-threaded processing (threading handled by MobHandler)
		UpdateState(delta);
		ExecuteCurrentState(delta);
		
		// Apply terrain collision and ground detection
		terrainComponent.HandleTerrainInteraction();
		
		// Update physics properties based on state
		movementComponent.UpdatePhysicsProperties(currentState, personality);
	}

	private void UpdateState(double delta)
	{
		personalityComponent.UpdateStateWithPersonality(delta);
	}

	private void ExecuteCurrentState(double delta)
	{
		Vector3 desiredDirection = Vector3.Zero;

		switch (currentState)
		{
			case MobState.Wandering:
				desiredDirection = behaviorStatesComponent.HandleWandering();
				break;
			case MobState.Flocking:
				desiredDirection = behaviorStatesComponent.HandleFlocking();
				break;
			case MobState.Chasing:
				desiredDirection = behaviorStatesComponent.HandleChasing();
				break;
			case MobState.Fleeing:
				desiredDirection = behaviorStatesComponent.HandleFleeing();
				break;
			case MobState.Investigating:
				desiredDirection = behaviorStatesComponent.HandleInvestigating();
				break;
			case MobState.Idle:
				desiredDirection = behaviorStatesComponent.HandleIdle();
				break;
			case MobState.Seeking:
				desiredDirection = HandleSeeking();
				break;
			case MobState.Eating:
				desiredDirection = Vector3.Zero; // Don't move while eating
				break;
			case MobState.Combat:
				desiredDirection = combatComponent.GetCombatMovement();
				// Try to attack if we have a target in range
				if (combatComponent.currentTarget != null)
				{
					combatComponent.TryAttack();
				}
				break;
			case MobState.Attacking:
				desiredDirection = Vector3.Zero; // Don't move while attacking
				// The attack execution is handled by the combat component's UpdateAttack method
				break;
			case MobState.Dead:
				desiredDirection = Vector3.Zero; // Dead mobs don't move
				break;
		}

		// Apply terrain-aware movement
		if (desiredDirection.Length() > 0)
		{
			movementComponent.ApplyTerrainAwareMovement(desiredDirection, delta);
		}
	}
	
	private Vector3 HandleSeeking()
	{
		// Look for food and move toward it
		if (foodComponent.currentFoodTarget == null)
		{
			var nearestFood = foodComponent.FindNearestFood();
			if (nearestFood != null)
			{
				foodComponent.SetFoodTarget(nearestFood);
			}
			else
			{
				// No food found, return to wandering
				ChangeState(MobState.Wandering);
				return Vector3.Zero;
			}
		}

		var toFood = foodComponent.currentFoodTarget.Position - Position;
		var distance = toFood.Length();

		// If close enough to food, start eating
		if (distance <= foodComponent.eatingRange)
		{
			foodComponent.StartEating(foodComponent.currentFoodTarget);
			return Vector3.Zero;
		}

		// Move toward food
		return new Vector3(toFood.X, 0, toFood.Z).Normalized();
	}

	public void ChangeState(MobState newState)
	{
		currentState = newState;
		stateTimer = 0f;
		
		// Update appearance based on new state
		appearanceComponent.UpdateAppearanceBasedOnState(newState);
		
		// State entry logic
		switch (newState)
		{
			case MobState.Wandering:
				SetRandomWanderTarget();
				break;
		}
	}

	public void SetRandomWanderTarget()
	{
		wanderTarget = terrainComponent.GetSmartWanderTarget();
	}

	// Mob behavior tuning - can be adjusted per mob type
	public void SetBehaviorParameters(MobType newMobType)
	{
		mobType = newMobType; // Store the mob type
		personalityComponent.SetBehaviorParameters(newMobType);
		
		// Initialize new systems with mob type
		healthComponent.Initialize(newMobType);
		foodComponent.SetFoodPreferences(newMobType, personality);
		combatComponent.Initialize(newMobType, personality);
		
		// Adjust detection range based on mob type
		switch (newMobType)
		{
			case MobType.SmallFriendly:
			case MobType.SmallSkittish:
				detectionRange = 12f;
				break;
			case MobType.Medium:
				detectionRange = 15f;
				break;
			case MobType.Large:
				detectionRange = 18f;
				break;
			case MobType.Predator:
				detectionRange = 25f; // Much larger detection for predators
				break;
			case MobType.HostileSmall:
				detectionRange = 15f; // Slightly higher than normal small mobs
				break;
			case MobType.HostileMedium:
				detectionRange = 20f; // Higher than normal medium mobs
				break;
			case MobType.HostileLarge:
				detectionRange = 30f; // Very high detection range for large hostiles
				break;
			case MobType.DefenderFriendly:
				detectionRange = 22f; // Good detection range to spot threats
				break;
			case MobType.DefenderNeutral:
				detectionRange = 20f; // Moderate detection range
				break;
			case MobType.DefenderWild:
				detectionRange = 25f; // High detection range to protect territory
				break;
		}
		
		// Update vision area if it exists
		UpdateVisionArea();
		
		// Apply visual appearance based on personality
		CallDeferred(nameof(SetPersonalityAppearance));
	}
	
	private void UpdateVisionArea()
	{
		if (visionArea != null)
		{
			// Find the CollisionShape3D child of the vision area
			var collisionShape = visionArea.GetNode<CollisionShape3D>("CollisionShape3D");
			if (collisionShape != null && collisionShape.Shape is SphereShape3D sphereShape)
			{
				sphereShape.Radius = detectionRange;
				GD.Print($"Updated vision area radius to {detectionRange} for {name}");
			}
		}
	}

	// Group behavior coordination
	public void NotifyNearbyMobs(string eventType, Vector3 eventPosition)
	{
		personalityComponent.NotifyNearbyMobs(eventType, eventPosition);
	}

	public void OnMobEvent(string eventType, Vector3 eventPosition, Mob sender)
	{
		personalityComponent.OnMobEvent(eventType, eventPosition, sender);
	}

	// Debug method for checking mob state
	public string GetDebugInfo()
	{
		return $"State: {currentState}, Personality: {personality}, Speed: {LinearVelocity.Length():F1}, Target Distance: {(wanderTarget - Position).Length():F1}";
	}

	// Visual customization based on personality
	public void SetPersonalityAppearance()
	{
		appearanceComponent.SetPersonalityAppearance();
	}	// Public accessors for behavior components
	public Vector3 GetWanderTarget() => wanderTarget;
	public float GetStateTimer() => stateTimer;
	public void ResetStateTimer() => stateTimer = 0f;
	public void SetLastInterestPoint(Node3D point) => lastInterestPoint = point;
	public float GetMaxSpeed() => maxSpeed;
	public void SetMaxSpeed(float speed) => maxSpeed = speed;
	public void SetPower(float newPower) => power = newPower;

	// Faction management methods
	public void SetFaction(MobFaction newFaction)
	{
		if (faction != newFaction)
		{
			faction = newFaction;
			
			// Update appearance to reflect new faction
			CallDeferred(nameof(SetPersonalityAppearance));
			
			// Notify nearby mobs of faction change
			NotifyNearbyMobs("faction_changed", Position);
			
			GD.Print($"Mob faction changed to: {newFaction}");
		}
	}

	public bool IsAlliedWith(MobFaction otherFaction)
	{
		// Player and Neutral factions are generally allied
		// Wild and Hostile factions are generally not allied with Player
		return faction == otherFaction || 
		       (faction == MobFaction.Player && otherFaction == MobFaction.Neutral) ||
		       (faction == MobFaction.Neutral && otherFaction == MobFaction.Player);
	}

	public bool IsAlliedWith(Mob otherMob)
	{
		return IsAlliedWith(otherMob.faction);
	}
	
	private void OnMobDeath()
	{
		// Handle death - create carrion, notify nearby mobs, etc.
		NotifyNearbyMobs("mob_died", Position);
		
		// Optionally create carrion food item
		CreateCarrion();
		
		// Visual changes for death
		appearanceComponent.UpdateAppearanceBasedOnState(MobState.Dead);
		
		// Start despawn timer
		if (despawnTimer != null)
		{
			despawnTimer.Start();
			GD.Print($"Mob {name} will despawn in 10 seconds");
		}
	}
	
	private void OnDespawnTimeout()
	{
		GD.Print($"Despawning dead mob: {name}");
		
		// Remove from any tracking systems
		if (GetTree().HasGroup("mobs"))
		{
			RemoveFromGroup("mobs");
		}
		
		// Queue for deletion
		QueueFree();
	}
	
	private void OnHealthChanged(float currentHealth, float maxHealth)
	{
		// Update health bar
		healthBar?.UpdateHealth(currentHealth, maxHealth);
		
		// Update visual indicators based on health
		var healthPercent = currentHealth / maxHealth;
		if (healthPercent < 0.3f)
		{
			// Show low health visual effects
			appearanceComponent.UpdateAppearanceBasedOnState(currentState);
		}
	}
	
	private void OnHungerChanged(float currentHunger, float maxHunger)
	{
		// Update health bar
		healthBar?.UpdateHunger(currentHunger, maxHunger);
		
		// Update behavior based on hunger
		if (healthComponent.IsHungry() && currentState == MobState.Wandering)
		{
			// Start seeking food
			var nearestFood = foodComponent.FindNearestFood();
			if (nearestFood != null)
			{
				foodComponent.SetFoodTarget(nearestFood);
				ChangeState(MobState.Seeking);
			}
		}
	}
	
	private void OnStarvationChanged(bool isStarving)
	{
		if (isStarving)
		{
			// Desperate behavior when starving
			GD.Print($"Mob {name} is starving!");
		}
	}
	
	private void OnFoodEaten(FoodType foodType, float nutritionGained)
	{
		GD.Print($"Mob {name} ate {foodType} and gained {nutritionGained:F1} nutrition");
	}
	
	private void OnEatingStateChanged(bool isEating)
	{
		if (!isEating && currentState == MobState.Eating)
		{
			ChangeState(MobState.Wandering);
		}
	}
	
	private void OnAttackPerformed(Node3D target, float damage)
	{
		GD.Print($"Mob {name} attacked {target.Name} for {damage} damage");
	}
	
	private void OnCombatStateChanged(bool inCombat)
	{
		if (inCombat)
		{
			NotifyNearbyMobs("combat_started", Position);
		}
	}
	
	private void OnThreatLevelChanged(float newThreatLevel)
	{
		// Adjust behavior based on threat level
		if (newThreatLevel > 0 && currentState == MobState.Wandering)
		{
			ChangeState(MobState.Combat);
		}
	}
	
	private void CreateCarrion()
	{
		// TODO: Create a carrion food item at mob's position
		// This would require a food item scene/class
		GD.Print($"Carrion created from {name}");
	}
	
	// Destructor to clean up
	public override void _ExitTree()
	{
		// Clean up health bar
		if (healthBar != null && IsInstanceValid(healthBar))
		{
			healthBar.QueueFree();
		}
		
		// Unregister from thread manager
		MobThreadManager.Instance.UnregisterMob(this);
		
		base._ExitTree();
	}
	
	private void ScanForThreatsAndTargets(double delta)
	{
		// Only aggressive mobs actively hunt for targets
		if (personality != MobPersonality.Aggressive) return;
		
		// Don't scan too frequently - every 0.5 seconds
		if (stateTimer % 0.5f > 0.1f) return;
		
		// Get all bodies in vision range
		if (visionArea == null) return;
		
		var bodies = visionArea.GetOverlappingBodies();
		
		foreach (var body in bodies)
		{
			if (body == this) continue; // Skip self
			
			var distance = Position.DistanceTo(body.Position);
			
			// Check if this body should be considered a threat/target
			if (body is Mob otherMob)
			{
				// Add threat if the mob is hostile towards this other mob
				if (combatComponent.IsHostileTo(otherMob))
				{
					float threatAmount = CalculateThreatAmount(otherMob, distance);
					combatComponent.AddThreat(otherMob, threatAmount);
					GD.Print($"{name} (Aggressive) detected hostile target: {otherMob.name} at distance {distance:F1}");
				}
			}
			else if (body is PlayerController player)
			{
				// Hostile faction mobs attack players
				if (faction == MobFaction.Hostile)
				{
					float threatAmount = CalculateThreatAmount(player, distance);
					combatComponent.AddThreat(player, threatAmount);
					GD.Print($"{name} (Hostile) detected player target at distance {distance:F1}");
				}
			}
		}
	}
	
	private float CalculateThreatAmount(Node3D target, float distance)
	{
		// Base threat level
		float baseThreat = 30f;
		
		// Closer targets are more threatening
		float distanceMultiplier = Mathf.Max(0.1f, (combatComponent.aggressionRadius - distance) / combatComponent.aggressionRadius);
		
		// Target type modifiers
		if (target is Mob targetMob)
		{
			// Weaker mobs are easier targets
			if (targetMob.mobType == MobType.SmallFriendly || targetMob.mobType == MobType.SmallSkittish)
				baseThreat *= 1.5f; // Preferred prey
			else if (targetMob.mobType == MobType.Large)
				baseThreat *= 0.7f; // Harder target
		}
		
		return baseThreat * distanceMultiplier;
	}
}