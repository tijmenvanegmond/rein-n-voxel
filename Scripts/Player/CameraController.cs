using Godot;

public partial class CameraController : Node3D
{
    [Export]
    public CharacterBody3D playerNode { get; set; }
    [Export]
    public float MOVEMENT_SPEED { get; set; } = 5f;
    [Export]
    public float ROTATION_SPEED { get; set; } = 5f;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        var rotation_delta = 0f;
        if (Input.IsActionPressed("rotate_right"))
        {
            rotation_delta += 1f;
        }
        if (Input.IsActionPressed("rotate_left"))
        {

            rotation_delta -= 1f;
        }

        RotateY(rotation_delta * ROTATION_SPEED * (float)delta);


        Position = Position.Lerp(playerNode.Position, MOVEMENT_SPEED * (float)delta);

    }
}
