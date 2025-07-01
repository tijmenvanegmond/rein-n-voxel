using Godot;

public partial class FoodItem : StaticBody3D, IFood
{
    [Export]
    public FoodType foodType = FoodType.Berries;    [Export]
    public float nutritionValue = 40f; // Increased nutrition to make food more valuable
    [Export]
    public float eatingDuration = 2f;
    [Export]
    public bool isConsumed = false;
    [Export]
    public float respawnTime 
    {
        get => _respawnTime;
        set => _respawnTime = value; // Allow 0.0 to mean no respawn
    }
    private float _respawnTime = 15f; // Faster respawn - was 30f, now 15f
    
    // Grass-specific growth properties
    [Export]
    public bool canGrow = false; // Can this food item grow over time?
    [Export]
    public int maxGrowthStages = 3; // Maximum growth stages
    [Export]
    public float growthRate = 1.0f; // Growth rate multiplier
    [Export]
    public float spreadChance = 0.1f; // Chance to spread to nearby areas
    
    private int currentGrowthStage = 0;
    private float baseNutritionValue; // Original nutrition before growth bonuses
    private Vector3 baseScale; // Original scale before growth

    private MeshInstance3D meshInstance;
    private CollisionShape3D collisionShape;
    private Area3D detectionArea;
    private Timer respawnTimer;
    private Vector3 originalPosition;
    private bool originalVisibility;

    public override void _Ready()
    {
        // Store original state
        originalPosition = Position;
        originalVisibility = Visible;
          // Find child nodes
        meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
        collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        detectionArea = GetNode<Area3D>("Area3D");
        
        // Connect Area3D signals for mob detection
        detectionArea.BodyEntered += OnBodyEntered;
        detectionArea.BodyExited += OnBodyExited;
          // Create respawn timer
        respawnTimer = new Timer();
        respawnTimer.WaitTime = Mathf.Max(respawnTime, 0.1f); // Ensure minimum 0.1 second wait time
        respawnTimer.OneShot = true;
        respawnTimer.Timeout += OnRespawnTimerTimeout;
        AddChild(respawnTimer);
        
        // Set up appearance based on food type
        SetupAppearance();
        
        // Store base nutrition value and scale
        baseNutritionValue = nutritionValue;
        baseScale = Scale;
    }

    private void SetupAppearance()
    {
        if (meshInstance?.MaterialOverride is StandardMaterial3D material)
        {
            // Set color based on food type
            switch (foodType)
            {
                case FoodType.Berries:
                    material.AlbedoColor = new Color(0.8f, 0.2f, 0.4f); // Red berries
                    material.EmissionEnabled = true;
                    material.Emission = new Color(0.2f, 0.05f, 0.1f);
                    break;
                case FoodType.Grass:
                    material.AlbedoColor = new Color(0.3f, 0.7f, 0.2f); // Green grass
                    break;
                case FoodType.Seeds:
                    material.AlbedoColor = new Color(0.6f, 0.5f, 0.3f); // Brown seeds
                    break;
                case FoodType.Mushrooms:
                    material.AlbedoColor = new Color(0.5f, 0.4f, 0.3f); // Brown mushrooms
                    material.EmissionEnabled = true;
                    material.Emission = new Color(0.1f, 0.08f, 0.06f);
                    break;
                case FoodType.Nectar:
                    material.AlbedoColor = new Color(1f, 0.8f, 0.2f); // Golden nectar
                    material.EmissionEnabled = true;
                    material.Emission = new Color(0.3f, 0.2f, 0.05f);
                    break;
                case FoodType.Meat:
                    material.AlbedoColor = new Color(0.7f, 0.3f, 0.2f); // Red meat
                    break;
                case FoodType.Carrion:
                    material.AlbedoColor = new Color(0.4f, 0.3f, 0.2f); // Dark carrion
                    break;
            }
        }
    }

    public FoodType GetFoodType()
    {
        return foodType;
    }

    public float GetNutritionValue()
    {
        return nutritionValue;
    }

    public float GetEatingDuration()
    {
        return eatingDuration;
    }

    public bool IsConsumed()
    {
        return isConsumed;
    }

    public void Consume()
    {
        if (isConsumed) return;

        isConsumed = true;
        
        // Hide the food item
        Visible = false;
        
        // Disable collision
        if (collisionShape != null)
        {
            collisionShape.Disabled = true;
        }
          // Start respawn timer if respawn time is set
        if (respawnTime > 0 && respawnTimer != null)
        {
            respawnTimer.WaitTime = Mathf.Max(respawnTime, 0.1f); // Ensure valid time
            respawnTimer.Start();
        }
        else
        {
            // If no respawn, queue for deletion
            QueueFree();
        }
        
        GD.Print($"Food item {foodType} consumed, nutrition: {nutritionValue}");
    }

    private void OnRespawnTimerTimeout()
    {
        Respawn();
    }

    public void Respawn()
    {
        if (!isConsumed) return;

        isConsumed = false;
        Position = originalPosition;
        Visible = originalVisibility;
        
        // Re-enable collision
        if (collisionShape != null)
        {
            collisionShape.Disabled = false;
        }
        
        GD.Print($"Food item {foodType} respawned");
    }

    // Static method to create food items programmatically
    public static FoodItem CreateFoodItem(FoodType type, Vector3 position, float nutrition = 0f)
    {
        var foodItem = new FoodItem();
        foodItem.foodType = type;
        foodItem.Position = position;
          // Set nutrition based on type if not specified
        if (nutrition <= 0f)
        {
            switch (type)
            {
                case FoodType.Grass: nutrition = 25f; break;      // Increased from 10f
                case FoodType.Berries: nutrition = 50f; break;   // Increased from 25f  
                case FoodType.Seeds: nutrition = 35f; break;     // Increased from 15f
                case FoodType.Mushrooms: nutrition = 45f; break; // Increased from 20f
                case FoodType.Nectar: nutrition = 70f; break;    // Increased from 35f
                case FoodType.Meat: nutrition = 80f; break;      // Increased from 40f
                case FoodType.Fish: nutrition = 70f; break;      // Increased from 35f
                case FoodType.Carrion: nutrition = 60f; break;   // Increased from 30f
            }
        }
        
        foodItem.nutritionValue = nutrition;
        return foodItem;
    }

    private void OnBodyEntered(Node3D body)
    {
        // Check if the body is a mob that can eat this food
        if (body is Mob mob && !isConsumed)
        {
            // Notify the mob that food is available
            mob.foodComponent.OnFoodDetected(this);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        // Check if the body is a mob
        if (body is Mob mob)
        {
            // Notify the mob that food is no longer in range
            mob.foodComponent.OnFoodLost(this);
        }
    }

    // Growth-related methods for grass progression
    public int GetCurrentGrowthStage()
    {
        return currentGrowthStage;
    }
    
    public int GetMaxGrowthStages()
    {
        return maxGrowthStages;
    }
    
    public float GetGrowthRate()
    {
        return growthRate;
    }
    
    public float GetSpreadChance()
    {
        return spreadChance;
    }
    
    public bool CanGrow()
    {
        return canGrow && currentGrowthStage < maxGrowthStages;
    }
    
    public void AdvanceGrowthStage()
    {
        if (!canGrow || currentGrowthStage >= maxGrowthStages) return;
        
        currentGrowthStage++;
        UpdateGrowthAppearance();
        UpdateNutritionForGrowth();
        
        GD.Print($"Food item {foodType} advanced to growth stage {currentGrowthStage}/{maxGrowthStages}");
    }
    
    public void SetGrowthStage(int stage)
    {
        if (!canGrow) return;
        
        currentGrowthStage = Mathf.Clamp(stage, 0, maxGrowthStages);
        UpdateGrowthAppearance();
        UpdateNutritionForGrowth();
    }
    
    private void UpdateGrowthAppearance()
    {
        if (!canGrow) return;
        
        // Scale the object based on growth stage
        float growthMultiplier = 1.0f + (currentGrowthStage * 0.3f); // 30% increase per stage
        Scale = baseScale * growthMultiplier;
        
        // Update material brightness for grass to show healthier growth
        if (foodType == FoodType.Grass && meshInstance?.MaterialOverride is StandardMaterial3D material)
        {
            float brightness = 1.0f + (currentGrowthStage * 0.2f); // 20% brighter per stage
            Color baseColor = new Color(0.3f, 0.7f, 0.2f); // Base grass green
            material.AlbedoColor = baseColor * brightness;
        }
    }
    
    private void UpdateNutritionForGrowth()
    {
        if (!canGrow) return;
        
        // Increase nutrition based on growth stage
        float nutritionMultiplier = 1.0f + (currentGrowthStage * 0.3f); // 30% more nutrition per stage
        nutritionValue = baseNutritionValue * nutritionMultiplier;
    }
    
    public bool ShouldSpread()
    {
        if (!canGrow || currentGrowthStage < 2) return false; // Must be at least stage 2 to spread
        
        return GD.Randf() < spreadChance;
    }
}
