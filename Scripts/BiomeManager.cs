using System;
using System.Collections.Generic;
using Godot;

public enum BiomeType
{
    Plains,
    Hills,
    Mountains,
    Valley,
    Plateau
}

public partial class BiomeManager : Node
{
    private FastNoiseLite temperatureNoise;
    private FastNoiseLite humidityNoise;
    private FastNoiseLite elevationNoise;
    
    public override void _Ready()
    {
        // Initialize noise generators for biome determination
        temperatureNoise = new FastNoiseLite();
        temperatureNoise.Seed = (int)Time.GetTicksMsec() + 100;
        temperatureNoise.Frequency = 0.01f;
        temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        
        humidityNoise = new FastNoiseLite();
        humidityNoise.Seed = (int)Time.GetTicksMsec() + 200;
        humidityNoise.Frequency = 0.008f;
        humidityNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        
        elevationNoise = new FastNoiseLite();
        elevationNoise.Seed = (int)Time.GetTicksMsec() + 300;
        elevationNoise.Frequency = 0.005f;
        elevationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        
        AddToGroup("biome_manager");
        
        GD.Print("BiomeManager: Initialized with procedural biome generation");
    }
    
    public BiomeType GetBiomeAt(Vector3 worldPosition)
    {
        float temperature = temperatureNoise.GetNoise2D(worldPosition.X, worldPosition.Z);
        float humidity = humidityNoise.GetNoise2D(worldPosition.X, worldPosition.Z);
        float elevation = elevationNoise.GetNoise2D(worldPosition.X, worldPosition.Z);
        
        // Normalize values to 0-1 range
        temperature = (temperature + 1f) * 0.5f;
        humidity = (humidity + 1f) * 0.5f;
        elevation = (elevation + 1f) * 0.5f;
        
        // Determine biome based on environmental factors
        if (elevation > 0.7f)
        {
            return BiomeType.Mountains;
        }
        else if (elevation > 0.5f && temperature > 0.4f)
        {
            return BiomeType.Hills;
        }
        else if (elevation < 0.3f)
        {
            return BiomeType.Valley;
        }
        else if (elevation > 0.6f && temperature < 0.4f)
        {
            return BiomeType.Plateau;
        }
        else
        {
            return BiomeType.Plains;
        }
    }
      public FoodDistribution GetFoodDistributionForBiome(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Plains:
                return new FoodDistribution
                {
                    grassChance = 0.85f,
                    berryChance = 0.12f,
                    meatChance = 0.03f,
                    density = 2.0f
                };
                
            case BiomeType.Hills:
                return new FoodDistribution
                {
                    grassChance = 0.75f,
                    berryChance = 0.22f,
                    meatChance = 0.03f,
                    density = 1.6f
                };
                
            case BiomeType.Mountains:
                return new FoodDistribution
                {
                    grassChance = 0.55f,
                    berryChance = 0.25f,
                    meatChance = 0.2f,
                    density = 1.2f
                };
                
            case BiomeType.Valley:
                return new FoodDistribution
                {
                    grassChance = 0.9f,
                    berryChance = 0.08f,
                    meatChance = 0.02f,
                    density = 2.5f
                };
                
            case BiomeType.Plateau:
                return new FoodDistribution
                {
                    grassChance = 0.65f,
                    berryChance = 0.25f,
                    meatChance = 0.1f,
                    density = 1.4f
                };
                
            default:                return new FoodDistribution
                {
                    grassChance = 0.8f,
                    berryChance = 0.18f,
                    meatChance = 0.02f,
                    density = 1.8f
                };
        }
    }
    
    public float GetBiomeTemperature(Vector3 worldPosition)
    {
        return (temperatureNoise.GetNoise2D(worldPosition.X, worldPosition.Z) + 1f) * 0.5f;
    }
    
    public float GetBiomeHumidity(Vector3 worldPosition)
    {
        return (humidityNoise.GetNoise2D(worldPosition.X, worldPosition.Z) + 1f) * 0.5f;
    }
    
    public float GetBiomeElevation(Vector3 worldPosition)
    {
        return (elevationNoise.GetNoise2D(worldPosition.X, worldPosition.Z) + 1f) * 0.5f;
    }
    
    public bool IsPositionSuitableForFood(Vector3 position, FoodType foodType, BiomeType biome)
    {
        float temperature = GetBiomeTemperature(position);
        float humidity = GetBiomeHumidity(position);
        
        switch (foodType)
        {
            case FoodType.Grass:
                // Grass prefers moderate temperature and humidity
                return temperature > 0.3f && temperature < 0.8f && humidity > 0.2f;
                
            case FoodType.Berries:
                // Berries prefer moderate to high humidity and temperature
                return temperature > 0.4f && humidity > 0.4f;
                
            case FoodType.Meat:
                // Meat (carrion) can appear anywhere but more common in harsh environments
                return biome == BiomeType.Mountains || temperature < 0.3f || temperature > 0.8f;
                
            default:
                return true;
        }
    }

    // Enhanced grass-specific suitability checks
    public bool IsPositionSuitableForGrass(Vector3 position)
    {
        BiomeType biome = GetBiomeAt(position);
        float temperature = GetBiomeTemperature(position);
        float humidity = GetBiomeHumidity(position);
        float elevation = GetBiomeElevation(position);
        
        // Grass growth suitability factors
        float grassSuitability = 1.0f;
        
        // Temperature preference (moderate temperatures are best)
        if (temperature < 0.2f || temperature > 0.9f)
            grassSuitability *= 0.3f; // Very hot or cold is bad
        else if (temperature < 0.4f || temperature > 0.7f)
            grassSuitability *= 0.7f; // Slightly unfavorable
        
        // Humidity preference (grass needs some moisture)
        if (humidity < 0.1f)
            grassSuitability *= 0.2f; // Very dry is bad
        else if (humidity < 0.3f)
            grassSuitability *= 0.6f; // Somewhat dry
        else if (humidity > 0.8f)
            grassSuitability *= 1.2f; // High humidity is good
        
        // Elevation preference (lower elevations are better)
        if (elevation > 0.8f)
            grassSuitability *= 0.4f; // High elevation is harsh
        else if (elevation < 0.3f)
            grassSuitability *= 1.3f; // Low elevation (valleys) are ideal
        
        // Biome-specific modifiers
        switch (biome)
        {
            case BiomeType.Plains:
                grassSuitability *= 1.5f; // Perfect for grass
                break;
            case BiomeType.Valley:
                grassSuitability *= 1.4f; // Excellent for grass
                break;
            case BiomeType.Hills:
                grassSuitability *= 1.0f; // Normal grass growth
                break;
            case BiomeType.Plateau:
                grassSuitability *= 0.8f; // Challenging conditions
                break;
            case BiomeType.Mountains:
                grassSuitability *= 0.5f; // Harsh mountain conditions
                break;
        }
        
        return grassSuitability > 0.5f; // Threshold for grass viability
    }

    public float GetGrassGrowthRate(Vector3 position)
    {
        BiomeType biome = GetBiomeAt(position);
        float temperature = GetBiomeTemperature(position);
        float humidity = GetBiomeHumidity(position);
        
        float growthRate = 1.0f;
        
        // Optimal temperature range for grass growth
        if (temperature >= 0.4f && temperature <= 0.7f)
            growthRate *= 1.3f; // Optimal temperature
        else if (temperature < 0.2f || temperature > 0.8f)
            growthRate *= 0.4f; // Poor temperature
        
        // Humidity affects growth rate
        if (humidity >= 0.5f)
            growthRate *= 1.2f; // Good moisture
        else if (humidity < 0.2f)
            growthRate *= 0.5f; // Too dry
          // Biome growth rate modifiers
        switch (biome)
        {
            case BiomeType.Plains: growthRate *= 2.2f; break;
            case BiomeType.Valley: growthRate *= 2.5f; break;
            case BiomeType.Hills: growthRate *= 1.8f; break;
            case BiomeType.Plateau: growthRate *= 1.4f; break;
            case BiomeType.Mountains: growthRate *= 0.8f; break;
        }
        
        return Mathf.Clamp(growthRate, 0.2f, 3.5f);
    }
}

public struct FoodDistribution
{
    public float grassChance;
    public float berryChance;
    public float meatChance;
    public float density;
}
