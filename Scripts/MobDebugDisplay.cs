using Godot;
using System.Linq;

public partial class MobDebugDisplay : Control
{
	private MobHandler mobHandler;
	private Label statusLabel;
	private float updateTimer = 0f;
	private const float UPDATE_INTERVAL = 1f; // Update every second

	public override void _Ready()
	{
		// Find the mob handler
		mobHandler = GetTree().GetFirstNodeInGroup("mob_handler") as MobHandler;
		if (mobHandler == null)
		{
			mobHandler = GetNode<MobHandler>("../MobHandler");
		}		// Create UI elements - position in top-left corner
		Position = new Vector2(10, 10);
		Size = new Vector2(300, 200);
		
		statusLabel = new Label();
		statusLabel.Position = Vector2.Zero;
		statusLabel.Size = Size;
		statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		AddChild(statusLabel);
		
		// Style the debug panel
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0.7f);
		styleBox.BorderColor = new Color(1, 1, 1, 0.5f);
		styleBox.SetBorderWidthAll(2);
		styleBox.SetContentMarginAll(10);
		
		AddThemeStyleboxOverride("panel", styleBox);
	}

	public override void _Process(double delta)
	{
		updateTimer += (float)delta;
		
		if (updateTimer >= UPDATE_INTERVAL)
		{
			UpdateDebugInfo();
			updateTimer = 0f;
		}
		
		// Toggle visibility with F3
		if (Input.IsActionJustPressed("ui_cancel")) // ESC key
		{
			Visible = !Visible;
		}
	}

	private void UpdateDebugInfo()
	{
		if (mobHandler == null || statusLabel == null)
			return;

		var activeMobs = mobHandler.GetActiveMobCount();
		var debugText = $"=== MOB DEBUG INFO ===\n";
		debugText += $"Active Mobs: {activeMobs}\n\n";

		if (activeMobs > 0)
		{
			// Count mobs by state
			int wandering = 0, flocking = 0, fleeing = 0, investigating = 0, idle = 0;
			float avgSpeed = 0f;
			int mobCount = 0;

			foreach (var mob in mobHandler.mobs)
			{
				if (GodotObject.IsInstanceValid(mob))
				{
					switch (mob.currentState)
					{
						case MobState.Wandering: wandering++; break;
						case MobState.Flocking: flocking++; break;
						case MobState.Fleeing: fleeing++; break;
						case MobState.Investigating: investigating++; break;
						case MobState.Idle: idle++; break;
					}
					
					avgSpeed += mob.LinearVelocity.Length();
					mobCount++;
				}
			}

			if (mobCount > 0)
				avgSpeed /= mobCount;

			debugText += $"States:\n";
			debugText += $"  Wandering: {wandering}\n";
			debugText += $"  Flocking: {flocking}\n";
			debugText += $"  Fleeing: {fleeing}\n";
			debugText += $"  Investigating: {investigating}\n";
			debugText += $"  Idle: {idle}\n\n";
			debugText += $"Avg Speed: {avgSpeed:F1}\n\n";
			debugText += $"Press ESC to toggle this display";
		}
		else
		{
			debugText += "No active mobs found.";
		}

		statusLabel.Text = debugText;
	}
}
