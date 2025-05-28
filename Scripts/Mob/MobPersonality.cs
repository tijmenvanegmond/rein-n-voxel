using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MobPersonalityBehavior : RefCounted
{
    private Mob mob;

    public MobPersonalityBehavior(Mob mob)
    {
        this.mob = mob;
    }

    public void UpdateStateWithPersonality(double delta)
    {
        var bodies = mob.visionArea.GetOverlappingBodies();
        var nearbyMobs = new List<Mob>();
        var playerInRange = false;
        var playerDistance = float.MaxValue;

        foreach (var body in bodies)
        {
            if (body == mob) continue;
            
            var distance = body.Position.DistanceTo(mob.Position);
            
            if (body is Mob mobNearby)
            {
                nearbyMobs.Add(mobNearby);
            }
            else if (body is PlayerController)
            {
                playerInRange = true;
                playerDistance = distance;
            }
        }

        // Personality-based state transition logic
        switch (mob.currentState)
        {
            case MobState.Wandering:
                HandleWanderingTransitions(nearbyMobs, playerInRange, playerDistance);
                break;

            case MobState.Flocking:
                HandleFlockingTransitions(nearbyMobs, playerInRange, playerDistance);
                break;

            case MobState.Fleeing:
                HandleFleeingTransitions(playerInRange, playerDistance);
                break;

            case MobState.Investigating:
                HandleInvestigatingTransitions(playerInRange, playerDistance);
                break;

            case MobState.Idle:
                if (mob.GetStateTimer() > 2f)
                    mob.ChangeState(MobState.Wandering);
                break;
        }
    }

    private void HandleWanderingTransitions(List<Mob> nearbyMobs, bool playerInRange, float playerDistance)
    {
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly mobs approach the player instead of fleeing
            if (playerInRange && playerDistance < mob.investigateDistance)
                mob.ChangeState(MobState.Investigating);
            else if (nearbyMobs.Count >= 2) // Easier to start flocking
                mob.ChangeState(MobState.Flocking);
        }
        else if (mob.personality == MobPersonality.Skittish)
        {
            // Skittish mobs flee at greater distances
            if (playerInRange && playerDistance < mob.fleeDistance)
                mob.ChangeState(MobState.Fleeing);
            else if (nearbyMobs.Where(m => m.personality == MobPersonality.Skittish).Count() >= 2)
                mob.ChangeState(MobState.Flocking); // Only flock with other skittish mobs
        }
        else // Neutral personality
        {
            if (playerInRange && playerDistance < mob.fleeDistance)
                mob.ChangeState(MobState.Fleeing);
            else if (nearbyMobs.Count >= 3)
                mob.ChangeState(MobState.Flocking);
            else if (playerInRange && playerDistance < mob.investigateDistance)
                mob.ChangeState(MobState.Investigating);
        }
    }

    private void HandleFlockingTransitions(List<Mob> nearbyMobs, bool playerInRange, float playerDistance)
    {
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly mobs don't flee as easily when flocking
            if (playerInRange && playerDistance < mob.fleeDistance * 0.5f)
                mob.ChangeState(MobState.Fleeing);
            else if (nearbyMobs.Count < 1)
                mob.ChangeState(MobState.Wandering);
        }
        else if (mob.personality == MobPersonality.Skittish)
        {
            // Skittish mobs flee more readily
            if (playerInRange && playerDistance < mob.fleeDistance * 1.5f)
                mob.ChangeState(MobState.Fleeing);
            else if (nearbyMobs.Where(m => m.personality == MobPersonality.Skittish).Count() < 1)
                mob.ChangeState(MobState.Wandering);
        }
        else // Neutral
        {
            if (playerInRange && playerDistance < mob.fleeDistance)
                mob.ChangeState(MobState.Fleeing);
            else if (nearbyMobs.Count < 2)
                mob.ChangeState(MobState.Wandering);
        }
    }

    private void HandleFleeingTransitions(bool playerInRange, float playerDistance)
    {
        var safeDistance = mob.personality == MobPersonality.Skittish ? mob.detectionRange * 1.5f : mob.detectionRange;
        if (!playerInRange || playerDistance > safeDistance)
            mob.ChangeState(MobState.Wandering);
    }

    private void HandleInvestigatingTransitions(bool playerInRange, float playerDistance)
    {
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly mobs investigate longer and get closer
            if (playerInRange && playerDistance < mob.fleeDistance * 0.3f)
                mob.ChangeState(MobState.Fleeing);
            else if (mob.GetStateTimer() > 5f) // Investigate longer
                mob.ChangeState(MobState.Wandering);
        }
        else
        {
            if (playerInRange && playerDistance < mob.fleeDistance)
                mob.ChangeState(MobState.Fleeing);
            else if (mob.GetStateTimer() > 3f)
                mob.ChangeState(MobState.Wandering);
        }
    }

    public void SetBehaviorParameters(MobType mobType)
    {
        switch (mobType)
        {
            case MobType.SmallFriendly:
                mob.SetMaxSpeed(6f);
                mob.SetPower(8f);
                mob.fleeDistance = 4f;  // Less likely to flee from player
                mob.investigateDistance = 10f;  // More eager to investigate
                mob.wanderRadius = 15f;
                mob.personality = MobPersonality.Friendly;
                break;
            case MobType.SmallSkittish:
                mob.SetMaxSpeed(7f);  // Faster to escape
                mob.SetPower(8f);
                mob.fleeDistance = 12f;  // Flees from player at greater distance
                mob.investigateDistance = 3f;  // Less curious
                mob.wanderRadius = 15f;
                mob.personality = MobPersonality.Skittish;
                break;
            case MobType.Medium:
                mob.SetMaxSpeed(4f);
                mob.SetPower(6f);
                mob.fleeDistance = 6f;
                mob.investigateDistance = 8f;
                mob.wanderRadius = 20f;
                mob.personality = MobPersonality.Neutral;
                break;
            case MobType.Large:
                mob.SetMaxSpeed(2f);
                mob.SetPower(4f);
                mob.fleeDistance = 4f;
                mob.investigateDistance = 12f;
                mob.wanderRadius = 25f;
                mob.personality = MobPersonality.Neutral;
                break;
        }
    }

    public void NotifyNearbyMobs(string eventType, Vector3 eventPosition)
    {
        if (mob.visionArea == null) return;
        
        var nearbyBodies = mob.visionArea.GetOverlappingBodies();
        foreach (var body in nearbyBodies)
        {
            if (body is Mob nearbyMob && nearbyMob != mob)
            {
                nearbyMob.OnMobEvent(eventType, eventPosition, mob);
            }
        }
    }

    public void OnMobEvent(string eventType, Vector3 eventPosition, Mob sender)
    {
        switch (eventType)
        {
            case "player_spotted":
                if (mob.currentState == MobState.Wandering)
                {
                    mob.ChangeState(MobState.Investigating);
                    mob.SetLastInterestPoint(mob.target); // Look toward player
                }
                break;
            case "fleeing":
                if (mob.Position.DistanceTo(eventPosition) < 10f)
                {
                    mob.ChangeState(MobState.Fleeing);
                }
                break;
        }
    }
}
