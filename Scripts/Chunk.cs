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
        var tmpMesh = MarchCubes(dataArray);

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

    public Mesh MarchCubes(byte[,,] data)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var color = new Color(20, 233, 0);

        vertices.Add(new Vector3(1, 0, 0));
        vertices.Add(new Vector3(1, 0, 1));
        vertices.Add(new Vector3(0, 0, 1));
        vertices.Add(new Vector3(0, 0, 0));

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));

        vertices.Add(new Vector3(1, 0, 0));
        vertices.Add(new Vector3(1, 1, 0));
        vertices.Add(new Vector3(1, 1, 1));
        vertices.Add(new Vector3(1, 0, 0));

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.TriangleStrip);

        for (int i = 0; i < vertices.Count; i++)
        {
            st.SetColor(color);
            st.SetUV(uvs[i]);
            st.AddVertex(vertices[i]);
        }

        st.Index();

        return st.Commit();
    }
}
