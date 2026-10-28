using UnityEngine;

public class BillboardUI : MonoBehaviour
{
	public bool lookAtCamera;

	private void Update()
	{
		if (lookAtCamera)
		{
			base.transform.LookAt(Singleton<Camera3D>.Instance.transform.position, -Vector3.up);
		}
		else
		{
			base.transform.forward = Singleton<Camera3D>.Instance.transform.forward;
		}
	}
}
