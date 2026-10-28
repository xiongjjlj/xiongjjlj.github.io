using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class UIAutoScroller : MonoBehaviour
{
	[SerializeField]
	private float scrollSpeed = 5f;

	[SerializeField]
	private float scrollPaddingTop = 200f;

	[SerializeField]
	private float scrollPaddingBottom = 100f;

	protected ScrollRect scrollRect;

	protected RectTransform scrollRectRT;

	public List<Selectable> selectables;

	[SerializeField]
	protected RectTransform selectedRT;

	public CanvasGroup softEdgeBottom;

	private bool wasJustEnabled;

	protected virtual void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		if (scrollRect != null)
		{
			scrollRect.content.GetComponentsInChildren(selectables);
		}
		scrollRectRT = scrollRect.GetComponent<RectTransform>();
	}

	protected virtual void Update()
	{
		if (selectables != null && selectables.Count != 0)
		{
			if (wasJustEnabled)
			{
				scrollRect.verticalNormalizedPosition = 1f;
				wasJustEnabled = false;
			}
			ScrollToSelected();
		}
	}

	private void ScrollToSelected()
	{
		if (selectedRT == null)
		{
			return;
		}
		float verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
		Vector3 vector = scrollRect.content.InverseTransformPoint(selectedRT.position);
		Vector3 vector2 = scrollRectRT.localPosition - vector;
		float num = scrollRect.content.rect.height - scrollRectRT.rect.height;
		float num2 = scrollRect.content.rect.height - vector2.y;
		float num3 = scrollRect.verticalNormalizedPosition * num;
		float num4 = num3 - selectedRT.rect.height / 2f + scrollRectRT.rect.height - scrollPaddingTop;
		float num5 = num3 + selectedRT.rect.height / 2f + scrollPaddingBottom;
		if (num2 > num4)
		{
			float num6 = num2 - num4;
			float b = (num3 + num6) / num;
			scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, b, scrollSpeed * Time.unscaledDeltaTime);
		}
		else if (num2 < num5)
		{
			float num7 = num2 - num5;
			float b2 = (num3 + num7) / num;
			scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, b2, scrollSpeed * Time.unscaledDeltaTime);
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

	private void OnEnable()
	{
		UIManager.OnSelectionChanged += HandleOnSelectionChanged;
		wasJustEnabled = true;
	}

	private void OnDisable()
	{
		UIManager.OnSelectionChanged -= HandleOnSelectionChanged;
	}

	private void HandleOnSelectionChanged(GameObject selectedGameObject)
	{
		if (selectedGameObject != null)
		{
			Selectable component = selectedGameObject.GetComponent<Selectable>();
			if (component != null && selectables.Contains(component))
			{
				selectedRT = selectedGameObject.GetComponent<RectTransform>();
			}
			else
			{
				selectedRT = null;
			}
		}
		else
		{
			selectedRT = null;
		}
	}
}
