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
	[Export]
	public float detectionRange = 15f;
	[Export]
	public float fleeDistance = 5f;
	[Export]
	public float investigateDistance = 8f;
	[Export]
	public float wanderRadius = 20f;
	
	public MobState currentState = MobState.Wandering;
	public MobPersonality personality = MobPersonality.Neutral;
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

	public override void _Ready()
	{
		rng.Randomize();
		
		// Initialize behavior components
		movementComponent = new MobMovement(this);
		behaviorStatesComponent = new MobBehaviorStates(this);
		personalityComponent = new MobPersonalityBehavior(this);
		terrainComponent = new MobTerrainInteraction(this);
		appearanceComponent = new MobAppearance(this);
		
		SetRandomWanderTarget();
		
		// Ensure physics properties are set
		GravityScale = 1.0f;
		CanSleep = false;
		
		// Find terrain manager
		terrainManager = terrainComponent.FindTerrainManager();
		
		// Set initial appearance based on personality
		SetPersonalityAppearance();
	}

	public override void _PhysicsProcess(double delta)
	{
		stateTimer += (float)delta;
		
		if (target == null)
		{
			return;
		}

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
			case MobState.Fleeing:
				desiredDirection = behaviorStatesComponent.HandleFleeing();
				break;
			case MobState.Investigating:
				desiredDirection = behaviorStatesComponent.HandleInvestigating();
				break;
			case MobState.Idle:
				desiredDirection = behaviorStatesComponent.HandleIdle();
				break;
		}

		// Apply terrain-aware movement
		if (desiredDirection.Length() > 0)
		{
			movementComponent.ApplyTerrainAwareMovement(desiredDirection, delta);
		}
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
	public void SetBehaviorParameters(MobType mobType)
	{
		personalityComponent.SetBehaviorParameters(mobType);
		
		// Apply visual appearance based on personality
		CallDeferred(nameof(SetPersonalityAppearance));
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
	}

	// Public accessors for behavior components
	public Vector3 GetWanderTarget() => wanderTarget;
	public float GetStateTimer() => stateTimer;
	public void ResetStateTimer() => stateTimer = 0f;
	public void SetLastInterestPoint(Node3D point) => lastInterestPoint = point;
	public float GetMaxSpeed() => maxSpeed;
	public void SetMaxSpeed(float speed) => maxSpeed = speed;
	public void SetPower(float newPower) => power = newPower;
}