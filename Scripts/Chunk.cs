using Godot;

public partial class Chunk : StaticBody3D
{
    [Export]
    public bool isGenerated { get; private set; }
   
    public const int CHUNK_SIZE = 12;
    public const int CHUNK_DIVISIONS = 12;
    byte[,,] dataArray;
    const int dataArrSize = CHUNK_SIZE + 1;
    [Export]
    MeshInstance3D surfaceMesh;
    Chunk[] directNeighbours = new Chunk[6];
    TerrainManager terrainManager;
    Vector3I key;
    int numOfSolid = 0;
    int doMeshUpdateInt = 0;
    public void PlanMeshUpdate(){
        if(doMeshUpdateInt == 0) doMeshUpdateInt = 1;
    }

    public void Init(Vector3I _key, TerrainManager _terrainManager)
    {
        key = _key;
        terrainManager = _terrainManager;
        Position = ((Vector3)key) * Chunk.CHUNK_SIZE;
        GenerateData();
        PlanMeshUpdate();
    }

    public override void _Process(double delta)
    {
        if (doMeshUpdateInt < 1)
            return;

        doMeshUpdateInt++;

        if (doMeshUpdateInt > 100)
        {
            GD.Print($"Mesh[{key}] failed to update for a 100 frames!");
            doMeshUpdateInt = 0; // early to prevent infinite loops
        }

        GenerateMesh();

        doMeshUpdateInt = 0;
    }

    public void GenerateData()
    {
        //dataArrSize is just so i over gen at the edges (and do quick checks on it)
        dataArray = new byte[dataArrSize, dataArrSize, dataArrSize];

        var HEIGHT_MULTIPLIER = 25f;
        var SCALE_MULTIPLIER = 3.2f;
        var ROCK_SCALE_MULTIPLIER = 1.0f;
        var CUBE_SIZE = (float)(CHUNK_SIZE / CHUNK_DIVISIONS);       

        for (int x = 0; x < dataArrSize; x++)
        {
            for (int z = 0; z < dataArrSize; z++)
            {
                float height = terrainManager.noise.GetNoise2D((x + Position.X) / CUBE_SIZE / SCALE_MULTIPLIER,
                                                                (z + Position.Z) / CUBE_SIZE / SCALE_MULTIPLIER) * HEIGHT_MULTIPLIER;
                for (int y = 0; y < dataArrSize; y++)
                {

                    float some3dnoise = terrainManager.noise.GetNoise3D((x + Position.X) / CUBE_SIZE / ROCK_SCALE_MULTIPLIER,
                                                                        (y + Position.Y) / CUBE_SIZE / ROCK_SCALE_MULTIPLIER,
                                                                        (z + Position.Z) / CUBE_SIZE / ROCK_SCALE_MULTIPLIER) * 40f;
                    float actualDensity = height - (Position.Y + y) + some3dnoise;
                    var byteValue = actualDensity < 1 ? (byte)1 : (byte)0;
                    dataArray[x, y, z] = byteValue;
                    
                    numOfSolid += byteValue;
                }
            }
        }

        isGenerated = true;
    }

    public bool SetLocalVoxel(Vector3I loc, byte byteValue){
        if (loc.X < dataArrSize && loc.X >= 0 &&
            loc.Y < dataArrSize && loc.Y >= 0 &&
            loc.Z < dataArrSize && loc.Z >= 0)
        {
            dataArray[loc.X, loc.Y, loc.Z] = byteValue;
            PlanMeshUpdate();            
            return true;
        }
        return false;
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
    }

    public Mesh GenerateMesh()
    {
        if(numOfSolid == 0 || numOfSolid == dataArrSize*dataArrSize*dataArrSize)
        {
            //Mesh all air or all solid
            surfaceMesh.Mesh = null;
            return null;
        }

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
