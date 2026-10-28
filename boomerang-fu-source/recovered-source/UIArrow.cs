using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIArrow : MonoBehaviour
{
	public static Color colorDisabled = Color.black;

	public static Color colorEnabled = Color.white;

	private Image image;

	private Vector2 startPos;

	private void Start()
	{
		if (image == null)
		{
			image = GetComponentInChildren<Image>();
		}
		startPos = GetRect().localPosition;
	}

	public void SetEnabled(bool enabled)
	{
		if (image == null)
		{
			image = GetComponentInChildren<Image>();
			if (image == null)
			{
				return;
			}
		}
		if (enabled)
		{
			image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
		}
		else
		{
			image.color = new Color(image.color.r, image.color.g, image.color.b, 0.25f);
		}
	}

	public void SetVisible(bool visible)
	{
		base.gameObject.SetActive(visible);
	}

	public RectTransform GetRect()
	{
		if (image == null)
		{
			return null;
		}
		return image.rectTransform;
	}

	public void NudgeLeft()
	{
		GetRect().localPosition = startPos;
		GetRect().DOLocalMoveX(startPos.x - 20f, 0.2f).From(isRelative: false);
	}

	public void NudgeRight()
	{
		GetRect().localPosition = startPos;
		GetRect().DOLocalMoveX(startPos.x + 20f, 0.2f).From(isRelative: false);
	}
}
