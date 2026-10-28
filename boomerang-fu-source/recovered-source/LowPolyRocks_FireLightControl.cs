using UnityEngine;

public class LowPolyRocks_FireLightControl : MonoBehaviour
{
	private Light fireLight;

	[Range(0f, 8f)]
	public float minIntensity = 1.5f;

	[Range(0f, 8f)]
	public float maxIntensity = 2.5f;

	private float randomValue;

	private void Start()
	{
		fireLight = GetComponent<Light>();
		randomValue = Random.Range(0f, 65000f);
	}

	private void Update()
	{
		float t = Mathf.PerlinNoise(randomValue, Time.time);
		fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
	}
}
