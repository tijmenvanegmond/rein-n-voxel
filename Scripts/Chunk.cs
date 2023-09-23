using Godot;

public partial class Chunk : StaticBody3D
{
    public void Initialize(Vector3 startPosition, Vector3 playerPosition)
    {
        Position = startPosition;
    }
}
