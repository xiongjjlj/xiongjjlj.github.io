using UnityEngine;

public class LowPolyRocks_CloudsControl : MonoBehaviour
{
	[Range(-10f, 10f)]
	public float cloudsMoveSpeed_x;

	[Range(-10f, 10f)]
	public float cloudsMoveSpeed_z;

	private void Update()
	{
		base.gameObject.transform.Translate(cloudsMoveSpeed_x * Time.deltaTime, 0f, cloudsMoveSpeed_z * Time.deltaTime);
	}
}
