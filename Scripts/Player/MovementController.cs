using Godot;
using System;

public partial class MovementController : CharacterBody3D
{
	[Export]
	public Node3D Pivot;
	[Export]
	public const float Speed = 5.0f;
	[Export]
	public const float JumpVelocity = 7f;
	public Vector3 MovementDirection { get; protected set; }

	public MovementAction Action_Down = new Blink();

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		MovementDirection = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (MovementDirection != Vector3.Zero)
		{
			velocity.X = MovementDirection.X * Speed;
			velocity.Z = MovementDirection.Z * Speed;

			Pivot.LookAt(Position + MovementDirection);

		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		if (Input.IsActionPressed("move_down") && Action_Down != null)
		{
				Action_Down.DoAction(this);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
