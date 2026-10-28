using System;
using System.Collections;
using BoomerangFu.Common.PlatformTools;
using DG.Tweening;
using TKOU_DLC_System;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuMain : UIMenuBase
{
	public Animator whiteBordersAnim;

	public RectTransform[] delayedTransforms;

	public Selectable[] rightSideButtons;

	private static float delayDelta = 0.15f;

	private IEnumerator introRoutine;

	public Selectable btnWishlist;

	public Selectable btnCredits;

	public GameObject demoTitle;

	public GameObject dlcOneTitle;

	public GameObject dlcTwoTitle;

	public GameObject dlcOneTitleJP;

	public GameObject dlcTwoTitleJP;

	public Selectable dlcOneAnnouncement;

	public Selectable dlcTwoAnnouncement;

	private void Awake()
	{
		if (Singleton<SettingsManager>.Instance.demoMode)
		{
			btnCredits.interactable = false;
			btnCredits.gameObject.SetActive(value: false);
		}
		else
		{
			btnWishlist.interactable = false;
			btnWishlist.gameObject.SetActive(value: false);
			demoTitle.gameObject.SetActive(value: false);
		}
	}

	public override void Setup()
	{
		base.Setup();
	}

	public override void Resume()
	{
		base.Resume();
		Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
		Singleton<UIManager>.Instance.mainMenuBGCamera.enabled = true;
		Singleton<Camera3D>.Instance.uiCameraPostProcessing.enabled = true;
		ZoomInWrapper();
		if (whiteBordersAnim != null)
		{
			whiteBordersAnim.ResetTrigger("End");
			whiteBordersAnim.SetTrigger("End");
		}
	}

	public override void Init()
	{
		base.Init();
		if (dlcOneTitle != null)
		{
			dlcOneTitle.gameObject.SetActive(Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.FreshFlavorsPack));
		}
		if (dlcOneTitleJP != null)
		{
			dlcOneTitleJP.gameObject.SetActive(Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.FreshFlavorsPack));
		}
		if (dlcTwoTitle != null)
		{
			dlcTwoTitle.gameObject.SetActive(Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.JustDessertsPack));
		}
		if (dlcTwoTitleJP != null)
		{
			dlcTwoTitleJP.gameObject.SetActive(Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.JustDessertsPack));
		}
		dlcOneAnnouncement.interactable = false;
		dlcOneAnnouncement.gameObject.SetActive(value: false);
		dlcTwoAnnouncement.interactable = false;
		dlcTwoAnnouncement.gameObject.SetActive(value: false);
		if (!Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.JustDessertsPack) && Singleton<SettingsManager>.Instance.announceDLCTwo && !Singleton<SettingsManager>.Instance.GameOptions.hideDLCTwoAnnouncement)
		{
			Debug.Log("Show DLC two announcement");
			dlcTwoAnnouncement.interactable = true;
			dlcTwoAnnouncement.gameObject.SetActive(value: true);
			for (int i = 0; i < rightSideButtons.Length; i++)
			{
				rightSideButtons[i].navigation = new Navigation
				{
					selectOnUp = rightSideButtons[i].navigation.selectOnUp,
					selectOnDown = rightSideButtons[i].navigation.selectOnDown,
					selectOnLeft = dlcTwoAnnouncement,
					selectOnRight = rightSideButtons[i].navigation.selectOnRight,
					mode = rightSideButtons[i].navigation.mode
				};
			}
		}
		else if (!Singleton<GameManager>.Instance.IsDLCUnlocked(DownloadableContentID.FreshFlavorsPack) && Singleton<SettingsManager>.Instance.announceDLCOne && !Singleton<SettingsManager>.Instance.GameOptions.hideDLCOneAnnouncement)
		{
			Debug.Log("Show DLC one announcement");
			dlcOneAnnouncement.interactable = true;
			dlcOneAnnouncement.gameObject.SetActive(value: true);
			for (int j = 0; j < rightSideButtons.Length; j++)
			{
				rightSideButtons[j].navigation = new Navigation
				{
					selectOnUp = rightSideButtons[j].navigation.selectOnUp,
					selectOnDown = rightSideButtons[j].navigation.selectOnDown,
					selectOnLeft = dlcOneAnnouncement,
					selectOnRight = rightSideButtons[j].navigation.selectOnRight,
					mode = rightSideButtons[j].navigation.mode
				};
			}
		}
		Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
		Singleton<UIManager>.Instance.mainMenuBGCamera.enabled = true;
		if (delayedTransforms.Length != 0)
		{
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		}
		ZoomInWrapper();
		if (whiteBordersAnim != null)
		{
			whiteBordersAnim.ResetTrigger("End");
		}
		float num = 0.3f;
		for (int k = 0; k < delayedTransforms.Length; k++)
		{
			if (delayedTransforms[k].gameObject.activeInHierarchy)
			{
				StartCoroutine(ShowTransformAfterDelay(delayedTransforms[k], num, k == delayedTransforms.Length - 1));
				num += delayDelta;
			}
		}
	}

	private IEnumerator ShowTransformAfterDelay(RectTransform tf, float delay, bool enableNavigationEvents)
	{
		tf.DOComplete();
		tf.localScale = Vector3.zero;
		yield return new WaitForSecondsRealtime(delay);
		tf.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
		if (enableNavigationEvents)
		{
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		}
	}

	public void UIResponse_Start()
	{
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			Singleton<UIManager>.Instance.PushMenu(uIMenuLobby);
			uIMenuLobby.ResetAllPlayers();
		}
	}

	public void UIResponse_Wishlist()
	{
		Singleton<UIManager>.Instance.ShowWishlistPrompt();
	}

	public void UIResponse_Options()
	{
		UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(MenuType.Options);
		if (menu != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menu);
		}
	}

	public void UIResponse_ChangeUser()
	{
		if (Singleton<PlatformManager>.Instance != null)
		{
			Singleton<PlatformManager>.Instance.ChangeMainUser();
		}
	}

	public void UIResponse_Credits()
	{
		UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(MenuType.Credits);
		if (menu != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menu);
		}
	}

	public void UIResponse_Quit()
	{
		Application.Quit();
	}

	public void UIResponse_ShowDLCOne()
	{
		DownloadableContentID downloadableContentID = DownloadableContentID.FreshFlavorsPack;
		string text = Enum.GetName(typeof(DownloadableContentID), downloadableContentID);
		if (text != null)
		{
			DlcPlatformManager.Instance.OpenInStore(text);
		}
	}

	public void UIResponse_ShowDLCTwo()
	{
		DownloadableContentID downloadableContentID = DownloadableContentID.JustDessertsPack;
		string text = Enum.GetName(typeof(DownloadableContentID), downloadableContentID);
		if (text != null)
		{
			DlcPlatformManager.Instance.OpenInStore(text);
		}
	}

	public void UIResponse_HideDLCOneAnnouncement()
	{
		Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		Singleton<SettingsManager>.Instance.GameOptions.hideDLCOneAnnouncement = true;
		Singleton<SettingsManager>.Instance.SaveOptions(null);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/restore_defaults");
		dlcOneAnnouncement.interactable = false;
		dlcOneAnnouncement.gameObject.SetActive(value: false);
	}

	public void UIResponse_HideDLCTwoAnnouncement()
	{
		Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		Singleton<SettingsManager>.Instance.GameOptions.hideDLCTwoAnnouncement = true;
		Singleton<SettingsManager>.Instance.SaveOptions(null);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/restore_defaults");
		dlcTwoAnnouncement.interactable = false;
		dlcTwoAnnouncement.gameObject.SetActive(value: false);
	}
}
