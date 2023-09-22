using Godot;

public partial class Chunk : StaticBody3D
{

    // This function will be called from the Main scene.
    public void Initialize(Vector3 startPosition, Vector3 playerPosition)
    {
        // We position the mob by placing it at startPosition
        // and rotate it towards playerPosition, so it looks at the player.
        Position = startPosition;
       
    }

}
