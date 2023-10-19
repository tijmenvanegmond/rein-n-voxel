using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene chunkScene { get; set; }
	Dictionary<Vector3I, Chunk> chunkDict = new Dictionary<Vector3I, Chunk>();
	const int CHUNK_SIZE = Chunk.CHUNK_SIZE;

	public FastNoiseLite noise = new FastNoiseLite();

	public override void _Ready()
	{
		noise.Seed = (int)Time.GetTicksMsec();
	}

	public void SpawnChunks(Vector3 origin, int radius = 4, int depth = 3)
	{
		var originChunkKey = new Vector3I(Mathf.FloorToInt(origin.X / CHUNK_SIZE), Mathf.FloorToInt(origin.Y / CHUNK_SIZE), Mathf.FloorToInt(origin.Z / CHUNK_SIZE));

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				for (int y = -depth; y <= depth; y++)
				{
					var key = new Vector3I(originChunkKey.X + x, originChunkKey.Y + y, originChunkKey.Z + z);
					if (GetChunk(key) == null)
					{
						CreateChunk(key);
					}
				}
			}
		}
	}

	public Chunk GetChunk(Vector3I key)
	{
		if (!chunkDict.ContainsKey(key))
			return null;

		return chunkDict[key];
	}

	public bool GetVoxel(Vector3I posKey, out byte voxelData)
	{
		voxelData = 0;

		Vector3I chunkKey = new Vector3I(Mathf.FloorToInt(posKey.X / CHUNK_SIZE),
										Mathf.FloorToInt(posKey.Y / CHUNK_SIZE),
										Mathf.FloorToInt(posKey.Z / CHUNK_SIZE));

		var foundChunk = GetChunk(chunkKey);
		if (foundChunk == null || !foundChunk.isGenerated)
			return false;

		Vector3I remains = new Vector3I(posKey.X % CHUNK_SIZE,
								   		posKey.Y % CHUNK_SIZE,
								   		posKey.Z % CHUNK_SIZE);

		voxelData = foundChunk.GetLocalVoxel(remains);
		return true;
	}

	private void CreateChunk(Vector3I key)
	{
		if (GetChunk(key) != null)
			return;

		Chunk newChunk = chunkScene.Instantiate<Chunk>();
		AddChild(newChunk);
		newChunk.Name = $"Chunk{key}";

		chunkDict.Add(key, newChunk);
		newChunk.Init(key, this);
	}
}