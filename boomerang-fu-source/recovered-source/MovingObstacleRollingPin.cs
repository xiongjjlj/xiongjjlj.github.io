using UnityEngine;

public class MovingObstacleRollingPin : MovingObstacle
{
	public Transform roller;

	public float spinFactor;

	protected override void Update()
	{
		base.Update();
		roller.Rotate(Vector3.forward, base.angularVelocity * spinFactor * Time.deltaTime);
	}
}
