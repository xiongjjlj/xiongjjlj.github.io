using DG.Tweening;
using UnityEngine;

public class UIPulse : MonoBehaviour
{
	public float bounceScaleAmt = 1.1f;

	public float bounceDur = 0.4f;

	public RectTransform rt;

	private Sequence bounceTween;

	public void OnEnable()
	{
		rt = GetComponent<RectTransform>();
		StopPulsing();
		bounceTween = DOTween.Sequence();
		bounceTween.Append(rt.DOScale(bounceScaleAmt, bounceDur).SetEase(Ease.InOutSine)).Append(rt.DOScale(1f, bounceDur).SetEase(Ease.InOutSine)).SetLoops(-1);
	}

	private void OnDisable()
	{
		StopPulsing();
	}

	public void StopPulsing()
	{
		if (bounceTween != null)
		{
			bounceTween.Kill();
		}
		rt.localScale = Vector3.one;
	}
}
