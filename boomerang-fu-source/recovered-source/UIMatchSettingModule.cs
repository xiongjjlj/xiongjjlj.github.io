using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIMatchSettingModule : MonoBehaviour
{
	public enum MatchSetting
	{
		GameMode,
		Teams,
		BotDifficulty,
		Rules,
		Modifiers,
		Maps
	}

	public MatchSetting matchSetting;

	public Canvas canvas;

	public GameObject initialSelection;

	private GameObject lastSelection;

	public CanvasGroup[] lockedButtonsInDemo;

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
		if (Singleton<SettingsManager>.Instance.demoMode)
		{
			for (int i = 0; i < lockedButtonsInDemo.Length; i++)
			{
				lockedButtonsInDemo[i].alpha = 0.33f;
				lockedButtonsInDemo[i].transform.GetComponentInChildren<Selectable>().interactable = false;
				lockedButtonsInDemo[i].transform.Find("Padlock").gameObject.SetActive(value: true);
			}
		}
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
