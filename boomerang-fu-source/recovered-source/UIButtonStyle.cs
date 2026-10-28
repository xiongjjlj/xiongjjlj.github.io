using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonStyle : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler, ISubmitHandler
{
	public enum Style
	{
		Light,
		Dark,
		Popup,
		LightBoolean,
		DarkBordered
	}

	protected Selectable selectable;

	public TextMeshProUGUI text;

	private TextMeshProUGUI label;

	[SerializeField]
	public bool isSelected;

	private LayoutGroup layoutGroup;

	private int layoutGroupPadding;

	private Tweener onSelectTweenX;

	private Tweener onSelectTweenY;

	protected Tweener onSubmitTween;

	private Tweener shakeTween;

	private static float shakeCooldown = 0.5f;

	private float timeOfLastShake;

	public Style style;

	public bool cameraShakeOnSelect = true;

	public bool cameraShakeOnSubmit = true;

	public bool animateOnEnable;

	public bool playPFXOnSubmit;

	public string submitSFXOverride = string.Empty;

	public virtual void Awake()
	{
		selectable = GetComponent<Selectable>();
		text = selectable.GetComponentInChildren<TextMeshProUGUI>();
		layoutGroup = GetComponent<LayoutGroup>();
		if (layoutGroup != null)
		{
			layoutGroupPadding = layoutGroup.padding.left;
		}
		isSelected = false;
		onSelectTweenX = base.transform.DOScaleX(1.2f, 0.16f).From(isRelative: false).SetEase(Ease.OutBack)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
		onSelectTweenY = base.transform.DOScaleY(1.14f, 0.2f).From(isRelative: false).SetEase(Ease.OutSine)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
		base.transform.localScale = Vector3.one;
		onSubmitTween = base.transform.DOScale(0.66f, 0.2f).From(isRelative: false).SetEase(Ease.OutBack)
			.OnComplete(OnSubmitTweenComplete)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
		base.transform.localScale = Vector3.one;
		if (selectable is UISliderButton)
		{
			label = (selectable as UISliderButton).label;
		}
	}

	protected virtual void OnEnable()
	{
		if (isSelected)
		{
			OnSelect(null);
		}
		else
		{
			OnDeselect(null);
		}
		onSelectTweenX.Goto(0f);
		onSelectTweenY.Goto(0f);
		onSubmitTween.Goto(0f);
		base.transform.localScale = Vector3.one;
		if (animateOnEnable)
		{
			base.transform.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
		}
	}

	public virtual void OnSelect(BaseEventData eventData)
	{
		Singleton<UIManager>.Instance.VibrateMasterPlayer(0, 0.3f, 0.03f);
		isSelected = true;
		UpdateButtonColors();
		onSelectTweenX.Goto(0f, andPlay: true);
		onSelectTweenY.Goto(0f, andPlay: true);
		if (cameraShakeOnSelect)
		{
			Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 0.4f), 1f, 0.1f);
		}
		if (label != null)
		{
			label.color = Singleton<ColorManager>.Instance.buttonLabelSelectedTextColor;
		}
		if (Singleton<UIManager>.Instance.UserChangedSelection && eventData != null)
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/select");
		}
	}

	public virtual void OnDeselect(BaseEventData eventData)
	{
		isSelected = false;
		UpdateButtonColors();
		if (selectable != null)
		{
			selectable.DOComplete();
		}
		if (label != null)
		{
			label.color = Singleton<ColorManager>.Instance.buttonLabelTextColor;
		}
	}

	public virtual void UpdateButtonColors()
	{
		if (selectable == null || text == null || !Application.isPlaying || !Singleton<ColorManager>.InstanceExists)
		{
			return;
		}
		if (isSelected)
		{
			switch (style)
			{
			case Style.Light:
			case Style.LightBoolean:
				if (text != null)
				{
					text.color = Singleton<ColorManager>.Instance.buttonNormalSelectedTextColor;
				}
				selectable.image.color = Singleton<ColorManager>.Instance.buttonNormalSelectedColor;
				if (layoutGroup != null)
				{
					layoutGroup.padding.left = layoutGroupPadding;
					layoutGroup.padding.right = layoutGroupPadding;
				}
				break;
			case Style.Dark:
			case Style.DarkBordered:
				if (text != null)
				{
					text.color = Singleton<ColorManager>.Instance.buttonDarkSelectedTextColor;
				}
				selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkSelectedColor;
				break;
			case Style.Popup:
				if (text != null)
				{
					text.color = Singleton<ColorManager>.Instance.buttonPopupSelectedTextColor;
				}
				break;
			}
			return;
		}
		switch (style)
		{
		case Style.Light:
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonNormalSelectedTextColor;
			}
			selectable.image.color = Singleton<ColorManager>.Instance.buttonNormalColor;
			break;
		case Style.Dark:
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonDarkTextColor;
			}
			selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkNormalColor;
			break;
		case Style.DarkBordered:
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonDarkTextColor;
			}
			selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkBorderedNormalColor;
			break;
		case Style.Popup:
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonPopupNormalColor;
			}
			break;
		case Style.LightBoolean:
			if (selectable is UISliderButton && (selectable as UISliderButton).value >= 1)
			{
				if (text != null)
				{
					text.color = Singleton<ColorManager>.Instance.buttonNormalSelectedTextColor;
				}
				selectable.image.color = Singleton<ColorManager>.Instance.buttonNormalColor;
				if (layoutGroup != null)
				{
					layoutGroup.padding.left = layoutGroupPadding;
					layoutGroup.padding.right = layoutGroupPadding;
				}
			}
			else
			{
				if (text != null)
				{
					text.color = Singleton<ColorManager>.Instance.buttonPopupNormalColor;
				}
				selectable.image.color = Singleton<ColorManager>.Instance.buttonTransparentColor;
				if (layoutGroup != null)
				{
					layoutGroup.padding.left = 0;
					layoutGroup.padding.right = 0;
				}
			}
			break;
		}
	}

	public void OnValueChanged(bool forceBounce = false)
	{
		if (cameraShakeOnSubmit)
		{
			Singleton<UIManager>.Instance.CameraShake(new Vector2(0.4f, 0f), 5f, 0.15f);
		}
		if (forceBounce || (!(selectable is UISliderButtonFader) && !(selectable is UISliderButtonNoLocalization)))
		{
			Bounce();
		}
	}

	public void Shake()
	{
		if (!(Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(timeOfLastShake) < shakeCooldown))
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/shake");
			base.transform.DOComplete();
			base.transform.localScale = Vector3.one;
			base.transform.DOPunchPosition(new Vector3(-15f, 0f, 0f), 0.4f, 15);
			timeOfLastShake = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		if (!(selectable != null) || (selectable is UISliderButton && !(selectable as UISliderButton).allowOnSubmit))
		{
			return;
		}
		if (selectable.interactable)
		{
			if (cameraShakeOnSubmit)
			{
				Singleton<UIManager>.Instance.CameraShake(new Vector2(0.3f, 0.6f), 10f, 0.25f);
			}
			Bounce();
			if (playPFXOnSubmit)
			{
				Singleton<UIManager>.Instance.PlayButtonSubmitPFX(GetComponent<RectTransform>(), Singleton<ColorManager>.Instance.buttonSubmitColor);
			}
			if (selectable is UISliderButton)
			{
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/slider");
			}
			else if (!(selectable is UIButtonBinding))
			{
				if (string.IsNullOrEmpty(submitSFXOverride))
				{
					Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/submit");
				}
				else
				{
					Singleton<AudioManager>.Instance.PlayOneShot(submitSFXOverride);
				}
			}
		}
		else
		{
			Shake();
		}
	}

	public virtual void Bounce()
	{
		base.transform.DOComplete();
		onSubmitTween.Goto(0f, andPlay: true);
		if (!(selectable.image == null))
		{
			if (style == Style.Dark)
			{
				selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkSubmitColor;
			}
			else if (style == Style.DarkBordered)
			{
				selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkBorderedSubmitColor;
			}
			else
			{
				selectable.image.color = Singleton<ColorManager>.Instance.buttonSubmitColor;
			}
			Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
		}
	}

	private void OnSubmitTweenComplete()
	{
		UpdateButtonColors();
	}
}
