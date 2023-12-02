using System;
using Godot;
using Godot.Collections;

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene chunkScene { get; set; }
	Dictionary<Vector3I, Chunk> chunkDict = new Dictionary<Vector3I, Chunk>();
	const int CHUNK_SIZE = Chunk.CHUNK_SIZE;
	const int CHUNK_DIVISIONS = Chunk.CHUNK_DIVISIONS;
	private FastNoiseLite noise = new FastNoiseLite();
	private CustomSignals customSignals;

	public override void _Ready()
	{
		customSignals = GetNode<CustomSignals>("/root/CustomSignals");

		customSignals.EditTerrain += OnTerrainEdit;

		noise.Seed = (int)Time.GetTicksMsec();
	}

	public void OnTerrainEdit(Vector3I coord, byte value)
	{
		GD.Print($"Doing an Terrain Edit at:{coord} to {value}");
		SetVoxel(coord, value);
		SetVoxel(coord + Vector3I.Down, value);
		SetVoxel(coord + Vector3I.Up, value);
	}

	public byte[,,] GenerateData(Vector3 startPos, int dataArrSize = CHUNK_SIZE)
	{
		//dataArrSize is just so i over gen at the edges (and do quick checks on it)
		var dataArray = new byte[dataArrSize, dataArrSize, dataArrSize];

		var HEIGHT_MULTIPLIER = 25f;
		var SCALE_MULTIPLIER = 3.2f;
		var ROCK_SCALE_MULTIPLIER = 1.0f;
		var CUBE_SIZE = (float)(CHUNK_SIZE / CHUNK_DIVISIONS);
		var numOfSolid = 0;

		for (int x = 0; x < dataArrSize; x++)
		{
			for (int z = 0; z < dataArrSize; z++)
			{
				for (int y = 0; y < dataArrSize; y++)
				{
					var byteValue = y + startPos.Y < 0 ? (byte)1 : (byte)0;
					dataArray[x, y, z] = byteValue;

					numOfSolid += byteValue;
				}
			}
		}

		return dataArray;
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
					if (GetChunkByKey(key) == null)
					{
						CreateChunk(key);
					}
				}
			}
		}
	}

	public Chunk GetChunkByKey(Vector3I key)
	{
		if (!chunkDict.ContainsKey(key))
			return null;

		return chunkDict[key];
	}

	public Chunk GetChunkByVoxelCoords(Vector3I coords)
	{
		Vector3I chunkKey = new Vector3I(Mathf.FloorToInt(coords.X / (float)CHUNK_SIZE),
										 Mathf.FloorToInt(coords.Y / (float)CHUNK_SIZE),
										 Mathf.FloorToInt(coords.Z / (float)CHUNK_SIZE));

		return GetChunkByKey(chunkKey);
	}

	public bool SetVoxel(Vector3I coords, byte voxelData)
	{
		var foundChunk = GetChunkByVoxelCoords(coords);
		if (foundChunk == null)
		{
			GD.Print($"Cant find chunk for coords:{coords}");
			return false;
		}

		var remains = coords - foundChunk.key * CHUNK_SIZE;

		return foundChunk.SetLocalVoxel(remains, voxelData);
	}

	public bool GetVoxel(Vector3I coords, out byte voxelData)
	{
		voxelData = 0;

		var foundChunk = GetChunkByVoxelCoords(coords);
		if (foundChunk == null)
		{
			GD.Print($"Cant find chunk for coords:{coords}");
			return false;
		}

		var remains = coords - foundChunk.key * CHUNK_SIZE;

		voxelData = foundChunk.GetLocalVoxel(remains);
		return true;
	}

	private void CreateChunk(Vector3I key)
	{
		if (GetChunkByKey(key) != null)
			return;

		Chunk newChunk = chunkScene.Instantiate<Chunk>();
		AddChild(newChunk);
		newChunk.Name = $"Chunk{key}";
		chunkDict.Add(key, newChunk);
		var data = GenerateData(key * CHUNK_SIZE);
		newChunk.Init(key, data, this);

		//add chunk to neighbours
		var neighbours = new Dictionary<Vector3I, Chunk>();
		var directions26 = new Vector3I[]
		{
			new Vector3I(-1, -1, -1), new Vector3I(-1, -1, 0), new Vector3I(-1, -1, 1),
			new Vector3I(-1, 0, -1),  new Vector3I(-1, 0, 0),  new Vector3I(-1, 0, 1),
			new Vector3I(-1, 1, -1),  new Vector3I(-1, 1, 0),  new Vector3I(-1, 1, 1),

			new Vector3I(0, -1, -1),  new Vector3I(0, -1, 0),  new Vector3I(0, -1, 1),
			new Vector3I(0, 0, -1), /*new Vector3I(0, 0, 0),*/   new Vector3I(0, 0, 1),
			new Vector3I(0, 1, -1),   new Vector3I(0, 1, 0),   new Vector3I(0, 1, 1),

			new Vector3I(1, -1, -1),  new Vector3I(1, -1, 0),  new Vector3I(1, -1, 1),
			new Vector3I(1, 0, -1),   new Vector3I(1, 0, 0),   new Vector3I(1, 0, 1),
			new Vector3I(1, 1, -1),   new Vector3I(1, 1, 0),   new Vector3I(1, 1, 1)
		};

		foreach (var direction in directions26)
		{
			var neighbourChunk = GetChunkByKey(key + direction);
			if (neighbourChunk != null){
				neighbourChunk.SetDirectNeighbour(-direction, newChunk); //add new chunk to neighbour
				neighbours.Add(direction, neighbourChunk); //for new chunk
			}
			if (neighbourChunk == null){

			}
		}

		newChunk.SetDirectNeighbours(neighbours);
	}
}