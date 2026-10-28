using DG.Tweening;
using UnityEngine;

public class UIOptionsModule : MonoBehaviour
{
	public enum OptionsModule
	{
		Home,
		Controls
	}

	public OptionsModule optionsModule;

	public Canvas canvas;

	public GameObject initialSelection;

	protected GameObject lastSelection;

	private void Update()
	{
		if (Singleton<UIManager>.Instance.GetButtonDownFromEventSystem("UICancel"))
		{
			Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
			UIMenuOptions uIMenuOptions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Options) as UIMenuOptions;
			if (uIMenuOptions != null)
			{
				uIMenuOptions.UIResponse_Apply();
			}
		}
	}

	public virtual void Enable()
	{
		base.gameObject.SetActive(value: true);
		if (lastSelection == null)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		}
		else
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(lastSelection);
		}
		CanvasGroup component = GetComponent<CanvasGroup>();
		component.alpha = 0.2f;
		component.DOFade(1f, 0.3f).SetEase(Ease.OutCubic);
	}

	public virtual void Disable()
	{
		base.gameObject.SetActive(value: false);
		if (Singleton<UIManager>.Instance.eventSystem != null)
		{
			lastSelection = Singleton<UIManager>.Instance.eventSystem.currentSelectedGameObject;
		}
	}
}
