using System.Collections;
using UnityEngine;

public class VFXBeam : MonoBehaviour
{
	private LineRenderer lr;

	private float duration = 0.2f;

	private void Awake()
	{
		lr = base.gameObject.GetComponent<LineRenderer>();
	}

	public IEnumerator ShootBeam(Vector3 startPos, Vector3 endPos, Color color)
	{
		float num = 2f;
		lr.positionCount = 2;
		lr.startWidth = num;
		lr.endWidth = num;
		lr.SetPosition(0, startPos);
		lr.SetPosition(1, endPos);
		lr.startColor = color - new Color(0f, 0f, 0f, 1f);
		lr.endColor = color;
		float timer = 0f;
		while (timer < duration)
		{
			timer += Time.unscaledDeltaTime;
			num = Mathf.Lerp(2f, 0.25f, Easing.EaseOut(timer / duration));
			lr.startWidth = num;
			lr.endWidth = num;
			yield return null;
		}
		base.gameObject.Recycle();
	}
}
