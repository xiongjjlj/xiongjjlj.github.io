using DG.Tweening;
using UnityEngine;

public class UIButtonStyleBinding : UIButtonStyle
{
	private UIButtonBinding buttonBinding;

	public override void Awake()
	{
		base.Awake();
		buttonBinding = GetComponent<UIButtonBinding>();
	}

	public override void UpdateButtonColors()
	{
		base.UpdateButtonColors();
		if (selectable == null || !Application.isPlaying || !(buttonBinding != null))
		{
			return;
		}
		if (isSelected && buttonBinding.bindingState == UIButtonBinding.BindingState.Listening)
		{
			selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkSelectedTextColor;
		}
		if (!isSelected && buttonBinding.bindingState == UIButtonBinding.BindingState.Missing)
		{
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonNormalSelectedColor;
			}
			selectable.image.color = new Color(1f, 1f, 1f, 0f);
		}
	}

	public override void Bounce()
	{
		base.transform.DOComplete();
		onSubmitTween.Goto(0f, andPlay: true);
		if (!(selectable.image == null))
		{
			Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
		}
	}
}
