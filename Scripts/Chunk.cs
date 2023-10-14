using System;
using System.Collections.Generic;
using Godot;

public partial class Chunk : StaticBody3D
{
    const int CHUNK_SIZE = 12;
    const float CUBE_SIZE = 1f;
    [Export]
    MeshInstance3D surfaceMesh;
    byte[,,] dataArray;

    public void Initialize(Vector3 startPosition)
    {
        Position = startPosition;
        dataArray = GenerateData();
        //load overlay data
        var newMesh = GenerateMesh(dataArray);
        surfaceMesh.Mesh = newMesh;
        surfaceMesh.CreateTrimeshCollision();
    }

    public byte[,,] GenerateData()
    {

        var dataArray = new byte[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
        var noise = new FastNoiseLite();

        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                float height = noise.GetNoise2D(x+(Position.X/CUBE_SIZE), z+(Position.Z/CUBE_SIZE)) * 12f;
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    dataArray[x, y, z] = (int)height < y ? (byte)1 : (byte)0;
                }

            }
        }

        return dataArray;
    }

    public Mesh GenerateMesh(byte[,,] data)
    {
        var mesh = MarchingCubes.CreateMesh(data);
        return mesh;
    }
}
