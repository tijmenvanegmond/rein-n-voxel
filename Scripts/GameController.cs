using System.Collections.Generic;
using Godot;

public partial class GameController : Node
{
	[Export]
	public PackedScene playerScene { get; set; }
	[Export]
	public TerrainManager terrainManager { get; set; }
	[Export]
	public MobHandler mobHandeler { get; set; }
	[Export]
	public int ChunkLoadRadius = 10;

	[Export]
	public int ChunkLoadDepth = 4;

	[Export]
	public int numberOfMobs = 100;

	public PlayerController Player;

	
	private bool mobsSpawned = false;

	public override void _Ready()
	{
		GD.Print("GameController: Starting initialization...");
		
		if (terrainManager == null)
		{
			GD.PrintErr("GameController: TerrainManager not assigned!");
			return;
		}
		
		if (mobHandeler == null)
		{
			GD.PrintErr("GameController: MobHandler not assigned!");
			return;
		}
		
		if (playerScene == null)
		{
			GD.PrintErr("GameController: PlayerScene not assigned!");
			return;
		}

		GD.Print("GameController: Spawning terrain chunks...");
		terrainManager.SpawnChunks(Vector3.Zero, ChunkLoadRadius, ChunkLoadDepth);

		GD.Print("GameController: Creating player...");
		Player = playerScene.Instantiate<PlayerController>();
		AddChild(Player);
		Player.playerCamera = GetNode<Camera3D>("MainCamera");

		if (Player.movementController == null)
		{
			GD.PrintErr("GameController: Player movement controller is null!");
			return;
		}

		// Delay mob spawning to ensure terrain is ready
		CallDeferred(nameof(SpawnMobsDeferred));
		
		GD.Print("GameController: Initialization complete!");
	}
	
	private void SpawnMobsDeferred()
	{
		// Ensure player and movement controller are initialized before spawning mobs
		if (Player == null || Player.movementController == null)
		{
			GD.PrintErr("GameController: Cannot spawn mobs - Player or MovementController is null!");
			return;
		}
		
		GD.Print($"GameController: Spawning {numberOfMobs} mobs...");
		mobHandeler.SpawnRandomMobs(numberOfMobs, Vector3.Zero, Player.movementController);
		mobsSpawned = true;
	}


	public override void _Process(double delta)
	{
		// Ensure all required components are initialized before processing
		if (Player == null || Player.movementController == null || terrainManager == null)
		{
			return;
		}
		
		terrainManager.SpawnChunks(Player.movementController.Position, ChunkLoadRadius);
	}
}
