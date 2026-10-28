using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIHelperText : UIHelper
{
	private TextMeshProUGUI helperText;

	public LocalizedString[] strings;

	protected override void Awake()
	{
		base.Awake();
		helperText = helperTarget.GetComponent<TextMeshProUGUI>();
	}

	public void SetValue(int value)
	{
		if (strings.Length <= 1 || helperTarget == null || helperText == null)
		{
			return;
		}
		UISliderButton uISliderButton = selectable as UISliderButton;
		if (!(uISliderButton != null) || uISliderButton.value < 0 || uISliderButton.value >= strings.Length)
		{
			return;
		}
		helperText.text = strings[uISliderButton.value];
		if (string.IsNullOrEmpty(strings[uISliderButton.value]))
		{
			helperTarget.SetActive(value: false);
			return;
		}
		if (isSelected && !helperTarget.activeSelf)
		{
			helperTarget.SetActive(value: true);
		}
		helperText.text = strings[uISliderButton.value];
	}
}
