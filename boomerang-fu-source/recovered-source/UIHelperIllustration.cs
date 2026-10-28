using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIHelperIllustration : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
{
	private Selectable selectable;

	private bool isSelected;

	public Image helperTarget;

	private void Awake()
	{
		selectable = GetComponent<Selectable>();
		helperTarget.color = Singleton<ColorManager>.Instance.buttonNormalColor;
	}

	public void OnSelect(BaseEventData eventData)
	{
		if (!(helperTarget == null))
		{
			helperTarget.color = Singleton<ColorManager>.Instance.buttonNormalSelectedColor;
			isSelected = true;
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		if (!(helperTarget == null))
		{
			helperTarget.color = Singleton<ColorManager>.Instance.buttonNormalColor;
			isSelected = false;
		}
	}
}
