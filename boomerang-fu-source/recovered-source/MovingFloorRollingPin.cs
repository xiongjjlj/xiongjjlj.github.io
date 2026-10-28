using UnityEngine;

public class MovingFloorRollingPin : MonoBehaviour
{
	public Transform roller;

	public float spinFactor;

	public MovingFloor pairedMovingFloor;

	private void Update()
	{
		if (!(pairedMovingFloor == null))
		{
			roller.Rotate(Vector3.forward, pairedMovingFloor.velocity.magnitude * spinFactor * Time.deltaTime * Vector2.Dot(base.transform.right.normalized, pairedMovingFloor.velocity.normalized));
		}
	}
}
