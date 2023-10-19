using Godot;

public partial class GameController : Node
{
	[Export]
	public PackedScene playerScene { get; set; }
	[Export]
	public TerrainManager terrainManager { get; set; }
	[Export]
	public int ChunkLoadRadius = 10;

	[Export]
	public int ChunkLoadDepth = 4;

	public override void _Ready()
	{
		terrainManager.SpawnChunks(Vector3.Zero, ChunkLoadRadius,ChunkLoadDepth);

		var newPlayer = playerScene.Instantiate<PlayerController>();
		AddChild(newPlayer);
		newPlayer.playerCamera = GetNode<Camera3D>("MainCamera");
	}

	public override void _Process(double delta)
	{
		// terrainManager.SpawnChunks(playerNode.Position, ChunkLoadRadius);

	}
}
