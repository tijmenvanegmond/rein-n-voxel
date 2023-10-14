using System.Diagnostics;
using Godot;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene _chunkScene { get; set; }

	[Export]
	public int TerrainSizeInChunks = 30;

	public override void _Ready()
	{
		spawnChunks(TerrainSizeInChunks);
	}

	public void spawnChunks(int size = 3)
	{
		var noise = new FastNoiseLite();

		for (int x = 0; x < size; x++)
		{
			for (int z = 0; z < size; z++)
			{				
				Vector3 position = new Vector3(x * 12, 0, z * 12);
				spawnChunk(position);
			}
		}
	}


	private void spawnChunk(Vector3 position)
	{
		GD.Print($"Spawning Chunk at {position} ");
		Chunk newChunk = _chunkScene.Instantiate<Chunk>();
		AddChild(newChunk);
		newChunk.Initialize(position);		
	}
}