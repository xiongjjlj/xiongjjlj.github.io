using UnityEngine;

public class cameraAspectFix : MonoBehaviour
{
	private Camera thisCam;

	private Camera refCam;

	private float startAspect;

	private void Start()
	{
		refCam = Singleton<Camera3D>.Instance.gameCamera;
		thisCam = base.gameObject.GetComponent<Camera>();
		startAspect = refCam.aspect;
		UpdateAspectRatio();
	}

	private void Update()
	{
		if (refCam != null && startAspect != refCam.aspect)
		{
			UpdateAspectRatio();
		}
	}

	public void UpdateAspectRatio()
	{
		if (refCam == null)
		{
			refCam = Singleton<Camera3D>.Instance.gameCamera;
		}
		if (thisCam == null)
		{
			thisCam = base.gameObject.GetComponent<Camera>();
		}
		if (thisCam != null && refCam != null)
		{
			thisCam.aspect = refCam.aspect;
		}
	}
}
