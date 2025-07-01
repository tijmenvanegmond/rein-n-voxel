using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MobBehaviorStates : RefCounted
{
    private Mob mob;
    private RandomNumberGenerator rng;

    public MobBehaviorStates(Mob mob)
    {
        this.mob = mob;
        this.rng = new RandomNumberGenerator();
        this.rng.Randomize();
    }

    public Vector3 HandleWandering()
    {
        var toTarget = mob.GetWanderTarget() - mob.Position;
        
        // If close to target, set new random target
        if (toTarget.Length() < 2f)
        {
            mob.SetRandomWanderTarget();
            toTarget = mob.GetWanderTarget() - mob.Position;
        }
        
        // Only use horizontal movement for wandering
        var horizontalDirection = new Vector3(toTarget.X, 0, toTarget.Z).Normalized();
        return horizontalDirection;
    }    public Vector3 HandleFlocking()
    {
        var bodies = mob.visionArea.GetOverlappingBodies();
        var directions = new List<Vector3>();
        var mobRepulsionDistanceSq = 8f;

        // Get nearby mobs, filtering by both personality and faction
        var nearbyMobs = bodies.OfType<Mob>().Where(m => m != mob);
        
        if (mob.personality == MobPersonality.Skittish)
        {
            // Skittish mobs prefer to flock with other skittish mobs of the same faction
            nearbyMobs = nearbyMobs.Where(m => m.personality == MobPersonality.Skittish && m.faction == mob.faction);
        }
        else
        {
            // Other personalities flock with same faction members
            nearbyMobs = nearbyMobs.Where(m => m.faction == mob.faction);
        }
        
        nearbyMobs = nearbyMobs.Take(7);

        // Handle mob-to-mob flocking
        foreach (var mobNearby in nearbyMobs)
        {
            var distSq = mobNearby.Position.DistanceSquaredTo(mob.Position);
            var toMob = mobNearby.Position - mob.Position;
            
            if (distSq < mobRepulsionDistanceSq)
            {
                // Separation - push away
                directions.Add(-toMob.Normalized() * 0.8f);
            }
            else
            {
                // Cohesion - move toward group
                directions.Add(toMob.Normalized() * 0.3f);
            }
            
            // Alignment - match velocity
            if (mobNearby.LinearVelocity.Length() > 0.1f)
            {
                var horizontalVel = new Vector3(mobNearby.LinearVelocity.X, 0, mobNearby.LinearVelocity.Z);
                if (horizontalVel.Length() > 0.1f)
                {
                    directions.Add(horizontalVel.Normalized() * 0.4f);
                }
            }
        }

        // Handle player interaction based on personality and faction
        var playerBody = bodies.OfType<PlayerController>().FirstOrDefault();
        if (playerBody != null)
        {
            var toPlayer = playerBody.Position - mob.Position;
            var playerDistance = toPlayer.Length();
            
            // Check if player and mob are allied (same faction)
            var isPlayerAllied = mob.faction == MobFaction.Player;
            
            if (mob.personality == MobPersonality.Friendly && isPlayerAllied)
            {
                // Friendly allied mobs are attracted to the player but maintain some distance
                if (playerDistance > 6f)
                {
                    // Move toward player if far away
                    directions.Add(toPlayer.Normalized() * 0.5f);
                }
                else if (playerDistance < 3f)
                {
                    // Don't get too close
                    directions.Add(-toPlayer.Normalized() * 0.3f);
                }
            }
            else if (mob.personality == MobPersonality.Skittish || !isPlayerAllied)
            {
                // Skittish mobs or non-allied mobs are repelled by the player
                if (playerDistance < mob.fleeDistance)
                {
                    directions.Add(-toPlayer.Normalized() * 1.2f);
                }
            }
            // Neutral allied mobs ignore the player in flocking mode
        }

        if (directions.Count == 0)
            return HandleWandering();

        var averageDirection = Vector3.Zero;
        foreach (var dir in directions)
        {
            averageDirection += dir;
        }
        
        var result = averageDirection / directions.Count;
        // Keep movement horizontal
        return new Vector3(result.X, 0, result.Z).Normalized();
    }

    public Vector3 HandleFleeing()
    {
        var bodies = mob.visionArea.GetOverlappingBodies();
        var playerBody = bodies.OfType<PlayerController>().FirstOrDefault();
        
        if (playerBody != null)
        {
            var fleeDirection = mob.Position - playerBody.Position;
            // Keep movement horizontal
            var horizontalFlee = new Vector3(fleeDirection.X, 0, fleeDirection.Z).Normalized();
            return horizontalFlee;
        }
        
        return HandleWandering();
    }

    public Vector3 HandleInvestigating()
    {
        var bodies = mob.visionArea.GetOverlappingBodies();
        var playerBody = bodies.OfType<PlayerController>().FirstOrDefault();
        
        if (playerBody != null)
        {
            var toPlayer = playerBody.Position - mob.Position;
            var playerDistance = toPlayer.Length();
            
            // Look at player
            var lookDirection = playerBody.GlobalTransform.Origin - mob.GlobalTransform.Origin;
            if (lookDirection.Length() > 0.1f)
            {
                mob.LookAt(mob.GlobalTransform.Origin + lookDirection, Vector3.Up);
            }
            
            if (mob.personality == MobPersonality.Friendly)
            {
                // Friendly mobs approach the player while investigating
                var horizontalDirection = new Vector3(toPlayer.X, 0, toPlayer.Z).Normalized();
                var approachSpeed = playerDistance > 4f ? 0.8f : 0.3f; // Slow down when close
                return horizontalDirection * approachSpeed;
            }
            else
            {
                // Neutral and skittish mobs investigate from a distance
                if (playerDistance > 6f)
                {
                    var horizontalDirection = new Vector3(toPlayer.X, 0, toPlayer.Z).Normalized();
                    return horizontalDirection * 0.3f; // Move slowly toward player
                }
                else
                {
                    // Stay at current distance, maybe circle around
                    var perpendicular = new Vector3(-toPlayer.Z, 0, toPlayer.X).Normalized();
                    return perpendicular * 0.2f;
                }
            }
        }
        
        return Vector3.Zero;
    }

    public Vector3 HandleIdle()
    {
        return Vector3.Zero; // No movement when idle
    }
    
    public Vector3 HandleSeeking()
    {
        // Handle food seeking behavior
        if (mob.foodComponent.currentFoodTarget != null)
        {
            var toFood = mob.foodComponent.currentFoodTarget.Position - mob.Position;
            var distance = toFood.Length();
            
            // If close enough to food, transition to eating
            if (distance < 1.5f)
            {
                mob.foodComponent.StartEating(mob.foodComponent.currentFoodTarget);
                mob.ChangeState(MobState.Eating);
                return Vector3.Zero;
            }
            
            // Move toward food
            return new Vector3(toFood.X, 0, toFood.Z).Normalized();
        }
        else
        {
            // No food target, look for nearby food
            var nearbyFood = FindNearbyFood();
            if (nearbyFood != null)
            {
                mob.foodComponent.SetFoodTarget(nearbyFood);
                var toFood = nearbyFood.Position - mob.Position;
                return new Vector3(toFood.X, 0, toFood.Z).Normalized();
            }
            else
            {
                // No food found, return to wandering
                mob.ChangeState(MobState.Wandering);
                return Vector3.Zero;
            }
        }
    }
    
    private FoodItem FindNearbyFood()
    {
        // Look for food items in vision area
        var bodies = mob.visionArea.GetOverlappingBodies();
        var nearbyFoodItems = bodies.OfType<FoodItem>()
            .Where(food => !food.isConsumed)
            .Where(food => mob.foodComponent.CanEat(food.foodType))
            .OrderBy(food => food.Position.DistanceTo(mob.Position))
            .ToList();
            
        return nearbyFoodItems.FirstOrDefault();
    }

    public Vector3 HandleChasing()
    {
        // Predator chasing behavior - pursue prey aggressively
        if (mob.combatComponent.currentTarget != null)
        {
            var targetMob = mob.combatComponent.currentTarget as Mob;
            if (targetMob != null && !targetMob.isDead)
            {
                var toPrey = targetMob.Position - mob.Position;
                var distance = toPrey.Length();
                
                // If close enough to attack, switch to combat
                if (distance <= mob.combatComponent.attackRange * 1.2f)
                {
                    mob.ChangeState(MobState.Combat);
                    return Vector3.Zero;
                }
                
                // Chase the prey aggressively
                var horizontalDirection = new Vector3(toPrey.X, 0, toPrey.Z).Normalized();
                return horizontalDirection;
            }
        }
        
        // No valid target, return to wandering
        mob.ChangeState(MobState.Wandering);
        return Vector3.Zero;
    }
}
