using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UILoadingOverlay : MonoBehaviour
{
	public Canvas canvas;

	public RectTransform canvasRT;

	public RectTransform circleRT;

	public AnimationCurve progressCurve;

	public float wipeInDuration;

	public GameObject spinner;

	private RectTransform textRT;

	public TextMeshProUGUI text;

	private void Start()
	{
		Reset();
		if (text != null)
		{
			textRT = text.GetComponent<RectTransform>();
		}
	}

	public void Reset()
	{
		canvas.enabled = false;
		spinner.SetActive(value: false);
	}

	public IEnumerator TransitionIn(UIButton button = null, bool textTransition = false)
	{
		if (text != null)
		{
			text.gameObject.SetActive(value: false);
		}
		spinner.SetActive(value: false);
		canvas.enabled = true;
		float size = canvasRT.rect.width * 1.75f;
		Vector2 targetAnchoredPosition = canvasRT.rect.max * 0.5f;
		if (button != null)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, RectTransformUtility.WorldToScreenPoint(Singleton<Camera3D>.Instance.uiCamera, button.transform.position), Singleton<Camera3D>.Instance.uiCamera, out targetAnchoredPosition);
			circleRT.anchoredPosition = targetAnchoredPosition;
			if (textTransition && text != null && textRT != null)
			{
				text.text = button.GetComponent<UIButtonStyle>().text.text;
				text.gameObject.SetActive(value: true);
			}
		}
		circleRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		circleRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		circleRT.localScale = Vector3.zero;
		if (textTransition && textRT != null)
		{
			textRT.localScale = Vector3.one;
			textRT.DOScale(3f, wipeInDuration).SetEase(progressCurve);
			textRT.anchoredPosition = targetAnchoredPosition;
			textRT.DOLocalMoveY((canvasRT.rect.max * 0.5f).y, wipeInDuration).SetEase(progressCurve);
			text.color = Color.white;
			text.DOFade(0f, wipeInDuration * 2f).SetEase(Ease.OutExpo);
		}
		Tween t = circleRT.DOScale(1f, wipeInDuration).SetEase(progressCurve);
		yield return t.WaitForCompletion();
		spinner.SetActive(value: true);
		spinner.GetComponent<RectTransform>().anchoredPosition = targetAnchoredPosition;
	}

	public void Close()
	{
		Reset();
	}
}
