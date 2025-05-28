using Godot;
using System;

public partial class ScreenshotManager : Node
{
    [Export]
    public string screenshotDirectory = "Screenshots";
    
    [Export]
    public string screenshotPrefix = "screenshot_";
    
    private bool screenshotRequested = false;    public override void _Ready()
    {
        // Add to screenshot_manager group for easy access
        AddToGroup("screenshot_manager");
        
        // Ensure screenshot directory exists
        if (!DirAccess.DirExistsAbsolute(screenshotDirectory))
        {
            DirAccess.MakeDirRecursiveAbsolute(screenshotDirectory);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Take screenshot on F12 key press
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.F12)
            {
                TakeScreenshot();
            }
        }
    }

    public void TakeScreenshot()
    {
        screenshotRequested = true;
    }

    public override void _Process(double delta)
    {
        if (screenshotRequested)
        {
            CaptureScreenshot();
            screenshotRequested = false;
        }
    }

    private void CaptureScreenshot()
    {
        var viewport = GetViewport();
        var image = viewport.GetTexture().GetImage();
        
        // Generate timestamp-based filename
        var timestamp = Time.GetDatetimeStringFromSystem().Replace(":", "-").Replace(" ", "_");
        var filename = $"{screenshotPrefix}{timestamp}.png";
        var fullPath = $"{screenshotDirectory}/{filename}";
        
        // Save the screenshot
        var error = image.SavePng(fullPath);
        
        if (error == Error.Ok)
        {
            GD.Print($"Screenshot saved: {fullPath}");
            
            // Optional: Show notification in game
            ShowScreenshotNotification();
        }
        else
        {
            GD.PrintErr($"Failed to save screenshot: {error}");
        }
    }
    
    private void ShowScreenshotNotification()
    {
        // Create a temporary notification label
        var notification = new Label();
        notification.Text = "Screenshot saved!";
        notification.Position = new Vector2(10, 50);
        notification.AddThemeStyleboxOverride("normal", new StyleBoxFlat() 
        { 
            BgColor = new Color(0, 0, 0, 0.7f) 
        });
        notification.AddThemeColorOverride("font_color", Colors.White);
        
        // Add to scene
        GetTree().CurrentScene.AddChild(notification);
        
        // Remove after 2 seconds
        GetTree().CreateTimer(2.0).Timeout += () => notification.QueueFree();
    }
}
