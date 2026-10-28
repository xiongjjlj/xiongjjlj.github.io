using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIScorePoint : MonoBehaviour
{
	public Color activeColor;

	public Color inactiveColor;

	public Image image;

	public Sprite pointSprite;

	public Sprite skullSprite;

	private float scaleInactive = 1f;

	private float scaleActive = 1.3f;

	private float scaleSkull = 1.5f;

	private bool isActive;

	public void SetActive(bool active, bool animate = true)
	{
		if (animate)
		{
			if (active)
			{
				base.transform.localScale = Vector3.one * scaleActive;
				base.transform.DOScale(Vector3.one * 0.5f, 0.3f).SetEase(Ease.OutBack).From(isRelative: false);
			}
			else
			{
				base.transform.localScale = Vector3.one * scaleSkull;
				base.transform.DOScale(Vector3.one * scaleSkull * 1.5f, 0.5f).SetEase(Ease.InExpo).From(isRelative: false);
			}
		}
		else if (active)
		{
			base.transform.localScale = Vector3.one * scaleActive;
		}
		else
		{
			base.transform.localScale = Vector3.one * scaleInactive;
		}
		SetColor(active, animate);
		isActive = animate;
	}

	private void SetColor(bool active, bool animate)
	{
		if (image != null)
		{
			if (animate && !active)
			{
				image.DOComplete();
				image.color = Color.Lerp(activeColor, Color.white, 1f);
				image.DOColor(inactiveColor, 0.66f).SetEase(Ease.InExpo);
			}
			else
			{
				image.color = (active ? activeColor : inactiveColor);
			}
		}
		else
		{
			Debug.Log("Image is null");
		}
	}

	public void ResetScaleAndIcon()
	{
		image.sprite = pointSprite;
		if (isActive)
		{
			base.transform.localScale = Vector3.one * scaleActive;
		}
		else
		{
			base.transform.localScale = Vector3.one * scaleInactive;
		}
	}
}
