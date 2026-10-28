using UnityEngine;

public class RotatingObstacleController : MonoBehaviour
{
	[Header("Rotation Change Behaviour")]
	public bool sinWaveSpeedChange;

	public float rotDurationDeltaMagnitude = 2f;

	public float speedChangeRate = 1f;

	private MovingObstacle[] rotObstacles;

	private float[] startingRotationDuration;

	private void Start()
	{
		rotObstacles = GetComponentsInChildren<MovingObstacle>();
		if (rotObstacles != null && rotObstacles.Length != 0)
		{
			startingRotationDuration = new float[rotObstacles.Length];
			for (int i = 0; i < rotObstacles.Length; i++)
			{
				MovingObstacle movingObstacle = rotObstacles[i];
				startingRotationDuration[i] = movingObstacle.rotatingDur;
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < rotObstacles.Length; i++)
		{
			rotObstacles[i].rotatingDur = startingRotationDuration[i] + rotDurationDeltaMagnitude * Mathf.Sin(speedChangeRate * Time.time);
		}
	}
}
