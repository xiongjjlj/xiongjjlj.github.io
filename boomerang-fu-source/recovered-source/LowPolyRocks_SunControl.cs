using UnityEngine;

public class LowPolyRocks_SunControl : MonoBehaviour
{
	[Range(-10f, 10f)]
	public float sunRotationSpeed_x;

	[Range(-10f, 10f)]
	public float sunRotationSpeed_y;

	private void Update()
	{
		base.gameObject.transform.Rotate(sunRotationSpeed_x * Time.deltaTime, sunRotationSpeed_y * Time.deltaTime, 0f);
	}
}
