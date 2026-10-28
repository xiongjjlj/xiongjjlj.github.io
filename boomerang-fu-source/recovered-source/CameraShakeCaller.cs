using UnityEngine;

public class CameraShakeCaller : MonoBehaviour
{
	public float ProjectileShakeDuration = 0.08f;

	public float ProjectileShakeAmount = 0.1f;

	private void Start()
	{
		CameraShake.shakeDuration = ProjectileShakeDuration;
		CameraShake.shakeAmount = ProjectileShakeAmount;
	}

	private void Update()
	{
	}
}
