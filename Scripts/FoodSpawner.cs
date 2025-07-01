using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class FoodSpawner : Node
{
    [Export]
    public PackedScene grassFoodScene { get; set; }
    [Export]
    public PackedScene berryFoodScene { get; set; }
    // [Export] // Meat will no longer spawn naturally via this spawner
    // public PackedScene meatFoodScene { get; set; }
      [Export]
    public int maxFoodItems { get; set; } = 750; // Increased from 500
    [Export]
    public float spawnRadius { get; set; } = 1000f;
    [Export]
    public float spawnInterval { get; set; } = 3f; // Faster spawning - was 5f, now 3f
    [Export]
    public float grassSpawnChance { get; set; } = 0.5f; // Reduced slightly
    [Export]
    public float berrySpawnChance { get; set; } = 0.4f; // Increased from 0.3f
    // [Export] // Meat will no longer spawn naturally via this spawner
    // public float meatSpawnChance { get; set; } = 0.1f;
    
    private List<FoodItem> activeFoodItems = new List<FoodItem>();
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private float spawnTimer = 0f;
    private Node3D playerReference;
    private TerrainManager terrainManager;
      public override void _Ready()
    {
        rng.Randomize();
        
        // Add to food_spawner group for easy finding
        AddToGroup("food_spawner");
        
        // Find player reference for spawning around player
        playerReference = GetTree().GetFirstNodeInGroup("player") as Node3D;
        if (playerReference == null)
        {
            playerReference = GetNode<Node3D>("../Player");
        }
        
        // Find terrain manager
        terrainManager = GetTree().GetFirstNodeInGroup("terrain_manager") as TerrainManager;
        if (terrainManager == null)
        {
            terrainManager = GetNode<TerrainManager>("../TerrainManager");
        }
    }
    
    public void InitializeFoodSpawning(MovementController player, TerrainManager terrain)
    {
        if (player != null)
        {
            playerReference = player as Node3D;
        }
        
        if (terrain != null)
        {
            terrainManager = terrain;
        }
        
        // Spawn initial food items
        SpawnInitialFood();
        
        GD.Print($"FoodSpawner: Initialized with {activeFoodItems.Count} initial food items");
    }
    
    public override void _Process(double delta)
    {
        spawnTimer += (float)delta;
        
        // Clean up consumed food items
        activeFoodItems.RemoveAll(food => !IsInstanceValid(food) || food.isConsumed);
        
        // Spawn new food if needed
        if (spawnTimer >= spawnInterval && activeFoodItems.Count < maxFoodItems)
        {
            SpawnRandomFood();
            spawnTimer = 0f;
        }
    }
    
    private void SpawnInitialFood()
    {
        int initialCount = maxFoodItems / 2; // Start with half the max food items
        
        for (int i = 0; i < initialCount; i++)
        {
            SpawnRandomFood();
        }
        
        GD.Print($"FoodSpawner: Spawned {initialCount} initial food items");
    }
    
    private void SpawnRandomFood()
    {
        if (playerReference == null) return;
        
        // Choose food type based on spawn chances
        FoodType foodType = ChooseRandomFoodType();
        PackedScene foodScene = GetFoodScene(foodType);
        
        if (foodScene == null)
        {
            GD.PrintErr($"FoodSpawner: No scene assigned for food type {foodType}");
            return;
        }
        
        // Find a suitable spawn position
        Vector3 spawnPosition = FindSpawnPosition();
        
        if (spawnPosition == Vector3.Zero)
        {
            return; // Failed to find suitable position
        }
        
        // Instantiate the food item
        try
        {
            FoodItem foodItem = foodScene.Instantiate<FoodItem>();
            if (foodItem == null)
            {
                GD.PrintErr($"FoodSpawner: Failed to instantiate food scene for {foodType}");
                return;
            }
            
            AddChild(foodItem);
            foodItem.Position = spawnPosition;
            
            activeFoodItems.Add(foodItem);
            
            GD.Print($"FoodSpawner: Spawned {foodType} at {spawnPosition}. Total food: {activeFoodItems.Count}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"FoodSpawner: Error spawning {foodType}: {e.Message}");
        }
    }
      private FoodType ChooseRandomFoodType()
    {
        float roll = rng.Randf();
        float totalChance = grassSpawnChance + berrySpawnChance; // Recalculate total chance without meat

        if (totalChance <= 0) // Avoid division by zero if all chances are zero
        {
            return FoodType.Grass; // Default to grass if no chances are set
        }

        float normalizedGrassChance = grassSpawnChance / totalChance;
        // float normalizedBerryChance = berrySpawnChance / totalChance; // Not strictly needed for if/else

        if (roll < normalizedGrassChance)
            return FoodType.Grass;
        else
            return FoodType.Berries;
        // Meat is no longer spawned by this method
    }
      private PackedScene GetFoodScene(FoodType foodType)
    {
        switch (foodType)
        {
            case FoodType.Grass:
                return grassFoodScene;
            case FoodType.Berries:
                return berryFoodScene;
            // case FoodType.Meat:
            //     return meatFoodScene;
            default:
                return grassFoodScene; // Default fallback
        }
    }
    
    private Vector3 FindSpawnPosition()
    {
        int maxAttempts = 20;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random position around player
            float angle = rng.Randf() * Mathf.Tau;
            float distance = rng.RandfRange(10f, spawnRadius);
            
            Vector3 candidatePos = playerReference.Position + new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
            
            // Find ground position
            Vector3 groundPos = FindGroundPosition(candidatePos);
            
            if (groundPos != Vector3.Zero && IsPositionSuitable(groundPos))
            {
                return groundPos;
            }
        }
        
        GD.Print("FoodSpawner: Failed to find suitable spawn position after maximum attempts");
        return Vector3.Zero;
    }
      private Vector3 FindGroundPosition(Vector3 position)
    {
        if (terrainManager == null)
        {
            // Fallback: simple raycast downward
            var spaceState = GetViewport().World3D.DirectSpaceState;
            var from = position + Vector3.Up * 50f;
            var to = position + Vector3.Down * 50f;
            
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var result = spaceState.IntersectRay(query);
            
            if (result.Count > 0)
            {
                return (Vector3)result["position"] + Vector3.Up * 0.5f;
            }
            
            return Vector3.Zero;
        }
        
        // Use terrain manager to find proper ground height - simplified for now
        // TODO: Implement proper terrain height lookup when TerrainManager has this method
        return new Vector3(position.X, 10f, position.Z); // Simple fallback height
    }
    
    private bool IsPositionSuitable(Vector3 position)
    {
        // Check if position is too close to existing food
        float minDistance = 3f;
        
        foreach (var food in activeFoodItems)
        {
            if (IsInstanceValid(food) && food.Position.DistanceTo(position) < minDistance)
            {
                return false;
            }
        }
        
        // Check if position is reasonable (not too high or low)
        if (position.Y < -10f || position.Y > 100f)
        {
            return false;
        }
        
        return true;
    }
    
    public void ClearAllFood()
    {
        foreach (var food in activeFoodItems)
        {
            if (IsInstanceValid(food))
            {
                food.QueueFree();
            }
        }
        activeFoodItems.Clear();
        GD.Print("FoodSpawner: Cleared all food items");
    }
    
    public int GetActiveFoodCount()
    {
        return activeFoodItems.Count(food => IsInstanceValid(food) && !food.isConsumed);
    }
    
    public List<FoodItem> GetNearbyFood(Vector3 position, float radius)
    {
        return activeFoodItems
            .Where(food => IsInstanceValid(food) && !food.isConsumed)
            .Where(food => food.Position.DistanceTo(position) <= radius)
            .ToList();
    }
}
