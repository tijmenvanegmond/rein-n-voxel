using Godot;
using System;

public partial class MovementAction : Node
{

    protected int coolDownMs = 1000;

    protected ulong lastUsed = 0;

    public virtual void DoAction(MovementController target)
    {
        lastUsed = Time.GetTicksMsec();
    }
}
