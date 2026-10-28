using UnityEngine;

public class UIRotateSeeSaw : MonoBehaviour
{
	private Quaternion initRotation;

	private Transform tf;

	private CanvasGroup canvasGroup;

	private bool hasCanvasGroup;

	public float duration;

	public float angleArc;

	public AnimationCurve curve;

	private float timer = 0.5f;

	private bool forwards = true;

	private void Start()
	{
		tf = base.transform;
		initRotation = tf.rotation;
		canvasGroup = GetComponentInParent<CanvasGroup>();
		if ((bool)canvasGroup)
		{
			hasCanvasGroup = true;
		}
		forwards = true;
		timer = 0.5f;
	}

	private void Update()
	{
		if (!hasCanvasGroup || canvasGroup.alpha > 0f)
		{
			if (forwards)
			{
				timer += Time.unscaledDeltaTime;
			}
			else
			{
				timer -= Time.unscaledDeltaTime;
			}
			if (timer < 0f)
			{
				forwards = !forwards;
				timer = 0f;
			}
			else if (timer > duration)
			{
				forwards = !forwards;
				timer = duration;
			}
			tf.rotation = Quaternion.Euler(0f, 0f, curve.Evaluate(timer / duration) * angleArc - 0.5f * angleArc);
		}
	}
}
