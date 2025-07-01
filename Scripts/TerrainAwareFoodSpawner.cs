using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class TerrainAwareFoodSpawner : Node
{
    [Export]
    public PackedScene grassFoodScene { get; set; }
    [Export]
    public PackedScene berryFoodScene { get; set; }
    [Export]
    public PackedScene meatFoodScene { get; set; }
      [Export]
    public int maxFoodItems { get; set; } = 3000;
    [Export]
    public float spawnRadius { get; set; } = 2000f;
    [Export]
    public float spawnInterval { get; set; } = 4f;
    [Export]
    public float chunkFoodDensity { get; set; } = 0.8f; // Food items per chunk area
    [Export]
    public bool enableWorldGenIntegration { get; set; } = true;
    [Export]
    public bool enableSeasonalVariation { get; set; } = true;
    
    private List<FoodItem> activeFoodItems = new List<FoodItem>();
    private Dictionary<Vector3I, List<FoodItem>> chunkFoodCache = new Dictionary<Vector3I, List<FoodItem>>();
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private float spawnTimer = 0f;
    private float seasonTimer = 0f;
    private Node3D playerReference;
    private TerrainManager terrainManager;
    private BiomeManager biomeManager;
    
    // Seasonal food availability
    private Dictionary<FoodType, SeasonalAvailability> seasonalFood = new Dictionary<FoodType, SeasonalAvailability>();
    
    public override void _Ready()
    {
        rng.Randomize();
        AddToGroup("food_spawner");
        
        // Find references
        playerReference = GetTree().GetFirstNodeInGroup("player") as Node3D;
        terrainManager = GetTree().GetFirstNodeInGroup("terrain_manager") as TerrainManager;
        biomeManager = GetTree().GetFirstNodeInGroup("biome_manager") as BiomeManager;
        
        // Initialize seasonal food availability
        InitializeSeasonalFood();
        
        GD.Print("TerrainAwareFoodSpawner: Initialized with biome and seasonal support");
    }
    
    private void InitializeSeasonalFood()
    {
        seasonalFood[FoodType.Grass] = new SeasonalAvailability
        {
            springMultiplier = 2.2f,
            summerMultiplier = 1.8f,
            fallMultiplier = 1.2f,
            winterMultiplier = 0.7f
        };

        seasonalFood[FoodType.Berries] = new SeasonalAvailability
        {
            springMultiplier = 0.5f,
            summerMultiplier = 1.8f,
            fallMultiplier = 1.3f,
            winterMultiplier = 0.1f
        };

        // Meat is no longer part of natural seasonal spawning
        // seasonalFood[FoodType.Meat] = new SeasonalAvailability
        // {
        //     springMultiplier = 0.8f,
        //     summerMultiplier = 0.7f,
        //     fallMultiplier = 1.1f,
        //     winterMultiplier = 1.4f // More carrion in harsh winter
        // };
    }

    private float GetSeasonalMultiplier(FoodType foodType)
    {
        if (!enableSeasonalVariation || !seasonalFood.ContainsKey(foodType) || biomeManager == null)
        {
            return 1.0f; // Default if no seasonal variation or data
        }

        // Placeholder for actual season logic - assuming BiomeManager.GetCurrentSeason() and Season enum exist elsewhere
        // For now, to avoid compilation errors, let's assume a default or simplified logic if BiomeManager methods are unavailable.
        // This part needs to be revisited if GetCurrentSeason() is not available.
        // For the purpose of removing meat, we can proceed with a simplified multiplier.
        // Example: just return the spring multiplier or an average if GetCurrentSeason() is problematic.        // Simple seasonal simulation based on time - no dynamic calls
        if (!enableSeasonalVariation)
        {
            return 1.0f;
        }
        
        // Use seasonTimer to simulate seasons (each season lasts ~60 seconds for testing)
        float seasonCycle = seasonTimer / 60.0f; // 4 seasons per 4 minutes
        int currentSeasonIndex = Mathf.FloorToInt(seasonCycle) % 4; // 0=Spring, 1=Summer, 2=Fall, 3=Winter
        
        if (seasonalFood.ContainsKey(foodType))
        {
            SeasonalAvailability availability = seasonalFood[foodType];
            
            return currentSeasonIndex switch
            {
                0 => availability.springMultiplier,    // Spring
                1 => availability.summerMultiplier,    // Summer  
                2 => availability.fallMultiplier,      // Fall
                3 => availability.winterMultiplier,    // Winter
                _ => 1.0f
            };
        }
        
        return 1.0f;
    }
    
    public void InitializeFoodSpawning(MovementController player, TerrainManager terrain, BiomeManager biome = null)
    {
        if (player != null)
            playerReference = player as Node3D;
            
        if (terrain != null)
            terrainManager = terrain;
            
        if (biome != null)
            biomeManager = biome;
            
        // Spawn initial food with terrain awareness
        SpawnInitialTerrainFood();
        
        GD.Print($"TerrainAwareFoodSpawner: Initialized with {activeFoodItems.Count} terrain-aware food items");
    }
    
    public override void _Process(double delta)
    {
        spawnTimer += (float)delta;
        seasonTimer += (float)delta;
        
        // Clean up consumed food items and update cache
        CleanupConsumedFood();
        
        // Spawn new food if needed
        if (spawnTimer >= spawnInterval && activeFoodItems.Count < maxFoodItems)
        {
            if (enableWorldGenIntegration)
            {
                SpawnTerrainAwareFood();
            }
            else
            {
                SpawnRandomFood(); // Fallback to original method
            }
            spawnTimer = 0f;
        }
        
        // Update seasonal variation every 60 seconds
        if (enableSeasonalVariation && seasonTimer >= 60f)
        {
            UpdateSeasonalAvailability();
            seasonTimer = 0f;
        }
    }
    
    private void SpawnInitialTerrainFood()
    {
        if (!enableWorldGenIntegration || terrainManager == null)
        {
            SpawnInitialFood(); // Fallback to original method
            return;
        }
        
        int initialCount = maxFoodItems / 3; // Start with 1/3 the max food items
        
        for (int i = 0; i < initialCount; i++)
        {
            SpawnTerrainAwareFood();
        }
        
        GD.Print($"TerrainAwareFoodSpawner: Spawned {activeFoodItems.Count} initial terrain-aware food items");
    }
    
    private void SpawnTerrainAwareFood()
    {
        if (playerReference == null || terrainManager == null)
        {
            SpawnRandomFood(); // Fallback
            return;
        }
        
        // Get nearby chunks for terrain-aware spawning
        var playerChunk = GetChunkKeyFromPosition(playerReference.Position);
        var candidateChunks = GetNearbyChunks(playerChunk, 3);
        
        foreach (var chunkKey in candidateChunks)
        {
            // Check if this chunk needs more food
            if (ShouldSpawnFoodInChunk(chunkKey))
            {
                SpawnFoodInChunk(chunkKey);
                return; // Only spawn one food item per call
            }
        }
    }
      private bool ShouldSpawnFoodInChunk(Vector3I chunkKey)
    {
        // First check if the chunk is actually loaded
        Vector3I testVoxelCoord = chunkKey * 16; // Convert chunk key to voxel coordinates
        if (!terrainManager.IsChunkLoadedAtVoxelCoords(testVoxelCoord))
        {
            return false; // Don't spawn food in unloaded chunks
        }
        
        if (!chunkFoodCache.ContainsKey(chunkKey))
        {
            chunkFoodCache[chunkKey] = new List<FoodItem>();
        }
        
        var chunkFood = chunkFoodCache[chunkKey];
        chunkFood.RemoveAll(food => !IsInstanceValid(food) || food.isConsumed);
          // Calculate desired food count based on biome
        Vector3 chunkWorldPos = (Vector3)chunkKey * 12f; // Chunk size
        BiomeType biome = biomeManager?.GetBiomeAt(chunkWorldPos) ?? BiomeType.Plains;
        FoodDistribution distribution = biomeManager?.GetFoodDistributionForBiome(biome) ?? new FoodDistribution { density = 1.0f };
        
        int maxFoodInChunk = Mathf.RoundToInt(chunkFoodDensity * distribution.density * 12f); // Base 12 food per chunk (tripled)
        
        return chunkFood.Count < maxFoodInChunk;
    }
    
    private void SpawnFoodInChunk(Vector3I chunkKey)
    {
        Vector3 chunkWorldPos = (Vector3)chunkKey * 12f; // Chunk size
        BiomeType biome = biomeManager?.GetBiomeAt(chunkWorldPos) ?? BiomeType.Plains;
        FoodDistribution distribution = biomeManager?.GetFoodDistributionForBiome(biome) ?? 
            new FoodDistribution { grassChance = 0.6f, berryChance = 0.3f, meatChance = 0.1f, density = 1.0f };
        
        // Choose food type based on biome
        FoodType foodType = ChooseBiomeAwareFoodType(distribution, biome);
        
        // Find surface position in chunk
        Vector3 spawnPosition = FindSurfacePositionInChunk(chunkKey, foodType, biome);
        
        if (spawnPosition == Vector3.Zero)
        {
            return; // Failed to find suitable position
        }
        
        // Spawn the food item
        SpawnFoodAtPosition(foodType, spawnPosition, chunkKey);
    }
    
    private Vector3 FindSurfacePositionInChunk(Vector3I chunkKey, FoodType foodType, BiomeType biome)
    {
        Vector3 chunkWorldPos = (Vector3)chunkKey * 12f;
        int attempts = 0;
        int maxAttempts = 15;
        
        while (attempts < maxAttempts)
        {
            // Random position within chunk
            Vector3 candidatePos = chunkWorldPos + new Vector3(
                rng.RandfRange(0f, 12f),
                0f,
                rng.RandfRange(0f, 12f)
            );
            
            // Find surface height using terrain voxel data
            Vector3 surfacePos = FindSurfaceHeight(candidatePos);
              if (surfacePos != Vector3.Zero && 
                IsPositionSuitableForFood(surfacePos, foodType, biome) &&
                !IsTooCloseToExistingFood(surfacePos, 1.0f))
            {
                return surfacePos + Vector3.Up * 0.5f; // Slightly above surface
            }
            
            attempts++;
        }
        
        return Vector3.Zero;
    }
    
    private Vector3 FindSurfaceHeight(Vector3 position)
    {
        if (terrainManager == null)
        {
            return FindGroundPositionRaycast(position);
        }
          // Use voxel data to find surface
        int maxHeight = 50;
        int minHeight = -10;
        
        // First check if any chunk is loaded in this area
        Vector3I testCoord = new Vector3I(
            Mathf.RoundToInt(position.X),
            0,
            Mathf.RoundToInt(position.Z)
        );
        
        if (!terrainManager.IsChunkLoadedAtVoxelCoords(testCoord))
        {
            // No chunk loaded here, fallback to raycast
            return FindGroundPositionRaycast(position);
        }
        
        for (int y = maxHeight; y >= minHeight; y--)
        {
            Vector3I voxelCoord = new Vector3I(
                Mathf.RoundToInt(position.X),
                y,
                Mathf.RoundToInt(position.Z)
            );
            
            if (terrainManager.GetVoxel(voxelCoord, out byte voxelData))
            {
                if (voxelData > 0) // Solid voxel found
                {
                    // Check if the voxel above is empty (surface)
                    Vector3I aboveCoord = voxelCoord + Vector3I.Up;
                    if (terrainManager.GetVoxel(aboveCoord, out byte aboveData) && aboveData == 0)
                    {
                        return new Vector3(position.X, y + 1, position.Z);
                    }
                }
            }
        }
        
        // Fallback to raycast if voxel method fails
        return FindGroundPositionRaycast(position);
    }
    
    private Vector3 FindGroundPositionRaycast(Vector3 position)
    {
        var spaceState = GetViewport().World3D.DirectSpaceState;
        var from = position + Vector3.Up * 50f;
        var to = position + Vector3.Down * 50f;
        
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);
        
        if (result.Count > 0)
        {
            return (Vector3)result["position"];
        }
        
        return Vector3.Zero;
    }
    
    private bool IsPositionSuitableForFood(Vector3 position, FoodType foodType, BiomeType biome)
    {
        // Check biome suitability
        if (biomeManager != null && !biomeManager.IsPositionSuitableForFood(position, foodType, biome))
        {
            return false;
        }
        
        // Check height constraints
        if (position.Y < -10f || position.Y > 100f)
        {
            return false;
        }
        
        // Check slope (approximate using nearby positions)
        float slopeThreshold = 0.8f; // Maximum slope for food spawning
        Vector3 nearby1 = FindSurfaceHeight(position + Vector3.Right);
        Vector3 nearby2 = FindSurfaceHeight(position + Vector3.Forward);
        
        if (nearby1 != Vector3.Zero && nearby2 != Vector3.Zero)
        {
            float slope1 = Mathf.Abs(nearby1.Y - position.Y);
            float slope2 = Mathf.Abs(nearby2.Y - position.Y);
            
            if (slope1 > slopeThreshold || slope2 > slopeThreshold)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private FoodType ChooseBiomeAwareFoodType(FoodDistribution distribution, BiomeType biome)
    {
        float seasonalMultiplierGrass = GetSeasonalMultiplier(FoodType.Grass);
        float seasonalMultiplierBerries = GetSeasonalMultiplier(FoodType.Berries);
        // Meat is no longer spawned naturally here

        float grassChance = distribution.grassChance * seasonalMultiplierGrass;
        float berryChance = distribution.berryChance * seasonalMultiplierBerries;
        // float meatChance = distribution.meatChance * GetSeasonalMultiplier(FoodType.Meat); // Removed

        float totalChance = grassChance + berryChance;

        if (totalChance <= 0) return FoodType.Grass; // Default if no chances

        float roll = rng.Randf() * totalChance;

        if (roll < grassChance)
            return FoodType.Grass;
        else
            return FoodType.Berries;
        // No longer returning FoodType.Meat from this method
    }
    
    private void SpawnFoodAtPosition(FoodType foodType, Vector3 position, Vector3I chunkKey)
    {
        PackedScene foodScene = GetFoodScene(foodType);
        
        if (foodScene == null)
        {
            GD.PrintErr($"TerrainAwareFoodSpawner: No scene assigned for food type {foodType}");
            return;
        }
        
        try
        {
            FoodItem foodItem = foodScene.Instantiate<FoodItem>();
            if (foodItem == null)
            {
                GD.PrintErr($"TerrainAwareFoodSpawner: Failed to instantiate food scene for {foodType}");
                return;
            }
            
            AddChild(foodItem);
            foodItem.Position = position;
            
            activeFoodItems.Add(foodItem);
            
            // Add to chunk cache
            if (!chunkFoodCache.ContainsKey(chunkKey))
            {
                chunkFoodCache[chunkKey] = new List<FoodItem>();
            }
            chunkFoodCache[chunkKey].Add(foodItem);
            
            BiomeType biome = biomeManager?.GetBiomeAt(position) ?? BiomeType.Plains;
            GD.Print($"TerrainAwareFoodSpawner: Spawned {foodType} in {biome} biome at {position}. Total food: {activeFoodItems.Count}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"TerrainAwareFoodSpawner: Error spawning {foodType}: {e.Message}");
        }
    }
    
    // Original methods for fallback compatibility
    private void SpawnInitialFood()
    {
        int initialCount = maxFoodItems / 2;
        
        for (int i = 0; i < initialCount; i++)
        {
            SpawnRandomFood();
        }
    }
    
    private void SpawnRandomFood()
    {
        if (playerReference == null) return;
        
        FoodType foodType = ChooseRandomFoodType();
        PackedScene foodScene = GetFoodScene(foodType);
        
        if (foodScene == null) return;
        
        Vector3 spawnPosition = FindRandomSpawnPosition();
        if (spawnPosition == Vector3.Zero) return;
        
        try
        {
            FoodItem foodItem = foodScene.Instantiate<FoodItem>();
            if (foodItem == null) return;
            
            AddChild(foodItem);
            foodItem.Position = spawnPosition;
            activeFoodItems.Add(foodItem);
        }
        catch (Exception e)
        {
            GD.PrintErr($"TerrainAwareFoodSpawner: Error in fallback spawning: {e.Message}");
        }
    }
    
    private FoodType ChooseRandomFoodType()
    {
        float roll = rng.Randf();
        
        if (roll < 0.6f)
            return FoodType.Grass;
        else if (roll < 0.9f)
            return FoodType.Berries;
        else
            return FoodType.Meat;
    }
    
    private Vector3 FindRandomSpawnPosition()
    {
        int maxAttempts = 20;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = rng.Randf() * Mathf.Tau;
            float distance = rng.RandfRange(10f, spawnRadius);
            
            Vector3 candidatePos = playerReference.Position + new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
            
            Vector3 groundPos = FindGroundPositionRaycast(candidatePos);
            
            if (groundPos != Vector3.Zero && !IsTooCloseToExistingFood(groundPos, 1.5f))
            {
                return groundPos + Vector3.Up * 0.5f;
            }
        }
        
        return Vector3.Zero;
    }
    
    private PackedScene GetFoodScene(FoodType foodType)
    {
        switch (foodType)
        {
            case FoodType.Grass:
                return grassFoodScene;
            case FoodType.Berries:
                return berryFoodScene;
            case FoodType.Meat:
                return meatFoodScene;
            default:
                return grassFoodScene;
        }
    }
    
    // Utility methods
    private Vector3I GetChunkKeyFromPosition(Vector3 position)
    {
        int chunkSize = 12; // Should match Chunk.CHUNK_SIZE
        return new Vector3I(
            Mathf.FloorToInt(position.X / chunkSize),
            Mathf.FloorToInt(position.Y / chunkSize),
            Mathf.FloorToInt(position.Z / chunkSize)
        );
    }
    
    private List<Vector3I> GetNearbyChunks(Vector3I centerChunk, int radius)
    {
        var chunks = new List<Vector3I>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                for (int y = -1; y <= 1; y++) // Limited Y range for surface spawning
                {
                    chunks.Add(centerChunk + new Vector3I(x, y, z));
                }
            }
        }
        
        return chunks;
    }
    
    private bool IsTooCloseToExistingFood(Vector3 position, float minDistance)
    {
        foreach (var food in activeFoodItems)
        {
            if (IsInstanceValid(food) && !food.isConsumed)
            {
                if (food.Position.DistanceTo(position) < minDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private void CleanupConsumedFood()
    {
        activeFoodItems.RemoveAll(food => !IsInstanceValid(food) || food.isConsumed);
        
        // Clean up chunk cache
        foreach (var chunkKey in chunkFoodCache.Keys.ToList())
        {
            chunkFoodCache[chunkKey].RemoveAll(food => !IsInstanceValid(food) || food.isConsumed);
            
            if (chunkFoodCache[chunkKey].Count == 0)
            {
                chunkFoodCache.Remove(chunkKey);
            }
        }
    }
    
    private float GetCurrentSeasonalFactor()
    {
        // Simple seasonal cycle based on game time
        float dayLength = 300f; // 5 minutes per day
        float seasonLength = dayLength * 30f; // 30 days per season
        float yearLength = seasonLength * 4f;
        
        float currentTime = seasonTimer + (float)Time.GetTicksMsec() / 1000f;
        float seasonProgress = (currentTime % yearLength) / seasonLength;
        
        return seasonProgress; // 0-4 representing spring, summer, fall, winter
    }
    
    private void UpdateSeasonalAvailability()
    {
        // This could trigger seasonal food migrations, spoilage, growth cycles, etc.
        float seasonalFactor = GetCurrentSeasonalFactor();
        int currentSeason = Mathf.FloorToInt(seasonalFactor);
        
        string[] seasonNames = { "Spring", "Summer", "Fall", "Winter" };
        if (currentSeason < seasonNames.Length)
        {
            GD.Print($"TerrainAwareFoodSpawner: Current season - {seasonNames[currentSeason]}");
        }
    }
    
    // Public interface methods
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
        chunkFoodCache.Clear();
        GD.Print("TerrainAwareFoodSpawner: Cleared all food items");
    }
    
    public BiomeType GetBiomeAtPosition(Vector3 position)
    {
        return biomeManager?.GetBiomeAt(position) ?? BiomeType.Plains;
    }
    
    public FoodDistribution GetFoodDistributionAtPosition(Vector3 position)
    {
        BiomeType biome = GetBiomeAtPosition(position);
        return biomeManager?.GetFoodDistributionForBiome(biome) ?? 
            new FoodDistribution { grassChance = 0.6f, berryChance = 0.3f, meatChance = 0.1f, density = 1.0f };
    }
}

public struct SeasonalAvailability
{
    public float springMultiplier;
    public float summerMultiplier;
    public float fallMultiplier;
    public float winterMultiplier;
    
    public float GetCurrentMultiplier(float seasonalFactor)
    {
        int season = Mathf.FloorToInt(seasonalFactor) % 4;
        float progress = seasonalFactor - Mathf.Floor(seasonalFactor);
        
        switch (season)
        {
            case 0: // Spring
                return Mathf.Lerp(winterMultiplier, springMultiplier, progress);
            case 1: // Summer
                return Mathf.Lerp(springMultiplier, summerMultiplier, progress);
            case 2: // Fall
                return Mathf.Lerp(summerMultiplier, fallMultiplier, progress);
            case 3: // Winter
                return Mathf.Lerp(fallMultiplier, winterMultiplier, progress);
            default:
                return 1.0f;
        }
    }
}
