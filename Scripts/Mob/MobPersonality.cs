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
    }    private void HandleWanderingTransitions(List<Mob> nearbyMobs, bool playerInRange, float playerDistance)
    {
        // Check for predators first - all mobs except other predators flee from predators
        var nearbyPredators = nearbyMobs.Where(m => m.personality == MobPersonality.Aggressive).ToList();
        if (nearbyPredators.Count > 0 && mob.personality != MobPersonality.Aggressive)
        {
            var closestPredator = nearbyPredators.OrderBy(p => p.Position.DistanceTo(mob.Position)).First();
            var predatorDistance = closestPredator.Position.DistanceTo(mob.Position);
            if (predatorDistance < mob.detectionRange * 1.5f) // Flee from predators at greater distance
            {
                mob.ChangeState(MobState.Fleeing);
                mob.combatComponent.AddThreat(closestPredator, 100f); // High threat from predators
                NotifyNearbyMobs("predator_spotted", closestPredator.Position);
                return;
            }
        }
        
        // Predator hunting behavior
        if (mob.personality == MobPersonality.Aggressive)
        {
            var potentialPrey = nearbyMobs.Where(m => m.personality != MobPersonality.Aggressive && 
                                                     m.faction != MobFaction.Hostile).ToList();
            if (potentialPrey.Count > 0)
            {
                var closestPrey = potentialPrey.OrderBy(p => p.Position.DistanceTo(mob.Position)).First();
                var preyDistance = closestPrey.Position.DistanceTo(mob.Position);
                if (preyDistance < mob.investigateDistance)
                {
                    mob.ChangeState(MobState.Chasing);
                    mob.combatComponent.AddThreat(closestPrey, 50f);
                    // Force the combat system to select this as the target
                    mob.combatComponent.SetTarget(closestPrey);
                    return;
                }
            }
        }
        
        // Filter nearby mobs by faction for flocking decisions
        var sameFactionMobs = nearbyMobs.Where(m => m.faction == mob.faction).ToList();
        
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly mobs approach the player if they're allied, flee if not
            var isPlayerAllied = mob.faction == MobFaction.Player;
            
            if (playerInRange && playerDistance < mob.investigateDistance && isPlayerAllied)
                mob.ChangeState(MobState.Investigating);
            else if (playerInRange && playerDistance < mob.fleeDistance && !isPlayerAllied)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Count >= 2) // Easier to start flocking with same faction
                mob.ChangeState(MobState.Flocking);
        }
        else if (mob.personality == MobPersonality.Skittish)
        {
            // Skittish mobs flee at greater distances, regardless of faction alignment
            if (playerInRange && playerDistance < mob.fleeDistance)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Where(m => m.personality == MobPersonality.Skittish).Count() >= 2)
                mob.ChangeState(MobState.Flocking); // Only flock with other skittish mobs of same faction
        }
        else if (mob.personality == MobPersonality.Neutral)
        {
            var isPlayerAllied = mob.faction == MobFaction.Player;
            
            if (playerInRange && playerDistance < mob.fleeDistance && !isPlayerAllied)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Count >= 3)
                mob.ChangeState(MobState.Flocking);
            else if (playerInRange && playerDistance < mob.investigateDistance && isPlayerAllied)
                mob.ChangeState(MobState.Investigating);
        }
        // Aggressive mobs (predators) continue wandering unless they find prey (handled above)
    }    private void HandleFlockingTransitions(List<Mob> nearbyMobs, bool playerInRange, float playerDistance)
    {
        // Check for predators first - all mobs except other predators flee from predators
        var nearbyPredators = nearbyMobs.Where(m => m.personality == MobPersonality.Aggressive).ToList();
        if (nearbyPredators.Count > 0 && mob.personality != MobPersonality.Aggressive)
        {
            var closestPredator = nearbyPredators.OrderBy(p => p.Position.DistanceTo(mob.Position)).First();
            var predatorDistance = closestPredator.Position.DistanceTo(mob.Position);
            if (predatorDistance < mob.detectionRange * 1.5f)
            {
                mob.ChangeState(MobState.Fleeing);
                mob.combatComponent.AddThreat(closestPredator, 100f);
                NotifyNearbyMobs("predator_spotted", closestPredator.Position);
                return;
            }
        }
        
        // Filter nearby mobs by faction for flocking decisions
        var sameFactionMobs = nearbyMobs.Where(m => m.faction == mob.faction).ToList();
        var isPlayerAllied = mob.faction == MobFaction.Player;
        
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly mobs don't flee as easily when flocking, but behavior depends on faction
            if (playerInRange && playerDistance < mob.fleeDistance * 0.5f && !isPlayerAllied)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Count < 1) // Only count same-faction mobs for flocking
                mob.ChangeState(MobState.Wandering);
        }
        else if (mob.personality == MobPersonality.Skittish)
        {
            // Skittish mobs flee more readily, regardless of faction alignment
            if (playerInRange && playerDistance < mob.fleeDistance * 1.5f)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Where(m => m.personality == MobPersonality.Skittish).Count() < 1)
                mob.ChangeState(MobState.Wandering);
        }
        else if (mob.personality == MobPersonality.Neutral)
        {
            if (playerInRange && playerDistance < mob.fleeDistance && !isPlayerAllied)
                mob.ChangeState(MobState.Fleeing);
            else if (sameFactionMobs.Count < 2)
                mob.ChangeState(MobState.Wandering);
        }
        // Aggressive mobs (predators) don't flock
    }

    private void HandleFleeingTransitions(bool playerInRange, float playerDistance)
    {
        // Check if still being threatened by predators
        var predatorThreat = mob.combatComponent.threatList.Keys
            .OfType<Mob>()
            .Any(m => m.personality == MobPersonality.Aggressive && 
                      m.Position.DistanceTo(mob.Position) < mob.detectionRange * 2f);
        
        if (predatorThreat)
            return; // Keep fleeing from predators
        
        var safeDistance = mob.personality == MobPersonality.Skittish ? mob.detectionRange * 1.5f : mob.detectionRange;
        if (!playerInRange || playerDistance > safeDistance)
            mob.ChangeState(MobState.Wandering);
    }    private void HandleInvestigatingTransitions(bool playerInRange, float playerDistance)
    {
        var isPlayerAllied = mob.faction == MobFaction.Player;
        
        if (mob.personality == MobPersonality.Friendly)
        {
            // Friendly allied mobs investigate longer and get closer
            if (playerInRange && playerDistance < mob.fleeDistance * (isPlayerAllied ? 0.1f : 0.3f))
                mob.ChangeState(MobState.Fleeing);
            else if (mob.GetStateTimer() > (isPlayerAllied ? 8f : 5f)) // Allied mobs investigate longer
                mob.ChangeState(MobState.Wandering);
        }
        else
        {
            if (playerInRange && playerDistance < mob.fleeDistance && !isPlayerAllied)
                mob.ChangeState(MobState.Fleeing);
            else if (mob.GetStateTimer() > 3f)
                mob.ChangeState(MobState.Wandering);
        }
    }    public void SetBehaviorParameters(MobType mobType)
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
                mob.faction = MobFaction.Player; // Friendly mobs are allied with player by default
                break;
            case MobType.SmallSkittish:
                mob.SetMaxSpeed(7f);  // Faster to escape
                mob.SetPower(8f);
                mob.fleeDistance = 12f;  // Flees from player at greater distance
                mob.investigateDistance = 3f;  // Less curious
                mob.wanderRadius = 15f;
                mob.personality = MobPersonality.Skittish;
                mob.faction = MobFaction.Wild; // Skittish mobs are wild by default
                break;
            case MobType.Medium:
                mob.SetMaxSpeed(4f);
                mob.SetPower(6f);
                mob.fleeDistance = 6f;
                mob.investigateDistance = 8f;
                mob.wanderRadius = 20f;
                mob.personality = MobPersonality.Neutral;
                mob.faction = MobFaction.Neutral; // Medium mobs are neutral by default
                break;
            case MobType.Large:
                mob.SetMaxSpeed(2f);
                mob.SetPower(4f);
                mob.fleeDistance = 4f;
                mob.investigateDistance = 12f;
                mob.wanderRadius = 25f;
                mob.personality = MobPersonality.Neutral;
                mob.faction = MobFaction.Neutral; // Large mobs are neutral by default
                break;
            case MobType.Predator:
                mob.SetMaxSpeed(5f);  // Fast and dangerous
                mob.SetPower(10f);    // Strong
                mob.fleeDistance = 2f;  // Rarely flees
                mob.investigateDistance = 20f;  // Large detection range
                mob.wanderRadius = 40f;  // Roams widely
                mob.personality = MobPersonality.Aggressive;
                mob.faction = MobFaction.Hostile; // Predators are hostile
                break;
            case MobType.HostileSmall:
                mob.SetMaxSpeed(8f);  // Very fast and aggressive
                mob.SetPower(12f);    // Stronger than normal small mobs
                mob.fleeDistance = 1f;  // Almost never flees
                mob.investigateDistance = 15f;  // Actively seeks targets
                mob.wanderRadius = 25f;  // Roams to find targets
                mob.personality = MobPersonality.Aggressive;
                mob.faction = MobFaction.Hostile;
                break;
            case MobType.HostileMedium:
                mob.SetMaxSpeed(6f);  // Faster than normal medium mobs
                mob.SetPower(15f);    // Much stronger
                mob.fleeDistance = 1f;  // Almost never flees
                mob.investigateDistance = 20f;  // Large detection range
                mob.wanderRadius = 30f;  // Roams widely
                mob.personality = MobPersonality.Aggressive;
                mob.faction = MobFaction.Hostile;
                break;
            case MobType.HostileLarge:
                mob.SetMaxSpeed(4f);  // Faster than normal large mobs
                mob.SetPower(25f);    // Very powerful
                mob.fleeDistance = 0f;  // Never flees
                mob.investigateDistance = 30f;  // Massive detection range
                mob.wanderRadius = 50f;  // Roams very widely
                mob.personality = MobPersonality.Aggressive;
                mob.faction = MobFaction.Hostile;
                break;
            case MobType.DefenderFriendly:
                mob.SetMaxSpeed(7f);  // Fast enough to respond to threats
                mob.SetPower(18f);    // Strong enough to defend
                mob.fleeDistance = 3f;  // Will retreat if severely wounded
                mob.investigateDistance = 25f;  // Large awareness for threats
                mob.wanderRadius = 35f;  // Patrols a moderate area
                mob.personality = MobPersonality.Friendly;
                mob.faction = MobFaction.Player;
                break;
            case MobType.DefenderNeutral:
                mob.SetMaxSpeed(6f);  // Moderate speed
                mob.SetPower(20f);    // Strong defender
                mob.fleeDistance = 2f;  // Rarely flees
                mob.investigateDistance = 22f;  // Good awareness
                mob.wanderRadius = 30f;  // Moderate patrol range
                mob.personality = MobPersonality.Neutral;
                mob.faction = MobFaction.Neutral;
                break;
            case MobType.DefenderWild:
                mob.SetMaxSpeed(8f);  // Fast and agile
                mob.SetPower(22f);    // Very strong
                mob.fleeDistance = 1f;  // Almost never flees
                mob.investigateDistance = 28f;  // High awareness of territory
                mob.wanderRadius = 45f;  // Large patrol range
                mob.personality = MobPersonality.Neutral;
                mob.faction = MobFaction.Wild;
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
    }    public void OnMobEvent(string eventType, Vector3 eventPosition, Mob sender)
    {
        // Predator events are always responded to by non-predators
        var shouldRespond = eventType.Contains("predator") ||
                           sender.faction == mob.faction || 
                           mob.faction == MobFaction.Neutral || 
                           sender.faction == MobFaction.Neutral;
        
        if (!shouldRespond && eventType != "faction_changed") return;
        
        switch (eventType)
        {
            case "player_spotted":
                if (mob.currentState == MobState.Wandering)
                {
                    // Faction influences response to player spotting
                    if (mob.faction == MobFaction.Player)
                    {
                        mob.ChangeState(MobState.Investigating); // Allied mobs investigate friendly
                    }
                    else if (mob.faction == MobFaction.Wild || mob.faction == MobFaction.Hostile)
                    {
                        mob.ChangeState(MobState.Fleeing); // Wild/hostile mobs flee
                    }
                    else // Neutral
                    {
                        mob.ChangeState(MobState.Investigating); // Neutral mobs investigate cautiously
                    }
                    mob.SetLastInterestPoint(mob.target); // Look toward player
                }
                break;
            case "predator_spotted":
                // All non-predator mobs flee from predators
                if (mob.personality != MobPersonality.Aggressive && 
                    mob.Position.DistanceTo(eventPosition) < mob.detectionRange * 2f)
                {
                    mob.ChangeState(MobState.Fleeing);
                    // Spread the panic to other nearby mobs
                    NotifyNearbyMobs("predator_panic", eventPosition);
                }
                break;
            case "predator_panic":
                // Secondary panic spreading - weaker but still causes some mobs to flee
                if (mob.personality != MobPersonality.Aggressive && 
                    mob.Position.DistanceTo(eventPosition) < mob.detectionRange &&
                    (mob.personality == MobPersonality.Skittish || GD.Randf() < 0.3f))
                {
                    mob.ChangeState(MobState.Fleeing);
                }
                break;
            case "fleeing":
                if (mob.Position.DistanceTo(eventPosition) < 10f)
                {
                    // Panic spreads more easily within same faction
                    var panicDistance = sender.faction == mob.faction ? 15f : 8f;
                    if (mob.Position.DistanceTo(eventPosition) < panicDistance)
                    {
                        mob.ChangeState(MobState.Fleeing);
                    }
                }
                break;
            case "faction_changed":
                // When a nearby mob changes faction, re-evaluate current state
                if (mob.Position.DistanceTo(eventPosition) < mob.detectionRange)
                {
                    // If currently flocking and the sender was in our flock, reconsider
                    if (mob.currentState == MobState.Flocking)
                    {
                        mob.ResetStateTimer(); // Reset timer to trigger transition check sooner
                    }
                }
                break;
        }
    }
}
