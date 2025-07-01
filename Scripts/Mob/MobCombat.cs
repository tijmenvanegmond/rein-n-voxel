using System;
using System.Collections.Generic;
using Godot;

public partial class MobCombat : RefCounted
{
    private Mob mob;
    private RandomNumberGenerator rng;

    [Export]
    public float attackDamage { get; private set; } = 10f;
    [Export]
    public float attackRange { get; private set; } = 3f;
    [Export]
    public float attackCooldown { get; private set; } = 2f;    [Export]
    public float attackSpeed { get; private set; } = 1f; // Animation speed multiplier
    [Export]
    public float knockbackStrength { get; private set; } = 0.5f; // Multiplier for knockback force
    [Export]
    public bool canAttack { get; private set; } = true;
    
    public Node3D currentTarget { get; private set; } = null;
    public bool isAttacking { get; private set; } = false;
    public float lastAttackTime = 0f;
    public float attackChargeTime = 0.5f; // Time to charge an attack
    public float attackChargeTimer = 0f;

    // Threat assessment
    public float threatLevel = 0f;
    public Dictionary<Node3D, float> threatList = new Dictionary<Node3D, float>();
    public float threatDecayRate = 10f; // Threat reduction per second
    public float maxThreatDistance = 20f;

    // Combat behavior settings
    public bool isAggressive = false;
    public bool isDefensive = false;
    public float aggressionRadius = 10f;
    public float defenseRadius = 8f;
    public float retreatHealthThreshold = 0.3f; // Retreat when health below 30%

    // Events
    public event Action<Node3D, float> OnAttackPerformed; // target, damage
    public event Action<Node3D> OnTargetChanged; // new target
    public event Action<float> OnThreatLevelChanged; // new threat level
    public event Action<bool> OnCombatStateChanged; // inCombat

    public MobCombat(Mob mob)
    {
        this.mob = mob;
        this.rng = new RandomNumberGenerator();
        this.rng.Randomize();
    }

    public void Initialize(MobType mobType, MobPersonality personality)
    {
        // Set combat stats based on mob type
        switch (mobType)
        {
            case MobType.SmallFriendly:
                attackDamage = 5f;
                attackRange = 2f;
                attackCooldown = 3f;
                isAggressive = false; // Friendly mobs don't initiate combat
                aggressionRadius = 0f;
                defenseRadius = 5f;
                break;
                
            case MobType.SmallSkittish:
                attackDamage = 3f;
                attackRange = 1.5f;
                attackCooldown = 4f;
                isAggressive = false;
                aggressionRadius = 0f;
                defenseRadius = 8f; // Larger defense radius
                retreatHealthThreshold = 0.8f; // Retreat very early
                break;
                
            case MobType.Medium:
                attackDamage = 15f;
                attackRange = 4f;
                attackCooldown = 2.5f;
                isAggressive = false;
                aggressionRadius = 6f;
                defenseRadius = 8f;
                break;
                  case MobType.Large:
                attackDamage = 25f;
                attackRange = 5f;
                attackCooldown = 3.5f;
                isAggressive = false;
                aggressionRadius = 8f;
                defenseRadius = 10f;
                retreatHealthThreshold = 0.2f; // Large mobs fight longer
                break;            case MobType.Predator:
                attackDamage = 20f; // Reduced from 40f - still strong but not instant kill
                attackRange = 5f;   // Reduced from 6f
                attackCooldown = 3f; // Increased from 2f - slower attacks
                isAggressive = true; // Always aggressive
                aggressionRadius = 12f; // Reduced from 15f
                defenseRadius = 15f; // Reduced from 20f
                retreatHealthThreshold = 0.15f; // Increased from 0.1f - retreat earlier
                break;
                
            case MobType.HostileSmall:
                attackDamage = 12f; // High damage for small size
                attackRange = 2.5f;
                attackCooldown = 1.5f; // Very fast attacks
                isAggressive = true;
                aggressionRadius = 10f; // Actively hunt
                defenseRadius = 12f;
                retreatHealthThreshold = 0.1f; // Fight to near death
                break;
                
            case MobType.HostileMedium:
                attackDamage = 25f; // High damage
                attackRange = 4f;
                attackCooldown = 2f; // Fast attacks
                isAggressive = true;
                aggressionRadius = 15f; // Wide hunt radius
                defenseRadius = 18f;
                retreatHealthThreshold = 0.08f; // Fight to near death
                break;
                  case MobType.HostileLarge:
                attackDamage = 45f; // Very high damage
                attackRange = 6f; // Long reach
                attackCooldown = 3f; // Moderate speed but devastating
                isAggressive = true;
                aggressionRadius = 20f; // Very wide hunt radius
                defenseRadius = 25f;
                retreatHealthThreshold = 0.05f; // Almost never retreat
                break;
                
            case MobType.DefenderFriendly:
                attackDamage = 20f; // Strong defensive damage
                attackRange = 4f; // Good reach
                attackCooldown = 2.5f; // Moderate attack speed
                isAggressive = false; // Only aggressive to hostiles
                aggressionRadius = 18f; // Wide awareness to protect allies
                defenseRadius = 20f; // Large protection zone
                retreatHealthThreshold = 0.15f; // Strategic retreat
                break;
                
            case MobType.DefenderNeutral:
                attackDamage = 30f; // High defensive damage
                attackRange = 5f; // Long reach
                attackCooldown = 2.8f; // Moderate speed
                isAggressive = false; // Defensive only
                aggressionRadius = 15f; // Moderate threat detection
                defenseRadius = 25f; // Large defense zone
                retreatHealthThreshold = 0.12f; // Fight longer
                break;
                
            case MobType.DefenderWild:
                attackDamage = 25f; // Balanced damage
                attackRange = 4.5f; // Good reach
                attackCooldown = 2.2f; // Faster attacks
                isAggressive = false; // Territorial defense only
                aggressionRadius = 20f; // Large territory awareness
                defenseRadius = 30f; // Very large territory defense
                retreatHealthThreshold = 0.08f; // Fight fiercely for territory
                break;
        }

        // Personality adjustments
        switch (personality)
        {
            case MobPersonality.Friendly:
                attackDamage *= 0.7f; // Less aggressive damage
                attackCooldown *= 1.5f; // Slower to attack
                retreatHealthThreshold += 0.2f; // Retreat earlier
                break;
                
            case MobPersonality.Skittish:
                attackDamage *= 0.5f; // Very low damage
                attackCooldown *= 2f; // Much slower to attack
                aggressionRadius = 0f; // Never aggressive
                defenseRadius += 3f; // Larger defense zone
                break;
                  case MobPersonality.Neutral:
                // No personality adjustments
                break;
                
            case MobPersonality.Aggressive:
                // Predators are highly aggressive
                attackDamage *= 1.5f; // Extra damage
                attackCooldown *= 0.6f; // Much faster attacks
                aggressionRadius *= 2f; // Hunt from far away
                defenseRadius *= 2f; // Large awareness
                retreatHealthThreshold = 0.05f; // Almost never retreat
                isAggressive = true;
                break;
        }

        // Faction adjustments
        switch (mob.faction)
        {
            case MobFaction.Player:
                // Player-allied mobs are less aggressive to neutrals
                aggressionRadius *= 0.7f;
                break;
                
            case MobFaction.Hostile:
                // Hostile mobs are more aggressive
                isAggressive = true;
                aggressionRadius *= 1.5f;
                attackDamage *= 1.3f;
                attackCooldown *= 0.8f;
                retreatHealthThreshold = 0.1f; // Fight to the death
                break;
                
            case MobFaction.Wild:
                // Wild mobs are territorial but not overly aggressive
                aggressionRadius *= 0.8f;
                break;
        }
    }

    public void UpdateCombat(double delta)
    {
        UpdateThreat(delta);
        UpdateCombatBehavior(delta);
        
        if (isAttacking)
        {
            UpdateAttack(delta);
        }
    }

    private void UpdateThreat(double delta)
    {
        // Decay all threat levels
        var keysToRemove = new List<Node3D>();
        var keysToUpdate = new List<Node3D>(threatList.Keys);
        
        foreach (var threat in keysToUpdate)
        {
            if (!GodotObject.IsInstanceValid(threat))
            {
                keysToRemove.Add(threat);
                continue;
            }

            var distance = mob.Position.DistanceTo(threat.Position);
            if (distance > maxThreatDistance)
            {
                keysToRemove.Add(threat);
                continue;
            }

            threatList[threat] = Mathf.Max(0f, threatList[threat] - threatDecayRate * (float)delta);
            
            if (threatList[threat] <= 0f)
            {
                keysToRemove.Add(threat);
            }
        }

        // Remove expired threats
        foreach (var key in keysToRemove)
        {
            threatList.Remove(key);
        }

        // Calculate total threat level
        float newThreatLevel = 0f;
        foreach (var threat in threatList.Values)
        {
            newThreatLevel += threat;
        }

        if (Math.Abs(newThreatLevel - threatLevel) > 0.1f)
        {
            threatLevel = newThreatLevel;
            OnThreatLevelChanged?.Invoke(threatLevel);
        }
    }

    private void UpdateCombatBehavior(double delta)
    {
        // Check if we should enter or exit combat state
        bool shouldBeInCombat = threatLevel > 0f || isAttacking;
        bool currentlyInCombat = mob.currentState == MobState.Combat;

        if (shouldBeInCombat && !currentlyInCombat)
        {
            mob.ChangeState(MobState.Combat);
            OnCombatStateChanged?.Invoke(true);
        }
        else if (!shouldBeInCombat && currentlyInCombat)
        {
            mob.ChangeState(MobState.Wandering);
            OnCombatStateChanged?.Invoke(false);
            currentTarget = null;
            OnTargetChanged?.Invoke(null);
        }

        // Update target selection
        if (shouldBeInCombat)
        {
            SelectCombatTarget();
        }

        // Check if we should retreat
        if (ShouldRetreat())
        {
            mob.ChangeState(MobState.Fleeing);
            OnCombatStateChanged?.Invoke(false);
        }
    }

    private void SelectCombatTarget()
    {
        Node3D bestTarget = null;
        float highestThreat = 0f;
        float closestDistance = float.MaxValue;

        foreach (var kvp in threatList)
        {
            var threat = kvp.Key;
            var threatValue = kvp.Value;
            
            if (!GodotObject.IsInstanceValid(threat)) continue;

            var distance = mob.Position.DistanceTo(threat.Position);
            
            // Prioritize by threat level, then by distance
            if (threatValue > highestThreat || 
                (Math.Abs(threatValue - highestThreat) < 0.1f && distance < closestDistance))
            {
                highestThreat = threatValue;
                bestTarget = threat;
                closestDistance = distance;
            }
        }

        if (currentTarget != bestTarget)
        {
            currentTarget = bestTarget;
            OnTargetChanged?.Invoke(currentTarget);
        }
    }

    public void AddThreat(Node3D source, float amount)
    {
        if (source == null || source == mob) return;

        // Don't add threat from allied mobs
        if (source is Mob sourceMob && mob.IsAlliedWith(sourceMob))
            return;

        if (threatList.ContainsKey(source))
        {
            threatList[source] += amount;
        }
        else
        {
            threatList[source] = amount;
        }

        // Cap individual threat levels
        threatList[source] = Mathf.Min(100f, threatList[source]);
    }

    public bool TryAttack(Node3D target = null)
    {
        if (!canAttack || isAttacking) return false;

        var attackTarget = target ?? currentTarget;
        if (attackTarget == null) return false;

        var distance = mob.Position.DistanceTo(attackTarget.Position);
        if (distance > attackRange) return false;

        var timeSinceLastAttack = Time.GetUnixTimeFromSystem() - lastAttackTime;
        if (timeSinceLastAttack < attackCooldown) return false;

        StartAttack(attackTarget);
        return true;
    }

    private void StartAttack(Node3D target)
    {
        isAttacking = true;
        currentTarget = target;
        attackChargeTimer = 0f;
        
        // Face the target
        if (target != null)
        {
            var lookDirection = target.GlobalTransform.Origin - mob.GlobalTransform.Origin;
            if (lookDirection.Length() > 0.1f)
            {
                mob.LookAt(mob.GlobalTransform.Origin + lookDirection, Vector3.Up);
            }
        }
        
        mob.ChangeState(MobState.Attacking);
    }

    private void UpdateAttack(double delta)
    {
        if (!isAttacking || currentTarget == null) 
        {
            FinishAttack();
            return;
        }

        attackChargeTimer += (float)delta;

        // Check if target is still in range
        var distance = mob.Position.DistanceTo(currentTarget.Position);
        if (distance > attackRange)
        {
            FinishAttack();
            return;
        }

        // Execute attack when charge time is complete
        if (attackChargeTimer >= attackChargeTime)
        {
            ExecuteAttack();
        }
    }

    private void ExecuteAttack()
    {
        if (currentTarget == null) 
        {
            FinishAttack();
            return;
        }        // Apply damage to target
        if (currentTarget is Mob targetMob)
        {
            bool killed = targetMob.healthComponent.TakeDamage(attackDamage, DamageType.Combat, mob);
            
            // Apply knockback to target
            ApplyKnockback(targetMob);
            
            // Add threat to target (they might fight back)
            targetMob.combatComponent.AddThreat(mob, attackDamage);
        }
        else if (currentTarget is PlayerController player)
        {
            // TODO: Apply damage to player when player health system is implemented
            // Apply knockback to player
            ApplyKnockback(player);
            GD.Print($"Mob {mob.name} attacks player for {attackDamage} damage!");
        }

        OnAttackPerformed?.Invoke(currentTarget, attackDamage);
        lastAttackTime = (float)Time.GetUnixTimeFromSystem();
        
        GD.Print($"Mob {mob.name} attacks {currentTarget.Name} for {attackDamage} damage!");
        
        FinishAttack();
    }

    private void FinishAttack()
    {
        isAttacking = false;
        attackChargeTimer = 0f;
        
        // Return to combat state if still in threat
        if (threatLevel > 0f)
        {
            mob.ChangeState(MobState.Combat);
        }
        else
        {
            mob.ChangeState(MobState.Wandering);
        }
    }

    public bool ShouldRetreat()
    {
        // Retreat if health is too low
        if (mob.healthComponent.GetHealthPercent() <= retreatHealthThreshold)
            return true;

        // Skittish mobs retreat if outnumbered
        if (mob.personality == MobPersonality.Skittish && threatList.Count > 1)
            return true;

        return false;
    }

    public Vector3 GetCombatMovement()
    {
        if (currentTarget == null) return Vector3.Zero;

        var toTarget = currentTarget.Position - mob.Position;
        var distance = toTarget.Length();

        // If target is too far, move closer
        if (distance > attackRange * 0.8f)
        {
            return new Vector3(toTarget.X, 0, toTarget.Z).Normalized();
        }
        // If too close, back away slightly
        else if (distance < attackRange * 0.3f)
        {
            return new Vector3(-toTarget.X, 0, -toTarget.Z).Normalized() * 0.5f;
        }
        // In optimal range, circle strafe
        else
        {
            var perpendicular = new Vector3(-toTarget.Z, 0, toTarget.X).Normalized();
            return perpendicular * (rng.Randf() > 0.5f ? 1f : -1f) * 0.7f;
        }
    }    public bool IsHostileTo(Node3D other)
    {
        if (other == mob || other == null) return false;

        // Check faction hostility
        if (other is Mob otherMob)
        {
            // Hostile faction is hostile to everyone except other hostile
            if (mob.faction == MobFaction.Hostile && otherMob.faction != MobFaction.Hostile)
                return true;
            
            // Defenders are protective of their faction and allied factions
            if (IsDefenderType())
            {
                // Defenders attack hostiles
                if (otherMob.faction == MobFaction.Hostile)
                    return true;
                    
                // Wild defenders also attack predators threatening wildlife
                if (mob.mobType == MobType.DefenderWild && otherMob.mobType == MobType.Predator)
                    return true;
            }
            
            // Wild mobs can be territorial
            if (mob.faction == MobFaction.Wild && otherMob.faction == MobFaction.Player)
                return rng.Randf() < 0.3f; // 30% chance of hostility
        }

        return false;
    }

    private bool IsDefenderType()
    {
        return mob.mobType == MobType.DefenderFriendly || 
               mob.mobType == MobType.DefenderNeutral || 
               mob.mobType == MobType.DefenderWild;
    }

    public void SetAggressiveness(bool aggressive)
    {
        isAggressive = aggressive;
    }    public void SetDefensiveness(bool defensive)
    {
        isDefensive = defensive;
    }

    public void SetTarget(Node3D target)
    {
        currentTarget = target;
        OnTargetChanged?.Invoke(currentTarget);
    }

    private void ApplyKnockback(Node3D target)
    {
        if (target == null || mob == null) return;        // Calculate knockback direction (from attacker to target)
        Vector3 knockbackDirection = (target.GlobalTransform.Origin - mob.GlobalTransform.Origin).Normalized();
        
        // Knockback strength based on attack damage and configured multiplier
        float knockbackForce = attackDamage * knockbackStrength;        // Apply knockback based on target type
        if (target is Mob targetMob)
        {
            // Adjust knockback based on target mob size - smaller mobs get knocked back more
            float sizeMultiplier = GetKnockbackMultiplierForMobType(targetMob.mobType);
            float adjustedKnockback = knockbackForce * sizeMultiplier;
            
            // Apply horizontal knockback with slight upward force
            Vector3 knockbackVector = new Vector3(
                knockbackDirection.X * adjustedKnockback,
                adjustedKnockback * 0.3f, // Slight upward force
                knockbackDirection.Z * adjustedKnockback
            );
            
            targetMob.ApplyCentralImpulse(knockbackVector);
            
            GD.Print($"Applied knockback to {targetMob.name} (size modifier: {sizeMultiplier:F2}): {knockbackVector}");
        }
        else if (target is PlayerController player)
        {
            // For player, we might need to access the player's RigidBody3D
            // This assumes the PlayerController has a method or property to access its physics body
            if (player.GetParent() is RigidBody3D playerBody)
            {
                Vector3 knockbackVector = new Vector3(
                    knockbackDirection.X * knockbackForce,
                    knockbackForce * 0.2f, // Less upward force for player
                    knockbackDirection.Z * knockbackForce
                );
                
                playerBody.ApplyCentralImpulse(knockbackVector);
                GD.Print($"Applied knockback to player: {knockbackVector}");
            }
            else
            {
                GD.Print("Could not apply knockback to player - no RigidBody3D found");
            }
        }
    }

    private float GetKnockbackMultiplierForMobType(MobType mobType)
    {
        // Different mob types receive different amounts of knockback
        // Smaller/lighter mobs get knocked back more, larger mobs resist more
        return mobType switch
        {
            MobType.SmallFriendly => 1.5f,  // Small mobs get knocked back more
            MobType.SmallSkittish => 1.5f,  // Small mobs get knocked back more
            MobType.Medium => 1.0f,          // Medium mobs get normal knockback
            MobType.Large => 0.7f,           // Large mobs resist knockback
            MobType.Predator => 0.6f,        // Predators resist knockback but not completely
            _ => 1.0f                        // Default normal knockback
        };
    }
}
