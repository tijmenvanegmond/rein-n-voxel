using System;
using Godot;

public partial class MobHealth : RefCounted
{
    private Mob mob;
    private RandomNumberGenerator rng;

    [Export]
    public float maxHealth { get; private set; } = 100f;
    [Export]
    public float currentHealth { get; private set; } = 100f;
    [Export]
    public float maxHunger { get; private set; } = 100f;
    [Export]
    public float currentHunger { get; private set; } = 100f;    [Export]
    public float hungerDecayRate { get; private set; } = 0.5f; // Much slower default hunger per second
    [Export]
    public float starvationDamageRate { get; private set; } = 1f; // Reduced damage per second when starving
    [Export]
    public float regenerationRate { get; private set; } = 1f; // health per second when well-fed
    [Export]
    public float armor { get; private set; } = 0f;
    [Export]
    public bool isDead { get; private set; } = false;
    [Export]
    public bool isStarving { get; private set; } = false;    // Health thresholds
    public float criticalHealthThreshold = 0.2f; // 20% health
    public float lowHealthThreshold = 0.5f; // 50% health
    public float hungerThreshold = 0.6f; // 60% hunger before seeking food (start seeking earlier)
    public float starvationThreshold = 0.05f; // 5% hunger before taking damage (much more forgiving)

    // Events
    public event Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public event Action<float, float> OnHungerChanged; // currentHunger, maxHunger
    public event Action<float, DamageType, Node3D> OnDamageTaken; // damage, type, source
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action<bool> OnStarvationChanged; // isStarving

    public MobHealth(Mob mob)
    {
        this.mob = mob;
        this.rng = new RandomNumberGenerator();
        this.rng.Randomize();
    }

    public void Initialize(MobType mobType)
    {        // Set health and hunger based on mob type
        switch (mobType)
        {
            case MobType.SmallFriendly:
                maxHealth = 60f;
                maxHunger = 80f;
                hungerDecayRate = 0.3f; // Much slower hunger decay - lasts ~4-5 minutes
                regenerationRate = 2f;
                armor = 0f;
                break;
            case MobType.SmallSkittish:
                maxHealth = 40f; // More fragile
                maxHunger = 70f;
                hungerDecayRate = 0.4f; // Slower hunger decay - lasts ~3 minutes
                regenerationRate = 1.5f;
                armor = 0f;
                break;
            case MobType.Medium:
                maxHealth = 120f;
                maxHunger = 100f;
                hungerDecayRate = 0.25f; // Much slower - lasts ~6-7 minutes
                regenerationRate = 1.2f;
                armor = 5f;
                break;            case MobType.Large:
                maxHealth = 200f;
                maxHunger = 150f;
                hungerDecayRate = 0.2f; // Slowest hunger decay - lasts ~12 minutes
                regenerationRate = 0.8f;
                armor = 10f;
                break;            case MobType.Predator:
                maxHealth = 150f; // Reduced from 300f - still tanky but not invincible
                maxHunger = 100f; // Reduced from 120f
                hungerDecayRate = 0.4f; // Increased from 0.3f - predators get hungry faster
                regenerationRate = 1.0f; // Reduced from 1.5f
                armor = 8f; // Reduced from 15f
                break;
            case MobType.HostileSmall:
                maxHealth = 80f; // Tougher than normal small mobs
                maxHunger = 90f;
                hungerDecayRate = 0.35f; // Slower decay to stay aggressive longer
                regenerationRate = 2.5f; // Fast regeneration
                armor = 2f; // Light armor
                break;
            case MobType.HostileMedium:
                maxHealth = 180f; // Much tougher than normal medium mobs
                maxHunger = 120f;
                hungerDecayRate = 0.3f; // Slow decay to maintain aggression
                regenerationRate = 1.5f; // Good regeneration
                armor = 8f; // Moderate armor
                break;            case MobType.HostileLarge:
                maxHealth = 300f; // Very tanky
                maxHunger = 180f;
                hungerDecayRate = 0.25f; // Very slow decay
                regenerationRate = 1.0f; // Steady regeneration
                armor = 15f; // Heavy armor
                break;
            case MobType.DefenderFriendly:
                maxHealth = 200f; // Tanky defenders
                maxHunger = 140f;
                hungerDecayRate = 0.25f; // Slow decay to stay effective
                regenerationRate = 1.8f; // Good regeneration for sustainability
                armor = 12f; // Heavy armor for protection
                break;
            case MobType.DefenderNeutral:
                maxHealth = 250f; // Very tanky
                maxHunger = 160f;
                hungerDecayRate = 0.22f; // Very slow decay
                regenerationRate = 1.5f; // Steady regeneration
                armor = 15f; // Heavy armor
                break;
            case MobType.DefenderWild:
                maxHealth = 220f; // Balanced tankiness
                maxHunger = 150f;
                hungerDecayRate = 0.28f; // Moderate decay (more active)
                regenerationRate = 2.0f; // Fast regeneration (nature's resilience)
                armor = 10f; // Moderate armor with high mobility
                break;
        }

        currentHealth = maxHealth;
        currentHunger = maxHunger;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void UpdateHealth(double delta)
    {
        if (isDead) return;

        // Update hunger
        currentHunger = Mathf.Max(0f, currentHunger - hungerDecayRate * (float)delta);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        // Check starvation
        bool wasStarving = isStarving;
        isStarving = (currentHunger / maxHunger) <= starvationThreshold;
        
        if (wasStarving != isStarving)
        {
            OnStarvationChanged?.Invoke(isStarving);
        }

        // Apply starvation damage
        if (isStarving)
        {
            TakeDamage(starvationDamageRate * (float)delta, DamageType.Starvation, null);
        }        // Regenerate health if well-fed (reduced threshold from 0.7f to 0.5f)
        else if ((currentHunger / maxHunger) > 0.5f && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + regenerationRate * (float)delta);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public bool TakeDamage(float damage, DamageType damageType = DamageType.Physical, Node3D source = null)
    {
        if (isDead || damage <= 0) return false;

        // Apply armor reduction (except for starvation damage)
        float finalDamage = damage;
        if (damageType != DamageType.Starvation)
        {
            finalDamage = Mathf.Max(1f, damage - armor);
        }

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        OnDamageTaken?.Invoke(finalDamage, damageType, source);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Check for death
        if (currentHealth <= 0f && !isDead)
        {
            Die();
            return true; // Indicates death occurred
        }

        return false; // No death
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Feed(float amount)
    {
        if (isDead || amount <= 0) return;

        currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0f;
        OnDeath?.Invoke();
        
        // Change mob state to dead and disable physics
        mob.ChangeState(MobState.Dead);
        mob.SetPhysicsProcess(false);
        
        // Spawn carrion food item
        SpawnCarrion();
        
        GD.Print($"Mob {mob.name} has died!");
    }

    public void Revive(float healthPercent = 0.5f)
    {
        if (!isDead) return;

        isDead = false;
        currentHealth = maxHealth * Mathf.Clamp(healthPercent, 0.1f, 1f);
        currentHunger = maxHunger * 0.3f; // Start hungry after revival
        
        OnRevive?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        
        // Re-enable physics and set to wandering state
        mob.SetPhysicsProcess(true);
        mob.ChangeState(MobState.Wandering);
        
        GD.Print($"Mob {mob.name} has been revived!");
    }

    // Health status queries
    public bool IsHealthy() => (currentHealth / maxHealth) > lowHealthThreshold;
    public bool IsCritical() => (currentHealth / maxHealth) <= criticalHealthThreshold;
    public bool IsHungry() => (currentHunger / maxHunger) <= hungerThreshold;
    public bool IsWellFed() => (currentHunger / maxHunger) > 0.8f;
    
    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetHungerPercent() => currentHunger / maxHunger;
    
    // Modify stats
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void IncreaseArmor(float amount)
    {
        armor += amount;
    }
      public void SetHungerDecayRate(float rate)
    {
        hungerDecayRate = Mathf.Max(0f, rate);
    }
    
    private void SpawnCarrion()
    {
        // Get the TerrainAwareFoodSpawner from the scene
        var terrainAwareFoodSpawner = mob.GetTree().GetFirstNodeInGroup("food_spawner") as TerrainAwareFoodSpawner;
        if (terrainAwareFoodSpawner == null)
        {
            // Try alternative way to find food spawner if the first one wasn't the TerrainAwareFoodSpawner
            // This assumes TerrainAwareFoodSpawner is also in the "food_spawner" group or accessible this way.
            // If there are multiple nodes in the "food_spawner" group, this might need refinement.
            var spawners = mob.GetTree().GetNodesInGroup("food_spawner");
            foreach (var spawnerNode in spawners)
            {
                if (spawnerNode is TerrainAwareFoodSpawner spawner)
                {
                    terrainAwareFoodSpawner = spawner;
                    break;
                }
            }
        }
        
        // If still not found, try a direct path if known, or log an error.
        if (terrainAwareFoodSpawner == null)
        {
             GD.PrintErr("MobHealth: TerrainAwareFoodSpawner not found, cannot spawn carrion.");
             return;
        }

        if (terrainAwareFoodSpawner.meatFoodScene != null)
        {
            try
            {
                // Create carrion food item
                var carrion = terrainAwareFoodSpawner.meatFoodScene.Instantiate<FoodItem>();
                if (carrion != null)
                {
                    // Set up as carrion
                    carrion.foodType = FoodType.Carrion;
                    carrion.nutritionValue = GetCarrionNutritionValue();
                    carrion.eatingDuration = 4f; // Takes longer to eat carrion
                    carrion.respawnTime = 0f; // Carrion doesn't respawn
                    
                    // Position it at the mob's location
                    carrion.Position = mob.Position;
                    
                    // Add to scene
                    mob.GetParent().AddChild(carrion);
                    
                    GD.Print($"Spawned carrion from dead {mob.mobType} mob with {carrion.nutritionValue} nutrition");
                }
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"Failed to spawn carrion: {e.Message}");
            }
        }
    }
    
    private float GetCarrionNutritionValue()
    {
        // Nutrition value based on mob size
        switch (mob.mobType)
        {
            case MobType.SmallFriendly:
            case MobType.SmallSkittish:
                return 30f;
            case MobType.Medium:
                return 60f;
            case MobType.Large:
                return 100f;
            default:
                return 40f;
        }
    }
}

public enum DamageType
{
    Physical,
    Starvation,
    Fall,
    Environmental,
    Combat
}
