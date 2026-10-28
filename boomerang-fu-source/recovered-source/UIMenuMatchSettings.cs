using System;
using DG.Tweening;
using Rewired;
using UnityEngine;

public class UIMenuMatchSettings : UIMenuBase
{
	public UIMatchSettingModule[] matchSettingModules;

	private UIMatchSettingModule currentMatchSettingModule;

	private UIMenuLobby uiMenuLobby;

	[SerializeField]
	private bool isUsingModifiers;

	public override void Setup()
	{
		base.Setup();
	}

	public override void Init()
	{
		base.Init();
		uiMenuLobby = Singleton<UIManager>.Instance.GetMenu(MenuType.Lobby) as UIMenuLobby;
		for (int i = 0; i < matchSettingModules.Length; i++)
		{
			matchSettingModules[i].gameObject.SetActive(value: false);
		}
		GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.GameMode);
		ZoomInWrapper();
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	public override void Suspend()
	{
		base.Suspend();
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
	}

	public override void Resume()
	{
		base.Resume();
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	public override void Cleanup()
	{
		base.Cleanup();
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
	}

	private void Update()
	{
		if (!Singleton<UIManager>.Instance.GetButtonDownFromEventSystem("UICancel"))
		{
			return;
		}
		Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
		if (currentMatchSettingModule.matchSetting == UIMatchSettingModule.MatchSetting.Modifiers)
		{
			SaveModifiers(OnMatchSettingsSaved);
		}
		else if (currentMatchSettingModule.matchSetting == UIMatchSettingModule.MatchSetting.Maps)
		{
			UIMatchSettingMapsModule uIMatchSettingMapsModule = currentMatchSettingModule as UIMatchSettingMapsModule;
			if (uIMatchSettingMapsModule != null)
			{
				if (uIMatchSettingMapsModule.AreMapsValid())
				{
					SaveModifiers(OnMapsSaved);
				}
				else
				{
					uIMatchSettingMapsModule.ShowErrorMessage();
				}
			}
		}
		else
		{
			GoToPreviousSetting();
		}
	}

	private void OnMatchSettingsSaved(bool areSettingsSaved)
	{
		if (!areSettingsSaved)
		{
			Debug.LogError("GoToPreviousSetting. Saving modifiers failed.");
		}
		GoToPreviousSetting();
	}

	private void OnMapsSaved(bool areMapsSaved)
	{
		if (!areMapsSaved)
		{
			Debug.LogError("GoToPreviousSetting. Saving modifiers failed.");
		}
		GoToPreviousSetting();
	}

	private void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		GoToLobby();
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		GoToLobby();
	}

	private void GoToLobby()
	{
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		if (currentMatchSettingModule != null)
		{
			currentMatchSettingModule.Disable();
			currentMatchSettingModule = null;
		}
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			if (uIMenuLobby.uiPlayersCanvas != null)
			{
				uIMenuLobby.uiPlayersCanvas.gameObject.SetActive(value: true);
			}
			uIMenuLobby.ResetAllPlayers();
		}
		Singleton<UIManager>.Instance.PopMenu();
	}

	private void GoToMatchSettingModule(UIMatchSettingModule.MatchSetting matchSetting)
	{
		if (currentMatchSettingModule != null)
		{
			currentMatchSettingModule.Disable();
		}
		UIMatchSettingModule matchSettingModule = GetMatchSettingModule(matchSetting);
		if (matchSettingModule != null)
		{
			currentMatchSettingModule = matchSettingModule;
			currentMatchSettingModule.Enable();
		}
		else
		{
			Debug.Log("Target module not found!");
		}
	}

	public UIMatchSettingModule GetMatchSettingModule(UIMatchSettingModule.MatchSetting matchSetting)
	{
		for (int i = 0; i < matchSettingModules.Length; i++)
		{
			if (matchSettingModules[i].matchSetting == matchSetting)
			{
				return matchSettingModules[i];
			}
		}
		return null;
	}

	public UIMatchSettingModule GetCurrentMatchSettingModule()
	{
		return currentMatchSettingModule;
	}

	public UIMatchSettingModule.MatchSetting GetCurrentMatchSettingModuleType()
	{
		if (currentMatchSettingModule == null)
		{
			return UIMatchSettingModule.MatchSetting.GameMode;
		}
		return currentMatchSettingModule.matchSetting;
	}

	private int GetMatchSettingModuleIndex(UIMatchSettingModule.MatchSetting matchSetting)
	{
		for (int i = 0; i < matchSettingModules.Length; i++)
		{
			if (matchSettingModules[i].matchSetting == matchSetting)
			{
				return i;
			}
		}
		Debug.Log("Match setting module not found");
		return 0;
	}

	private int GetMatchSettingModuleIndex(UIMatchSettingModule module)
	{
		for (int i = 0; i < matchSettingModules.Length; i++)
		{
			if (matchSettingModules[i] == module)
			{
				return i;
			}
		}
		Debug.Log("Match setting module not found");
		return 0;
	}

	private bool IsMatchSettingLaterInQueue(UIMatchSettingModule.MatchSetting matchSetting)
	{
		return GetMatchSettingModuleIndex(currentMatchSettingModule) < GetMatchSettingModuleIndex(matchSetting);
	}

	private bool IsMatchSettingEarlierInQueue(UIMatchSettingModule.MatchSetting matchSetting)
	{
		return GetMatchSettingModuleIndex(currentMatchSettingModule) > GetMatchSettingModuleIndex(matchSetting);
	}

	public void UIResponse_DeathMatch()
	{
		Singleton<SettingsManager>.Instance.matchType = SettingsManager.MatchType.DeathMatch;
		Singleton<SettingsManager>.Instance.teamMatch = false;
		GoToNextSetting();
	}

	public void UIResponse_TeamUp()
	{
		Singleton<SettingsManager>.Instance.matchType = SettingsManager.MatchType.DeathMatch;
		Singleton<SettingsManager>.Instance.teamMatch = true;
		GoToNextSetting();
	}

	public void UIResponse_GoldenBoomerang()
	{
		Singleton<SettingsManager>.Instance.matchType = SettingsManager.MatchType.GoldenDisc;
		Singleton<SettingsManager>.Instance.teamMatch = false;
		GoToNextSetting();
	}

	public void UIResponse_HideAndSeek()
	{
		Singleton<SettingsManager>.Instance.matchType = SettingsManager.MatchType.HideAndSeek;
		Singleton<SettingsManager>.Instance.teamMatch = false;
		GoToNextSetting();
	}

	public void UIResponse_Proceed()
	{
		GoToNextSetting();
	}

	public void UIResponse_BotDifficulty(int difficulty)
	{
		Singleton<SettingsManager>.Instance.SelectBotDifficulty(difficulty);
		GoToNextSetting();
	}

	public void UIResponse_Modifiers()
	{
		GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Modifiers);
	}

	public void UIResponse_Maps()
	{
		GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Maps);
	}

	public void SaveModifiers(Action<bool> OnMatchSettingsSaved)
	{
		Singleton<SettingsManager>.Instance.SaveModifiedMatchSettings(OnMatchSettingsSaved);
	}

	public void UIResponse_Fight(UIButton button = null)
	{
		Singleton<SettingsManager>.Instance.PrepareMatchLength();
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		if (currentMatchSettingModule != null)
		{
			currentMatchSettingModule.Disable();
			currentMatchSettingModule = null;
		}
		Singleton<SettingsManager>.Instance.SaveUnlockedItems(null);
		Singleton<AudioManager>.Instance.StopMenuMusic();
		Singleton<GameManager>.Instance.StartCoroutine(Singleton<UIManager>.Instance.WaitForMatchStartThenClose(button));
	}

	private void GoToNextSetting()
	{
		if (IsMatchSettingLaterInQueue(UIMatchSettingModule.MatchSetting.GameMode))
		{
			GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.GameMode);
		}
		else if (Singleton<SettingsManager>.Instance.teamMatch && IsMatchSettingLaterInQueue(UIMatchSettingModule.MatchSetting.Teams))
		{
			if (uiMenuLobby != null)
			{
				uiMenuLobby.ResetTeams();
			}
			GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Teams);
		}
		else if (IsMatchSettingLaterInQueue(UIMatchSettingModule.MatchSetting.Rules))
		{
			GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Rules);
		}
		wrapper.localPosition = Vector3.zero;
		wrapper.DOLocalMoveX(30f, 0.3f).SetEase(Ease.OutCubic).From(isRelative: false);
	}

	private void GoToPreviousSetting()
	{
		if (IsMatchSettingEarlierInQueue(UIMatchSettingModule.MatchSetting.Rules))
		{
			if (isUsingModifiers && currentMatchSettingModule.matchSetting != UIMatchSettingModule.MatchSetting.Modifiers)
			{
				GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Modifiers);
			}
			else
			{
				GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Rules);
			}
		}
		else if (Singleton<SettingsManager>.Instance.teamMatch && IsMatchSettingEarlierInQueue(UIMatchSettingModule.MatchSetting.Teams))
		{
			GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.Teams);
		}
		else if (IsMatchSettingEarlierInQueue(UIMatchSettingModule.MatchSetting.GameMode))
		{
			if (uiMenuLobby != null)
			{
				uiMenuLobby.ResetTeams();
			}
			GoToMatchSettingModule(UIMatchSettingModule.MatchSetting.GameMode);
		}
		else
		{
			if (currentMatchSettingModule != null)
			{
				currentMatchSettingModule.Disable();
				currentMatchSettingModule = null;
			}
			Singleton<UIManager>.Instance.PopMenu();
		}
		wrapper.localPosition = Vector3.zero;
		wrapper.DOLocalMoveX(-30f, 0.3f).SetEase(Ease.OutCubic).From(isRelative: false);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/back");
	}
}
