using System;
using UnityEngine;

public static class VectorExt
{
	public static Vector2 VectorFromAngle(float input)
	{
		input *= MathF.PI / 180f;
		return new Vector2(Mathf.Cos(input), Mathf.Sin(input));
	}

	public static float AngleFromVector(Vector2 input)
	{
		return Mathf.Atan2(input.x, input.y) * 57.29578f;
	}

	public static Quaternion RotationZFromVectorDirection(Vector2 direction)
	{
		return Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * 57.29578f - 90f, Vector3.forward);
	}

	public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
	{
		Vector3 vector = point - pivot;
		vector = Quaternion.Euler(angles) * vector;
		point = vector + pivot;
		return point;
	}

	public static Vector3 FlipYAndZ(Vector3 input)
	{
		return new Vector3(input.x, input.z, input.y);
	}

	public static Vector2 AddForce(Vector2 amount, ForceMode2D forceMode = ForceMode2D.Force, float mass = 1f)
	{
		if (forceMode == ForceMode2D.Impulse)
		{
			return amount * (1f / mass);
		}
		return amount * (1f / mass) * Time.fixedDeltaTime;
	}

	public static Vector2 ClosestPointOnBoxColliderEdge(Vector2 position, BoxCollider2D boxCollider)
	{
		Bounds bounds = new Bounds
		{
			size = new Vector2(boxCollider.size.x * boxCollider.transform.localScale.x, boxCollider.size.y * boxCollider.transform.localScale.y),
			center = (Vector2)boxCollider.transform.position
		};
		Vector2 vector = RotatePointAroundPivot(position, bounds.center, new Vector3(0f, 0f, 0f - boxCollider.transform.eulerAngles.z));
		Vector2 zero = Vector2.zero;
		if (bounds.Contains(vector))
		{
			float num = Mathf.Abs(vector.x - bounds.min.x);
			zero = new Vector2(bounds.min.x, vector.y);
			if (Mathf.Abs(bounds.max.x - vector.x) < num)
			{
				num = Mathf.Abs(bounds.max.x - vector.x);
				zero = new Vector2(bounds.max.x, vector.y);
			}
			if (Mathf.Abs(bounds.max.y - vector.y) < num)
			{
				num = Mathf.Abs(bounds.max.y - vector.y);
				zero = new Vector2(vector.x, bounds.max.y);
			}
			if (Mathf.Abs(bounds.min.y - vector.y) < num)
			{
				num = Mathf.Abs(bounds.min.y - vector.y);
				zero = new Vector2(vector.x, bounds.min.y);
			}
		}
		else
		{
			zero = bounds.ClosestPoint(vector);
		}
		return RotatePointAroundPivot(zero, bounds.center, new Vector3(0f, 0f, boxCollider.transform.eulerAngles.z));
	}

	public static Vector2 GetPointOnCircle(float angle, float radius)
	{
		angle *= MathF.PI / 180f;
		return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
	}
}
