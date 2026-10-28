using UnityEngine;

public class LowPolyRocks_CameraControl : MonoBehaviour
{
	[Range(-10f, 10f)]
	public float cameraMoveSpeed_x;

	[Range(-10f, 10f)]
	public float cameraMoveSpeed_y;

	[Range(-10f, 10f)]
	public float cameraMoveSpeed_z;

	private void Update()
	{
		base.gameObject.transform.Translate(cameraMoveSpeed_x * Time.deltaTime, cameraMoveSpeed_y * Time.deltaTime, cameraMoveSpeed_z * Time.deltaTime);
	}
}
