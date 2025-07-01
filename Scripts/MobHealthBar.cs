using Godot;

public partial class MobHealthBar : Control
{
    private ProgressBar healthBar;
    private ProgressBar hungerBar;
    private Camera3D camera;
    private Node3D targetMob;
    private Vector3 offset = new Vector3(0, 3, 0);
    
    [Export]
    public bool alwaysVisible = false; // Show health bar always or only when damaged/hungry
    [Export]
    public float hideDelay = 3f; // Time to hide after full health/hunger
    
    private float hideTimer = 0f;
    private bool shouldBeVisible = false;

    public override void _Ready()
    {
        healthBar = GetNode<ProgressBar>("HealthBar");
        hungerBar = GetNode<ProgressBar>("HungerBar");
        
        // Find main camera
        camera = GetViewport().GetCamera3D();
        
        // Set up health bar colors
        var healthStyle = new StyleBoxFlat();
        healthStyle.BgColor = new Color(0.8f, 0.2f, 0.2f); // Red
        healthBar.AddThemeStyleboxOverride("fill", healthStyle);
        
        // Set up hunger bar colors  
        var hungerStyle = new StyleBoxFlat();
        hungerStyle.BgColor = new Color(0.2f, 0.6f, 0.2f); // Green
        hungerBar.AddThemeStyleboxOverride("fill", hungerStyle);
        
        // Start hidden
        Visible = false;
    }
    
    public void SetTarget(Node3D mob)
    {
        targetMob = mob;
    }
    
    public void UpdateHealth(float current, float max)
    {
        if (healthBar == null) return;
        
        float percent = (current / max) * 100f;
        healthBar.Value = percent;
        
        // Show if health is not full or if always visible
        shouldBeVisible = alwaysVisible || percent < 100f;
        UpdateVisibility();
    }
    
    public void UpdateHunger(float current, float max)
    {
        if (hungerBar == null) return;
        
        float percent = (current / max) * 100f;
        hungerBar.Value = percent;
        
        // Show if hunger is low or if always visible
        shouldBeVisible = alwaysVisible || percent < 80f;
        UpdateVisibility();
    }
    
    private void UpdateVisibility()
    {
        if (shouldBeVisible)
        {
            Visible = true;
            hideTimer = 0f;
        }
        else
        {
            hideTimer = hideDelay;
        }
    }

    public override void _Process(double delta)
    {
        // Handle hiding delay
        if (!shouldBeVisible && Visible)
        {
            hideTimer -= (float)delta;
            if (hideTimer <= 0f)
            {
                Visible = false;
            }
        }
        
        // Update position to follow mob
        if (targetMob != null && camera != null && Visible)
        {
            Vector3 worldPos = targetMob.GlobalPosition + offset;
            Vector2 screenPos = camera.UnprojectPosition(worldPos);
            
            // Check if position is behind camera
            Vector3 cameraTransform = camera.GlobalTransform.Basis.Z;
            Vector3 toTarget = worldPos - camera.GlobalPosition;
            if (toTarget.Dot(cameraTransform) > 0)
            {
                // Behind camera, hide
                Modulate = new Color(1, 1, 1, 0);
                return;
            }
            
            // Update position and make visible
            GlobalPosition = screenPos - Size / 2;
            Modulate = new Color(1, 1, 1, 1);
        }
    }
}
