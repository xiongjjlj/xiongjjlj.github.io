using System.Collections;
using DG.Tweening;
using Rewired;
using UnityEngine;

public class UIMenuBase : MonoBehaviour
{
	public enum MenuType
	{
		None = 0,
		Main = 1,
		Options = 2,
		Lobby = 3,
		MatchSettings = 4,
		Pause = 6,
		RoundTitles = 7,
		RoundEnd = 8,
		MatchEnd = 9,
		Instructions = 10,
		Credits = 11,
		Splash = 12,
		Alert = 13,
		Unlocked = 15
	}

	public MenuType MenuID;

	public GameObject initialSelection;

	public GameObject lastSelection;

	public ButtonPrompt[] masterButtonPrompts;

	protected RectTransform wrapper;

	private IEnumerator currentAnim;

	public virtual void Setup()
	{
		wrapper = base.transform.Find("Wrapper").GetComponent<RectTransform>();
	}

	public virtual void Init()
	{
		if (Singleton<UIManager>.Instance.eventSystem != null)
		{
			Singleton<UIManager>.Instance.eventSystem.SetSelectedGameObject(null);
		}
		if (lastSelection != null)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(lastSelection);
		}
		else if (initialSelection != null)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		}
		UpdateMasterButtonPrompts();
	}

	public virtual void UpdateMasterButtonPrompts()
	{
		if (masterButtonPrompts == null)
		{
			return;
		}
		Rewired.Player currentMasterPlayer = Singleton<UIManager>.Instance.GetCurrentMasterPlayer();
		if (currentMasterPlayer != null)
		{
			for (int i = 0; i < masterButtonPrompts.Length; i++)
			{
				masterButtonPrompts[i].Init(currentMasterPlayer);
			}
		}
	}

	protected virtual void ZoomInWrapper()
	{
		if (wrapper != null)
		{
			wrapper.localScale = Vector3.one;
			wrapper.DOScale(0.9f, 0.1f).From(isRelative: false);
		}
	}

	public virtual void Suspend()
	{
		if (Singleton<UIManager>.Instance.eventSystem != null)
		{
			lastSelection = Singleton<UIManager>.Instance.eventSystem.currentSelectedGameObject;
		}
	}

	public virtual void Resume()
	{
		if (Singleton<UIManager>.Instance.eventSystem != null)
		{
			Singleton<UIManager>.Instance.eventSystem.SetSelectedGameObject(null);
		}
		if (lastSelection != null)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(lastSelection);
		}
		else if (initialSelection != null)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		}
	}

	public virtual void Cleanup()
	{
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
		lastSelection = null;
	}

	public virtual void UIResponse_Close()
	{
		if (Singleton<UIManager>.Instance.IsOverlayMenu(this))
		{
			Singleton<UIManager>.Instance.RemoveOverlayMenu(this);
		}
		else if (Singleton<UIManager>.Instance.IsMenu(this))
		{
			Singleton<UIManager>.Instance.PopMenu();
		}
	}
}
