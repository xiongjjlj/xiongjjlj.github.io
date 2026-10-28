using UnityEngine;

public class IgnoreParentRotation : MonoBehaviour
{
	private void Update()
	{
		base.transform.rotation = Quaternion.identity;
	}
}
