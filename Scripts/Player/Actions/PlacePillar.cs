using Godot;
using System;

public partial class PlacePylon : PlayerAction
{

    public override void DoAction(PlayerController target)
    {
        var timeBetween = Time.GetTicksMsec() - lastUsed;
        if (timeBetween < (ulong)coolDownMs)
            return;

        var coords = target.movementController.Position;

        base.DoAction(target);
    }
}
