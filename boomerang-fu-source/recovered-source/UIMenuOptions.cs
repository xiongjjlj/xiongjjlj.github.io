using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuOptions : UIMenuBase
{
	public UIOptionsModule[] optionsModules;

	private UIOptionsModule currentOptionsModule;

	public UISliderButtonNoLocalization language;

	public UISliderButtonNoLocalization resolution;

	public UISliderButton quality;

	public UISliderButton vsync;

	public UISliderButton fullscreen;

	public UISliderButton sfxVolume;

	public UISliderButton musicVolume;

	public UISliderButton showHints;

	public UISliderButton slowMo;

	public UISliderButton botsWearHats;

	public UISliderButton recordReplays;

	public UISliderButton screenShake;

	public UISliderButton freezeFrames;

	public UISliderButton vibration;

	public UISliderButton sharedKeyboard;

	public UISliderButton xinput;

	public UISliderButton conventionMode;

	public UIFillButton restoreDefaultPrompt;

	private float restoreDefaultCharge;

	private static float restoreDefaultDuration = 1f;

	private bool restoreDefaultInputBlocked;

	public GameObject[] PCPlatformOnlyRows;

	public GameObject[] windowsPlatformOnlyRows;

	public void OnEnable()
	{
		language.OnSubmitEvent.AddListener(OnSubmitLanguage);
		language.OnDeselectEvent.AddListener(OnDeselectLanguage);
		resolution.OnSubmitEvent.AddListener(OnSubmitResolution);
		resolution.OnDeselectEvent.AddListener(OnDeselectResolution);
		quality.OnSubmitEvent.AddListener(OnSubmitQuality);
		quality.OnDeselectEvent.AddListener(OnDeselectQuality);
		vsync.OnValueChanged.AddListener(OnChangeVsync);
		fullscreen.OnSubmitEvent.AddListener(OnSubmitFullscreen);
		fullscreen.OnDeselectEvent.AddListener(OnDeselectFullscreen);
		sfxVolume.OnValueChanged.AddListener(OnChangeSFXVolume);
		musicVolume.OnValueChanged.AddListener(OnChangeMusicVolume);
		showHints.OnValueChanged.AddListener(OnChangeShowHints);
		slowMo.OnValueChanged.AddListener(OnChangeSlowMo);
		botsWearHats.OnValueChanged.AddListener(OnChangeBotsWearHats);
		recordReplays.OnValueChanged.AddListener(OnChangeRecordReplays);
		freezeFrames.OnValueChanged.AddListener(OnChangeFreezeFrames);
		screenShake.OnValueChanged.AddListener(OnChangeScreenShake);
		vibration.OnValueChanged.AddListener(OnChangeVibration);
		sharedKeyboard.OnValueChanged.AddListener(OnChangeSharedKeyboard);
		xinput.OnValueChanged.AddListener(OnChangeXinput);
		conventionMode.OnValueChanged.AddListener(OnChangeConventionMode);
	}

	public void OnDisable()
	{
		language.OnSubmitEvent.RemoveListener(OnSubmitLanguage);
		language.OnDeselectEvent.RemoveListener(OnDeselectLanguage);
		resolution.OnSubmitEvent.RemoveListener(OnSubmitResolution);
		resolution.OnDeselectEvent.RemoveListener(OnDeselectResolution);
		quality.OnSubmitEvent.RemoveListener(OnSubmitQuality);
		quality.OnDeselectEvent.RemoveListener(OnDeselectQuality);
		vsync.OnValueChanged.RemoveListener(OnChangeVsync);
		fullscreen.OnSubmitEvent.RemoveListener(OnSubmitFullscreen);
		fullscreen.OnDeselectEvent.RemoveListener(OnDeselectFullscreen);
		sfxVolume.OnValueChanged.RemoveListener(OnChangeSFXVolume);
		musicVolume.OnValueChanged.RemoveListener(OnChangeMusicVolume);
		showHints.OnValueChanged.RemoveListener(OnChangeShowHints);
		slowMo.OnValueChanged.RemoveListener(OnChangeSlowMo);
		botsWearHats.OnValueChanged.RemoveListener(OnChangeBotsWearHats);
		recordReplays.OnValueChanged.RemoveListener(OnChangeRecordReplays);
		freezeFrames.OnValueChanged.RemoveListener(OnChangeFreezeFrames);
		screenShake.OnValueChanged.RemoveListener(OnChangeScreenShake);
		vibration.OnValueChanged.RemoveListener(OnChangeVibration);
		sharedKeyboard.OnValueChanged.RemoveListener(OnChangeSharedKeyboard);
		xinput.OnValueChanged.RemoveListener(OnChangeXinput);
		conventionMode.OnValueChanged.RemoveListener(OnChangeConventionMode);
	}

	public override void Init()
	{
		base.Init();
		Singleton<UIManager>.Instance.mainMenuBGCamera.enabled = false;
		ZoomInWrapper();
		LoadSliderValues();
		restoreDefaultPrompt.InitButtonPrompt(Singleton<UIManager>.Instance.GetCurrentMasterPlayer());
		for (int i = 0; i < optionsModules.Length; i++)
		{
			optionsModules[i].gameObject.SetActive(value: false);
		}
		GoToOptionsModule(UIOptionsModule.OptionsModule.Home);
		for (int j = 0; j < PCPlatformOnlyRows.Length; j++)
		{
			PCPlatformOnlyRows[j].SetActive(Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.PC);
			Selectable componentInChildren = PCPlatformOnlyRows[j].GetComponentInChildren<Selectable>();
			if (componentInChildren != null)
			{
				componentInChildren.interactable = Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.PC;
			}
		}
		for (int k = 0; k < windowsPlatformOnlyRows.Length; k++)
		{
			bool flag = false;
			flag = Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.PC;
			windowsPlatformOnlyRows[k].SetActive(flag);
			Selectable componentInChildren2 = windowsPlatformOnlyRows[k].GetComponentInChildren<Selectable>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.interactable = flag;
			}
		}
	}

	private void LoadSliderValues()
	{
		language.SetValue(Singleton<SettingsManager>.Instance.GetCurrentLanguageIndex());
		List<Resolution> availableResolutions = Singleton<SettingsManager>.Instance.GetAvailableResolutions();
		if (availableResolutions.Count > 0)
		{
			resolution.rawStringMap = new string[availableResolutions.Count];
			for (int i = 0; i < resolution.rawStringMap.Length; i++)
			{
				resolution.rawStringMap[i] = availableResolutions[i].width + " x " + availableResolutions[i].height;
			}
			resolution.SetMaxValue(availableResolutions.Count - 1);
			resolution.SetValue(GetCurrentResolutionIndex());
		}
		quality.SetValue(Singleton<SettingsManager>.Instance.GameOptions.graphicsQuality);
		vsync.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.graphicsVsync));
		fullscreen.SetValue(GetCurrentFullscreenValue());
		sfxVolume.SetValue(Singleton<SettingsManager>.Instance.GameOptions.sfxVolume);
		musicVolume.SetValue(Singleton<SettingsManager>.Instance.GameOptions.musicVolume);
		showHints.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.showButtonHints));
		slowMo.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.enableSlowMo));
		botsWearHats.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.botsWearHats));
		recordReplays.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.recordReplays));
		freezeFrames.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.freezeFramesEnabled));
		screenShake.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.screenShakeEnabled));
		vibration.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled));
		sharedKeyboard.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.secondKeyboardAllowed));
		xinput.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.xinput));
		conventionMode.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.GameOptions.conventionMode));
	}

	private int GetCurrentResolutionIndex()
	{
		List<Resolution> availableResolutions = Singleton<SettingsManager>.Instance.GetAvailableResolutions();
		for (int i = 0; i < availableResolutions.Count; i++)
		{
			if (availableResolutions[i].width == Screen.width && availableResolutions[i].height == Screen.height)
			{
				return i;
			}
		}
		return 0;
	}

	private void Update()
	{
		if (restoreDefaultInputBlocked && Singleton<UIManager>.Instance.GetCurrentMasterPlayer().GetButtonUp("UIRestore"))
		{
			restoreDefaultInputBlocked = false;
		}
		if (!restoreDefaultInputBlocked && Singleton<UIManager>.Instance.GetCurrentMasterPlayer().GetButton("UIRestore"))
		{
			if (restoreDefaultPrompt != null)
			{
				restoreDefaultCharge += Time.unscaledDeltaTime;
				if (restoreDefaultCharge >= restoreDefaultDuration)
				{
					restoreDefaultCharge = 0f;
					restoreDefaultInputBlocked = true;
					restoreDefaultPrompt.Flash();
					UIResponse_RestoreDefault();
					Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.5f);
				}
			}
		}
		else
		{
			restoreDefaultCharge = 0f;
		}
		restoreDefaultPrompt.SetFill(restoreDefaultCharge / restoreDefaultDuration);
	}

	private IEnumerator ResetEventSystemSwapper()
	{
		yield return 0;
		Singleton<UIManager>.Instance.eventSystemPlayerSwapper.AssignAllPlayersToEventSystem();
	}

	public override void Cleanup()
	{
		base.Cleanup();
	}

	private void GoToOptionsModule(UIOptionsModule.OptionsModule optionsModule)
	{
		if (currentOptionsModule != null)
		{
			currentOptionsModule.Disable();
		}
		UIOptionsModule optionsModule2 = GetOptionsModule(optionsModule);
		if (optionsModule2 != null)
		{
			currentOptionsModule = optionsModule2;
			currentOptionsModule.Enable();
		}
		else
		{
			Debug.Log("Target module not found!");
		}
	}

	private UIOptionsModule GetOptionsModule(UIOptionsModule.OptionsModule optionsModule)
	{
		for (int i = 0; i < optionsModules.Length; i++)
		{
			if (optionsModules[i].optionsModule == optionsModule)
			{
				return optionsModules[i];
			}
		}
		return null;
	}

	private void OnSubmitLanguage(int value)
	{
		string languageString = Singleton<SettingsManager>.Instance.GetLanguageString(value);
		Singleton<SettingsManager>.Instance.ChangeLanguage(languageString);
		LoadSliderValues();
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			uIMenuLobby.ResetAllPlayers();
		}
	}

	private void OnDeselectLanguage()
	{
		if (language != null)
		{
			language.SetValue(Singleton<SettingsManager>.Instance.GetCurrentLanguageIndex());
		}
	}

	private void OnSubmitQuality(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.graphicsQuality = value;
		QualitySettings.SetQualityLevel(value, applyExpensiveChanges: true);
		if (QualitySettings.vSyncCount != Singleton<SettingsManager>.Instance.GameOptions.graphicsVsync)
		{
			QualitySettings.vSyncCount = Singleton<SettingsManager>.Instance.GameOptions.graphicsVsync;
		}
	}

	private void OnDeselectQuality()
	{
		quality.SetValue(Singleton<SettingsManager>.Instance.GameOptions.graphicsQuality);
	}

	private void OnChangeVsync(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.graphicsVsync = value;
		if (QualitySettings.vSyncCount != value)
		{
			QualitySettings.vSyncCount = Singleton<SettingsManager>.Instance.GameOptions.graphicsVsync;
		}
	}

	private void OnSubmitResolution(int value)
	{
		List<Resolution> availableResolutions = Singleton<SettingsManager>.Instance.GetAvailableResolutions();
		if (availableResolutions.Count == 0 || value < 0 || value >= availableResolutions.Count)
		{
			Debug.LogWarning("Selected resolution index not available on this screen for some reason!");
			return;
		}
		Singleton<SettingsManager>.Instance.GameOptions.graphicsResolutionWidth = availableResolutions[value].width;
		Singleton<SettingsManager>.Instance.GameOptions.graphicsResolutionHeight = availableResolutions[value].height;
		Singleton<SettingsManager>.Instance.ApplyGraphicsOptions();
	}

	private void OnDeselectResolution()
	{
		if (resolution != null && resolution.maxValue > 0)
		{
			resolution.SetValue(GetCurrentResolutionIndex());
		}
	}

	private void OnSubmitFullscreen(int value)
	{
		switch (value)
		{
		case 0:
			Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen = FullScreenMode.ExclusiveFullScreen;
			break;
		case 1:
			Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen = FullScreenMode.FullScreenWindow;
			break;
		case 2:
			Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen = FullScreenMode.Windowed;
			break;
		}
		Singleton<SettingsManager>.Instance.ApplyGraphicsOptions();
	}

	private void OnDeselectFullscreen()
	{
		fullscreen.SetValue(GetCurrentFullscreenValue());
	}

	private int GetCurrentFullscreenValue()
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen == FullScreenMode.ExclusiveFullScreen)
		{
			return 0;
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen == FullScreenMode.FullScreenWindow)
		{
			return 1;
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.graphicsFullscreen == FullScreenMode.Windowed)
		{
			return 2;
		}
		return 0;
	}

	private void OnChangeMusicVolume(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.musicVolume = value;
		Singleton<AudioManager>.Instance.SetMusicBusVolume(value);
	}

	private void OnChangeSFXVolume(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.sfxVolume = value;
		Singleton<AudioManager>.Instance.SetSFXBusVolume(value);
	}

	private void OnChangeShowHints(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.showButtonHints = Convert.ToBoolean(value);
	}

	private void OnChangeSlowMo(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.enableSlowMo = Convert.ToBoolean(value);
	}

	private void OnChangeBotsWearHats(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.botsWearHats = Convert.ToBoolean(value);
	}

	private void OnChangeRecordReplays(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.recordReplays = Convert.ToBoolean(value);
	}

	private void OnChangeFreezeFrames(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.freezeFramesEnabled = Convert.ToBoolean(value);
	}

	private void OnChangeScreenShake(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.screenShakeEnabled = Convert.ToBoolean(value);
	}

	private void OnChangeVibration(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled = Convert.ToBoolean(value);
	}

	private void OnChangeSharedKeyboard(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.secondKeyboardAllowed = Convert.ToBoolean(value);
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			uIMenuLobby.ResetAllPlayers();
		}
		Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
	}

	private void OnChangeXinput(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.xinput = Convert.ToBoolean(value);
		Singleton<SettingsManager>.Instance.ApplyControllerOptions();
	}

	private void OnChangeConventionMode(int value)
	{
		Singleton<SettingsManager>.Instance.GameOptions.conventionMode = Convert.ToBoolean(value);
	}

	public void UIResponse_Apply()
	{
		Singleton<SettingsManager>.Instance.SaveOptions(OnOptionsSaved);
	}

	private void OnOptionsSaved(bool areSettingsSaved)
	{
		if (!areSettingsSaved)
		{
			Debug.LogError("UIResponse_Apply. Saving options failed.");
		}
		Debug.Log("OnOptionsSaved");
		UIResponse_Close();
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/back");
	}

	private void UIResponse_RestoreDefault()
	{
		if (currentOptionsModule == null || currentOptionsModule.optionsModule == UIOptionsModule.OptionsModule.Home)
		{
			KeyBindings keyboardOneBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardOneBindings;
			KeyBindings keyboardTwoBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardTwoBindings;
			Singleton<SettingsManager>.Instance.RestoreDefaultOptions();
			Singleton<SettingsManager>.Instance.GameOptions.keyboardOneBindings = keyboardOneBindings;
			Singleton<SettingsManager>.Instance.GameOptions.keyboardTwoBindings = keyboardTwoBindings;
			Debug.Log("Restore default options");
		}
		else if (currentOptionsModule.optionsModule == UIOptionsModule.OptionsModule.Controls)
		{
			Singleton<SettingsManager>.Instance.LoadDefaultKeyBindings();
			RefreshAllBindingButtonLabels();
			Debug.Log("Restore default key bindings");
		}
		Singleton<SettingsManager>.Instance.SaveOptions(OnOptionsRestarted);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/restore_defaults");
	}

	private void OnOptionsRestarted(bool areSettingsSaved)
	{
		if (!areSettingsSaved)
		{
			Debug.LogError("UIResponse_Apply. Saving options failed.");
		}
		LoadSliderValues();
	}

	public void UIResponse_GoToOptionsModuleControls()
	{
		GoToOptionsModule(UIOptionsModule.OptionsModule.Controls);
	}

	public void UIResponse_GoToOptionsModuleHome()
	{
		GoToOptionsModule(UIOptionsModule.OptionsModule.Home);
	}

	public void RefreshAllBindingButtonLabels()
	{
		UIOptionsModuleControls uIOptionsModuleControls = GetOptionsModule(UIOptionsModule.OptionsModule.Controls) as UIOptionsModuleControls;
		if (uIOptionsModuleControls != null)
		{
			uIOptionsModuleControls.RefreshAllBindingButtonLabels();
		}
		UpdateMasterButtonPrompts();
	}
}
