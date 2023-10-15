using System.Diagnostics;
using Godot;

public partial class World : Node
{
	[Export]
	public CharacterBody3D playerNode { get; set; }
	[Export]
	public TerrainManager _terrainManager { get; set; }

	[Export]
	public int ChunkLoadRadius = 10;

	public override void _Ready()
	{
		_terrainManager.spawnChunks(playerNode.Position, ChunkLoadRadius);
	}

	public override void _Process(double delta)
	{

	}
}
