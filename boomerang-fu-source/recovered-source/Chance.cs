using System;
using System.Collections.Generic;
using UnityEngine;

public static class Chance
{
	private static System.Random rng = new System.Random();

	public static bool PercentageTrue(float probability)
	{
		if ((float)UnityEngine.Random.Range(0, 100) <= probability)
		{
			return true;
		}
		return false;
	}

	public static void Shuffle<T>(this IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = rng.Next(num + 1);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}
}
