using System;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISliderButton : Selectable, ICanvasElement, ISubmitHandler, IEventSystemHandler
{
	[Serializable]
	public class SlideButtonEvent : UnityEvent<int>
	{
	}

	public UIArrow leftArrow;

	public UIArrow rightArrow;

	public int minValue;

	public int maxValue = 1;

	public int value;

	[SerializeField]
	public SlideButtonEvent OnValueChanged = new SlideButtonEvent();

	public SlideButtonEvent OnSubmitEvent = new SlideButtonEvent();

	public UnityEvent OnDeselectEvent = new UnityEvent();

	public TextMeshProUGUI text;

	public LocalizedString[] stringMap;

	public TextMeshProUGUI label;

	public bool allowOnSubmit = true;

	public bool specialSubmitAction;

	protected ScrollRect scrollRect;

	protected UIHelperText helperText;

	protected UIButtonStyle buttonStyle;

	public bool playSliderSFX = true;

	protected override void Awake()
	{
		base.Awake();
		buttonStyle = GetComponent<UIButtonStyle>();
		scrollRect = GetComponentInParent<ScrollRect>();
	}

	protected override void Start()
	{
		base.Start();
		if (helperText == null)
		{
			helperText = GetComponent<UIHelperText>();
		}
		if (leftArrow == null)
		{
			leftArrow = base.transform.Find("LeftArrow").GetComponent<UIArrow>();
		}
		if (rightArrow == null)
		{
			rightArrow = base.transform.Find("RightArrow").GetComponent<UIArrow>();
		}
		UpdateVisuals();
	}

	public virtual void SetValue(int input, bool sendCallback = true)
	{
		int num = Mathf.Clamp(input, minValue, maxValue);
		if (value == num)
		{
			UpdateVisuals();
			return;
		}
		value = num;
		UpdateVisuals();
		if (sendCallback && OnValueChanged != null)
		{
			OnValueChanged.Invoke(num);
			if (buttonStyle != null)
			{
				buttonStyle.OnValueChanged();
			}
		}
	}

	protected void PlayValueChangedSound(int input)
	{
		if (!playSliderSFX)
		{
			return;
		}
		int num = Mathf.Clamp(input, minValue, maxValue);
		if (num == value)
		{
			return;
		}
		if (buttonStyle != null && buttonStyle.style == UIButtonStyle.Style.LightBoolean)
		{
			if (num == 0)
			{
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/off");
			}
			else
			{
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/on");
			}
		}
		else
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/slider");
		}
	}

	public void SetMaxValue(int max)
	{
		if (max != maxValue)
		{
			maxValue = max;
			UpdateVisuals();
		}
	}

	public void SetMinValue(int min)
	{
		if (min != minValue)
		{
			minValue = min;
			UpdateVisuals();
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetValue(value, sendCallback: false);
		ToggleArrowVisibility(base.currentSelectionState == SelectionState.Highlighted);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		if (scrollRect != null)
		{
			scrollRect.transform.InverseTransformPoint(base.transform.position);
		}
		ToggleArrowVisibility(visible: true);
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		ToggleArrowVisibility(visible: false);
		if (OnDeselectEvent != null)
		{
			OnDeselectEvent.Invoke();
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		if (allowOnSubmit)
		{
			PlayValueChangedSound(value);
			SetValue((value + 1) % (maxValue + 1));
		}
		if (OnSubmitEvent != null)
		{
			OnSubmitEvent.Invoke(value);
			if (specialSubmitAction && buttonStyle != null)
			{
				buttonStyle.OnValueChanged(forceBounce: true);
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/on");
			}
		}
	}

	protected virtual void UpdateVisuals()
	{
		if (text != null)
		{
			if (value > stringMap.Length - 1 || value < 0)
			{
				Debug.LogError("Slider toggle is missing string for value " + value);
			}
			if (string.IsNullOrEmpty(LocalizationManager.GetTranslation(stringMap[value].mTerm)))
			{
				text.text = stringMap[value].mTerm + " missing";
			}
			else
			{
				text.text = LocalizationManager.GetTranslation(stringMap[value].mTerm);
			}
		}
		if (leftArrow != null)
		{
			leftArrow.SetEnabled(value > minValue);
		}
		if (rightArrow != null)
		{
			rightArrow.SetEnabled(value < maxValue);
		}
		if (helperText != null)
		{
			helperText.SetValue(value);
		}
		if (buttonStyle != null)
		{
			buttonStyle.UpdateButtonColors();
		}
	}

	private void ToggleArrowVisibility(bool visible)
	{
		if (leftArrow != null)
		{
			leftArrow.SetVisible(visible);
		}
		if (rightArrow != null)
		{
			rightArrow.SetVisible(visible);
		}
	}

	public override void OnMove(AxisEventData eventData)
	{
		if (!IsActive() || !IsInteractable())
		{
			base.OnMove(eventData);
			return;
		}
		switch (eventData.moveDir)
		{
		case MoveDirection.Up:
			base.OnMove(eventData);
			break;
		case MoveDirection.Down:
			base.OnMove(eventData);
			break;
		case MoveDirection.Left:
			PlayValueChangedSound(value - 1);
			SetValue(value - 1);
			break;
		case MoveDirection.Right:
			PlayValueChangedSound(value + 1);
			SetValue(value + 1);
			break;
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		if (leftArrow != null && RectTransformUtility.RectangleContainsScreenPoint(leftArrow.GetRect(), eventData.position, eventData.enterEventCamera))
		{
			SetValue(value - 1);
		}
		if (rightArrow != null && RectTransformUtility.RectangleContainsScreenPoint(rightArrow.GetRect(), eventData.position, eventData.enterEventCamera))
		{
			SetValue(value + 1);
		}
	}

	public virtual void Rebuild(CanvasUpdate executing)
	{
	}

	public void LayoutComplete()
	{
	}

	public void GraphicUpdateComplete()
	{
	}

	public override Selectable FindSelectableOnUp()
	{
		if (base.navigation.mode == Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnUp;
		}
		if ((base.navigation.mode & Navigation.Mode.Vertical) != Navigation.Mode.None)
		{
			return FindSelectable(base.transform.rotation * Vector3.up);
		}
		return null;
	}

	public override Selectable FindSelectableOnDown()
	{
		if (base.navigation.mode == Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnDown;
		}
		if ((base.navigation.mode & Navigation.Mode.Vertical) != Navigation.Mode.None)
		{
			return FindSelectable(base.transform.rotation * Vector3.down);
		}
		return null;
	}

	private new Selectable FindSelectable(Vector3 dir)
	{
		_ = Quaternion.Inverse(base.transform.rotation) * dir;
		Vector3 vector = base.transform.TransformPoint((base.transform as RectTransform).rect.min);
		float num = float.NegativeInfinity;
		Selectable result = null;
		for (int i = 0; i < Selectable.allSelectablesArray.Length; i++)
		{
			Selectable selectable = Selectable.allSelectablesArray[i];
			if (selectable == this || !selectable.IsInteractable() || selectable.navigation.mode == Navigation.Mode.None)
			{
				continue;
			}
			RectTransform rectTransform = selectable.transform as RectTransform;
			Vector3 position = ((rectTransform != null) ? ((Vector3)rectTransform.rect.min) : Vector3.zero);
			Vector3 rhs = selectable.transform.TransformPoint(position) - vector;
			float num2 = Vector3.Dot(dir, rhs);
			if (!(num2 <= 0f))
			{
				float num3 = num2 / rhs.sqrMagnitude;
				if (num3 > num)
				{
					num = num3;
					result = selectable;
				}
			}
		}
		return result;
	}
}
