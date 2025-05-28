using System;
using Godot;

public partial class MobAppearance : RefCounted
{
    private Mob mob;

    public MobAppearance(Mob mob)
    {
        this.mob = mob;
    }    public void SetPersonalityAppearance()
    {
        // Find the mesh instance in the mob's children
        var meshInstance = FindMeshInstance();
        if (meshInstance == null)
        {
            GD.Print("Mob: Could not find MeshInstance3D to apply color");
            return;
        }

        // Create a new material based on personality and faction
        var material = CreatePersonalityMaterial(mob.personality, mob.faction);
        
        // Apply the material
        meshInstance.MaterialOverride = material;
        GD.Print($"Mob: Applied {mob.personality} color scheme with {mob.faction} faction indicators");
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
    }    private StandardMaterial3D CreatePersonalityMaterial(MobPersonality personality, MobFaction faction)
    {
        var material = new StandardMaterial3D();
        material.MetallicSpecular = 0.3f;
        material.Roughness = 0.7f;
        material.EmissionEnabled = true;
        
        // Base color determined by personality
        Color baseColor;
        Color baseEmission;
        
        switch (personality)
        {
            case MobPersonality.Friendly:
                // Friendly mobs are green with a soft glow
                baseColor = new Color(0.3f, 0.8f, 0.3f); // Bright green
                baseEmission = new Color(0.1f, 0.3f, 0.1f); // Soft green glow
                break;
            case MobPersonality.Skittish:
                // Skittish mobs are orange/red with nervous energy
                baseColor = new Color(0.9f, 0.5f, 0.2f); // Orange
                baseEmission = new Color(0.3f, 0.1f, 0.0f); // Warm orange glow
                break;
            case MobPersonality.Neutral:
            default:
                // Neutral mobs keep default colors (blue/gray)
                baseColor = new Color(0.4f, 0.4f, 0.7f); // Blue-gray
                baseEmission = new Color(0.05f, 0.05f, 0.15f); // Subtle blue glow
                break;
        }
        
        // Modify color based on faction
        switch (faction)
        {
            case MobFaction.Player:
                // Player-allied mobs have a bright, warm tint
                baseColor = baseColor * new Color(1.2f, 1.1f, 0.9f); // Warmer, brighter
                baseEmission = baseEmission * 1.3f; // Stronger emission
                break;
            case MobFaction.Wild:
                // Wild mobs have natural, earthy tones
                baseColor = baseColor * new Color(0.8f, 0.9f, 0.7f); // Slightly muted, green tint
                baseEmission = baseEmission * 0.8f; // Dimmer emission
                break;
            case MobFaction.Hostile:
                // Hostile mobs have a reddish tint
                baseColor = baseColor * new Color(1.3f, 0.7f, 0.7f); // Red tint
                baseEmission = baseEmission * new Color(1.5f, 0.5f, 0.5f); // Red-tinted emission
                break;
            case MobFaction.Neutral:
            default:
                // Neutral mobs keep base colors
                break;
        }
        
        material.AlbedoColor = baseColor;
        material.Emission = baseEmission;
        
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
                    break;                default:
                    // Reset to normal emission - recreate material to ensure base values
                    material = CreatePersonalityMaterial(mob.personality, mob.faction);
                    meshInstance.MaterialOverride = material;
                    break;
            }
        }
    }
}
