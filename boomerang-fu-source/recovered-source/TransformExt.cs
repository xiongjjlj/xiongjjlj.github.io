using UnityEngine;

public static class TransformExt
{
	public static bool IsUniformScale(Transform tf)
	{
		if (tf.localScale.x == tf.localScale.y && tf.localScale.y == tf.localScale.z)
		{
			return true;
		}
		return false;
	}

	public static void DrawBox(Vector2 center, Vector2 size, Color color, float duration = 1f)
	{
		Vector2 vector = center - size * 0.5f;
		Vector2 vector2 = center + size * 0.5f;
		Debug.DrawLine(vector, new Vector3(vector.x, vector2.y), color, duration);
		Debug.DrawLine(new Vector3(vector.x, vector2.y), new Vector3(vector2.x, vector2.y), color, duration);
		Debug.DrawLine(new Vector3(vector2.x, vector2.y), new Vector3(vector2.x, vector.y), color, duration);
		Debug.DrawLine(new Vector3(vector2.x, vector.y), vector, color, duration);
	}
}
