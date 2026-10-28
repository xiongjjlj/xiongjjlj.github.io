using DG.Tweening;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class UICreditsScroller : MonoBehaviour
{
	protected ScrollRect scrollRect;

	protected RectTransform scrollRectRT;

	public CanvasGroup softEdgeBottom;

	[SerializeField]
	private float scrollTargetPosition;

	private float inputScrollDistance = 40f;

	private float scrollSpeedAuto = 0.05f;

	private float scrollSpeedManual = 5f;

	private float timeSinceLastPlayerInput;

	private static float waitBeforeAutoScrolling = 2f;

	private bool wasJustEnabled;

	protected virtual void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		if (scrollRect != null)
		{
			scrollRectRT = scrollRect.GetComponent<RectTransform>();
		}
	}

	private void OnEnable()
	{
		wasJustEnabled = true;
	}

	private void Update()
	{
		if (wasJustEnabled)
		{
			scrollRect.verticalNormalizedPosition = 1f;
			scrollTargetPosition = 1f;
			timeSinceLastPlayerInput = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled - 1f;
			wasJustEnabled = false;
			return;
		}
		float verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
		Rewired.Player currentMasterPlayer = Singleton<UIManager>.Instance.GetCurrentMasterPlayer();
		if (currentMasterPlayer.GetAxis("UIVertical") < 0f)
		{
			scrollTargetPosition -= inputScrollDistance / scrollRect.content.rect.height;
			timeSinceLastPlayerInput = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
		else if (currentMasterPlayer.GetAxis("UIVertical") > 0f)
		{
			scrollTargetPosition += inputScrollDistance / scrollRect.content.rect.height;
			timeSinceLastPlayerInput = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
		if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(timeSinceLastPlayerInput) >= waitBeforeAutoScrolling)
		{
			if (scrollRect.verticalNormalizedPosition > 0.01f)
			{
				scrollRect.verticalNormalizedPosition -= scrollSpeedAuto * Time.unscaledDeltaTime;
				scrollTargetPosition = scrollRect.verticalNormalizedPosition;
			}
			else
			{
				scrollRect.verticalNormalizedPosition = 0f;
			}
		}
		else
		{
			scrollTargetPosition = Mathf.Clamp01(scrollTargetPosition);
			scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, scrollTargetPosition, scrollSpeedManual * Time.unscaledDeltaTime);
		}
		if (!(softEdgeBottom != null))
		{
			return;
		}
		if (SoftEdgesShouldBeVisible())
		{
			if (scrollRect.verticalNormalizedPosition > 0.01f && softEdgeBottom.alpha == 0f)
			{
				softEdgeBottom.DOComplete();
				softEdgeBottom.DOFade(1f, 0.3f);
			}
			else if (scrollRect.verticalNormalizedPosition <= 0.01f && verticalNormalizedPosition > 0.01f)
			{
				softEdgeBottom.DOComplete();
				softEdgeBottom.DOFade(0f, 0.2f);
			}
		}
		else
		{
			softEdgeBottom.alpha = 0f;
		}
	}

	private bool SoftEdgesShouldBeVisible()
	{
		return scrollRect.content.rect.height > scrollRect.viewport.rect.height;
	}
}
