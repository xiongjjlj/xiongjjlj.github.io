using UnityEngine;

public class Billboard : MonoBehaviour
{
	private void Update()
	{
		base.transform.LookAt(Singleton<Camera3D>.Instance.transform.position, Vector3.up);
	}
}
