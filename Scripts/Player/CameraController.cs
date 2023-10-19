using Godot;

public partial class CameraController : Node3D
{
    [Export]
    public Camera3D camera3D;
    [Export]
    public Node3D cameraTarget;
    [Export]
    public CharacterBody3D playerNode { get; set; }
    [Export]
    public float MOVEMENT_SPEED { get; set; } = 5f;

    private Vector2 oldMousePosition;
    
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        var rotation_delta = 0f;
        if (Input.IsActionJustPressed("rotate_right"))
        {
            rotation_delta -= 45f;
        }
        if (Input.IsActionJustPressed("rotate_left"))
        {

            rotation_delta += 45f;
        }

        RotateY(Mathf.DegToRad(rotation_delta));

        var tilt_delta = 0f;
        if (Input.IsActionPressed("tilt_camera"))
        {
            var mouseDelta = GetViewport().GetMousePosition() - oldMousePosition;
            if(mouseDelta.Y > 0)
                tilt_delta = 1f;
            else if(mouseDelta.Y < 0)
                tilt_delta = -1f;
        }
        oldMousePosition = GetViewport().GetMousePosition();
        cameraTarget.Translate(new Vector3(0,.5f,0) * tilt_delta);


        var zoom_delta = 0f;
        if (Input.IsActionJustPressed("zoom_out"))
        {
            zoom_delta += 1f;
        }
        if (Input.IsActionJustPressed("zoom_in"))
        {
            zoom_delta -= 1f;
        }

        cameraTarget.Translate(cameraTarget.Position.Normalized() * zoom_delta);


        //move on top of player
        Position = Position.Lerp(playerNode.Position, MOVEMENT_SPEED * (float)delta);
        //move camera to cameraTarget
        camera3D.GlobalPosition = camera3D.GlobalPosition.Lerp(cameraTarget.GlobalPosition, MOVEMENT_SPEED * (float)delta);
        camera3D.LookAt(playerNode.GlobalPosition);



    }
}
