using System;
using UnityEngine;

public static class Easing
{
	public static float SmoothInOut(float t)
	{
		return t * t * (3f - 2f * t);
	}

	public static float SmoothOutIn(float t)
	{
		return t + (t - t * t * (3f - 2f * t));
	}

	public static float SubtleOutIn(float t)
	{
		return (t * 7.15f + SmoothInOut(t) * 2.85f) * 0.1f;
	}

	public static float EaseIn(float t)
	{
		return 1f - Mathf.Cos(MathF.PI / 2f * t);
	}

	public static float EaseOut(float t)
	{
		return Mathf.Sin(MathF.PI / 2f * t);
	}

	public static float EaseInOut(float t)
	{
		return 0.5f * (1f - Mathf.Cos(MathF.PI * t));
	}

	public static float SineInOut(float t)
	{
		return (0f - Mathf.Cos(MathF.PI * t)) / 2f + 0.5f;
	}

	public static float CubicIn(float t)
	{
		return t * t * t;
	}

	public static float CubicOut(float t)
	{
		return 1f + --t * t * t;
	}

	public static float CubicInOut(float t)
	{
		if ((t *= 2f) < 1f)
		{
			return 0.5f * t * t * t;
		}
		return 0.5f * ((t -= 2f) * t * t + 2f);
	}

	public static float QuartIn(float t)
	{
		return t * t * t * t;
	}

	public static float QuartOut(float t)
	{
		return 1f - --t * t * t * t;
	}

	public static float QuartInOut(float t)
	{
		if ((t *= 2f) < 1f)
		{
			return 0.5f * t * t * t;
		}
		return 0.5f * ((t -= 2f) * t * t + 2f);
	}

	public static float ExpoIn(float t)
	{
		if (t != 0f)
		{
			return Mathf.Pow(1024f, t - 1f);
		}
		return 0f;
	}

	public static float ExpoOut(float t)
	{
		if (t != 1f)
		{
			return 1f - Mathf.Pow(2f, -10f * t);
		}
		return 1f;
	}

	public static float ExpoInOut(float t)
	{
		if (t == 0f)
		{
			return 0f;
		}
		if (t == 1f)
		{
			return 1f;
		}
		if ((t *= 2f) < 1f)
		{
			return 0.5f * Mathf.Pow(1024f, t - 1f);
		}
		return 0.5f * (0f - Mathf.Pow(2f, -10f * (t - 1f)) + 2f);
	}

	public static float CircIn(float t)
	{
		return 1f - Mathf.Sqrt(1f - t * t);
	}

	public static float CircOut(float t)
	{
		return Mathf.Sqrt(1f - --t * t);
	}

	public static float CircInOut(float t)
	{
		if ((t *= 2f) < 1f)
		{
			return -0.5f * (Mathf.Sqrt(1f - t * t) - 1f);
		}
		return 0.5f * (Mathf.Sqrt(1f - (t -= 2f) * t) + 1f);
	}

	public static float ElasticIn(float t)
	{
		if (t == 0f)
		{
			return 0f;
		}
		if (t == 1f)
		{
			return 1f;
		}
		return (0f - Mathf.Pow(2f, 10f * --t)) * Mathf.Sin((t - 0.1f) * (MathF.PI * 2f) / 0.4f);
	}

	public static float ElasticOut(float t)
	{
		if (t == 0f)
		{
			return 0f;
		}
		if (t == 1f)
		{
			return 1f;
		}
		return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.075f) * (MathF.PI * 2f) / 0.3f) + 1f;
	}

	public static float ElasticInOut(float t)
	{
		if ((t *= 2f) < 1f)
		{
			return -0.5f * Mathf.Pow(2f, 10f * --t) * Mathf.Sin((t - 0.1f) * (MathF.PI * 2f) / 0.4f);
		}
		return Mathf.Pow(2f, -10f * --t) * Mathf.Sin((t - 0.1f) * (MathF.PI * 2f) / 0.4f) * 0.5f + 1f;
	}
}
