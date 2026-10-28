using UnityEngine.UI;

public class UIButtonStyleSteam : UIButtonStyle
{
	public Image logo;

	public override void UpdateButtonColors()
	{
		base.UpdateButtonColors();
		_ = isSelected;
		logo.color = text.color;
	}
}
