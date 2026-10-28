using UnityEngine;

public class CameraShake : MonoBehaviour
{
	public Transform camTransform;

	public static float shakeDuration = 0f;

	public static float shakeAmount = 0.7f;

	public float decreaseFactor = 1f;

	private Vector3 originalPos;

	private void Awake()
	{
		if (camTransform == null)
		{
			camTransform = GetComponent(typeof(Transform)) as Transform;
		}
	}

	private void OnEnable()
	{
		originalPos = camTransform.localPosition;
	}

	private void Update()
	{
		if (shakeDuration > 0f)
		{
			camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
			shakeDuration -= Time.deltaTime * decreaseFactor;
		}
		if (shakeAmount > 0f)
		{
			shakeAmount -= Time.deltaTime * decreaseFactor;
			return;
		}
		shakeAmount = 0f;
		shakeDuration = 0f;
		camTransform.localPosition = originalPos;
	}
}
