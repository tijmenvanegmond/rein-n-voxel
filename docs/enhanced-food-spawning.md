# Enhanced Food Spawning System

## Overview

The enhanced food spawning system integrates with the world generation to create realistic vegetation distribution based on biomes, terrain characteristics, and seasonal variations. This system replaces the simple player-centered random food spawning with a sophisticated ecosystem simulation.

## Key Components

### BiomeManager

The `BiomeManager` determines environmental characteristics at any world position using noise-based generation:

- **Temperature**: Affects what types of food can grow
- **Humidity**: Influences vegetation density and berry availability  
- **Elevation**: Determines terrain type and food distribution
- **Biome Types**: Plains, Hills, Mountains, Valley, Plateau

#### Biome Characteristics

| Biome | Grass | Berries | Meat | Density | Notes |
|-------|-------|---------|------|---------|-------|
| Plains | 70% | 25% | 5% | 1.2x | Abundant vegetation |
| Hills | 50% | 40% | 10% | 1.0x | Mixed terrain |
| Mountains | 30% | 20% | 50% | 0.6x | Harsh environment, more carrion |
| Valley | 80% | 15% | 5% | 1.5x | Fertile lowlands |
| Plateau | 40% | 30% | 30% | 0.8x | Elevated plains |

### TerrainAwareFoodSpawner

The enhanced spawner integrates with the voxel terrain system to:

- **Surface Detection**: Uses voxel data to find exact ground positions
- **Slope Analysis**: Avoids spawning food on steep terrain
- **Chunk-Based Distribution**: Distributes food evenly across terrain chunks
- **Biome Awareness**: Adjusts food types based on environmental factors
- **Seasonal Variation**: Changes food availability throughout the year

#### Key Features

**World Generation Integration**
- Spawns food during chunk generation
- Maintains persistent food distribution across chunks
- Respects terrain characteristics and accessibility

**Realistic Placement**
- Food spawns on appropriate surface types
- Considers slope, elevation, and biome suitability
- Maintains minimum distances between food items

**Performance Optimization**
- Chunk-based caching system
- Efficient voxel-based surface detection
- Configurable density and spawn intervals

## Seasonal System

### Seasonal Multipliers

Food availability changes throughout the year:

**Spring**
- Grass: +50% (new growth)
- Berries: -50% (not yet ripe)
- Meat: -20% (animals healthy)

**Summer**
- Grass: +20% (peak growth)
- Berries: +80% (ripe season)
- Meat: -30% (animals thriving)

**Fall**
- Grass: -20% (dying back)
- Berries: +30% (late harvest)
- Meat: +10% (hunting season)

**Winter**
- Grass: -70% (dormant)
- Berries: -90% (scarce)
- Meat: +40% (harsh conditions)

### Implementation

The seasonal system uses a continuous time-based cycle:
- 5 minutes = 1 day
- 30 days = 1 season
- 4 seasons = 1 year

Food spawning probabilities are adjusted in real-time based on the current seasonal factor.

## Configuration Options

### TerrainAwareFoodSpawner Properties

```csharp
[Export] public int maxFoodItems = 100;          // Maximum food items in world
[Export] public float spawnRadius = 150f;        // Spawn radius around player
[Export] public float spawnInterval = 8f;        // Seconds between spawn attempts
[Export] public float chunkFoodDensity = 0.3f;   // Food items per chunk area
[Export] public bool enableWorldGenIntegration = true;  // Use terrain integration
[Export] public bool enableSeasonalVariation = true;    // Enable seasonal changes
```

### BiomeManager Noise Settings

- **Temperature Noise**: Frequency 0.01, affects food type suitability
- **Humidity Noise**: Frequency 0.008, influences vegetation density
- **Elevation Noise**: Frequency 0.005, determines biome classification

## Integration with Existing Systems

### Mob Food System Compatibility

The enhanced food spawning is fully compatible with the existing mob food system:

- Mobs can find and consume all food types
- Food preferences are respected (herbivores prefer grass/berries)
- Combat system still spawns carrion when mobs die
- Health and hunger systems work unchanged

### Performance Considerations

**Chunk-Based Caching**
- Food items are tracked per chunk
- Cleanup only processes relevant chunks
- Avoids full-world iterations

**Voxel Integration**
- Direct voxel queries for surface detection
- No expensive raycast operations for initial placement
- Fallback to raycast when voxel data unavailable

**Configurable Density**
- Adjustable food density per biome
- Balances ecosystem realism with performance
- Scalable for different world sizes

## Usage Instructions

### Basic Setup

1. Add `BiomeManager` node to scene
2. Add `TerrainAwareFoodSpawner` node to scene
3. Configure `GameController` to use enhanced spawning
4. Set `useTerrainAwareFoodSpawning = true`

### Scene Configuration

```
Main Scene
├── TerrainManager (existing)
├── BiomeManager (new)
├── TerrainAwareFoodSpawner (new)
├── FoodSpawner (fallback, optional)
└── GameController (updated)
```

### Food Scene Assignment

Assign the same food scenes to both spawners:
- `grassFoodScene`: grass food item scene
- `berryFoodScene`: berry food item scene  
- `meatFoodScene`: meat/carrion food item scene

## Future Enhancements

### Planned Features

**Advanced Biome System**
- Temperature and humidity gradients
- Seasonal weather effects
- Climate change simulation

**Ecosystem Interactions**
- Food chain relationships
- Predator-prey dynamics
- Population-based spawning

**Player Impact**
- Terrain modification affects local biomes
- Player actions influence food availability
- Conservation vs exploitation mechanics

**Multiplayer Considerations**
- Synchronized seasonal cycles
- Shared biome state
- Collaborative ecosystem management

## Technical Notes

### Surface Detection Algorithm

1. Start from high Y position above target
2. Search downward through voxel data
3. Find first solid voxel with air above
4. Validate slope and accessibility
5. Return surface position + small offset

### Biome Determination Logic

```csharp
// Environmental factors (normalized 0-1)
float temperature = temperatureNoise.GetNoise2D(x, z);
float humidity = humidityNoise.GetNoise2D(x, z);
float elevation = elevationNoise.GetNoise2D(x, z);

// Biome classification
if (elevation > 0.7f) return Mountains;
else if (elevation > 0.5f && temperature > 0.4f) return Hills;
// ... additional classification rules
```

### Seasonal Calculation

```csharp
float dayLength = 300f;           // 5 minutes
float seasonLength = dayLength * 30f;  // 30 days
float currentTime = GetGameTime();
float seasonalFactor = (currentTime % (seasonLength * 4)) / seasonLength;
```

## Debugging and Monitoring

### Debug Information

The system provides detailed logging:
- Biome detection at spawn locations
- Seasonal factor changes
- Food spawning success/failure reasons
- Chunk-based food distribution

### Performance Metrics

Monitor these values for optimization:
- Active food items per chunk
- Voxel query performance
- Seasonal update frequency
- Memory usage of chunk cache

### Common Issues

**Food Not Spawning**
- Check biome manager initialization
- Verify terrain manager is available
- Ensure voxel data is generated
- Confirm surface detection is working

**Poor Distribution**
- Adjust chunk food density
- Check biome classification accuracy
- Verify slope calculations
- Review minimum distance constraints

## Conclusion

The enhanced food spawning system provides a robust foundation for realistic ecosystem simulation while maintaining compatibility with existing mob behaviors. The biome-based approach ensures that food distribution feels natural and responds to environmental factors, creating a more immersive and dynamic world for players and mobs to inhabit.
