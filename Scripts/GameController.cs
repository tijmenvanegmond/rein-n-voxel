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

	
	public override void _Ready()
	{
		terrainManager.SpawnChunks(Vector3.Zero, ChunkLoadRadius, ChunkLoadDepth);

		Player = playerScene.Instantiate<PlayerController>();
		AddChild(Player);
		Player.playerCamera = GetNode<Camera3D>("MainCamera");

		mobHandeler.SpawnRandomMobs(numberOfMobs, Vector3.Zero, Player.movementController);		

	}


	public override void _Process(double delta)
	{
		terrainManager.SpawnChunks(Player.movementController.Position, ChunkLoadRadius);
	}
}
