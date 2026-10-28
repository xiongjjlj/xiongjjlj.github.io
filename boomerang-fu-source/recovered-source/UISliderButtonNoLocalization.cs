using UnityEngine;

public class UISliderButtonNoLocalization : UISliderButton
{
	public string[] rawStringMap;

	protected override void UpdateVisuals()
	{
		if (text != null)
		{
			if (value > rawStringMap.Length - 1 || value < 0)
			{
				Debug.LogError("Slider toggle is missing rawstring for value " + value);
			}
			else
			{
				text.text = rawStringMap[value];
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
}
