# Rein-n-Voxel

A dynamic voxel-based world simulation built with Godot 4 and C#, featuring intelligent mob behaviors, faction systems, and procedural terrain generation.

## 📸 Screenshots

![Game Screenshot](/Screenshots/screenshot_2025-05-28T16-42-05.png)

*Main gameplay showing voxel terrain and intelligent mobs*

![Mob Interactions](/Screenshots/screenshot_2025-05-28T16-45-44.png)
*Different mob factions and their behaviors*

## 🎮 Features

### Intelligent Mob System
- **Faction-Based Behavior**: Mobs belong to different factions (Player, Neutral, Wild, Hostile) that influence their interactions
- **Dynamic Personalities**: Three distinct personality types with unique behaviors:
  - **Friendly**: Approach players, easy to befriend, green coloration
  - **Skittish**: Flee quickly from threats, orange coloration, prefer safety in numbers
  - **Neutral**: Balanced behavior, blue-gray coloration
- **State-Driven AI**: Wandering, Flocking, Fleeing, Investigating, and Idle states
- **Visual Faction Indicators**: Color-coded appearances based on faction and personality
- **Smart Flocking**: Mobs only flock with same-faction allies

### Modular Architecture
- **Component-Based Design**: Mob behaviors split into specialized components:
  - `MobMovement`: Physics and terrain navigation
  - `MobBehaviorStates`: State machine implementation
  - `MobPersonality`: Personality-driven behavior modifications
  - `MobTerrainInteraction`: Pathfinding and ground detection
  - `MobAppearance`: Visual customization and effects

### Terrain System
- **Marching Cubes**: GPU-accelerated voxel terrain generation
- **Dynamic Editing**: Real-time terrain modification
- **Smart Pathfinding**: Mobs navigate complex terrain intelligently

## 🛠️ Building

```bash
dotnet build
```

## 🏗️ Project Structure

```
Scripts/
├── Mob.cs                    # Core mob class with faction system
├── MobHandler.cs             # Mob spawning and management
├── MobTypes.cs               # Shared enums and types
├── Mob/                      # Modular behavior components
│   ├── MobMovement.cs
│   ├── MobBehaviorStates.cs
│   ├── MobPersonality.cs
│   ├── MobTerrainInteraction.cs
│   └── MobAppearance.cs
├── Player/                   # Player control system
├── TerrainManager.cs         # Voxel terrain generation
└── GameController.cs         # Main game logic
```

## 🤖 Mob Faction System

### Faction Types
- **Player**: Allied mobs that assist and follow the player
- **Neutral**: Independent mobs with no strong allegiances
- **Wild**: Natural creatures that avoid player contact
- **Hostile**: Aggressive entities (planned for future implementation)

### Faction Behaviors
- **Flocking**: Only occurs between same-faction mobs
- **Player Interaction**: 
  - Allied mobs investigate and approach players
  - Wild mobs flee at greater distances
  - Neutral mobs maintain cautious behavior
- **Event Propagation**: Faction influences how mobs respond to alerts and panic

### Visual Indicators
- **Player Faction**: Bright, warm colors with strong emission
- **Wild Faction**: Muted, earthy tones with dim emission
- **Hostile Faction**: Red-tinted appearance
- **Neutral Faction**: Standard personality-based colors

## 🎯 Mob Types

| Type | Default Faction | Personality | Characteristics |
|------|----------------|-------------|-----------------|
| SmallFriendly | Player | Friendly | Fast, approaches player, green glow |
| SmallSkittish | Wild | Skittish | Very fast, flees easily, orange glow |
| Medium | Neutral | Neutral | Balanced stats, blue-gray appearance |
| Large | Neutral | Neutral | Slow but thorough, investigates longer |

## 🔮 Future Features

- **Dynamic Faction Switching**: Player actions influence mob allegiances
- **Combat System**: Hostile faction implementation
- **Advanced AI**: Learning behaviors and memory systems
- **Multiplayer Support**: Faction-based team mechanics
- **Resource Economy**: Faction-specific resource gathering

## 🏃‍♂️ Getting Started

1. Open the project in Godot 4
2. Build the C# project with `dotnet build`
3. Run the main scene to spawn in the voxel world
4. Observe different mob behaviors and faction interactions
5. Use terrain editing tools to modify the environment

### 📸 Taking Screenshots

The game includes built-in screenshot functionality:

- **F12**: Take a screenshot (also works from ScreenshotManager)
- **F11**: Take a screenshot from player input (alternative binding)

Screenshots are automatically saved to the `Screenshots/` directory with timestamps.

**Available Methods:**
- **In-Game**: Press F12 or F11 while playing
- **Manual**: Call `TakeScreenshot()` on the ScreenshotManager node
- **Player Action**: The player controller has integrated screenshot functionality

Screenshots are saved as PNG files with automatic timestamping for easy organization.

## 📝 Development Notes

The project evolved from a monolithic 851-line Mob class into a clean, modular component architecture. The faction system was designed with extensibility in mind, preparing for future features like player faction switching and complex alliance mechanics.

**Build Status**: ✅ Compiles successfully with 0 errors