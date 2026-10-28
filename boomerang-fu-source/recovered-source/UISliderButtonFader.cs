using UnityEngine;

public class UISliderButtonFader : UISliderButton
{
	public RectTransform handle;

	public float minHandleXPos;

	public float maxHandleXPos;

	private float targetHandleXPos;

	private static float handleSpeed = 20f;

	protected override void OnEnable()
	{
		base.OnEnable();
		targetHandleXPos = Mathf.Lerp(minHandleXPos, maxHandleXPos, (float)value / (float)maxValue);
		handle.anchoredPosition = new Vector2(targetHandleXPos, handle.anchoredPosition.y);
	}

	protected override void UpdateVisuals()
	{
		base.UpdateVisuals();
		targetHandleXPos = Mathf.Lerp(minHandleXPos, maxHandleXPos, (float)value / (float)maxValue);
	}

	private void Update()
	{
		if (handle != null)
		{
			handle.anchoredPosition = new Vector2(Mathf.Lerp(handle.anchoredPosition.x, targetHandleXPos, Time.unscaledDeltaTime * handleSpeed), handle.anchoredPosition.y);
		}
	}
}
