using System;

[Flags]
public enum PowerupType
{
	None = 0,
	MoveFaster = 2,
	DashThroughWalls = 4,
	ReverseInputs = 8,
	Shield = 0x10,
	ExplosiveDisc = 0x20,
	MultiDisc = 0x40,
	TeleportDisc = 0x80,
	FireDisc = 0x100,
	ExtraDisc = 0x200,
	SlimeDisc = 0x400,
	Disguise = 0x800,
	TimeImmune = 0x1000,
	ControlledDisc = 0x2000,
	ShrinkingBounds = 0x4000,
	Decoy = 0x8000
}
