using System.Diagnostics;
using Godot;

public partial class World : Node
{
	[Export]
	public PackedScene ChunkScene { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		spawnChunks(10);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void spawnChunks(int size = 3)
	{
		var noise = new FastNoiseLite();
		noise.FractalOctaves = 4;

		for (int x = 0; x < size; x++)
		{
			for (int z = 0; z < size; z++)
			{
				float height = noise.GetNoise2D(x,z) * 20f;
				Vector3 position = new Vector3(x*6,height,z*6);
				GD.Print($"Spawning Chunk at [{position}] ");
				Chunk chunk = ChunkScene.Instantiate<Chunk>();
				chunk.Initialize(position, Vector3.Up );
				// add to the World scene.
				AddChild(chunk);
			}
		}
	}
}
