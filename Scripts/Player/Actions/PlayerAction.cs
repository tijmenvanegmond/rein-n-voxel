using Godot;
using System;

public partial class PlayerAction : Node
{
    protected int coolDownMs = 1000;

    protected ulong lastUsed = 0;

    public virtual void DoAction(PlayerController target)
    {
        lastUsed = Time.GetTicksMsec();
    }
}
