using System.Diagnostics;
using Godot;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene _chunkScene { get; set; }

	public override void _Ready()
	{
		spawnChunks(10);
	}

	public void spawnChunks(int size = 3)
	{
		var noise = new FastNoiseLite();

		for (int x = 0; x < size; x++)
		{
			for (int z = 0; z < size; z++)
			{
				float height = noise.GetNoise2D(x, z) * 20f;
				Vector3 position = new Vector3(x * 6, height, z * 6);
				spawnChunk(position);
			}
		}
	}


	private void spawnChunk(Vector3 position)
	{
		GD.Print($"Spawning Chunk at {position} ");
		Chunk newChunk = _chunkScene.Instantiate<Chunk>();
		GD.Print($"DFEWF");
		//newChunk.Position = position;
		GD.Print($"DWADWADW");
		AddChild(newChunk);
		newChunk.Initialize(position, Vector3.Up);
		GD.Print($"GGGGDW");
		
	}
}