using System.Collections;
using DG.Tweening;
using FMOD.Studio;
using Rewired;
using UnityEngine;

public class UIMenuSplash : UIMenuBase
{
	public RectTransform logo;

	public RectTransform buttonPrompt;

	private CanvasGroup buttonPromptCanvasGroup;

	public Animator splashCameraAnimator;

	public Animator logoAnimator;

	private IEnumerator introRoutine;

	private EventInstance logoRiserInstance;

	private static float submitDelay = 0.2f;

	private Tweener onSubmitTween;

	[SerializeField]
	private bool allowInput = true;

	private void Awake()
	{
		onSubmitTween = buttonPrompt.DOScale(0.66f, 0.2f).From(isRelative: false).SetEase(Ease.OutBack)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
		buttonPromptCanvasGroup = buttonPrompt.GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		if (ReInput.controllers.GetAnyButtonDown())
		{
			Singleton<UIManager>.Instance.eventSystemPlayerSwapper.SwapFirstPlayerPressingAnyButtonToEventSystem();
			UIResponse_Begin();
		}
	}

	public override void Setup()
	{
		Singleton<SettingsManager>.Instance.LoadUnlockedItems(OnUnlockedItemsLoaded);
		Singleton<SettingsManager>.Instance.ApplyLocalizationOption();
		Singleton<AudioManager>.Instance.PlayMenuMusic();
		base.Setup();
	}

	public override void Resume()
	{
		base.Resume();
		Init();
	}

	public override void Init()
	{
		base.Init();
		Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
		logo.localScale = Vector3.one;
		logo.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
		ZoomInWrapper();
		if (splashCameraAnimator != null)
		{
			splashCameraAnimator.ResetTrigger("Go");
			splashCameraAnimator.SetTrigger("Go");
		}
		if (introRoutine != null)
		{
			StopCoroutine(introRoutine);
		}
		introRoutine = PlayIntro();
		StartCoroutine(introRoutine);
		onSubmitTween.Goto(0f);
		buttonPrompt.localScale = Vector3.one;
		allowInput = true;
	}

	public void UIResponse_Begin()
	{
		if (allowInput)
		{
			buttonPrompt.DOComplete();
			onSubmitTween.Goto(0f, andPlay: true);
			if (logoRiserInstance.isValid())
			{
				logoRiserInstance.stop(STOP_MODE.ALLOWFADEOUT);
			}
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/begin");
			allowInput = false;
			StartCoroutine(DelayedClickEvent());
		}
	}

	private void LoadData()
	{
		Debug.Log("LoadData");
		Singleton<SettingsManager>.Instance.LoadOptions(OnOptionsLoaded);
	}

	private void OnOptionsLoaded(GameOptions gameOptions)
	{
		Singleton<SettingsManager>.Instance.LoadModifiedMatchSettings(OnMatchSettingsLoaded);
	}

	private void OnMatchSettingsLoaded(MatchSettings matchSettings)
	{
		Singleton<SettingsManager>.Instance.LoadUnlockedItems(OnUnlockedItemsLoaded);
	}

	private void OnUnlockedItemsLoaded(UnlockedItems unlockedItems)
	{
		if (unlockedItems == null || unlockedItems.unlockedCharacterHats == null)
		{
			Debug.Log("WARNING - SplashMenu -> unlockedItems or unlockedCharacterHats is null an error will likely occur ");
		}
		Singleton<GameManager>.Instance.UnlockDLCItems();
		Singleton<SettingsManager>.Instance.SaveUnlockedItems(null);
	}

	private void GoToMainMenu()
	{
		UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(MenuType.Main);
		if (menu != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menu);
		}
	}

	private IEnumerator DelayedClickEvent()
	{
		float t = 0f;
		while (t < submitDelay)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		allowInput = true;
		GoToMainMenu();
	}

	private IEnumerator PlayIntro()
	{
		buttonPromptCanvasGroup.DOComplete();
		buttonPromptCanvasGroup.alpha = 0f;
		logoRiserInstance = Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/logo_riser");
		yield return new WaitForSecondsRealtime(0.8f);
		if (logoAnimator != null)
		{
			logoAnimator.ResetTrigger("Go");
			logoAnimator.SetTrigger("Go");
		}
		yield return new WaitForSecondsRealtime(1.5f);
		buttonPromptCanvasGroup.DOFade(1f, 0.5f);
	}
}
