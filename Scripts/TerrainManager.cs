using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Godot;
using GodotCollections = Godot.Collections;

// Data structures for multi-threaded terrain generation
public struct ChunkGenerationJob
{
	public Vector3I ChunkKey { get; set; }
	public Vector3 StartPosition { get; set; }
	public int DataArraySize { get; set; }
	public int Priority { get; set; } // Higher priority = generate first
	public bool NeedsImmediateCollision { get; set; }
}

public struct ChunkGenerationResult
{
	public Vector3I ChunkKey { get; set; }
	public byte[,,] VoxelData { get; set; }
	public bool Success { get; set; }
	public bool NeedsImmediateCollision { get; set; }
}

public partial class TerrainManager : Node
{
	[Export]
	public PackedScene chunkScene { get; set; }
	GodotCollections.Dictionary<Vector3I, Chunk> chunkDict = new GodotCollections.Dictionary<Vector3I, Chunk>();
	const int CHUNK_SIZE = Chunk.CHUNK_SIZE;
	private FastNoiseLite noise = new FastNoiseLite();
	private CustomSignals customSignals;
	
	// Multi-threading for terrain generation
	private ConcurrentQueue<ChunkGenerationJob> generationQueue = new ConcurrentQueue<ChunkGenerationJob>();
	private ConcurrentQueue<ChunkGenerationResult> completedJobs = new ConcurrentQueue<ChunkGenerationResult>();
	private readonly object pendingChunksLock = new object();
	private HashSet<Vector3I> pendingChunks = new HashSet<Vector3I>();
	private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
	private int maxConcurrentJobs = System.Environment.ProcessorCount;
	private int activeJobs = 0;
	
	// Player tracking for priority generation
	private Vector3 lastPlayerPosition = Vector3.Zero;
	private const float IMMEDIATE_COLLISION_DISTANCE = 32.0f; // Distance where chunks need immediate collision
	
	// Warning throttling system
	private Dictionary<Vector3I, double> lastWarningTime = new Dictionary<Vector3I, double>();
	private const double WARNING_COOLDOWN = 30.0; // Only warn about same coords every 30 seconds
	private double lastGlobalWarning = 0.0;
	private const double GLOBAL_WARNING_LIMIT = 2.0; // Limit all warnings to once every 2 seconds
	private int warningCount = 0;
	private const int MAX_WARNINGS_PER_MINUTE = 10; // Maximum 10 warnings per minute

	public override void _Ready()
	{
		// Add to terrain_manager group so food spawners can find this node
		AddToGroup("terrain_manager");
		
		// Initialize custom signals and connect to EditTerrain signal
		customSignals = GetNode<CustomSignals>("/root/CustomSignals");
		customSignals.EditTerrain += OnTerrainEdit;
		
		// Start background terrain generation
		StartBackgroundGeneration();
		
		// Clean up old warning entries periodically
		var timer = new Godot.Timer();
		timer.WaitTime = 60.0; // Clean up every minute
		timer.Timeout += CleanupOldWarnings;
		timer.Autostart = true;
		AddChild(timer);
	}
	
	private void CleanupOldWarnings()
	{
		double currentTime = Time.GetUnixTimeFromSystem();
		var keysToRemove = new List<Vector3I>();
		
		foreach (var kvp in lastWarningTime)
		{
			if (currentTime - kvp.Value > WARNING_COOLDOWN * 2) // Remove entries older than double the cooldown
			{
				keysToRemove.Add(kvp.Key);
			}
		}
		
		foreach (var key in keysToRemove)
		{
			lastWarningTime.Remove(key);
		}
		
		// Reset warning count every minute
		warningCount = 0;
	}

	public void OnTerrainEdit(Vector3I coord, byte value)
	{
		GD.Print($"Doing an Terrain Edit at:{coord} to {value}");

		var radius = 0;

		for (int x = -radius; x <= radius; x++)
		{
			for (int y = -radius; y <= radius; y++)
			{
				for (int z = -radius; z <= radius; z++)
				{
					var voxelCoord = coord + new Vector3I(x, y, z);
					SetVoxel(voxelCoord, value); // Update the voxel with the desired value
				}
			}
		}
	}


	public byte[,,] GenerateData(Vector3 startPos, int dataArrSize = CHUNK_SIZE)
	{
		//dataArrSize is just so i over gen at the edges (and do quick checks on it)
		var dataArray = new byte[dataArrSize, dataArrSize, dataArrSize];

		var HEIGHT_MULTIPLIER = 25f;
		var SCALE_MULTIPLIER = 3.2f;
		var numOfSolid = 0;


		for (int x = 0; x < dataArrSize; x++)
		{
			for (int z = 0; z < dataArrSize; z++)
			{
				float valueX = (x + startPos.X) / SCALE_MULTIPLIER;
				float valueZ = (z + startPos.Z) / SCALE_MULTIPLIER;
				float height = noise.GetNoise3D(valueX, 0, valueZ) * HEIGHT_MULTIPLIER;


				for (int y = 0; y < dataArrSize; y++)
				{
					var byteValue = y + startPos.Y < height ? (byte)1 : (byte)0;
					dataArray[x, y, z] = byteValue;

					numOfSolid += byteValue;
				}
			}
		}

		return dataArray;
	}

	public void SpawnChunks(Vector3 origin, int radius = 4, int depth = 3)
	{
		lastPlayerPosition = origin;
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
						// Calculate distance from player for priority
						var chunkWorldPos = (Vector3)key * CHUNK_SIZE;
						var distanceToPlayer = origin.DistanceTo(chunkWorldPos);
						
						CreateChunk(key, distanceToPlayer);
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

	public bool IsChunkLoadedAtVoxelCoords(Vector3I coords)
	{
		Vector3I chunkKey = new Vector3I(Mathf.FloorToInt(coords.X / (float)CHUNK_SIZE),
										 Mathf.FloorToInt(coords.Y / (float)CHUNK_SIZE),
										 Mathf.FloorToInt(coords.Z / (float)CHUNK_SIZE));

		return chunkDict.ContainsKey(chunkKey);
	}

	private bool ShouldWarnAboutMissingChunk(Vector3I coords)
	{
		double currentTime = Time.GetUnixTimeFromSystem();
		
		// Global rate limiting - don't spam warnings too frequently
		if (currentTime - lastGlobalWarning < GLOBAL_WARNING_LIMIT)
		{
			return false;
		}
		
		// Check warning count per minute
		if (warningCount >= MAX_WARNINGS_PER_MINUTE)
		{
			return false;
		}
		
		// Check per-coordinate throttling
		if (lastWarningTime.ContainsKey(coords))
		{
			if (currentTime - lastWarningTime[coords] < WARNING_COOLDOWN)
			{
				return false; // Too soon to warn again about this coordinate
			}
		}
		
		lastWarningTime[coords] = currentTime;
		lastGlobalWarning = currentTime;
		warningCount++;
		return true;
	}

	public bool SetVoxel(Vector3I coords, byte voxelData)
	{
		var foundChunk = GetChunkByVoxelCoords(coords);
		if (foundChunk == null)
		{
			// Only warn occasionally about specific coordinates
			if (ShouldWarnAboutMissingChunk(coords))
			{
				GD.PrintRich($"[color=yellow]Warning: Cant find chunk for coords:{coords}[/color]");
			}
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
			// Only warn occasionally about specific coordinates
			if (ShouldWarnAboutMissingChunk(coords))
			{
				GD.PrintRich($"[color=yellow]Warning: Cant find chunk for coords:{coords}[/color]");
			}
			return false;
		}

		var remains = coords - foundChunk.key * CHUNK_SIZE;

		voxelData = foundChunk.GetLocalVoxel(remains);
		return true;
	}

	// Enhanced methods for food spawning and surface detection
	public Vector3 GetSurfaceHeightAt(Vector3 worldPosition)
	{
		Vector3I startCoord = new Vector3I(
			Mathf.RoundToInt(worldPosition.X),
			50, // Start from high up
			Mathf.RoundToInt(worldPosition.Z)
		);

		// Search downward for the first solid voxel with air above it
		for (int y = 50; y >= -20; y--)
		{
			Vector3I currentCoord = new Vector3I(startCoord.X, y, startCoord.Z);
			Vector3I aboveCoord = new Vector3I(startCoord.X, y + 1, startCoord.Z);

			if (GetVoxel(currentCoord, out byte currentVoxel) && 
				GetVoxel(aboveCoord, out byte aboveVoxel))
			{
				// Found surface: solid voxel with air above
				if (currentVoxel > 0 && aboveVoxel == 0)
				{
					return new Vector3(worldPosition.X, y + 1, worldPosition.Z);
				}
			}
		}

		// No surface found
		return Vector3.Zero;
	}

	public bool IsPositionOnSurface(Vector3I voxelCoord)
	{
		Vector3I aboveCoord = voxelCoord + Vector3I.Up;

		if (GetVoxel(voxelCoord, out byte currentVoxel) && 
			GetVoxel(aboveCoord, out byte aboveVoxel))
		{
			return currentVoxel > 0 && aboveVoxel == 0;
		}

		return false;
	}

	public float GetTerrainSlopeAt(Vector3 worldPosition)
	{
		Vector3 center = GetSurfaceHeightAt(worldPosition);
		if (center == Vector3.Zero) return 0f;

		Vector3 right = GetSurfaceHeightAt(worldPosition + Vector3.Right);
		Vector3 forward = GetSurfaceHeightAt(worldPosition + Vector3.Forward);

		if (right == Vector3.Zero || forward == Vector3.Zero) return 0f;

		float slopeX = Mathf.Abs(right.Y - center.Y);
		float slopeZ = Mathf.Abs(forward.Y - center.Y);

		return Mathf.Max(slopeX, slopeZ);
	}

	public List<Vector3> GetSurfacePositionsInArea(Vector3 center, int radius)
	{
		List<Vector3> surfacePositions = new List<Vector3>();

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				Vector3 checkPos = center + new Vector3(x, 0, z);
				Vector3 surface = GetSurfaceHeightAt(checkPos);

				if (surface != Vector3.Zero)
				{
					surfacePositions.Add(surface);
				}
			}
		}

		return surfacePositions;
	}

	public bool IsChunkGenerated(Vector3I chunkKey)
	{
		var chunk = GetChunkByKey(chunkKey);
		return chunk != null && chunk.isGenerated;
	}

	public FastNoiseLite GetTerrainNoise()
	{
		return noise;
	}

	private void CreateChunk(Vector3I key, float distanceToPlayer = float.MaxValue)
	{
		if (GetChunkByKey(key) != null)
			return;

		// Check if this chunk is already being generated
		lock (pendingChunksLock)
		{
			if (pendingChunks.Contains(key))
				return;
			pendingChunks.Add(key);
		}

		// Determine priority and if immediate collision is needed
		bool needsImmediateCollision = distanceToPlayer <= IMMEDIATE_COLLISION_DISTANCE;
		int priority = needsImmediateCollision ? 1000 : (int)(1000 - distanceToPlayer);

		// If the chunk needs immediate collision, generate it synchronously
		if (needsImmediateCollision)
		{
			GD.Print($"Generating chunk {key} with immediate collision (distance: {distanceToPlayer:F1})");
			var data = GenerateDataThreadSafe(key * CHUNK_SIZE);
			CreateChunkFromData(key, data, true);
			
			// Remove from pending since we're done
			lock (pendingChunksLock)
			{
				pendingChunks.Remove(key);
			}
		}
		else
		{
			// Queue chunk for background generation
			var job = new ChunkGenerationJob
			{
				ChunkKey = key,
				StartPosition = key * CHUNK_SIZE,
				DataArraySize = CHUNK_SIZE,
				Priority = priority,
				NeedsImmediateCollision = false
			};
			
			generationQueue.Enqueue(job);
		}
	}
	
	private void CreateChunkFromData(Vector3I key, byte[,,] data, bool generateImmediateCollision = false)
	{
		if (GetChunkByKey(key) != null)
			return;

		Chunk newChunk = chunkScene.Instantiate<Chunk>();
		AddChild(newChunk);
		newChunk.Name = $"Chunk{key}";
		chunkDict.Add(key, newChunk);
		newChunk.Init(key, data, this);

		// If this chunk needs immediate collision, force mesh generation now
		if (generateImmediateCollision)
		{
			// Force immediate mesh generation to create collision
			newChunk.ForceImmediateMeshGeneration();
			GD.Print($"Forced immediate mesh generation for chunk {key}");
		}

		//add chunk to neighbours
		var neighbours = new GodotCollections.Dictionary<Vector3I, Chunk>();
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
			if (neighbourChunk != null)
			{
				neighbourChunk.SetDirectNeighbour(-direction, newChunk); //add new chunk to neighbour
				neighbours.Add(direction, neighbourChunk); //for new chunk
			}
			if (neighbourChunk == null)
			{

			}
		}

		newChunk.SetDirectNeighbours(neighbours);
	}

	// Multi-threading methods for terrain generation
	private void StartBackgroundGeneration()
	{
		// Start background threads for terrain generation
		for (int i = 0; i < maxConcurrentJobs; i++)
		{
			Task.Run(() => BackgroundGenerationWorker(cancellationTokenSource.Token));
		}
	}
	
	private async Task BackgroundGenerationWorker(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (generationQueue.TryDequeue(out ChunkGenerationJob job))
			{
				Interlocked.Increment(ref activeJobs);
				
				try
				{
					// Generate terrain data on background thread
					var voxelData = await Task.Run(() => GenerateDataThreadSafe(job.StartPosition, job.DataArraySize), cancellationToken);
					
					var result = new ChunkGenerationResult
					{
						ChunkKey = job.ChunkKey,
						VoxelData = voxelData,
						Success = true
					};
					
					completedJobs.Enqueue(result);
				}
				catch (OperationCanceledException)
				{
					// Expected when cancellation is requested
					break;
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Error generating chunk {job.ChunkKey}: {ex.Message}");
					
					var result = new ChunkGenerationResult
					{
						ChunkKey = job.ChunkKey,
						VoxelData = null,
						Success = false
					};
					
					completedJobs.Enqueue(result);
				}
				finally
				{
					Interlocked.Decrement(ref activeJobs);
				}
			}
			else
			{
				// No work available, wait a bit before checking again
				await Task.Delay(10, cancellationToken);
			}
		}
	}
	
	private byte[,,] GenerateDataThreadSafe(Vector3 startPos, int dataArrSize = CHUNK_SIZE)
	{
		// Thread-safe version of terrain generation
		// Create a new noise instance for this thread to avoid concurrency issues
		var localNoise = new FastNoiseLite();
		localNoise.Seed = noise.Seed;
		localNoise.NoiseType = noise.NoiseType;
		localNoise.Frequency = noise.Frequency;
		
		var dataArray = new byte[dataArrSize, dataArrSize, dataArrSize];
		var HEIGHT_MULTIPLIER = 25f;
		var SCALE_MULTIPLIER = 3.2f;

		for (int x = 0; x < dataArrSize; x++)
		{
			for (int z = 0; z < dataArrSize; z++)
			{
				float valueX = (x + startPos.X) / SCALE_MULTIPLIER;
				float valueZ = (z + startPos.Z) / SCALE_MULTIPLIER;
				float height = localNoise.GetNoise3D(valueX, 0, valueZ) * HEIGHT_MULTIPLIER;

				for (int y = 0; y < dataArrSize; y++)
				{
					var byteValue = y + startPos.Y < height ? (byte)1 : (byte)0;
					dataArray[x, y, z] = byteValue;
				}
			}
		}

		return dataArray;
	}

	public override void _Process(double delta)
	{
		// Process completed generation jobs on main thread
		ProcessCompletedJobs();
	}
	
	private void ProcessCompletedJobs()
	{
		int processedCount = 0;
		const int maxJobsPerFrame = 2; // Limit to prevent frame drops
		
		while (processedCount < maxJobsPerFrame && completedJobs.TryDequeue(out ChunkGenerationResult result))
		{
			if (result.Success && result.VoxelData != null)
			{
				// Create the chunk on the main thread with pre-generated data
				CreateChunkFromData(result.ChunkKey, result.VoxelData);
			}
			
			// Remove from pending chunks
			lock (pendingChunksLock)
			{
				pendingChunks.Remove(result.ChunkKey);
			}
			
			processedCount++;
		}
	}

	public override void _ExitTree()
	{
		// Clean up background threads when exiting
		cancellationTokenSource.Cancel();
		cancellationTokenSource.Dispose();
	}

	// Utility methods for monitoring performance
	public int GetPendingChunkCount()
	{
		lock (pendingChunksLock)
		{
			return pendingChunks.Count;
		}
	}
	
	public int GetGenerationQueueCount()
	{
		return generationQueue.Count;
	}
	
	public int GetCompletedJobsCount()
	{
		return completedJobs.Count;
	}
	
	public int GetActiveJobsCount()
	{
		return activeJobs;
	}
	
	// Optional: Method to adjust thread count at runtime
	public void SetMaxConcurrentJobs(int count)
	{
		if (count > 0 && count <= 16) // Reasonable limits
		{
			maxConcurrentJobs = count;
		}
	}
}