using UnityEngine;

public class DynamicRope : MonoBehaviour
{
	public Transform endTransform;

	public float defaultLength = 12f;

	private void Start()
	{
		UpdateConnection();
	}

	private void Update()
	{
	}

	public void UpdateConnection()
	{
		base.transform.rotation = Quaternion.LookRotation(endTransform.position - base.transform.position, Vector3.forward);
		base.transform.localScale = new Vector3(1f, 1f, Vector3.Magnitude(endTransform.position - base.transform.position) / defaultLength);
	}
}
