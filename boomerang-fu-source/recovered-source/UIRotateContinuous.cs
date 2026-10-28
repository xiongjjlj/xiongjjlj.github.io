using UnityEngine;

public class UIRotateContinuous : MonoBehaviour
{
	private Quaternion initRotation;

	private Transform tf;

	private CanvasGroup canvasGroup;

	private bool hasCanvasGroup;

	public float clockwiseSpeed;

	private void Start()
	{
		tf = base.transform;
		initRotation = tf.rotation;
		canvasGroup = GetComponentInParent<CanvasGroup>();
		if ((bool)canvasGroup)
		{
			hasCanvasGroup = true;
		}
	}

	private void Update()
	{
		if (!hasCanvasGroup || canvasGroup.alpha > 0f)
		{
			tf.Rotate(new Vector3(0f, 0f, clockwiseSpeed * Time.unscaledDeltaTime));
		}
	}
}
