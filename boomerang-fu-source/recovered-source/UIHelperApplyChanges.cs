using UnityEngine.EventSystems;

public class UIHelperApplyChanges : UIHelper, ISubmitHandler, IEventSystemHandler
{
	private UISliderButton sliderButton;

	private int initialValue;

	protected override void Awake()
	{
		base.Awake();
		sliderButton = GetComponent<UISliderButton>();
	}

	public override void OnSelect(BaseEventData eventData)
	{
		if (!(helperTarget == null))
		{
			if (sliderButton != null)
			{
				initialValue = sliderButton.value;
				helperTarget.SetActive(value: false);
				sliderButton.OnValueChanged.AddListener(OnValueChanged);
			}
			else
			{
				helperTarget.SetActive(value: true);
			}
			isSelected = true;
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		if (sliderButton != null)
		{
			sliderButton.OnValueChanged.RemoveListener(OnValueChanged);
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		if (sliderButton != null)
		{
			initialValue = sliderButton.value;
			helperTarget.SetActive(value: false);
		}
	}

	private void OnValueChanged(int newValue)
	{
		if (newValue == initialValue)
		{
			helperTarget.SetActive(value: false);
		}
		else
		{
			helperTarget.SetActive(value: true);
		}
	}
}
