using System;
using Godot;
using Godot.Collections;

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
    Dictionary<Vector3I, Chunk> neighbours = new Dictionary<Vector3I, Chunk>();
    TerrainManager terrainManager;
    public Vector3I key { private set; get; }
    int doMeshUpdateInt = 0;
    public void PlanMeshUpdate()
    {
        if (doMeshUpdateInt == 0) doMeshUpdateInt = 1;
    }

    public void Init(Vector3I _key, byte[,,] _dataArray, TerrainManager _terrainManager)
    {
        key = _key;
        terrainManager = _terrainManager;
        Position = ((Vector3)key) * CHUNK_SIZE;
        dataArray = _dataArray;
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
    }

    private bool HasAllNeighboursCheck()
    {
        return neighbours.Count == 26;
    }

    public void SetDirectNeighbours(Dictionary<Vector3I, Chunk> _neighbours)
    {
        neighbours = _neighbours;
    }

    public void SetDirectNeighbour(Vector3I direction, Chunk chunk)
    {
        if (neighbours.ContainsKey(direction))
        {
            throw new Exception($"Chunk[{key}] already has neighbour in direction {direction}");
        }
        neighbours.Add(direction, chunk);

        if (HasAllNeighboursCheck())
        {

            PlanMeshUpdate();
        }
    }

    public Chunk GetDirectNeighbour(Vector3I direction)
    {
        if (!neighbours.ContainsKey(direction))
            return null;

        else return neighbours[direction];
    }

    public bool SetLocalBlock(Vector3I coords, byte[/*27*/] byteValue)
    {
        return false;
    }

    public bool SetLocalVoxel(Vector3I loc, byte byteValue)
    {
        if (loc.X >= dataArrSize || loc.X < 0 ||
            loc.Y >= dataArrSize || loc.Y < 0 ||
            loc.Z >= dataArrSize || loc.Z < 0)
        {  // out of bounds
            return false;
        }

        dataArray[loc.X, loc.Y, loc.Z] = byteValue;
        PlanMeshUpdate();

        //update neighbouring chunks
        var chunkKey = key;
        int x = loc.X;
        int y = loc.Y;
        int z = loc.Z;

        if (x == CHUNK_SIZE - 1) chunkKey.X++;
        if (x == 0) chunkKey.X--;
        if (y == CHUNK_SIZE - 1) chunkKey.Y++;
        if (y == 0) chunkKey.Y--;
        if (z == CHUNK_SIZE - 1) chunkKey.Z++;
        if (z == 0) chunkKey.Z--;

        if (chunkKey != key)
        {
            Chunk neighborChunk = GetDirectNeighbour(chunkKey - key);
            if (neighborChunk == null)
            {
                GD.Print($"Chunk[{key}] failed to find neighbour in direction {chunkKey - key}");
            }
            neighborChunk.PlanMeshUpdate();
        }
        return true;
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
        // if(numOfSolid == 0 || numOfSolid == dataArrSize*dataArrSize*dataArrSize)
        // {
        //     //Mesh all air or all solid
        //     surfaceMesh.Mesh = null;
        //     return null;
        // }
        doMeshUpdateInt++;

        byte[,,] dataPlus1 = new byte[CHUNK_SIZE + 1, CHUNK_SIZE + 1, CHUNK_SIZE + 1];

        // get the edges of the chunks (positive) next to it
        for (int x = 0; x < CHUNK_SIZE + 1; x++)
        {
            for (int y = 0; y < CHUNK_SIZE + 1; y++)
            {
                for (int z = 0; z < CHUNK_SIZE + 1; z++)
                {
                    byte voxelValue = CheckFromNeighbourIfNeighbour(x, y, z);
                    dataPlus1[x, y, z] = voxelValue;
                }
            }
        }

        var mesh = MarchingCubes.CreateMesh(dataPlus1);

        surfaceMesh.Mesh = mesh;
        surfaceMesh.CreateTrimeshCollision();

        doMeshUpdateInt = 0;

        return mesh;
    }

    private byte CheckFromNeighbourIfNeighbour(int x, int y, int z)
    {
        var chunkKey = key;

        if (x == CHUNK_SIZE)
        {
            chunkKey.X++;
            x = 0;
        }
        if (y == CHUNK_SIZE)
        {
            chunkKey.Y++;
            y = 0;
        }
        if (z == CHUNK_SIZE)
        {
            chunkKey.Z++;
            z = 0;
        }

        if (chunkKey == key)
        {
            return GetLocalVoxel(x, y, z);
        }
        else
        {
            Chunk neighborChunk = GetDirectNeighbour(chunkKey - key);
            if (neighborChunk == null)
            {
                GD.Print($"Chunk[{key}] failed to find neighbour in direction {chunkKey - key}");
                return 0; // Default value for neighboring chunk not found
            }

            byte voxelValue = neighborChunk.GetLocalVoxel(x, y, z);
            return voxelValue;

        }
    }

}
