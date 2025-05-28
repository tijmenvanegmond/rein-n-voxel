using Godot;

public interface IMob
{
    // Position and movement properties
    Vector3 Position { get; set; }
    Vector3 LinearVelocity { get; set; }
    Vector3 AngularVelocity { get; set; }
    Transform3D GlobalTransform { get; }
    float power { get; }
    
    // State management
    MobState currentState { get; set; }
    MobPersonality personality { get; set; }
    Area3D visionArea { get; }
    Node3D target { get; }
    
    // Behavior parameters
    float detectionRange { get; }
    float fleeDistance { get; }
    float investigateDistance { get; }
    float wanderRadius { get; }
    
    // Physics methods
    void ApplyCentralImpulse(Vector3 impulse);
    void LookAt(Vector3 target, Vector3 up);
    World3D GetWorld3D();
    Node GetTree();
    
    // State and behavior methods
    void ChangeState(MobState newState);
    void SetRandomWanderTarget();
    Vector3 GetWanderTarget();
    float GetStateTimer();
    void ResetStateTimer();
    void SetLastInterestPoint(Node3D point);
    
    // Property setters
    void SetMaxSpeed(float speed);
    void SetPower(float power);
    float GetMaxSpeed();
    
    // Utility methods
    Node FindChild(string name);
}
