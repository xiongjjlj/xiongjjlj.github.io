using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonReplayStyle : UIButtonStyle
{
	public RectTransform wrapper;

	public Image outlineImage;

	public Image floppyDisk;

	public Image upArrow;

	public override void Awake()
	{
		base.Awake();
		outlineImage.enabled = false;
		floppyDisk.enabled = false;
		upArrow.enabled = false;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	public override void UpdateButtonColors()
	{
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		outlineImage.enabled = true;
		floppyDisk.enabled = true;
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		outlineImage.enabled = false;
		floppyDisk.enabled = false;
		upArrow.enabled = false;
	}
}
