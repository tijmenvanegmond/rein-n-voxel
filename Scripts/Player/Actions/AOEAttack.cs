using Godot;
using System.Collections.Generic;

public partial class AOEAttack : PlayerAction
{
    private float aoeRadius = 12f; // Increased radius of the AOE sphere
    private float pushbackForce = 1500f; // Force applied to pushback
    private float damage = 50f; // Base damage
    private PackedScene explosionEffectScene; // Visual effect (optional)
    
    public AOEAttack()
    {
        coolDownMs = 3000; // 3 second cooldown
    }    public override void DoAction(PlayerController target)
    {
        GD.Print("AOEAttack.DoAction called!");
          var timeBetween = Time.GetTicksMsec() - lastUsed;
        if (timeBetween < (ulong)coolDownMs)
        {
            GD.Print($"AOE attack on cooldown, time remaining: {(ulong)coolDownMs - timeBetween}ms");
            return;
        }        // Get the player position from the MovementController which is the actual CharacterBody3D
        Vector3 playerPosition = Vector3.Zero;
        
        if (target.movementController != null)
        {
            playerPosition = target.movementController.GlobalPosition;
            GD.Print($"Using movement controller position: {playerPosition}");
        }
        else
        {
            // Fallback to PlayerController position if MovementController is not available
            playerPosition = target.GlobalPosition;
            GD.Print($"Fallback to PlayerController position: {playerPosition}");
            
            // If still zero, try getting from the collider
            if (playerPosition == Vector3.Zero)
            {
                var collider = target.GetNode<CollisionShape3D>("CollisionShape3D");
                if (collider != null)
                {
                    playerPosition = collider.GlobalPosition;
                    GD.Print($"Using collider position: {playerPosition}");
                }
            }
        }
        
        GD.Print($"AOE Attack triggered at player position: {playerPosition}");
        
        // Find all mobs within AOE radius
        var affectedTargets = new List<Node3D>();
        
        // Get all mobs in the scene
        var mobHandler = target.GetNode<MobHandler>("/root/MobHandler");
        if (mobHandler != null)
        {
            foreach (var mob in mobHandler.mobs)
            {
                if (mob != null && IsInstanceValid(mob))
                {
                    var distance = playerPosition.DistanceTo(mob.GlobalPosition);
                    if (distance <= aoeRadius)
                    {
                        affectedTargets.Add(mob);
                    }
                }
            }
        }
        
        // Simple additional check for other potential targets in the scene
        // (This is a simplified version that focuses on mobs)
        var scene = target.GetTree().CurrentScene;
        var allNodes = scene.GetChildren();
        
        foreach (Node node in allNodes)
        {
            if (node is Node3D node3D && node3D != target && !affectedTargets.Contains(node3D))
            {
                var distance = playerPosition.DistanceTo(node3D.GlobalPosition);
                if (distance <= aoeRadius)
                {
                    // Only add certain types that we want to affect
                    if (node3D is RigidBody3D || node3D is CharacterBody3D)
                    {
                        affectedTargets.Add(node3D);
                    }
                }
            }
        }
        
        // Apply damage and pushback
        foreach (var affected in affectedTargets)
        {
            ApplyAOEEffects(affected, playerPosition);
        }        // Create visual effect
        CreateVisualEffect(playerPosition, target);
        GD.Print($"Created visual effect at position: {playerPosition}");
        
        // Print feedback
        GD.Print($"AOE Attack hit {affectedTargets.Count} targets!");
        
        base.DoAction(target);
    }
    
    private void ApplyAOEEffects(Node3D target, Vector3 sourcePosition)
    {
        // Calculate direction from source to target
        var direction = (target.GlobalPosition - sourcePosition).Normalized();
        var distance = sourcePosition.DistanceTo(target.GlobalPosition);
        
        // Calculate falloff based on distance (closer = stronger effect)
        var falloff = 1f - (distance / aoeRadius);
        falloff = Mathf.Max(0f, falloff);
          // Apply damage if target is a mob
        if (target is Mob mob)
        {
            var actualDamage = damage * falloff;
            mob.healthComponent?.TakeDamage(actualDamage, DamageType.Combat, null);
            GD.Print($"AOE dealt {actualDamage:F1} damage to {mob.name}");
        }
        
        // Apply pushback if target has physics
        if (target is RigidBody3D rigidBody)
        {
            var pushback = direction * pushbackForce * falloff;
            rigidBody.ApplyCentralImpulse(pushback);
        }
        else if (target is CharacterBody3D characterBody)
        {
            // For CharacterBody3D, we need to apply velocity (not impulse)
            var pushback = direction * pushbackForce * falloff * 0.001f; // Scale down for velocity
            if (characterBody.HasMethod("add_velocity"))
            {
                characterBody.Call("add_velocity", pushback);
            }
        }
    }    private void CreateVisualEffect(Vector3 position, PlayerController player)
    {
        GD.Print($"Creating visual effect at position: {position}");
        
        // Create temporary visual sphere
        var visualSphere = new MeshInstance3D();
        var sphereMesh = new SphereMesh();
        sphereMesh.Radius = aoeRadius;
        sphereMesh.Height = aoeRadius * 2f;
        visualSphere.Mesh = sphereMesh;
        
        // Create glowing material
        var material = new StandardMaterial3D();
        material.AlbedoColor = new Color(1f, 0.5f, 0f, 0.8f); // Even more visible
        material.EmissionEnabled = true;
        material.Emission = new Color(1f, 0.8f, 0f); // Bright orange emission
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // Make visible from inside
        material.NoDepthTest = true; // Always visible
        visualSphere.MaterialOverride = material;
        
        // Add to scene root for absolute positioning
        var scene = Engine.GetMainLoop() as SceneTree;
        scene.CurrentScene.AddChild(visualSphere);
        
        // Position it at the player's feet (ground level)
        var effectPosition = position;
        effectPosition.Y = position.Y - 1f; // Lower it to ground level
        visualSphere.GlobalPosition = effectPosition;
        
        GD.Print($"Visual sphere created with radius {aoeRadius} at {effectPosition}");
        GD.Print($"Visual sphere actual global position: {visualSphere.GlobalPosition}");
        
        // Start at smaller scale and grow, then shrink
        visualSphere.Scale = Vector3.Zero;
        
        // Animate and remove
        var tween = visualSphere.CreateTween();
        tween.SetParallel(true);
        
        // Scale animation: grow quickly then shrink
        tween.TweenProperty(visualSphere, "scale", Vector3.One, 0.1f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(visualSphere, "scale", Vector3.Zero, 0.4f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In).SetDelay(0.1f);
        
        // Fade animation
        tween.TweenMethod(Callable.From<float>((alpha) => {
            if (IsInstanceValid(material))
            {
                var color = material.AlbedoColor;
                color.A = alpha;
                material.AlbedoColor = color;
            }
        }), 0.6f, 0f, 0.5f);
        
        // Remove after animation
        tween.TweenCallback(Callable.From(() => {
            if (IsInstanceValid(visualSphere))
                visualSphere.QueueFree();
        })).SetDelay(0.5f);
    }
}
