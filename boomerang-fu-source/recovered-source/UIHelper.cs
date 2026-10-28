using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIHelper : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
{
	protected Selectable selectable;

	protected bool isSelected;

	public GameObject helperTarget;

	protected virtual void Awake()
	{
		selectable = GetComponent<Selectable>();
		helperTarget.SetActive(value: false);
	}

	public virtual void OnSelect(BaseEventData eventData)
	{
		if (!(helperTarget == null))
		{
			helperTarget.SetActive(value: true);
			isSelected = true;
		}
	}

	public virtual void OnDeselect(BaseEventData eventData)
	{
		if (!(helperTarget == null))
		{
			helperTarget.SetActive(value: false);
			isSelected = false;
		}
	}
}
