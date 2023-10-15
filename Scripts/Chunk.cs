using Godot;

public partial class Chunk : StaticBody3D
{
    public const int CHUNK_SIZE = 12;
    public const int CHUNK_DIVISIONS = 12;
    [Export]
    bool isGenerated { get; set; }
    [Export]
    bool isRendered { get; set; }
    [Export]
    MeshInstance3D surfaceMesh;
    byte[,,] dataArray;
    Chunk[] neighbours;
    TerrainManager terrainManager;
    Vector3I key;

    public void Init(Vector3I _key, TerrainManager _terrainManager)
    {
        key = _key;
        terrainManager = _terrainManager;
        Position = ((Vector3)key) * Chunk.CHUNK_SIZE;
        GD.Print($"Initializing Chunk at {Position}");
        dataArray = GenerateData();
        GD.Print($"Genned Data at {Position}");
        //load overlay data
        var newMesh = GenerateMesh(dataArray);
        GD.Print($"Genned mesh at {Position}");
        surfaceMesh.Mesh = newMesh;
        surfaceMesh.CreateTrimeshCollision();
    }

    public byte[,,] GenerateData()
    {
        var dataArray = new byte[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
        var noise = new FastNoiseLite();

        var HEIGHT_MULTIPLIER = 12f;
        var CUBE_SIZE = (float)(CHUNK_SIZE / CHUNK_DIVISIONS);

        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                float height = noise.GetNoise2D((x + Position.X) / CUBE_SIZE, (z + Position.Z) / CUBE_SIZE) * HEIGHT_MULTIPLIER;
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    dataArray[x, y, z] = (int)height < y ? (byte)1 : (byte)0;
                }

            }
        }

        return dataArray;
    }

    public byte GetVoxel(Vector3I input)
    {
        return GetVoxel(input.X, input.Y, input.Z);
    }

    public byte GetVoxel(int x, int y, int z)
    {

        if (x < CHUNK_SIZE && x >= 0 && y < CHUNK_SIZE && y >= 0 && z < CHUNK_SIZE && z >= 0)
        {
            return dataArray[x, y, z];

        }

        //outside array
        Vector3I worldPos = key * CHUNK_SIZE + new Vector3I(x, y, z); //voxelpos in "world" pos
                                                                      //calculate in which chunkkey the new chunk has
        Vector3I newKey = new Vector3I(Mathf.FloorToInt(worldPos.X / CHUNK_SIZE),
                                        Mathf.FloorToInt(worldPos.Y / CHUNK_SIZE),
                                        Mathf.FloorToInt(worldPos.Z / CHUNK_SIZE));
        Vector3I newLocalPos = worldPos - (newKey * CHUNK_SIZE); //the new local pos

        var foundChunk = terrainManager.GetChunk(newLocalPos);
        if (foundChunk == null || !foundChunk.isGenerated)
            return 1;

        return foundChunk.GetVoxel(newLocalPos);
    }

    public Mesh GenerateMesh(byte[,,] data)
    {

        byte[,,] dataPlus1 = new byte[CHUNK_SIZE + 1, CHUNK_SIZE + 1, CHUNK_SIZE + 1];

        //get the edges of the chunks (positve) next to it
        for (int x = 0; x < CHUNK_SIZE + 1; x++)
        {
            for (int y = 0; y < CHUNK_SIZE + 1; y++)
            {
                for (int z = 0; z < CHUNK_SIZE + 1; z++)
                {
                    dataPlus1[x, y, z] = GetVoxel(x, y, z);
                }
            }
        }

        var mesh = MarchingCubes.CreateMesh(dataPlus1);
        return mesh;
    }
}
