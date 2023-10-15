using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene chunkScene { get; set; }
	Dictionary<Vector3I, Chunk> chunkDict = new Dictionary<Vector3I, Chunk>();

	public void spawnChunks(Vector3 origin, int radius = 10)
	{
		var curentChunkKey = new Vector3I(Mathf.FloorToInt(origin.X / Chunk.CHUNK_SIZE), 0, Mathf.FloorToInt(origin.Z / Chunk.CHUNK_SIZE));

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				var key = new Vector3I(curentChunkKey.X + x, 0, curentChunkKey.Z + z);
				createChunk(key);
			}
		}
	}

	public Chunk GetChunk(Vector3I key)
	{
		if (!chunkDict.ContainsKey(key))
			return null;

		return chunkDict[key];
	}


	private void createChunk(Vector3I key)
	{
		if(GetChunk(key) != null)
			return;

		Chunk newChunk = chunkScene.Instantiate<Chunk>();

		chunkDict.Add(key, newChunk);
		AddChild(newChunk);
		newChunk.Init(key, this);
	}
}