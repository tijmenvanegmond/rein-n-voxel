using System;
using Godot;

public partial class MobAppearance : RefCounted
{
    private Mob mob;

    public MobAppearance(Mob mob)
    {
        this.mob = mob;
    }

    public void SetPersonalityAppearance()
    {
        // Find the mesh instance in the mob's children
        var meshInstance = FindMeshInstance();
        if (meshInstance == null)
        {
            GD.Print("Mob: Could not find MeshInstance3D to apply color");
            return;
        }

        // Create a new material based on personality
        var material = CreatePersonalityMaterial(mob.personality);
        
        // Apply the material
        meshInstance.MaterialOverride = material;
        GD.Print($"Mob: Applied {mob.personality} color scheme");
    }

    private MeshInstance3D FindMeshInstance()
    {
        // Try to find the mesh instance in various ways
        var meshInstance = mob.FindChild("MeshInstance3D") as MeshInstance3D;
        if (meshInstance == null)
        {
            // Try alternative names
            meshInstance = mob.FindChild("Body") as MeshInstance3D;
            if (meshInstance == null)
            {
                try
                {
                    meshInstance = mob.GetNode<MeshInstance3D>("MeshInstance3D");
                }
                catch
                {
                    // Last resort - search all children
                    meshInstance = FindMeshInstanceRecursive(mob);
                }
            }
        }
        return meshInstance;
    }

    private MeshInstance3D FindMeshInstanceRecursive(Node node)
    {
        if (node is MeshInstance3D mesh)
            return mesh;

        foreach (Node child in node.GetChildren())
        {
            var result = FindMeshInstanceRecursive(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private StandardMaterial3D CreatePersonalityMaterial(MobPersonality personality)
    {
        var material = new StandardMaterial3D();
        material.MetallicSpecular = 0.3f;
        material.Roughness = 0.7f;
        material.EmissionEnabled = true;
        
        switch (personality)
        {
            case MobPersonality.Friendly:
                // Friendly mobs are green with a soft glow
                material.AlbedoColor = new Color(0.3f, 0.8f, 0.3f); // Bright green
                material.Emission = new Color(0.1f, 0.3f, 0.1f); // Soft green glow
                break;
            case MobPersonality.Skittish:
                // Skittish mobs are orange/red with nervous energy
                material.AlbedoColor = new Color(0.9f, 0.5f, 0.2f); // Orange
                material.Emission = new Color(0.3f, 0.1f, 0.0f); // Warm orange glow
                break;
            case MobPersonality.Neutral:
            default:
                // Neutral mobs keep default colors (blue/gray)
                material.AlbedoColor = new Color(0.4f, 0.4f, 0.7f); // Blue-gray
                material.Emission = new Color(0.05f, 0.05f, 0.15f); // Subtle blue glow
                break;
        }
        
        return material;
    }

    public void UpdateAppearanceBasedOnState(MobState state)
    {
        var meshInstance = FindMeshInstance();
        if (meshInstance?.MaterialOverride is StandardMaterial3D material)
        {
            // Adjust emission intensity based on state
            switch (state)
            {
                case MobState.Fleeing:
                    // Brighter emission when fleeing (stressed)
                    material.Emission = material.Emission * 1.5f;
                    break;
                case MobState.Investigating:
                    // Slightly brighter when curious
                    material.Emission = material.Emission * 1.2f;
                    break;
                default:
                    // Reset to normal emission - recreate material to ensure base values
                    material = CreatePersonalityMaterial(mob.personality);
                    meshInstance.MaterialOverride = material;
                    break;
            }
        }
    }
}
