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
}
