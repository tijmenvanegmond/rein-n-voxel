using System;
using Godot;

public partial class MobTerrainInteraction : RefCounted
{
    private Mob mob;
    private RandomNumberGenerator rng;

    public MobTerrainInteraction(Mob mob)
    {
        this.mob = mob;
        this.rng = new RandomNumberGenerator();
        this.rng.Randomize();
    }

    public void HandleTerrainInteraction()
    {
        // Check if mob has fallen below the world
        if (mob.Position.Y < -50)
        {
            // Respawn at a safe location
            mob.Position = new Vector3(mob.Position.X, 20, mob.Position.Z);
            mob.LinearVelocity = Vector3.Zero;
            GD.Print($"Mob respawned from falling below world at {mob.Position}");
        }
        
        // Limit fall speed to prevent excessive velocity
        if (mob.LinearVelocity.Y < -25f)
        {
            mob.LinearVelocity = new Vector3(mob.LinearVelocity.X, -25f, mob.LinearVelocity.Z);
        }
        
        // Check if mob is stuck (not moving for too long)
        if (mob.LinearVelocity.Length() < 0.1f && mob.currentState != MobState.Idle)
        {
            // Use a separate stuck timer instead of stateTimer
            if (mob.GetStateTimer() > 5f) // Stuck for 5 seconds
            {
                // Give a small random impulse to unstuck
                var randomDirection = new Vector3(
                    rng.RandfRange(-1f, 1f),
                    0,
                    rng.RandfRange(-1f, 1f)
                ).Normalized();
                mob.ApplyCentralImpulse(randomDirection * mob.power * 0.5f);
                mob.SetRandomWanderTarget(); // Set new target
                mob.ResetStateTimer(); // Reset timer
            }
        }
    }

    public Vector3 FindNearestGroundPosition(Vector3 targetPos)
    {
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        
        // Raycast down from high above the target position
        var query = PhysicsRayQueryParameters3D.Create(
            targetPos + Vector3.Up * 20f,
            targetPos + Vector3.Down * 10f
        );
        
        var result = spaceState.IntersectRay(query);
        
        if (result.Count > 0)
        {
            var hitPoint = (Vector3)result["position"];
            return hitPoint + Vector3.Up * 0.5f; // Slightly above ground
        }
        
        // If no ground found, return original position
        return mob.Position;
    }

    public Vector3 GetSmartWanderTarget()
    {
        int attempts = 0;
        Vector3 bestTarget = mob.Position;
        
        while (attempts < 8) // Try up to 8 different directions
        {
            var angle = rng.Randf() * Mathf.Tau;
            var distance = rng.RandfRange(8f, mob.wanderRadius);
            
            var potentialTarget = mob.Position + new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
            
            // Find ground position for this target
            var groundTarget = FindNearestGroundPosition(potentialTarget);
            
            // Check if we can reach this target
            if (CanReachTarget(groundTarget) && Mathf.Abs(groundTarget.Y - mob.Position.Y) < 5f)
            {
                return groundTarget;
            }
            
            bestTarget = groundTarget; // Keep last valid ground position as backup
            attempts++;
        }
        
        return bestTarget;
    }

    private bool CanReachTarget(Vector3 targetPosition)
    {
        var direction = (targetPosition - mob.Position).Normalized();
        var distance = mob.Position.DistanceTo(targetPosition);
        var checkDistance = Mathf.Min(distance, 5f); // Check up to 5 units ahead
        
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            mob.Position + Vector3.Up * 0.5f,
            mob.Position + Vector3.Up * 0.5f + direction * checkDistance
        );
        query.CollideWithAreas = false;
        
        var result = spaceState.IntersectRay(query);
        return result.Count == 0; // Can reach if no obstacles
    }

    public TerrainManager FindTerrainManager()
    {
        // Try to find terrain manager in the scene
        var terrainManager = mob.GetNode<TerrainManager>("../../Terrain");
        if (terrainManager == null)
        {
            // Try alternative paths
            terrainManager = mob.GetTree().GetFirstNodeInGroup("terrain_manager") as TerrainManager;
            if (terrainManager == null)
            {
                // Try finding by class name
                terrainManager = mob.GetTree().GetFirstNodeInGroup("TerrainManager") as TerrainManager;
            }
        }

        if (terrainManager == null)
        {
            GD.PrintErr("Mob: Could not find TerrainManager!");
        }
        else
        {
            GD.Print("Mob: Found TerrainManager successfully");
        }

        return terrainManager;
    }
}
