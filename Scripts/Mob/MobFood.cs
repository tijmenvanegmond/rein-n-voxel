using System;
using System.Collections.Generic;
using Godot;

public partial class MobFood : RefCounted
{
    private Mob mob;
    private RandomNumberGenerator rng;

    // Food preferences based on personality and type
    public Dictionary<FoodType, float> foodPreferences { get; private set; }
    public float seekingFoodRadius = 25f;
    public float eatingRange = 2f;
    public float eatingDuration = 3f; // seconds to eat food
    public bool isEating { get; private set; } = false;
    public Node3D currentFoodTarget { get; private set; } = null;
    public float eatingTimer = 0f;

    // Events
    public event Action<FoodType, float> OnFoodEaten; // foodType, nutritionGained
    public event Action<Node3D> OnFoodTargetChanged; // new food target
    public event Action<bool> OnEatingStateChanged; // isEating

    public MobFood(Mob mob)
    {
        this.mob = mob;
        this.rng = new RandomNumberGenerator();
        this.rng.Randomize();
        
        InitializeFoodPreferences();
    }

    private void InitializeFoodPreferences()
    {
        foodPreferences = new Dictionary<FoodType, float>();
        
        // Set default preferences - can be modified per mob type
        foodPreferences[FoodType.Grass] = 0.5f;
        foodPreferences[FoodType.Berries] = 0.8f;
        foodPreferences[FoodType.Meat] = 0.3f;
        foodPreferences[FoodType.Seeds] = 0.7f;
        foodPreferences[FoodType.Mushrooms] = 0.4f;
        foodPreferences[FoodType.Fish] = 0.6f;
        foodPreferences[FoodType.Nectar] = 0.9f;
    }

    public void SetFoodPreferences(MobType mobType, MobPersonality personality)
    {
        // Adjust preferences based on mob type and personality
        switch (mobType)
        {
            case MobType.SmallFriendly:
                // Small friendly mobs prefer sweet foods
                foodPreferences[FoodType.Berries] = 1.0f;
                foodPreferences[FoodType.Nectar] = 1.0f;
                foodPreferences[FoodType.Seeds] = 0.8f;
                foodPreferences[FoodType.Grass] = 0.3f;
                foodPreferences[FoodType.Meat] = 0.1f;
                seekingFoodRadius = 20f;
                break;
                
            case MobType.SmallSkittish:
                // Skittish mobs are opportunistic but prefer easily accessible food
                foodPreferences[FoodType.Seeds] = 1.0f;
                foodPreferences[FoodType.Grass] = 0.8f;
                foodPreferences[FoodType.Berries] = 0.9f;
                foodPreferences[FoodType.Meat] = 0.2f;
                seekingFoodRadius = 15f; // Shorter range due to caution
                break;
                
            case MobType.Medium:
                // Medium mobs are omnivorous with balanced preferences
                foodPreferences[FoodType.Grass] = 0.7f;
                foodPreferences[FoodType.Berries] = 0.8f;
                foodPreferences[FoodType.Meat] = 0.6f;
                foodPreferences[FoodType.Seeds] = 0.6f;
                foodPreferences[FoodType.Mushrooms] = 0.7f;
                seekingFoodRadius = 30f;
                break;
                  case MobType.Large:
                // Large mobs prefer substantial food sources
                foodPreferences[FoodType.Grass] = 1.0f;
                foodPreferences[FoodType.Meat] = 0.8f;
                foodPreferences[FoodType.Mushrooms] = 0.9f;
                foodPreferences[FoodType.Berries] = 0.4f; // Too small for them
                foodPreferences[FoodType.Seeds] = 0.3f;
                seekingFoodRadius = 40f;
                break;
                
            case MobType.Predator:
                // Predators are primarily carnivorous
                foodPreferences[FoodType.Meat] = 1.0f;
                foodPreferences[FoodType.Berries] = 0.2f;
                foodPreferences[FoodType.Grass] = 0.1f;
                foodPreferences[FoodType.Seeds] = 0.1f;
                foodPreferences[FoodType.Mushrooms] = 0.3f;
                seekingFoodRadius = 50f; // Wide hunting range
                break;
                
            case MobType.HostileSmall:
                // Hostile small mobs are opportunistic carnivores
                foodPreferences[FoodType.Meat] = 1.0f;
                foodPreferences[FoodType.Berries] = 0.4f;
                foodPreferences[FoodType.Seeds] = 0.3f;
                foodPreferences[FoodType.Grass] = 0.2f;
                seekingFoodRadius = 25f;
                break;
                
            case MobType.HostileMedium:
                // Hostile medium mobs prefer meat but eat other things
                foodPreferences[FoodType.Meat] = 1.0f;
                foodPreferences[FoodType.Mushrooms] = 0.6f;
                foodPreferences[FoodType.Berries] = 0.3f;
                foodPreferences[FoodType.Grass] = 0.3f;
                foodPreferences[FoodType.Seeds] = 0.2f;
                seekingFoodRadius = 35f;
                break;
                  case MobType.HostileLarge:
                // Hostile large mobs are apex predators
                foodPreferences[FoodType.Meat] = 1.0f;
                foodPreferences[FoodType.Mushrooms] = 0.4f;
                foodPreferences[FoodType.Grass] = 0.2f;
                foodPreferences[FoodType.Berries] = 0.1f; // Too small
                foodPreferences[FoodType.Seeds] = 0.1f; // Too small
                seekingFoodRadius = 60f; // Very wide hunting range
                break;
                
            case MobType.DefenderFriendly:
                // Friendly defenders are omnivorous but prefer substantial food
                foodPreferences[FoodType.Meat] = 0.8f;
                foodPreferences[FoodType.Mushrooms] = 0.9f;
                foodPreferences[FoodType.Berries] = 0.7f;
                foodPreferences[FoodType.Grass] = 0.6f;
                foodPreferences[FoodType.Seeds] = 0.5f;
                seekingFoodRadius = 45f; // Wide range for patrol duties
                break;
                
            case MobType.DefenderNeutral:
                // Neutral defenders prefer balanced nutrition
                foodPreferences[FoodType.Meat] = 0.7f;
                foodPreferences[FoodType.Mushrooms] = 1.0f; // Prefer mushrooms
                foodPreferences[FoodType.Grass] = 0.8f;
                foodPreferences[FoodType.Berries] = 0.6f;
                foodPreferences[FoodType.Seeds] = 0.5f;
                seekingFoodRadius = 40f; // Moderate range
                break;
                
            case MobType.DefenderWild:
                // Wild defenders prefer natural foods
                foodPreferences[FoodType.Grass] = 1.0f; // Love natural vegetation
                foodPreferences[FoodType.Berries] = 0.9f;
                foodPreferences[FoodType.Mushrooms] = 0.8f;
                foodPreferences[FoodType.Seeds] = 0.7f;
                foodPreferences[FoodType.Meat] = 0.4f; // Less carnivorous
                seekingFoodRadius = 50f; // Large territory range
                break;
        }

        // Personality adjustments
        switch (personality)
        {
            case MobPersonality.Friendly:
                // Friendly mobs are less picky
                foreach (var key in new List<FoodType>(foodPreferences.Keys))
                {
                    foodPreferences[key] = Mathf.Min(1f, foodPreferences[key] + 0.2f);
                }
                break;
                
            case MobPersonality.Skittish:
                // Skittish mobs avoid meat (aggressive food)
                foodPreferences[FoodType.Meat] *= 0.3f;
                // But love safe plant foods more
                foodPreferences[FoodType.Grass] += 0.2f;
                foodPreferences[FoodType.Seeds] += 0.2f;
                break;
                  case MobPersonality.Neutral:
                // No adjustments for neutral
                break;
                
            case MobPersonality.Aggressive:
                // Aggressive mobs strongly prefer meat
                foodPreferences[FoodType.Meat] = Mathf.Min(1f, foodPreferences[FoodType.Meat] + 0.3f);
                // And are less interested in plant foods
                foodPreferences[FoodType.Grass] *= 0.7f;
                foodPreferences[FoodType.Seeds] *= 0.7f;
                foodPreferences[FoodType.Berries] *= 0.8f;
                break;
        }
    }

    public Node3D FindNearestFood()
    {
        if (mob.visionArea == null) return null;

        var bodies = mob.visionArea.GetOverlappingBodies();
        Node3D bestFood = null;
        float bestScore = 0f;
        float closestDistance = float.MaxValue;

        foreach (var body in bodies)
        {
            if (body is IFood food)
            {
                var foodNode = body as Node3D;
                var distance = mob.Position.DistanceTo(foodNode.Position);
                
                if (distance > seekingFoodRadius) continue;

                // Calculate food preference score
                float preference = foodPreferences.GetValueOrDefault(food.GetFoodType(), 0f);
                float nutritionValue = food.GetNutritionValue();
                
                // Distance penalty (closer food is better)
                float distanceFactor = 1f - (distance / seekingFoodRadius);
                
                // Calculate overall score
                float score = preference * nutritionValue * distanceFactor;
                
                if (score > bestScore || (score == bestScore && distance < closestDistance))
                {
                    bestScore = score;
                    bestFood = foodNode;
                    closestDistance = distance;
                }
            }
        }

        return bestFood;
    }

    public bool SetFoodTarget(Node3D foodTarget)
    {
        if (currentFoodTarget != foodTarget)
        {
            currentFoodTarget = foodTarget;
            OnFoodTargetChanged?.Invoke(currentFoodTarget);
            return true;
        }
        return false;
    }

    public void StartEating(Node3D food)
    {
        if (isEating || food == null || !(food is IFood)) return;

        var foodComponent = food as IFood;
        var distance = mob.Position.DistanceTo(food.Position);
        
        if (distance > eatingRange) return;

        isEating = true;
        currentFoodTarget = food;
        eatingTimer = 0f;
        
        // Adjust eating duration based on food type and mob size
        float durationMultiplier = 1f;
        switch (mob.personality)
        {
            case MobPersonality.Skittish:
                durationMultiplier = 0.7f; // Eat faster (nervous)
                break;
            case MobPersonality.Friendly:
                durationMultiplier = 1.2f; // Eat more leisurely
                break;
        }
        
        eatingDuration = foodComponent.GetEatingDuration() * durationMultiplier;
        
        OnEatingStateChanged?.Invoke(true);
        
        // Change mob state to eating
        mob.ChangeState(MobState.Eating);
        
        GD.Print($"Mob {mob.name} started eating {foodComponent.GetFoodType()}");
    }

    public void UpdateEating(double delta)
    {
        if (!isEating || currentFoodTarget == null) return;

        eatingTimer += (float)delta;

        // Check if food still exists and is still in range
        if (!GodotObject.IsInstanceValid(currentFoodTarget))
        {
            StopEating();
            return;
        }

        var distance = mob.Position.DistanceTo(currentFoodTarget.Position);
        if (distance > eatingRange)
        {
            StopEating();
            return;
        }

        // Finish eating when timer completes
        if (eatingTimer >= eatingDuration)
        {
            FinishEating();
        }
    }

    private void FinishEating()
    {
        if (!isEating || currentFoodTarget == null) return;

        var food = currentFoodTarget as IFood;
        if (food != null)
        {
            // Consume the food
            float nutritionGained = food.GetNutritionValue();
            FoodType foodType = food.GetFoodType();
            
            // Apply preference multiplier to nutrition gained
            float preferenceMultiplier = foodPreferences.GetValueOrDefault(foodType, 0.5f);
            float actualNutrition = nutritionGained * preferenceMultiplier;
            
            // Feed the mob
            mob.healthComponent.Feed(actualNutrition);
            
            // Consume the food item
            food.Consume();
            
            OnFoodEaten?.Invoke(foodType, actualNutrition);
            
            GD.Print($"Mob {mob.name} ate {foodType} and gained {actualNutrition:F1} hunger");
        }

        StopEating();
    }

    public void StopEating()
    {
        if (!isEating) return;

        isEating = false;
        currentFoodTarget = null;
        eatingTimer = 0f;
        
        OnEatingStateChanged?.Invoke(false);
        
        // Return to previous state (usually wandering)
        if (mob.currentState == MobState.Eating)
        {
            mob.ChangeState(MobState.Wandering);
        }
    }

    public bool WantsTosSeekFood()
    {
        return mob.healthComponent.IsHungry() && !isEating;
    }

    public bool CanEat(FoodType foodType)
    {
        // Check if the mob has any preference for this food type
        // A preference of 0 means the mob won't eat this food type
        return foodPreferences.GetValueOrDefault(foodType, 0f) > 0f;
    }

    public float GetFoodPreference(FoodType foodType)
    {
        return foodPreferences.GetValueOrDefault(foodType, 0f);
    }

    public void ModifyFoodPreference(FoodType foodType, float change)
    {
        if (foodPreferences.ContainsKey(foodType))
        {
            foodPreferences[foodType] = Mathf.Clamp(foodPreferences[foodType] + change, 0f, 1f);
        }
    }

    // New methods for Area3D detection integration
    public void OnFoodDetected(FoodItem foodItem)
    {
        // Only consider food if the mob is hungry and not already eating
        if (!WantsTosSeekFood() || isEating) return;
        
        // Check if the mob can eat this food type
        if (!CanEat(foodItem.GetFoodType())) return;
        
        // If no current target or this food is better than current target
        if (currentFoodTarget == null || IsBetterFoodTarget(foodItem))
        {
            SetFoodTarget(foodItem);
        }
    }

    public void OnFoodLost(FoodItem foodItem)
    {
        // If this was the current target, clear it
        if (currentFoodTarget == foodItem)
        {
            currentFoodTarget = null;
            OnFoodTargetChanged?.Invoke(null);
            
            // If we were eating this food, stop eating
            if (isEating)
            {
                StopEating();
            }
        }
    }

    private bool IsBetterFoodTarget(FoodItem newFood)
    {
        if (currentFoodTarget == null) return true;
        
        var currentFood = currentFoodTarget as IFood;
        if (currentFood == null) return true;
        
        // Calculate scores for both foods
        float currentDistance = mob.Position.DistanceTo(currentFoodTarget.Position);
        float newDistance = mob.Position.DistanceTo(newFood.Position);
        
        float currentPreference = foodPreferences.GetValueOrDefault(currentFood.GetFoodType(), 0f);
        float newPreference = foodPreferences.GetValueOrDefault(newFood.GetFoodType(), 0f);
        
        float currentScore = currentPreference * currentFood.GetNutritionValue() * (1f - currentDistance / seekingFoodRadius);
        float newScore = newPreference * newFood.GetNutritionValue() * (1f - newDistance / seekingFoodRadius);
        
        return newScore > currentScore;
    }

    private void SetFoodTarget(FoodItem foodItem)
    {
        currentFoodTarget = foodItem;
        OnFoodTargetChanged?.Invoke(currentFoodTarget);
          // Change mob state to seeking food if not already eating
        if (mob.currentState != MobState.Eating)
        {
            mob.ChangeState(MobState.Seeking);
        }
    }
}

// Interface for food items
public interface IFood
{
    FoodType GetFoodType();
    float GetNutritionValue();
    float GetEatingDuration();
    bool IsConsumed();
    void Consume();
}

public enum FoodType
{
    Grass,      // Common, low nutrition
    Berries,    // Sweet, medium nutrition
    Meat,       // High nutrition, rare
    Seeds,      // Small, good nutrition
    Mushrooms,  // Medium nutrition, sometimes special effects
    Fish,       // High nutrition, water-based
    Nectar,     // Very sweet, high energy
    Carrion     // Meat from dead mobs
}
