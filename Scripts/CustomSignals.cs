using Godot;
using System;

public partial class CustomSignals : Node
{
	[Signal]
	public delegate void EditTerrainEventHandler(Vector3I key, byte newValue);
}
