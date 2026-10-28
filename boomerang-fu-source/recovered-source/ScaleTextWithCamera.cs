using UnityEngine;

public class ScaleTextWithCamera : MonoBehaviour
{
	public void Scale()
	{
		Vector3 localScale = GetComponent<RectTransform>().localScale;
		GetComponent<RectTransform>().localScale = localScale * (0.66f + 1.25f * (Singleton<Camera3D>.Instance.currentZoom / Singleton<Camera3D>.Instance.maximumZoom));
	}
}
