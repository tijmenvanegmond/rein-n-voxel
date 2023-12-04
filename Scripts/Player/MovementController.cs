using System;
using Godot;

public partial class MovementController : CharacterBody3D
{
	[Export]
	public Node3D cameraPivot;
	[Export]
	public Node3D BodyPivot;
	[Export]
	public const float normalMovementSpeed = 5.0f;
	[Export]
	public const float crouchedMovementSpeed = 2.5f;
	[Export]
	public const float jumpVelocity = 7f;
	public Vector3 movementDirection { get; protected set; }
	float currentMovementSpeed = normalMovementSpeed;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = jumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		movementDirection = (cameraPivot.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y) * -1).Normalized();

		if (movementDirection != Vector3.Zero)
		{
			velocity.X = movementDirection.X * currentMovementSpeed;
			velocity.Z = movementDirection.Z * currentMovementSpeed;

			BodyPivot.LookAt(Position - movementDirection);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, currentMovementSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentMovementSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

    public void Crouch()
    {
        currentMovementSpeed = crouchedMovementSpeed;
    }

    public void UnCrouch()
    {
        currentMovementSpeed = normalMovementSpeed;
    }
}
