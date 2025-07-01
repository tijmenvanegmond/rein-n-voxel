using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GrassSpawner : Node
{
    [Export]
    public PackedScene grassFoodScene { get; set; }    [Export]
    public int maxGrassPatches { get; set; } = 500;
    [Export]
    public float grassSpawnRadius { get; set; } = 150f;
    [Export]
    public float grassGrowthInterval { get; set; } = 8f; // Time between growth cycles
    [Export]
    public float grassSpreadChance { get; set; } = 0.6f; // Chance for grass to spread
    [Export]
    public float grassSpreadRadius { get; set; } = 5f; // How far grass can spread
    [Export]
    public float minGrassDistance { get; set; } = 1.0f; // Minimum distance between grass patches
    [Export]
    public bool enableSeasonalGrowth { get; set; } = true;
    [Export]
    public bool enableBiomeAwareGrowth { get; set; } = true;
    
    private List<GrassPatch> grassPatches = new List<GrassPatch>();
    private Dictionary<Vector3I, List<GrassPatch>> grassChunkCache = new Dictionary<Vector3I, List<GrassPatch>>();
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private float growthTimer = 0f;
    private float seasonTimer = 0f;
    private Node3D playerReference;
    private TerrainManager terrainManager;
    private BiomeManager biomeManager;
      // Growth rates per biome
    private Dictionary<BiomeType, float> biomeGrowthRates = new Dictionary<BiomeType, float>
    {
        { BiomeType.Plains, 2.5f },      // Excellent for grass
        { BiomeType.Valley, 2.8f },      // Perfect fertile valley
        { BiomeType.Hills, 1.8f },       // Good growth
        { BiomeType.Plateau, 1.4f },     // Moderate conditions
        { BiomeType.Mountains, 0.8f }    // Challenging but improved
    };
      // Seasonal growth multipliers
    private Dictionary<string, float> seasonalGrowthRates = new Dictionary<string, float>
    {
        { "spring", 3.0f },   // Explosive spring growth
        { "summer", 2.0f },   // Strong summer growth
        { "fall", 1.2f },     // Continued growth in fall
        { "winter", 0.6f }    // Reduced but still present winter growth
    };
    
    public override void _Ready()
    {
        rng.Randomize();
        AddToGroup("grass_spawner");
        
        // Find references
        playerReference = GetTree().GetFirstNodeInGroup("player") as Node3D;
        terrainManager = GetTree().GetFirstNodeInGroup("terrain_manager") as TerrainManager;
        biomeManager = GetTree().GetFirstNodeInGroup("biome_manager") as BiomeManager;
        
        GD.Print("GrassSpawner: Initialized with growth and spreading mechanics");
    }
    
    public void InitializeGrassSpawning()
    {
        // Spawn initial grass patches across the world
        SpawnInitialGrassPatches();
        GD.Print($"GrassSpawner: Spawned {grassPatches.Count} initial grass patches");
    }
    
    public override void _Process(double delta)
    {
        growthTimer += (float)delta;
        seasonTimer += (float)delta;
        
        // Update grass growth and spreading
        if (growthTimer >= grassGrowthInterval)
        {
            ProcessGrassGrowth();
            growthTimer = 0f;
        }
        
        // Clean up consumed or invalid grass
        CleanupInvalidGrass();
    }
    
    private void SpawnInitialGrassPatches()
    {
        if (playerReference == null) return;
        
        int patchesSpawned = 0;
        int attempts = 0;
        int maxAttempts = maxGrassPatches * 3;
        
        while (patchesSpawned < maxGrassPatches && attempts < maxAttempts)
        {
            // Generate random position around player
            Vector3 randomPos = playerReference.Position + new Vector3(
                rng.RandfRange(-grassSpawnRadius, grassSpawnRadius),
                0f,
                rng.RandfRange(-grassSpawnRadius, grassSpawnRadius)
            );
            
            // Find surface position
            Vector3 surfacePos = FindSurfacePosition(randomPos);
            
            if (surfacePos != Vector3.Zero && IsPositionSuitableForGrass(surfacePos))
            {
                SpawnGrassPatch(surfacePos);
                patchesSpawned++;
            }
            
            attempts++;
        }
    }
    
    private void ProcessGrassGrowth()
    {
        List<GrassPatch> grassToGrow = new List<GrassPatch>(grassPatches);
        
        foreach (var grass in grassToGrow)
        {
            if (!GodotObject.IsInstanceValid(grass.foodItem))
            {
                continue;
            }
            
            // Calculate growth chance based on biome and season
            float growthChance = CalculateGrowthChance(grass.position);
            
            if (rng.Randf() < growthChance * grassSpreadChance)
            {
                TrySpreadGrass(grass);
            }
            
            // Chance for grass to mature (increase nutrition)
            if (rng.Randf() < growthChance * 0.1f)
            {
                MaturityGrass(grass);
            }
        }
    }
    
    private void TrySpreadGrass(GrassPatch sourceGrass)
    {
        if (grassPatches.Count >= maxGrassPatches)
        {
            return; // Maximum grass reached
        }
        
        // Try to spread in random directions
        for (int i = 0; i < 3; i++)
        {
            Vector3 spreadDirection = new Vector3(
                rng.RandfRange(-1f, 1f),
                0f,
                rng.RandfRange(-1f, 1f)
            ).Normalized();
            
            Vector3 newPosition = sourceGrass.position + spreadDirection * rng.RandfRange(1f, grassSpreadRadius);
            Vector3 surfacePos = FindSurfacePosition(newPosition);
            
            if (surfacePos != Vector3.Zero && 
                IsPositionSuitableForGrass(surfacePos) &&
                !IsGrassNearby(surfacePos, minGrassDistance))
            {
                SpawnGrassPatch(surfacePos);
                GD.Print($"GrassSpawner: Grass spread to {surfacePos}");
                break; // Only spread once per growth cycle
            }
        }
    }
    
    private void MaturityGrass(GrassPatch grass)
    {
        if (grass.maturityLevel < 3) // Max maturity level
        {
            grass.maturityLevel++;
            
            // Increase nutrition and size based on maturity
            float nutritionMultiplier = 1f + (grass.maturityLevel * 0.3f);
            grass.foodItem.nutritionValue = grass.baseNutrition * nutritionMultiplier;
            
            // Scale the grass visually
            float scaleMultiplier = 1f + (grass.maturityLevel * 0.2f);
            grass.foodItem.Scale = Vector3.One * scaleMultiplier;
            
            // Update material to show maturity (richer green color)
            UpdateGrassAppearance(grass);
            
            GD.Print($"GrassSpawner: Grass matured to level {grass.maturityLevel}, nutrition: {grass.foodItem.nutritionValue}");
        }
    }
    
    private void UpdateGrassAppearance(GrassPatch grass)
    {
        var meshInstance = grass.foodItem.GetNode<MeshInstance3D>("MeshInstance3D");
        if (meshInstance?.MaterialOverride is StandardMaterial3D material)
        {
            // Richer green color for mature grass
            float maturityFactor = grass.maturityLevel / 3f;
            Color baseColor = new Color(0.2f, 0.7f, 0.3f);
            Color matureColor = new Color(0.1f, 0.8f, 0.2f);
            
            material.AlbedoColor = baseColor.Lerp(matureColor, maturityFactor);
            
            // Stronger emission for mature grass
            Color baseEmission = new Color(0.05f, 0.2f, 0.08f);
            Color matureEmission = new Color(0.08f, 0.3f, 0.12f);
            material.Emission = baseEmission.Lerp(matureEmission, maturityFactor);
        }
    }
    
    private float CalculateGrowthChance(Vector3 position)
    {
        float baseChance = 0.3f;
        
        // Apply biome modifier
        if (enableBiomeAwareGrowth && biomeManager != null)
        {
            BiomeType biome = biomeManager.GetBiomeAt(position);
            baseChance *= biomeGrowthRates.GetValueOrDefault(biome, 1f);
        }
        
        // Apply seasonal modifier
        if (enableSeasonalGrowth)
        {
            string currentSeason = GetCurrentSeason();
            baseChance *= seasonalGrowthRates.GetValueOrDefault(currentSeason, 1f);
        }
        
        return Mathf.Clamp(baseChance, 0f, 1f);
    }
    
    private string GetCurrentSeason()
    {
        // Simple seasonal cycle based on timer (60 seconds = 1 year)
        float yearProgress = (seasonTimer % 60f) / 60f;
        
        if (yearProgress < 0.25f) return "spring";
        else if (yearProgress < 0.5f) return "summer";
        else if (yearProgress < 0.75f) return "fall";
        else return "winter";
    }
    
    private Vector3 FindSurfacePosition(Vector3 position)
    {
        if (terrainManager != null)
        {
            return terrainManager.GetSurfaceHeightAt(position);
        }
        
        // Fallback raycast method
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
    
    private bool IsPositionSuitableForGrass(Vector3 position)
    {
        // Check height constraints
        if (position.Y < -5f || position.Y > 80f)
        {
            return false;
        }
        
        // Check if position is near water (grass likes moisture)
        // This is a simplified check - in a real system you'd check actual water bodies
        
        // Check slope - grass prefers flatter areas
        float slopeThreshold = 1.2f;
        Vector3 nearby1 = FindSurfacePosition(position + Vector3.Right);
        Vector3 nearby2 = FindSurfacePosition(position + Vector3.Forward);
        
        if (nearby1 != Vector3.Zero && nearby2 != Vector3.Zero)
        {
            float slope1 = Mathf.Abs(nearby1.Y - position.Y);
            float slope2 = Mathf.Abs(nearby2.Y - position.Y);
            
            if (slope1 > slopeThreshold || slope2 > slopeThreshold)
            {
                return false;
            }
        }
        
        // Check biome suitability
        if (enableBiomeAwareGrowth && biomeManager != null)
        {
            BiomeType biome = biomeManager.GetBiomeAt(position);
            // Mountains are harsh for grass growth
            if (biome == BiomeType.Mountains && rng.Randf() > 0.3f)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private bool IsGrassNearby(Vector3 position, float radius)
    {
        foreach (var grass in grassPatches)
        {
            if (grass.position.DistanceTo(position) < radius)
            {
                return true;
            }
        }
        return false;
    }
    
    private void SpawnGrassPatch(Vector3 position)
    {
        if (grassFoodScene == null)
        {
            GD.PrintErr("GrassSpawner: No grass food scene assigned");
            return;
        }
        
        try
        {
            FoodItem grassItem = grassFoodScene.Instantiate<FoodItem>();
            if (grassItem == null)
            {
                GD.PrintErr("GrassSpawner: Failed to instantiate grass scene");
                return;
            }
            
            AddChild(grassItem);
            grassItem.Position = position + Vector3.Up * 0.1f; // Slightly above surface
            
            // Create grass patch data
            GrassPatch grassPatch = new GrassPatch
            {
                foodItem = grassItem,
                position = position,
                maturityLevel = 0,
                baseNutrition = grassItem.nutritionValue,
                spawnTime = Time.GetUnixTimeFromSystem()
            };
            
            grassPatches.Add(grassPatch);
            
            // Add to chunk cache for efficient lookup
            Vector3I chunkKey = new Vector3I(
                Mathf.FloorToInt(position.X / 12f),
                0,
                Mathf.FloorToInt(position.Z / 12f)
            );
            
            if (!grassChunkCache.ContainsKey(chunkKey))
            {
                grassChunkCache[chunkKey] = new List<GrassPatch>();
            }
            grassChunkCache[chunkKey].Add(grassPatch);
            
            BiomeType biome = biomeManager?.GetBiomeAt(position) ?? BiomeType.Plains;
            GD.Print($"GrassSpawner: Spawned grass in {biome} biome at {position}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"GrassSpawner: Error spawning grass: {e.Message}");
        }
    }
    
    private void CleanupInvalidGrass()
    {
        grassPatches.RemoveAll(grass => 
        {
            if (!GodotObject.IsInstanceValid(grass.foodItem) || grass.foodItem.IsConsumed())
            {
                // Remove from chunk cache
                Vector3I chunkKey = new Vector3I(
                    Mathf.FloorToInt(grass.position.X / 12f),
                    0,
                    Mathf.FloorToInt(grass.position.Z / 12f)
                );
                
                if (grassChunkCache.ContainsKey(chunkKey))
                {
                    grassChunkCache[chunkKey].Remove(grass);
                    if (grassChunkCache[chunkKey].Count == 0)
                    {
                        grassChunkCache.Remove(chunkKey);
                    }
                }
                
                return true;
            }
            return false;
        });
    }
    
    // Public methods for external systems
    public int GetGrassCount()
    {
        return grassPatches.Count;
    }
    
    public List<GrassPatch> GetGrassInRadius(Vector3 center, float radius)
    {
        return grassPatches.Where(grass => 
            grass.position.DistanceTo(center) <= radius
        ).ToList();
    }
    
    public void ForceGrassGrowth()
    {
        ProcessGrassGrowth();
    }
    
    public void ClearAllGrass()
    {
        foreach (var grass in grassPatches)
        {
            if (GodotObject.IsInstanceValid(grass.foodItem))
            {
                grass.foodItem.QueueFree();
            }
        }
        
        grassPatches.Clear();
        grassChunkCache.Clear();
        GD.Print("GrassSpawner: Cleared all grass patches");
    }
}

// Data structure for grass patches
public class GrassPatch
{
    public FoodItem foodItem;
    public Vector3 position;
    public int maturityLevel; // 0-3, affects nutrition and size
    public float baseNutrition; // Original nutrition value
    public double spawnTime; // When this patch was created
}
