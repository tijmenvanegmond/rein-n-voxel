using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class MobThreadManager : RefCounted
{
    private static MobThreadManager instance;
    public static MobThreadManager Instance => instance ??= new MobThreadManager();

    private readonly object lockObject = new object();
    private List<Mob> mobsToProcess = new List<Mob>();
    private List<MobProcessingData> processingResults = new List<MobProcessingData>();
    private bool isProcessing = false;
    private CancellationTokenSource cancellationTokenSource;
    private Task processingTask;    // Threading configuration
    public int maxThreads { get; set; } = Math.Max(1, System.Environment.ProcessorCount - 1);
    public bool enableMultithreading { get; set; } = false; // Disabled due to thread safety issues
    public float processInterval { get; set; } = 1f / 60f; // 60 FPS target
    public int mobsPerBatch { get; set; } = 10;

    // Performance metrics
    public float lastProcessingTime { get; private set; } = 0f;
    public int mobsProcessedLastFrame { get; private set; } = 0;
    public float averageProcessingTime { get; private set; } = 0f;
    private Queue<float> processingTimeHistory = new Queue<float>();
    private const int maxHistorySize = 60; // 1 second of history at 60 FPS

    public MobThreadManager()
    {
        cancellationTokenSource = new CancellationTokenSource();
    }

    public void StartProcessing()
    {
        if (isProcessing) return;

        isProcessing = true;
        
        if (enableMultithreading)
        {
            processingTask = Task.Run(async () => await ProcessingLoop(), cancellationTokenSource.Token);
        }
    }

    public void StopProcessing()
    {
        if (!isProcessing) return;

        isProcessing = false;
        cancellationTokenSource?.Cancel();
        
        try
        {
            processingTask?.Wait(1000); // Wait up to 1 second
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();
    }

    public void RegisterMob(Mob mob)
    {
        lock (lockObject)
        {
            if (!mobsToProcess.Contains(mob))
            {
                mobsToProcess.Add(mob);
            }
        }
    }

    public void UnregisterMob(Mob mob)
    {
        lock (lockObject)
        {
            mobsToProcess.Remove(mob);
        }
    }

    public void ProcessMobsSync(double delta)
    {
        if (enableMultithreading)
        {
            // Apply results from multithreaded processing
            ApplyProcessingResults();
        }
        else
        {
            // Process mobs on main thread
            ProcessMobsSingleThreaded(delta);
        }
    }

    private void ProcessMobsSingleThreaded(double delta)
    {
        var startTime = Time.GetUnixTimeFromSystem();
        int processed = 0;

        lock (lockObject)
        {
            foreach (var mob in mobsToProcess)
            {
                if (!GodotObject.IsInstanceValid(mob) || mob.isDead) continue;

                ProcessMobLogic(mob, delta);
                processed++;
            }
        }

        var endTime = Time.GetUnixTimeFromSystem();
        UpdatePerformanceMetrics((float)(endTime - startTime), processed);
    }

    private async Task ProcessingLoop()
    {
        while (isProcessing && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var mobsSnapshot = new List<Mob>();
                
                lock (lockObject)
                {
                    mobsSnapshot.AddRange(mobsToProcess);
                }

                if (mobsSnapshot.Count > 0)
                {
                    await ProcessMobsMultithreaded(mobsSnapshot, processInterval);
                }

                var processingTime = (DateTime.UtcNow - startTime).TotalSeconds;
                var sleepTime = Math.Max(0, processInterval - processingTime);
                
                if (sleepTime > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(sleepTime), cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in mob processing loop: {ex.Message}");
                await Task.Delay(100); // Brief pause before retrying
            }
        }
    }

    private async Task ProcessMobsMultithreaded(List<Mob> mobs, double delta)
    {
        var batches = CreateBatches(mobs, mobsPerBatch);
        var tasks = new List<Task<List<MobProcessingData>>>();

        foreach (var batch in batches)
        {
            var batchCopy = new List<Mob>(batch);
            tasks.Add(Task.Run(() => ProcessMobBatch(batchCopy, delta), cancellationTokenSource.Token));
        }

        var results = await Task.WhenAll(tasks);
        
        lock (lockObject)
        {
            processingResults.Clear();
            foreach (var resultBatch in results)
            {
                processingResults.AddRange(resultBatch);
            }
        }
    }

    private List<List<Mob>> CreateBatches(List<Mob> mobs, int batchSize)
    {
        var batches = new List<List<Mob>>();
        
        for (int i = 0; i < mobs.Count; i += batchSize)
        {
            var batch = new List<Mob>();
            for (int j = i; j < Math.Min(i + batchSize, mobs.Count); j++)
            {
                batch.Add(mobs[j]);
            }
            batches.Add(batch);
        }
        
        return batches;
    }

    private List<MobProcessingData> ProcessMobBatch(List<Mob> batch, double delta)
    {
        var results = new List<MobProcessingData>();

        foreach (var mob in batch)
        {
            if (mob == null || !GodotObject.IsInstanceValid(mob) || mob.isDead) continue;

            try
            {
                var data = CalculateMobBehavior(mob, delta);
                if (data != null)
                {
                    results.Add(data);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error processing mob {mob.name}: {ex.Message}");
            }
        }

        return results;
    }    private MobProcessingData CalculateMobBehavior(Mob mob, double delta)
    {
        // This runs on a background thread - only calculate data, don't modify Godot objects
        var data = new MobProcessingData
        {
            mobInstance = mob,
            deltaTime = delta,
            currentPosition = mob.Position,
            currentVelocity = mob.LinearVelocity,
            currentState = mob.currentState
        };

        // Calculate desired movement based on current state
        switch (mob.currentState)
        {
            case MobState.Wandering:
                data.desiredDirection = CalculateWanderDirection(mob, data);
                break;
            case MobState.Seeking:
                data.desiredDirection = CalculateSeekDirection(mob);
                break;
            case MobState.Fleeing:
                data.desiredDirection = CalculateFleeDirection(mob);
                break;
            case MobState.Combat:
                data.desiredDirection = CalculateCombatDirection(mob);
                break;
            default:
                data.desiredDirection = Vector3.Zero;
                break;
        }

        // Calculate state changes
        data.newState = CalculateStateTransition(mob);
        
        return data;
    }    private Vector3 CalculateWanderDirection(Mob mob, MobProcessingData data)
    {
        // Thread-safe wander calculation
        var toTarget = mob.GetWanderTarget() - mob.Position;
        if (toTarget.Length() < 2f)
        {
            data.shouldSetNewWanderTarget = true;
        }
        return new Vector3(toTarget.X, 0, toTarget.Z).Normalized();
    }

    private Vector3 CalculateSeekDirection(Mob mob)
    {
        // Calculate direction to food or other targets
        if (mob.foodComponent.currentFoodTarget != null)
        {
            var toFood = mob.foodComponent.currentFoodTarget.Position - mob.Position;
            return new Vector3(toFood.X, 0, toFood.Z).Normalized();
        }
        return Vector3.Zero;
    }

    private Vector3 CalculateFleeDirection(Mob mob)
    {
        // Calculate flee direction from threats
        Vector3 fleeDirection = Vector3.Zero;
        int threatCount = 0;

        foreach (var threat in mob.combatComponent.threatList.Keys)
        {
            if (GodotObject.IsInstanceValid(threat))
            {
                var away = mob.Position - threat.Position;
                fleeDirection += new Vector3(away.X, 0, away.Z).Normalized();
                threatCount++;
            }
        }

        return threatCount > 0 ? fleeDirection / threatCount : Vector3.Zero;
    }

    private Vector3 CalculateCombatDirection(Mob mob)
    {
        // Calculate combat movement
        return mob.combatComponent.GetCombatMovement();
    }

    private MobState CalculateStateTransition(Mob mob)
    {
        // Calculate what state the mob should transition to
        // This is a simplified version that runs thread-safely
        
        if (mob.healthComponent.isDead)
            return MobState.Dead;
            
        if (mob.healthComponent.IsCritical() && mob.combatComponent.threatLevel > 0)
            return MobState.Fleeing;
            
        if (mob.combatComponent.threatLevel > 0)
            return MobState.Combat;
            
        if (mob.healthComponent.IsHungry() && mob.foodComponent.WantsTosSeekFood())
            return MobState.Seeking;
            
        return mob.currentState; // No change
    }

    private void ApplyProcessingResults()
    {
        var startTime = Time.GetUnixTimeFromSystem();
        int processed = 0;

        lock (lockObject)
        {
            foreach (var data in processingResults)
            {
                if (data?.mobInstance == null || !GodotObject.IsInstanceValid(data.mobInstance))
                    continue;

                ApplyMobData(data);
                processed++;
            }
            processingResults.Clear();
        }

        var endTime = Time.GetUnixTimeFromSystem();
        UpdatePerformanceMetrics((float)(endTime - startTime), processed);
    }    private void ApplyMobData(MobProcessingData data)
    {
        var mob = data.mobInstance;
        
        // Apply state changes
        if (data.newState != mob.currentState)
        {
            mob.ChangeState(data.newState);
        }

        // Apply movement through mob's public API
        if (data.desiredDirection.Length() > 0)
        {
            // Since we can't access movementComponent directly, apply the movement differently
            var impulse = data.desiredDirection.Normalized() * mob.power * (float)data.deltaTime;
            mob.ApplyCentralImpulse(impulse);
        }

        // Apply other data changes
        if (data.shouldSetNewWanderTarget)
        {
            mob.SetRandomWanderTarget();
        }
    }    private void ProcessMobLogic(Mob mob, double delta)
    {
        // Direct processing for single-threaded mode
        mob.healthComponent.UpdateHealth(delta);
        mob.foodComponent.UpdateEating(delta);
        mob.combatComponent.UpdateCombat(delta);
        
        // Handle state-specific behavior through mob's public API
        // Since we can't access personalityComponent directly, this will be handled in the main thread
    }

    private void UpdatePerformanceMetrics(float processingTime, int mobsProcessed)
    {
        lastProcessingTime = processingTime;
        mobsProcessedLastFrame = mobsProcessed;
        
        processingTimeHistory.Enqueue(processingTime);
        while (processingTimeHistory.Count > maxHistorySize)
        {
            processingTimeHistory.Dequeue();
        }
        
        if (processingTimeHistory.Count > 0)
        {
            float total = 0f;
            foreach (var time in processingTimeHistory)
            {
                total += time;
            }
            averageProcessingTime = total / processingTimeHistory.Count;
        }
    }

    public void SetMultithreadingEnabled(bool enabled)
    {
        if (enableMultithreading == enabled) return;
        
        StopProcessing();
        enableMultithreading = enabled;
        StartProcessing();
    }

    public string GetPerformanceInfo()
    {
        return $"Mobs: {mobsToProcess.Count}, " +
               $"Last Frame: {lastProcessingTime*1000:F2}ms, " +
               $"Avg: {averageProcessingTime*1000:F2}ms, " +
               $"Threading: {(enableMultithreading ? "ON" : "OFF")}";
    }

    ~MobThreadManager()
    {
        StopProcessing();
    }
}

public class MobProcessingData
{
    public Mob mobInstance;
    public double deltaTime;
    public Vector3 currentPosition;
    public Vector3 currentVelocity;
    public MobState currentState;
    public MobState newState;
    public Vector3 desiredDirection;
    public bool shouldSetNewWanderTarget;
}
