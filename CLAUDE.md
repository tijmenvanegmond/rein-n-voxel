# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rein-n-Voxel is a dynamic voxel-based world simulation built with **Godot 4** and **C#**, featuring intelligent mob behaviors, faction systems, and procedural terrain generation using Marching Cubes algorithm.

## Build and Development Commands

**⚠️ IMPORTANT: Requires Godot with C# Support (Godot Mono)**

This project requires **Godot Mono** (not standard Godot) to run C# scripts.

### Installation Options:

**Option 1: Homebrew (Recommended)**
```bash
brew install godot-mono
```

**Option 2: Manual Download**
- Download "Godot Engine - .NET" from https://godotengine.org/download
- Look for "Godot_v4.4.1-stable_mono_macos.universal.zip"

### Build and Run Commands:

```bash
# Build the C# project
dotnet build

# Run with Godot Mono (if installed via homebrew)
godot-mono project.godot

# Run with manually installed Godot Mono
/Applications/Godot_mono.app/Contents/MacOS/Godot project.godot

# Run main scene directly with debugger
/Applications/Godot_mono.app/Contents/MacOS/Godot --debug res://Scenes/main_scene.tscn

# Debug configuration available in .vscode/launch.json (updated for Godot Mono)
```

### Troubleshooting C# Issues:
- If you get "No loader found for resource" errors for .cs files: You're using standard Godot instead of Godot Mono
- Version compatibility: Ensure Godot.NET.Sdk version matches your Godot version
- .NET Framework: Godot 4.4.1 requires .NET 8.0 (configured in .csproj)

## Architecture Overview

### Core Systems

The game is built around several interconnected systems:

1. **GameController** (`Scripts/GameController.cs`): Central coordinator that initializes terrain, player, and mobs. Manages chunk loading based on player position.

2. **TerrainManager** (`Scripts/TerrainManager.cs`): Handles voxel terrain generation using FastNoiseLite, chunk management, and real-time terrain editing via custom signals.

3. **MobHandler** (`Scripts/MobHandler.cs`): Manages mob spawning, lifecycle, and provides debug utilities for monitoring mob states.

### Component-Based Mob System

The mob system uses a modular component architecture (evolved from a monolithic 851-line class):

- **Mob** (`Scripts/Mob.cs`): Core mob entity with faction system
- **MobMovement** (`Scripts/Mob/MobMovement.cs`): Physics and terrain navigation
- **MobBehaviorStates** (`Scripts/Mob/MobBehaviorStates.cs`): State machine (Wandering, Flocking, Fleeing, Investigating, Idle)
- **MobPersonality** (`Scripts/Mob/MobPersonality.cs`): Personality-driven behavior modifications (Friendly, Skittish, Neutral)
- **MobTerrainInteraction** (`Scripts/Mob/MobTerrainInteraction.cs`): Pathfinding and ground detection
- **MobAppearance** (`Scripts/Mob/MobAppearance.cs`): Visual customization and faction-based coloring

### Faction System

Mobs belong to factions that influence behavior and visual appearance:
- **Player**: Allied mobs (bright, warm colors with strong emission)
- **Neutral**: Independent mobs (standard personality-based colors)
- **Wild**: Natural creatures that avoid player (muted, earthy tones)
- **Hostile**: Aggressive entities (red-tinted, planned for future)

Faction affects flocking behavior (same-faction only), player interaction distances, and visual indicators.

### Player System

- **PlayerController** (`Scripts/Player/PlayerController.cs`): Main player entity
- **MovementController** (`Scripts/Player/MovementController.cs`): Player physics and movement
- **CameraController** (`Scripts/Player/CameraController.cs`): Camera control system
- **Player Actions**: Modular action system (Blink, PlacePillar, etc.)
- **TerrainEdit**: Real-time terrain modification tools

### Input Configuration

Key bindings are defined in `project.godot`:
- WASD: Movement
- Space: Jump  
- Q/E: Rotation
- Shift: Crouch/movement ability
- Mouse: Camera tilt, terrain editing (LMB place, RMB remove)
- F12/F11: Screenshots
- Mouse wheel: Camera zoom

### Terrain System

Uses Marching Cubes algorithm with:
- **Chunk-based loading**: Dynamic chunks around player position
- **FastNoiseLite**: Procedural height generation 
- **Real-time editing**: Custom signals system for terrain modification
- **26-directional neighbors**: Full chunk connectivity for seamless terrain

### Custom Signals System

`Scripts/CustomSignals.cs` provides autoloaded global signal management, particularly for terrain editing events.

## Development Guidelines

### Working with Mobs

When modifying mob behavior:
1. Check the specific component responsible (movement, states, personality, etc.)
2. Respect the faction system - behaviors should be faction-aware
3. Use the MobTypes enum for spawning configurations
4. Test with different mob personalities and factions

### Working with Terrain

- Terrain edits go through the CustomSignals system
- Chunk coordinates use Vector3I, world coordinates use Vector3
- CHUNK_SIZE and CHUNK_DIVISIONS are constants in Chunk class
- Always consider neighbor chunk updates when modifying terrain

### Scene Structure

- **Main scene**: `Scenes/main_scene.tscn`
- **Player scene**: `Scenes/player.tscn`  
- **Mob scenes**: `Scenes/mob_small.tscn`, `mob_med.tscn`, `mob_big.tscn`
- **Testing**: `Scenes/playerTest.tscn` available for isolated testing

### C# Namespace

Project uses `Reinnvoxel` namespace (defined in .csproj), targeting .NET 6.0 with Godot.NET.Sdk/4.3.0.

### Screenshot System

Built-in screenshot functionality via `Scripts/ScreenshotManager.cs`:
- F12/F11 key bindings
- Automatic timestamping
- Saves to `Screenshots/` directory as PNG files

## Key Constants and Configuration

- Chunk size and terrain generation parameters in TerrainManager
- Mob behavior parameters (detection range, speeds, etc.) in mob components  
- Input mappings in project.godot
- Export variables for mob scenes in MobHandler

The architecture emphasizes modularity, extensibility, and clean separation of concerns, making it suitable for continued development of complex AI behaviors and world simulation features.