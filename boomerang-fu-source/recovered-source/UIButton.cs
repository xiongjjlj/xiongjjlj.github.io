using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(UIButtonStyle))]
public class UIButton : Button
{
	public bool delayOnClick = true;

	protected static float submitDelay = 0.2f;

	protected override void Start()
	{
		base.Start();
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		if (delayOnClick)
		{
			StartCoroutine(DelayedClickEvent(eventData));
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		}
		else
		{
			base.OnSubmit(eventData);
		}
	}

	protected virtual IEnumerator DelayedClickEvent(BaseEventData eventData)
	{
		float t = 0f;
		while (t < submitDelay)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		base.OnSubmit(eventData);
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

	public override Selectable FindSelectableOnRight()
	{
		if (base.navigation.mode == Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnRight;
		}
		if ((base.navigation.mode & Navigation.Mode.Horizontal) != Navigation.Mode.None)
		{
			return FindSelectable(base.transform.rotation * Vector3.right);
		}
		return null;
	}

	public override Selectable FindSelectableOnLeft()
	{
		if (base.navigation.mode == Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnLeft;
		}
		if ((base.navigation.mode & Navigation.Mode.Horizontal) != Navigation.Mode.None)
		{
			return FindSelectable(base.transform.rotation * Vector3.left);
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
