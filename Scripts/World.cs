using System.Diagnostics;
using Godot;

public partial class World : Node
{
	private TerrainManager _terrainManager { get; set; }

	public override void _Ready()
	{
		_terrainManager = new TerrainManager();
		_terrainManager.spawnChunks(10);
	}

	public override void _Process(double delta)
	{

	}
}
