using System;
using System.Collections.Generic;
using I2.Loc;
using Rewired;
using UnityEngine;

public class SettingsManager : Singleton<SettingsManager>
{
	public enum Platform
	{
		PC,
		Switch,
		Xbox
	}

	public enum MatchType
	{
		DeathMatch,
		GoldenDisc,
		KingOfTheHill,
		HideAndSeek
	}

	public enum BotDifficulty
	{
		easy,
		medium,
		hard,
		impossible
	}

	[SerializeField]
	private GameOptions _gameOptions = new GameOptions();

	public Platform currentPlatform;

	public static int Rewired_MapID_Keyboard_One = 2;

	public static int Rewired_MapID_Keyboard_Two = 4;

	public static int Rewired_MapID_Keyboard_UI = 7;

	private const int Action_ID_MoveHorizontal = 0;

	private const int Action_ID_MoveVertical = 1;

	private const int Action_ID_Dash = 2;

	private const int Action_ID_Swipe = 3;

	private const int Action_ID_Throw = 4;

	private const int Action_ID_Back = 5;

	private const int Action_ID_UISubmit = 18;

	private const int Action_ID_UICancel = 19;

	private const int Action_ID_UITabLeft = 24;

	private const int Action_ID_UITabRight = 25;

	private const int Action_ID_UIRestore = 23;

	private List<Resolution> availableResolutions = new List<Resolution>();

	[SerializeField]
	public MatchSettings MatchSettings = new MatchSettings();

	private MatchSettings customMatchSettings = new MatchSettings();

	public MatchType matchType;

	public bool teamMatch;

	public int matchScoreGoal = 10;

	public int numBots;

	public int numPlayers;

	public bool tieBreaker;

	public bool showcaseMode;

	public float goalTimeStart;

	public static float goalTimeMin = 1f;

	public float roundDuration;

	public BotDifficulty botDifficulty = BotDifficulty.medium;

	public bool trailerMode;

	public bool localizationMode;

	public bool demoMode;

	public bool debugMode;

	public bool announceDLCOne;

	public bool announceDLCTwo;

	public bool limitSeekAttempts = true;

	private List<GameObjectLocalization> gameObjectLocalizations = new List<GameObjectLocalization>();

	[SerializeField]
	public UnlockedItems UnlockedItems = new UnlockedItems();

	public GameOptions GameOptions
	{
		get
		{
			if (_gameOptions == null)
			{
				Debug.LogError("GameOptions are null");
			}
			return _gameOptions;
		}
		set
		{
			_gameOptions = value;
		}
	}

	public VictoryCondition GetVictoryCondition
	{
		get
		{
			if (teamMatch)
			{
				return MatchSettings.victoryConditionTeamUp;
			}
			if (matchType == MatchType.GoldenDisc)
			{
				return MatchSettings.victoryConditionGoldenBoomerang;
			}
			return MatchSettings.victoryConditionDeathMatch;
		}
	}

	public bool UseCustomMaps
	{
		get
		{
			if (matchType == MatchType.DeathMatch)
			{
				if (!MatchSettings.useCustomMapsForDeathmatch)
				{
					return false;
				}
				if (MatchSettings.disabledDeathmatchLevels.Count > 0)
				{
					return true;
				}
				return false;
			}
			if (matchType == MatchType.GoldenDisc)
			{
				if (!MatchSettings.useCustomMapsForGoldenDisc)
				{
					return false;
				}
				if (MatchSettings.disabledGoldenDiscLevels.Count > 0)
				{
					return true;
				}
				return false;
			}
			return false;
		}
		set
		{
			if (matchType == MatchType.DeathMatch)
			{
				MatchSettings.useCustomMapsForDeathmatch = value;
			}
			if (matchType == MatchType.GoldenDisc)
			{
				MatchSettings.useCustomMapsForGoldenDisc = value;
			}
		}
	}

	public int NumDisabledMapsForCurrentMatchType
	{
		get
		{
			if (matchType == MatchType.DeathMatch)
			{
				return MatchSettings.disabledDeathmatchLevels.Count;
			}
			if (matchType == MatchType.GoldenDisc)
			{
				return MatchSettings.disabledGoldenDiscLevels.Count;
			}
			return 0;
		}
	}

	private void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		currentPlatform = Platform.PC;
	}

	public void LoadOptions(Action<GameOptions> OnOptionsLoaded)
	{
		Action<GameOptions> a = this.OnOptionsLoaded;
		a = (Action<GameOptions>)Delegate.Combine(a, OnOptionsLoaded);
		Singleton<DataManager>.Instance.LoadOptionsFromDisk(a);
	}

	private void OnOptionsLoaded(GameOptions loadedOptions)
	{
		if (loadedOptions == null)
		{
			loadedOptions = new GameOptions();
		}
		_gameOptions = loadedOptions;
		ApplyLocalizationOption();
		ApplyGraphicsOptions();
		ApplySoundOptions();
	}

	public void SaveOptions(Action<bool> OnOptionsSaved)
	{
		Singleton<DataManager>.Instance.SaveOptionsToDisk(_gameOptions, OnOptionsSaved);
	}

	public void RestoreDefaultOptions()
	{
		_gameOptions = Singleton<DataManager>.Instance.RestoreDefaultOptions();
	}

	public List<Resolution> GetAvailableResolutions()
	{
		if (availableResolutions != null)
		{
			availableResolutions.Clear();
		}
		Resolution[] resolutions = Screen.resolutions;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < resolutions.Length; i++)
		{
			if (resolutions[i].width != num || resolutions[i].height != num2)
			{
				availableResolutions.Add(resolutions[i]);
				num = resolutions[i].width;
				num2 = resolutions[i].height;
			}
		}
		return availableResolutions;
	}

	public void ApplyGraphicsOptions()
	{
		List<Resolution> list = GetAvailableResolutions();
		if (list.Count == 0)
		{
			Debug.LogWarning("No available resolutions found on this screen!");
			return;
		}
		if (GameOptions.graphicsResolutionWidth == 0 || GameOptions.graphicsResolutionHeight == 0)
		{
			if (currentPlatform != Platform.Switch)
			{
				GameOptions.graphicsResolutionWidth = Screen.currentResolution.width;
				GameOptions.graphicsResolutionHeight = Screen.currentResolution.height;
				return;
			}
			GameOptions.graphicsResolutionWidth = 1280;
			GameOptions.graphicsResolutionHeight = 720;
		}
		bool flag = false;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].width == GameOptions.graphicsResolutionWidth && list[i].height == GameOptions.graphicsResolutionHeight)
			{
				num = list[i].width;
				num2 = list[i].height;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Debug.Log("Saved resolution " + num + " x " + num2 + " is not valid on this screen, using current screen resolution " + Screen.currentResolution.width + " x " + Screen.currentResolution.height);
			num = Screen.currentResolution.width;
			num2 = Screen.currentResolution.height;
		}
		Debug.Log("Set resolution to " + num + " x " + num2);
		if (Screen.currentResolution.width != num || Screen.currentResolution.height != num2 || Screen.fullScreenMode != GameOptions.graphicsFullscreen)
		{
			Screen.SetResolution(num, num2, GameOptions.graphicsFullscreen);
		}
	}

	public void ApplySoundOptions()
	{
		Singleton<AudioManager>.Instance.SetMusicBusVolume(GameOptions.musicVolume);
		Singleton<AudioManager>.Instance.SetSFXBusVolume(GameOptions.sfxVolume);
	}

	public void ApplyControllerOptions()
	{
		if (ReInput.configuration.useXInput != GameOptions.xinput)
		{
			ReInput.configuration.useXInput = GameOptions.xinput;
			Singleton<GameManager>.Instance.ResetRewired();
			UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
			if (uIMenuLobby != null)
			{
				uIMenuLobby.ResetAllPlayers();
			}
			Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
		}
	}

	public void ApplyKeyBindings()
	{
		if (GameOptions.keyboardOneBindings.useCustomBindings)
		{
			if (GameOptions.keyboardOneBindings.UIRestore == KeyCode.None)
			{
				GameOptions.keyboardOneBindings.UIRestore = GameOptions.keyboardOneBindings.Throw;
				GameOptions.keyboardOneBindings.UITabRight = GameOptions.keyboardOneBindings.Swipe;
				GameOptions.keyboardOneBindings.UITabLeft = KeyCode.None;
				SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_One, GameOptions.keyboardOneBindings);
			}
			foreach (Rewired.Player player in ReInput.players.Players)
			{
				ReplaceKeyboardMapWithCustomBindings(player, Rewired_MapID_Keyboard_One, GameOptions.keyboardOneBindings);
			}
		}
		else
		{
			Debug.LogWarning("Custom Bindings not initialized for keyboard one. Saving Rewired defaults into GameOptions.");
			SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_One, GameOptions.keyboardOneBindings);
		}
		if (GameOptions.keyboardTwoBindings.useCustomBindings)
		{
			if (GameOptions.keyboardTwoBindings.UIRestore == KeyCode.None)
			{
				GameOptions.keyboardTwoBindings.UIRestore = GameOptions.keyboardTwoBindings.Throw;
				GameOptions.keyboardTwoBindings.UITabRight = GameOptions.keyboardTwoBindings.Swipe;
				GameOptions.keyboardTwoBindings.UITabLeft = KeyCode.None;
				SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_Two, GameOptions.keyboardTwoBindings);
			}
			{
				foreach (Rewired.Player player2 in ReInput.players.Players)
				{
					ReplaceKeyboardMapWithCustomBindings(player2, Rewired_MapID_Keyboard_Two, GameOptions.keyboardTwoBindings);
				}
				return;
			}
		}
		Debug.LogWarning("Custom Bindings not initialized for keyboard two. Saving Rewired defaults into GameOptions.");
		SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_Two, GameOptions.keyboardTwoBindings);
	}

	public void LoadDefaultKeyBindings()
	{
		foreach (Rewired.Player player in ReInput.players.Players)
		{
			RestoreDefaultKeyboardMaps(player);
		}
		SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_One, GameOptions.keyboardOneBindings);
		SaveRewiredKeyBindingsToSettingsManager(Rewired_MapID_Keyboard_Two, GameOptions.keyboardTwoBindings);
	}

	private void SaveRewiredKeyBindingsToSettingsManager(int mapID, KeyBindings keyBindings)
	{
		Rewired.Player firstPlayerWithKeyboardMap = GetFirstPlayerWithKeyboardMap(mapID);
		if (firstPlayerWithKeyboardMap == null)
		{
			Debug.LogError("No Rewired Players have a keyboard map for mapID " + mapID + " for some reason...");
			return;
		}
		ControllerMap keyboardMap = GetKeyboardMap(firstPlayerWithKeyboardMap, mapID);
		if (keyboardMap == null)
		{
			Debug.Log("P" + firstPlayerWithKeyboardMap.id + " keyboard map not found for mapID " + mapID);
			return;
		}
		IEnumerator<ActionElementMap> enumerator = keyboardMap.AllMaps.GetEnumerator();
		while (enumerator.MoveNext())
		{
			switch (enumerator.Current.actionId)
			{
			case 2:
				keyBindings.Dash = enumerator.Current.keyCode;
				break;
			case 3:
				keyBindings.Swipe = enumerator.Current.keyCode;
				break;
			case 4:
				keyBindings.Throw = enumerator.Current.keyCode;
				break;
			case 5:
				keyBindings.Back = enumerator.Current.keyCode;
				break;
			case 0:
				if (enumerator.Current.axisContribution == Pole.Positive)
				{
					keyBindings.MoveRight = enumerator.Current.keyCode;
				}
				else
				{
					keyBindings.MoveLeft = enumerator.Current.keyCode;
				}
				break;
			case 1:
				if (enumerator.Current.axisContribution == Pole.Positive)
				{
					keyBindings.MoveUp = enumerator.Current.keyCode;
				}
				else
				{
					keyBindings.MoveDown = enumerator.Current.keyCode;
				}
				break;
			case 18:
				keyBindings.UISubmit = enumerator.Current.keyCode;
				break;
			case 19:
				keyBindings.UICancel = enumerator.Current.keyCode;
				break;
			case 24:
				keyBindings.UITabLeft = enumerator.Current.keyCode;
				break;
			case 25:
				keyBindings.UITabRight = enumerator.Current.keyCode;
				break;
			case 23:
				keyBindings.UIRestore = enumerator.Current.keyCode;
				break;
			}
		}
		keyBindings.useCustomBindings = true;
	}

	private KeyCode GetDefaultKeyBindingForAction(int mapID, int actionID)
	{
		Rewired.Player firstPlayerWithKeyboardMap = GetFirstPlayerWithKeyboardMap(mapID);
		if (firstPlayerWithKeyboardMap == null)
		{
			Debug.LogError("No Rewired Players have a keyboard map for mapID " + mapID + " for some reason...");
			return KeyCode.None;
		}
		ControllerMap keyboardMap = GetKeyboardMap(firstPlayerWithKeyboardMap, mapID);
		if (keyboardMap == null)
		{
			Debug.Log("P" + firstPlayerWithKeyboardMap.id + " keyboard map not found for mapID " + mapID);
			return KeyCode.None;
		}
		IEnumerator<ActionElementMap> enumerator = keyboardMap.AllMaps.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.actionId == actionID)
			{
				return enumerator.Current.keyCode;
			}
		}
		Debug.Log("No KeyCode found for actionID " + actionID + " in mapID " + mapID);
		return KeyCode.None;
	}

	private void ReplaceKeyboardMapWithCustomBindings(Rewired.Player rwPlayer, int mapID, KeyBindings keyBindings)
	{
		ControllerMap keyboardMap = GetKeyboardMap(rwPlayer, mapID);
		if (keyboardMap == null)
		{
			Debug.Log("P" + rwPlayer.id + " keyboard map not found for mapID " + mapID);
			return;
		}
		IEnumerator<ActionElementMap> enumerator = keyboardMap.AllMaps.GetEnumerator();
		while (enumerator.MoveNext())
		{
			switch (enumerator.Current.actionId)
			{
			case 2:
				enumerator.Current.keyCode = keyBindings.Dash;
				break;
			case 3:
				enumerator.Current.keyCode = keyBindings.Swipe;
				break;
			case 4:
				enumerator.Current.keyCode = keyBindings.Throw;
				break;
			case 5:
				enumerator.Current.keyCode = keyBindings.Back;
				break;
			case 0:
				if (enumerator.Current.axisContribution == Pole.Positive)
				{
					enumerator.Current.keyCode = keyBindings.MoveRight;
				}
				else
				{
					enumerator.Current.keyCode = keyBindings.MoveLeft;
				}
				break;
			case 1:
				if (enumerator.Current.axisContribution == Pole.Positive)
				{
					enumerator.Current.keyCode = keyBindings.MoveUp;
				}
				else
				{
					enumerator.Current.keyCode = keyBindings.MoveDown;
				}
				break;
			case 18:
				enumerator.Current.keyCode = keyBindings.UISubmit;
				break;
			case 19:
				enumerator.Current.keyCode = keyBindings.UICancel;
				break;
			case 24:
				enumerator.Current.keyCode = keyBindings.UITabLeft;
				break;
			case 25:
				if (keyBindings.UITabRight == KeyCode.None)
				{
					keyBindings.UITabRight = keyBindings.Swipe;
				}
				enumerator.Current.keyCode = keyBindings.UITabRight;
				break;
			case 23:
				if (keyBindings.UIRestore == KeyCode.None)
				{
					keyBindings.UIRestore = keyBindings.Throw;
				}
				enumerator.Current.keyCode = keyBindings.UIRestore;
				break;
			}
		}
		rwPlayer.controllers.maps.RemoveMap(ControllerType.Keyboard, 0, keyboardMap.categoryId, 0);
		rwPlayer.controllers.maps.AddMap(ControllerType.Keyboard, 0, keyboardMap);
	}

	private void RestoreDefaultKeyboardMaps(Rewired.Player rwPlayer)
	{
		rwPlayer.controllers.maps.LoadDefaultMaps(ControllerType.Keyboard);
		rwPlayer.controllers.maps.SetMapsEnabled(state: true, Rewired_MapID_Keyboard_UI);
	}

	public Rewired.Player GetFirstPlayerWithKeyboardMap(int mapID)
	{
		foreach (Rewired.Player player in ReInput.players.Players)
		{
			if (GetKeyboardMap(player, mapID) != null)
			{
				return player;
			}
		}
		return null;
	}

	public ControllerMap GetKeyboardMap(Rewired.Player rwPlayer, int mapID)
	{
		foreach (ControllerMap allMap in rwPlayer.controllers.maps.GetAllMaps())
		{
			if (allMap.controllerType == ControllerType.Keyboard && allMap.categoryId == mapID)
			{
				return allMap;
			}
		}
		return null;
	}

	public void LoadModifiedMatchSettings(Action<MatchSettings> OnMatchSettingsLoaded)
	{
		if (!demoMode)
		{
			OnMatchSettingsLoaded = (Action<MatchSettings>)Delegate.Combine(OnMatchSettingsLoaded, new Action<MatchSettings>(this.OnMatchSettingsLoaded));
			Singleton<DataManager>.Instance.LoadMatchSettingsFromDisk(OnMatchSettingsLoaded);
		}
	}

	private void OnMatchSettingsLoaded(MatchSettings loadedMatchSettings)
	{
		if (loadedMatchSettings == null)
		{
			loadedMatchSettings = new MatchSettings();
		}
		MatchSettings = (customMatchSettings = loadedMatchSettings);
	}

	public void SaveModifiedMatchSettings(Action<bool> OnMatchSettingsSaved)
	{
		Singleton<DataManager>.Instance.SaveMatchSettingsToDisk(customMatchSettings, OnMatchSettingsSaved);
	}

	public void RestoreDefaultMatchSettings(Action<bool> OnMatchSettingsSaved)
	{
		MatchSettings = (customMatchSettings = new MatchSettings());
		SaveModifiedMatchSettings(OnMatchSettingsSaved);
	}

	public void RestoreDefaultMaps(Action<bool> OnMapsSaved)
	{
		MatchSettings.useCustomMapsForDeathmatch = false;
		MatchSettings.useCustomMapsForGoldenDisc = false;
		MatchSettings.disabledDeathmatchLevels.Clear();
		MatchSettings.disabledGoldenDiscLevels.Clear();
		SaveModifiedMatchSettings(OnMapsSaved);
	}

	public void ToggleStealOpponentDiscs(bool toggle)
	{
		if (toggle)
		{
			MatchSettings.canStealOpponentDiscs = true;
			MatchSettings.dashDisarmsOpponents = true;
		}
		else
		{
			MatchSettings.canStealOpponentDiscs = false;
			MatchSettings.dashDisarmsOpponents = false;
		}
	}

	public void ToggleFriendlyFire(bool toggle)
	{
		if (toggle)
		{
			MatchSettings.friendlyFire = true;
		}
		else
		{
			MatchSettings.friendlyFire = false;
		}
	}

	public void PrepareMatchLength()
	{
		MatchLength matchLength = MatchSettings.matchLength;
		if (GameOptions.conventionMode)
		{
			matchLength = MatchLength.Short;
		}
		switch (matchLength)
		{
		case MatchLength.Short:
			if (matchType == MatchType.GoldenDisc)
			{
				matchScoreGoal = 5;
			}
			else if (matchType == MatchType.HideAndSeek)
			{
				matchScoreGoal = 3 * (numPlayers + numBots - 1) + 1;
			}
			else if (numPlayers + numBots < 3 || (GetVictoryCondition == VictoryCondition.Survival && !teamMatch))
			{
				matchScoreGoal = 10;
			}
			else if (teamMatch)
			{
				matchScoreGoal = 12;
			}
			else
			{
				matchScoreGoal = 15;
			}
			break;
		case MatchLength.Medium:
			if (matchType == MatchType.GoldenDisc)
			{
				matchScoreGoal = 7;
			}
			else if (matchType == MatchType.HideAndSeek)
			{
				matchScoreGoal = 4 * (numPlayers + numBots - 1) + 1;
			}
			else if (numPlayers + numBots < 3 || (GetVictoryCondition == VictoryCondition.Survival && !teamMatch))
			{
				matchScoreGoal = 14;
			}
			else if (teamMatch)
			{
				matchScoreGoal = 14;
			}
			else
			{
				matchScoreGoal = 16;
			}
			break;
		case MatchLength.Long:
			if (matchType == MatchType.GoldenDisc)
			{
				matchScoreGoal = 10;
			}
			else if (matchType == MatchType.HideAndSeek)
			{
				matchScoreGoal = 5 * (numPlayers + numBots - 1) + 1;
			}
			else if (numPlayers + numBots < 3 || (GetVictoryCondition == VictoryCondition.Survival && !teamMatch))
			{
				matchScoreGoal = 16;
			}
			else if (teamMatch)
			{
				matchScoreGoal = 20;
			}
			else
			{
				matchScoreGoal = 24;
			}
			break;
		}
	}

	public void SelectMatchType(int index)
	{
		switch (index)
		{
		case 0:
			matchType = MatchType.DeathMatch;
			teamMatch = false;
			break;
		case 1:
			matchType = MatchType.DeathMatch;
			teamMatch = true;
			break;
		case 2:
			if (debugMode)
			{
				matchType = MatchType.HideAndSeek;
				teamMatch = false;
			}
			else
			{
				matchType = MatchType.GoldenDisc;
				teamMatch = false;
			}
			break;
		}
	}

	public void SelectVictoryCondition(int index)
	{
		VictoryCondition victoryCondition = VictoryCondition.Kills;
		switch (index)
		{
		case 0:
			victoryCondition = VictoryCondition.Kills;
			break;
		case 1:
			victoryCondition = VictoryCondition.Survival;
			break;
		}
		if (teamMatch)
		{
			MatchSettings.victoryConditionTeamUp = victoryCondition;
		}
		else if (matchType == MatchType.GoldenDisc)
		{
			MatchSettings.victoryConditionGoldenBoomerang = victoryCondition;
		}
		else
		{
			MatchSettings.victoryConditionDeathMatch = victoryCondition;
		}
	}

	public void SelectBotDifficulty(int index)
	{
		switch (index)
		{
		case 0:
			botDifficulty = BotDifficulty.easy;
			break;
		case 1:
			botDifficulty = BotDifficulty.medium;
			break;
		case 2:
			botDifficulty = BotDifficulty.hard;
			break;
		case 3:
			botDifficulty = BotDifficulty.impossible;
			break;
		}
	}

	public void SelectNumberOfBots(int val)
	{
		numBots = val;
	}

	public void SetupGoldenDisc()
	{
		if (GetVictoryCondition == VictoryCondition.Survival)
		{
			goalTimeStart = 14f;
			if (Singleton<GameManager>.Instance.players.Count == 3)
			{
				goalTimeStart = 12f;
			}
			else if (Singleton<GameManager>.Instance.players.Count == 4)
			{
				goalTimeStart = 11f;
			}
			else if (Singleton<GameManager>.Instance.players.Count == 5)
			{
				goalTimeStart = 10f;
			}
			else if (Singleton<GameManager>.Instance.players.Count == 6)
			{
				goalTimeStart = 9f;
			}
		}
		else if (GetVictoryCondition == VictoryCondition.Kills)
		{
			goalTimeStart = 5f;
		}
	}

	public void SetupKingOfTheHill()
	{
		goalTimeStart = 5f;
		roundDuration = 45f;
	}

	public void LoadSystemLanguage()
	{
		string text = Application.systemLanguage.ToString();
		if (string.IsNullOrEmpty(text))
		{
			Debug.LogError("System language returned an empty string for some reason!");
			return;
		}
		Debug.Log("Load system language:" + text);
		ChangeLanguage(text);
	}

	public void ChangeLanguage(string newLanguage)
	{
		if (string.IsNullOrEmpty(newLanguage))
		{
			LoadSystemLanguage();
		}
		else if (LocalizationManager.HasLanguage(newLanguage))
		{
			LocalizationManager.CurrentLanguage = newLanguage;
			GameOptions.language = LocalizationManager.CurrentLanguage;
		}
		else
		{
			Debug.LogWarning("Language " + newLanguage + " not found in i2!");
		}
		_ = gameObjectLocalizations.Count;
		for (int i = 0; i < gameObjectLocalizations.Count; i++)
		{
			gameObjectLocalizations[i].UpdateLanguage();
		}
	}

	public string GetLanguageString(int value)
	{
		string result = "English";
		switch (value)
		{
		case 1:
			result = "Japanese";
			break;
		case 2:
			result = "ChineseSimplified";
			break;
		case 3:
			result = "ChineseTraditional";
			break;
		case 4:
			result = "Korean";
			break;
		case 5:
			result = "German";
			break;
		case 6:
			result = "French";
			break;
		case 7:
			result = "Italian";
			break;
		case 8:
			result = "Spanish";
			break;
		case 9:
			result = "SpanishSpain";
			break;
		case 10:
			result = "Portuguese";
			break;
		case 11:
			result = "PortuguesePT";
			break;
		case 12:
			result = "Turkish";
			break;
		case 13:
			result = "Polish";
			break;
		case 14:
			result = "Dutch";
			break;
		case 15:
			result = "Russian";
			break;
		case 16:
			result = "Ukranian";
			break;
		case 17:
			result = "Greek";
			break;
		case 18:
			result = "Catalan";
			break;
		}
		return result;
	}

	public int GetCurrentLanguageIndex()
	{
		switch (LocalizationManager.CurrentLanguage)
		{
		case "English":
			return 0;
		case "Japanese":
			return 1;
		case "ChineseSimplified":
			return 2;
		case "ChineseTraditional":
			return 3;
		case "Korean":
			return 4;
		case "German":
			return 5;
		case "French":
			return 6;
		case "Italian":
			return 7;
		case "Spanish":
			return 8;
		case "SpanishSpain":
			return 9;
		case "Portuguese":
			return 10;
		case "PortuguesePT":
			return 11;
		case "Turkish":
			return 12;
		case "Polish":
			return 13;
		case "Dutch":
			return 14;
		case "Russian":
			return 15;
		case "Ukranian":
			return 16;
		case "Greek":
			return 17;
		case "Catalan":
			return 18;
		default:
			Debug.LogWarning("Current language not found!");
			return 0;
		}
	}

	public void ApplyLocalizationOption()
	{
		ChangeLanguage(_gameOptions.language);
	}

	public void RegisterGameObjectLocalization(GameObjectLocalization goLoc)
	{
		gameObjectLocalizations.Add(goLoc);
	}

	public void LoadUnlockedItems(Action<UnlockedItems> OnUnlockedItemsLoadedCallback)
	{
		Singleton<DataManager>.Instance.LoadUnlockedItemsFromDisk(delegate(UnlockedItems unlockedItems)
		{
			OnUnlockedItemsLoaded(unlockedItems);
			OnUnlockedItemsLoadedCallback?.Invoke(unlockedItems);
		});
	}

	private void OnUnlockedItemsLoaded(UnlockedItems loadedUnlockedItems)
	{
		if (loadedUnlockedItems != null)
		{
			UnlockedItems = loadedUnlockedItems;
		}
		else if (UnlockedItems == null)
		{
			Debug.Log("WARNING - all the unlocked data was null, creating default UnlockedItems object - this should not occur ");
			UnlockedItems = new UnlockedItems();
		}
	}

	public void SaveUnlockedItems(Action<bool> OnUnlockedItemsSaved)
	{
		Singleton<DataManager>.Instance.SaveUnlockedItemsToDisk(UnlockedItems, OnUnlockedItemsSaved);
	}
}
