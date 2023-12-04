using Godot;
using System;

public partial class Blink : PlayerAction
{
    float DASH_LENGTH = 2f;

    public override void DoAction(PlayerController target)
    {
        var timeBetween = Time.GetTicksMsec() - lastUsed;
        if (timeBetween < (ulong)coolDownMs)
            return;

        var goalPos = target.movementController.Position + target.movementController.MovementDirection * DASH_LENGTH;
       target.movementController.Position = target.movementController.Position.Lerp(goalPos, DASH_LENGTH);

        base.DoAction(target);
    }
}
