using System;
using Godot;
using Godot.Collections;

public partial class Chunk : Node3D
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

    public bool SetLocalVoxel(Vector3I coord, byte byteValue)
    {
        int x = coord.X;
        int y = coord.Y;
        int z = coord.Z;

        if (x >= dataArrSize || x < 0 ||
            y >= dataArrSize || y < 0 ||
            z >= dataArrSize || z < 0)
        {  // out of bounds
            return false;
        }

        dataArray[x, y, z] = byteValue;
        PlanMeshUpdate();

        //update neighbouring chunks
        var neighborChunkKey = key;
        if (x == CHUNK_SIZE - 1) neighborChunkKey.X++;
        if (x == 0) neighborChunkKey.X--;
        if (y == CHUNK_SIZE - 1) neighborChunkKey.Y++;
        if (y == 0) neighborChunkKey.Y--;
        if (z == CHUNK_SIZE - 1) neighborChunkKey.Z++;
        if (z == 0) neighborChunkKey.Z--;

        if (neighborChunkKey != key) //its actually a neighbour chunk
        {
            Chunk neighborChunk = GetDirectNeighbour(neighborChunkKey - key);
            if (neighborChunk == null)
            {
                GD.Print($"Chunk[{key}] failed to find neighbour in direction {neighborChunkKey - key}");
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

        var newMesh = MarchingCubes.CreateMesh(dataPlus1);

    
        //TODO: check if correct child is removed
        var oldCollider = surfaceMesh.GetChild(0);
        if (oldCollider != null)
        {
            oldCollider.QueueFree();
        }


        surfaceMesh.Mesh = newMesh;
        surfaceMesh.CreateTrimeshCollision();

        doMeshUpdateInt = 0;

        return newMesh;
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
