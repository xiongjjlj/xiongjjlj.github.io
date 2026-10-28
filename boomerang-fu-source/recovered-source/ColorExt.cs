using UnityEngine;

public static class ColorExt
{
	public static void SetAlpha(Material material, float value)
	{
		Color color = material.color;
		color.a = value;
		material.color = color;
	}

	public static Color SetAlpha(Color color, float value)
	{
		return new Color(color.r, color.g, color.b, value);
	}
}
