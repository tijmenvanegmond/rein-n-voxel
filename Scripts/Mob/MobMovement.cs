using System;
using Godot;

public partial class MobMovement : RefCounted
{
    private Mob mob;
    private float power;
    private float maxSpeed;

    public MobMovement(Mob mob)
    {
        this.mob = mob;
        this.power = mob.power;
        this.maxSpeed = mob.GetMaxSpeed();
    }

    public void ApplyTerrainAwareMovement(Vector3 desiredDirection, double delta)
    {
        if (desiredDirection.Length() < 0.1f) return;
        
        // Check if path is clear
        var adjustedDirection = GetTerrainAdjustedDirection(desiredDirection);
        
        // Calculate target velocity (horizontal movement only)
        var targetVelocity = adjustedDirection.Normalized() * maxSpeed;
        
        // Get current horizontal velocity
        var currentHorizontalVel = new Vector3(mob.LinearVelocity.X, 0, mob.LinearVelocity.Z);
        
        // Calculate force needed to reach target velocity
        var velocityDifference = targetVelocity - currentHorizontalVel;
        var force = velocityDifference * power;
        
        // Apply horizontal force
        mob.ApplyCentralImpulse(new Vector3(force.X, 0, force.Z) * (float)delta);
        
        // Handle jumping over small obstacles
        if (ShouldJump(adjustedDirection))
        {
            ApplyJumpForce();
        }
    }

    private Vector3 GetTerrainAdjustedDirection(Vector3 desiredDirection)
    {
        var currentPos = mob.GlobalTransform.Origin;
        var checkDistance = 2.0f;
        var targetPos = currentPos + desiredDirection.Normalized() * checkDistance;
        
        // Raycast forward to check for obstacles
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            currentPos + Vector3.Up * 0.5f, 
            targetPos + Vector3.Up * 0.5f
        );
        
        var result = spaceState.IntersectRay(query);
        
        if (result.Count > 0)
        {
            // Hit an obstacle, try to go around it
            var hitNormal = (Vector3)result["normal"];
            
            // Calculate alternative directions (left and right of obstacle)
            var right = desiredDirection.Cross(Vector3.Up).Normalized();
            var leftDirection = desiredDirection + right * 0.7f;
            var rightDirection = desiredDirection - right * 0.7f;
            
            // Test both alternative directions
            if (IsPathClear(leftDirection))
            {
                return leftDirection.Normalized();
            }
            else if (IsPathClear(rightDirection))
            {
                return rightDirection.Normalized();
            }
            
            // If both are blocked, try to move perpendicular to obstacle
            var perpendicular = hitNormal.Cross(Vector3.Up).Normalized();
            return perpendicular;
        }
        
        return desiredDirection;
    }

    private bool IsPathClear(Vector3 direction)
    {
        var currentPos = mob.GlobalTransform.Origin;
        var checkDistance = 1.5f;
        var targetPos = currentPos + direction.Normalized() * checkDistance;
        
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            currentPos + Vector3.Up * 0.5f, 
            targetPos + Vector3.Up * 0.5f
        );
        
        var result = spaceState.IntersectRay(query);
        return result.Count == 0;
    }

    private bool ShouldJump(Vector3 direction)
    {
        // Check for low obstacles that can be jumped over
        var currentPos = mob.GlobalTransform.Origin;
        var checkPos = currentPos + direction.Normalized() * 1.0f;
        
        // Raycast down from slightly ahead and up
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            checkPos + Vector3.Up * 2.0f,
            checkPos + Vector3.Down * 0.5f
        );
        
        var result = spaceState.IntersectRay(query);
        
        if (result.Count > 0)
        {
            var hitPoint = (Vector3)result["position"];
            var heightDifference = hitPoint.Y - currentPos.Y;
            
            // Jump if there's a small step up (between 0.5 and 2.0 units)
            return heightDifference > 0.5f && heightDifference < 2.0f && IsOnGround();
        }
        
        return false;
    }

    private bool IsOnGround()
    {
        // Check if mob is on the ground
        var spaceState = mob.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            mob.GlobalTransform.Origin,
            mob.GlobalTransform.Origin + Vector3.Down * 1.1f
        );
        
        var result = spaceState.IntersectRay(query);
        return result.Count > 0;
    }

    private void ApplyJumpForce()
    {
        // Only jump if not already jumping
        if (Mathf.Abs(mob.LinearVelocity.Y) < 0.5f)
        {
            mob.ApplyCentralImpulse(Vector3.Up * power * 2.0f);
        }
    }

    public void UpdatePhysicsProperties(MobState currentState, MobPersonality personality)
    {
        // Adjust physics based on mob state and personality
        switch (currentState)
        {
            case MobState.Fleeing:
                // Increase speed when fleeing, more for skittish mobs
                var fleeMultiplier = personality == MobPersonality.Skittish ? 1.8f : 1.5f;
                if (mob.LinearVelocity.Length() > maxSpeed * fleeMultiplier)
                {
                    mob.LinearVelocity = mob.LinearVelocity.Normalized() * maxSpeed * fleeMultiplier;
                }
                break;
            case MobState.Investigating:
                // Friendly mobs can move faster when investigating, others move slower
                var investigateMultiplier = personality == MobPersonality.Friendly ? 0.8f : 0.6f;
                if (mob.LinearVelocity.Length() > maxSpeed * investigateMultiplier)
                {
                    mob.LinearVelocity = mob.LinearVelocity.Normalized() * maxSpeed * investigateMultiplier;
                }
                break;
            case MobState.Flocking:
                // Friendly mobs in flocking mode can move a bit faster to keep up with player
                var flockMultiplier = personality == MobPersonality.Friendly ? 1.2f : 1.0f;
                if (mob.LinearVelocity.Length() > maxSpeed * flockMultiplier)
                {
                    mob.LinearVelocity = mob.LinearVelocity.Normalized() * maxSpeed * flockMultiplier;
                }
                break;
            default:
                // Normal speed limiting
                if (mob.LinearVelocity.Length() > maxSpeed)
                {
                    mob.LinearVelocity = mob.LinearVelocity.Normalized() * maxSpeed;
                }
                break;
        }
        
        // Prevent excessive rotation
        mob.AngularVelocity = mob.AngularVelocity * 0.8f;
    }

    public bool CanReachTarget(Vector3 targetPosition)
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
}
