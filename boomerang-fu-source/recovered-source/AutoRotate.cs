using UnityEngine;

public class AutoRotate : MonoBehaviour
{
	private Vector3 angle;

	public float speed = 60f;

	public bool rotateX;

	public bool rotateY = true;

	public bool rotateZ;

	private void Start()
	{
		angle = base.transform.localEulerAngles;
	}

	private void Update()
	{
		if (rotateX)
		{
			angle.x += Time.deltaTime * speed;
		}
		if (rotateY)
		{
			angle.y += Time.deltaTime * speed;
		}
		if (rotateZ)
		{
			angle.z += Time.deltaTime * speed;
		}
		base.transform.localEulerAngles = angle;
	}
}
