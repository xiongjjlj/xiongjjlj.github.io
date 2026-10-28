using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeInOnEnable : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	public float fadeInDuration;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		if (canvasGroup != null)
		{
			canvasGroup.DOComplete();
			canvasGroup.alpha = 0f;
			canvasGroup.DOFade(1f, fadeInDuration);
		}
	}
}
