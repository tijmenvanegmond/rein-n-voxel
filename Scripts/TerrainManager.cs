using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene chunkScene { get; set; }
	Dictionary<Vector3I, Chunk> chunkDict = new Dictionary<Vector3I, Chunk>();
	const int CHUNK_SIZE = Chunk.CHUNK_SIZE;

	public void SpawnChunks(Vector3 origin, int radius = 4)
	{
		var curentChunkKey = new Vector3I(Mathf.FloorToInt(origin.X / CHUNK_SIZE), 0, Mathf.FloorToInt(origin.Z / CHUNK_SIZE));

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				var key = new Vector3I(curentChunkKey.X + x, 0, curentChunkKey.Z + z);
				CreateChunk(key);
			}
		}
	}

	public Chunk GetChunk(Vector3I key)
	{
		if (!chunkDict.ContainsKey(key))
			return null;

		return chunkDict[key];
	}

	public bool GetVoxel(Vector3I posKey, out byte VoxelData)
	{
		VoxelData = 0;

		Vector3I chunkKey = new Vector3I(Mathf.FloorToInt(posKey.X / CHUNK_SIZE),
										Mathf.FloorToInt(posKey.Y / CHUNK_SIZE),
										Mathf.FloorToInt(posKey.Z / CHUNK_SIZE));		

		var foundChunk = GetChunk(chunkKey);
		if (foundChunk == null || !foundChunk.isGenerated)
			return false;

		Vector3I remains = new Vector3I(posKey.X % CHUNK_SIZE,
								   		posKey.Y % CHUNK_SIZE,
								   		posKey.Z % CHUNK_SIZE);

		VoxelData = foundChunk.GetVoxel(remains);
		return true;
	}

	private void CreateChunk(Vector3I key)
	{
		if (GetChunk(key) != null)
			return;

		Chunk newChunk = chunkScene.Instantiate<Chunk>();
		AddChild(newChunk);

		chunkDict.Add(key, newChunk);
		newChunk.Init(key, this);
	}
}