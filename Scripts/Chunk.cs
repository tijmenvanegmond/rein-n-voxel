using System;
using System.Collections.Generic;
using Godot;

public partial class Chunk : StaticBody3D
{
    const int CHUNK_SIZE = 10;
    const float CUBE_SIZE = .3f;
    [Export]
    Material material;
    [Export]
    MeshInstance3D surfaceMesh;
    [Export]
    CollisionShape3D collisionMesh;

    byte[,,] dataArray;

    public void Initialize(Vector3 startPosition)
    {
        Position = startPosition;

        dataArray = GenerateData();
        //load overlay data
        var tmpMesh = GenerateMesh(dataArray);

        surfaceMesh.Mesh = tmpMesh;
        // surfaceMesh.CreateTrimeshCollision();
    }

    public byte[,,] GenerateData()
    {

        var dataArray = new byte[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
        var noise = new FastNoiseLite();

        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                float height = noise.GetNoise2D(x, z) * 10f;
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
        var mesh = MarchingCubes.CreateMesh(dataArray);
        return mesh;
    }
}
