using Godot;

public partial class MobInfoUI : Control
{
	private Mob selectedMob;
	private bool isVisible = false;
	
	// UI Elements
	private Label mobNameLabel;
	private Label mobStateLabel;
	private Label mobHealthLabel;
	private Label mobHungerLabel;
	private Label mobThreatLabel;
	private Label mobVisionRangeLabel;
	private Label mobPersonalityLabel;
	private Label mobFactionLabel;
	private Label mobPositionLabel;
	private Label mobVelocityLabel;
	private Panel backgroundPanel;

	public override void _Ready()
	{
		// Create UI elements
		SetupUI();
		
		// Initially hide the panel
		SetMobInfoVisible(false);
		
		// Add to mob_info_ui group so other systems can find this
		AddToGroup("mob_info_ui");
	}

	private void SetupUI()
	{
		// Create background panel
		backgroundPanel = new Panel();
		backgroundPanel.Size = new Vector2(300, 250);
		backgroundPanel.Position = new Vector2(10, 10);
		
		// Style the panel
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0.8f);
		styleBox.BorderColor = new Color(1, 1, 1, 0.5f);
		styleBox.BorderWidthTop = 2;
		styleBox.BorderWidthBottom = 2;
		styleBox.BorderWidthLeft = 2;
		styleBox.BorderWidthRight = 2;
		styleBox.CornerRadiusTopLeft = 5;
		styleBox.CornerRadiusTopRight = 5;
		styleBox.CornerRadiusBottomLeft = 5;
		styleBox.CornerRadiusBottomRight = 5;
		backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);
		
		AddChild(backgroundPanel);

		// Create labels container
		var vbox = new VBoxContainer();
		vbox.Position = new Vector2(10, 10);
		vbox.Size = new Vector2(280, 230);
		backgroundPanel.AddChild(vbox);

		// Create title
		var titleLabel = new Label();
		titleLabel.Text = "MOB INFORMATION";
		titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1)); // Yellow
		vbox.AddChild(titleLabel);

		// Create info labels
		mobNameLabel = CreateInfoLabel("Name: ");
		mobStateLabel = CreateInfoLabel("State: ");
		mobHealthLabel = CreateInfoLabel("Health: ");
		mobHungerLabel = CreateInfoLabel("Hunger: ");
		mobThreatLabel = CreateInfoLabel("Threat Level: ");
		mobVisionRangeLabel = CreateInfoLabel("Vision Range: ");
		mobPersonalityLabel = CreateInfoLabel("Personality: ");
		mobFactionLabel = CreateInfoLabel("Faction: ");
		mobPositionLabel = CreateInfoLabel("Position: ");
		mobVelocityLabel = CreateInfoLabel("Velocity: ");

		vbox.AddChild(mobNameLabel);
		vbox.AddChild(mobStateLabel);
		vbox.AddChild(mobHealthLabel);
		vbox.AddChild(mobHungerLabel);
		vbox.AddChild(mobThreatLabel);
		vbox.AddChild(mobVisionRangeLabel);
		vbox.AddChild(mobPersonalityLabel);
		vbox.AddChild(mobFactionLabel);
		vbox.AddChild(mobPositionLabel);
		vbox.AddChild(mobVelocityLabel);
	}

	private Label CreateInfoLabel(string prefix)
	{
		var label = new Label();
		label.Text = prefix + "N/A";
		label.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1)); // White
		return label;
	}

	public void SelectMob(Mob mob)
	{
		selectedMob = mob;
		
		if (mob != null)
		{
			SetMobInfoVisible(true);
			UpdateMobInfo();
		}
		else
		{
			SetMobInfoVisible(false);
		}
	}

	public void DeselectMob()
	{
		selectedMob = null;
		SetMobInfoVisible(false);
	}

	private void SetMobInfoVisible(bool visible)
	{
		isVisible = visible;
		backgroundPanel.Visible = visible;
	}

	public override void _Process(double delta)
	{
		// Update mob info in real-time if a mob is selected
		if (selectedMob != null && isVisible && GodotObject.IsInstanceValid(selectedMob))
		{
			UpdateMobInfo();
		}
		else if (selectedMob != null && (!GodotObject.IsInstanceValid(selectedMob) || selectedMob.isDead))
		{
			// Mob was deleted or died, deselect it
			DeselectMob();
		}
	}

	private void UpdateMobInfo()
	{
		if (selectedMob == null) return;

		mobNameLabel.Text = $"Name: {selectedMob.name}";
		mobStateLabel.Text = $"State: {selectedMob.currentState}";
		
		// Health info
		if (selectedMob.healthComponent != null)
		{
			mobHealthLabel.Text = $"Health: {selectedMob.healthComponent.currentHealth:F1}/{selectedMob.healthComponent.maxHealth:F1}";
			mobHungerLabel.Text = $"Hunger: {selectedMob.healthComponent.currentHunger:F1}/{selectedMob.healthComponent.maxHunger:F1}";
		}
				// Threat level
		if (selectedMob.combatComponent != null)
		{
			mobThreatLabel.Text = $"Threat Level: {selectedMob.combatComponent.threatLevel:F1}";
		}
		
		// Vision range and other properties
		mobVisionRangeLabel.Text = $"Vision Range: {selectedMob.detectionRange:F1}";
		mobPersonalityLabel.Text = $"Personality: {selectedMob.personality}";
		mobFactionLabel.Text = $"Faction: {selectedMob.faction}";
		
		// Position and velocity
		var pos = selectedMob.GlobalPosition;
		var vel = selectedMob.LinearVelocity;
		mobPositionLabel.Text = $"Position: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})";
		mobVelocityLabel.Text = $"Velocity: {vel.Length():F1} m/s";
	}

	public override void _Input(InputEvent @event)
	{
		// ESC key to deselect
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Escape && selectedMob != null)
			{
				DeselectMob();
			}
		}
	}
}
