public static class EnumExt
{
	public static bool HasPowerup(this PowerupType property, PowerupType value)
	{
		return (property & value) == value;
	}

	public static int CountSetFlags(ulong lValue)
	{
		int num = 0;
		while (lValue != 0L)
		{
			lValue &= lValue - 1;
			num++;
		}
		return num;
	}
}
