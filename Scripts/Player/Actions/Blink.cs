using Godot;
using System;

public partial class Blink : MovementAction
{
    float DASH_LENGTH = 2f;

    public override void DoAction(MovementController target)
    {
        var timeBetween = Time.GetTicksMsec() - lastUsed;
        if (timeBetween < (ulong)coolDownMs)
            return;

        var goalPos = target.Position + target.MovementDirection * DASH_LENGTH;
        target.Position = target.Position.Lerp(goalPos, DASH_LENGTH);

        base.DoAction(target);
    }
}
