using Godot;

public partial class Chunk : StaticBody3D
{
    [Export]
    public bool isGenerated { get; private set; }
    [Export]
    public int doUpdate { get; private set; }
    public const int CHUNK_SIZE = 12;
    public const int CHUNK_DIVISIONS = 12;
    byte[,,] dataArray;
    const int dataArrSize = CHUNK_SIZE + 1;
    [Export]
    MeshInstance3D surfaceMesh;
    Chunk[] directNeighbours = new Chunk[6];
    TerrainManager terrainManager;
    Vector3I key;

    public void Init(Vector3I _key, TerrainManager _terrainManager)
    {
        key = _key;
        terrainManager = _terrainManager;
        Position = ((Vector3)key) * Chunk.CHUNK_SIZE;
        GD.Print($"Initializing Chunk{key} at {Position}");
        GenerateData();
        doUpdate = 1;
    }

    public override void _Process(double delta)
    {
        if (doUpdate < 1)
            return;
        doUpdate++;

        if (doUpdate > 100)
        {
            GD.Print($"Mesh[{key}] failed to update for a 100 frames!");
            doUpdate = 0; // early to prevent infinite loops
        }

        GenerateMesh();

        doUpdate = 0;
    }

    public void GenerateData()
    {
        //dataArrSize is just so i over gen at the edges (and do quick checks on it)
        dataArray = new byte[dataArrSize, dataArrSize, dataArrSize];

        var HEIGHT_MULTIPLIER = 12f;
        var CUBE_SIZE = (float)(CHUNK_SIZE / CHUNK_DIVISIONS);

        for (int x = 0; x < dataArrSize; x++)
        {
            for (int z = 0; z < dataArrSize; z++)
            {
                float height = terrainManager.noise.GetNoise2D((x + Position.X) / CUBE_SIZE, (z + Position.Z) / CUBE_SIZE) * HEIGHT_MULTIPLIER;
                for (int y = 0; y < dataArrSize; y++)
                {
                    dataArray[x, y, z] = (int)height < Position.Y + y ? (byte)1 : (byte)0;
                }
            }
        }

        isGenerated = true;
    }

    public byte GetLocalVoxel(Vector3I input)
    {
        return GetLocalVoxel(input.X, input.Y, input.Z);
    }

    public byte GetLocalVoxel(int x, int y, int z)
    {
        if (x < dataArrSize && x >= 0 &&
            y < dataArrSize && y >= 0 &&
            z < dataArrSize && z >= 0)
        {
            return dataArray[x, y, z];
        }

        return 0;

        //outside array

        var voxelCoords = new Vector3I(key.X * CHUNK_SIZE + x, key.Y * CHUNK_SIZE + y, key.Z * CHUNK_SIZE + z);

        byte foundVoxel;
        if (terrainManager.GetVoxel(voxelCoords, out foundVoxel))
        {
            //success
        }
        else
        {
            //chunk probably not loaded in (yet)
        }

        return foundVoxel;
    }

    public Mesh GenerateMesh()
    {

        byte[,,] dataPlus1 = new byte[CHUNK_SIZE + 1, CHUNK_SIZE + 1, CHUNK_SIZE + 1];

        //get the edges of the chunks (positve) next to it
        for (int x = 0; x < CHUNK_SIZE + 1; x++)
        {
            for (int y = 0; y < CHUNK_SIZE + 1; y++)
            {
                for (int z = 0; z < CHUNK_SIZE + 1; z++)
                {
                    dataPlus1[x, y, z] = GetLocalVoxel(x, y, z);
                }
            }
        }

        var mesh = MarchingCubes.CreateMesh(dataPlus1);

        surfaceMesh.Mesh = mesh;
        surfaceMesh.CreateTrimeshCollision();

        return mesh;
    }
}
