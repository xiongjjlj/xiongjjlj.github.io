using System.Collections;
using DG.Tweening;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UILobbyPlayer : UIPlayer
{
	public enum InputDevice
	{
		None,
		Joystick,
		Keyboard,
		KeyboardTwo
	}

	public enum ReadyState
	{
		Disabled,
		Available,
		Selecting,
		Ready
	}

	public InputDevice inputDevice;

	private UIMenuLobby uiMenuLobby;

	private bool isSelectingTeam;

	public Sprite circleSpriteDashed;

	public Sprite circleSpriteSolid;

	public Sprite circleSpriteFilled;

	public Sprite circleSpriteLocked;

	private Color circleColorSelecting = new Color(0.85f, 0.85f, 0.85f, 1f);

	protected UIPlayerIcon playerIcon;

	private GameObject joinPrompt;

	private GameObject readyPrompt;

	private GameObject lockedPrompt;

	private GameObject lockedOverlay;

	public UIDotNavigation hatNavigation;

	private static Vector3 modelEulerAngles = new Vector3(80f, 0f, 90f);

	public ReadyState readyState;

	public UICharacterGridCell currentGridCell;

	private bool inputDashBlocked;

	private bool controllerIsVibrating;

	private bool xAxisInUse;

	private bool yAxisInUse;

	private static float deadZone = 0.2f;

	private IEnumerator vibrationRoutine;

	public UnityEvent OnBackButtonPressed = new UnityEvent();

	public static string joinAction = "Dash";

	public static string joinActionSwitch = "UISubmit";

	private static string backAction = "Back";

	private static string backActionSwitch = "UICancel";

	public override void SetupUIPlayer(int playerID)
	{
		uiMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		base.SetupUIPlayer(playerID);
		circle.color = circleColorSelecting;
		circle.sprite = circleSpriteDashed;
		joinPrompt = circle.transform.Find("JoinPrompt").gameObject;
		readyPrompt = circle.transform.Find("ReadyPrompt").gameObject;
		lockedPrompt = circle.transform.Find("LockedPrompt").gameObject;
		lockedOverlay = circle.transform.Find("LockedOverlay").gameObject;
		lockedOverlay.SetActive(value: false);
		if (playerIcon == null)
		{
			playerIcon = Object.Instantiate(uiMenuLobby.lobbyPlayerPlayerIconPrefab, circle.transform);
			playerIcon.SetColor(circleColorSelecting * 0.9f);
		}
		ScheduleLayoutUpdate();
		isSelectingTeam = false;
	}

	public override void Reset()
	{
		base.Reset();
		rewiredPlayer = ReInput.players.GetPlayer(playerID);
		playerType = PlayerType.Human;
		playerIcon.SetPlayer(playerID, playerType);
		AssignInputDevice();
		isSelectingTeam = false;
		controllerIsVibrating = false;
		if (readyState != ReadyState.Disabled)
		{
			SetReadyState(ReadyState.Available);
		}
	}

	public void AssignInputDevice()
	{
		inputDevice = InputDevice.None;
		if (rewiredPlayer.controllers.joystickCount >= 1)
		{
			inputDevice = InputDevice.Joystick;
		}
		else
		{
			rewiredPlayer.controllers.maps.SetMapsEnabled(state: false, SettingsManager.Rewired_MapID_Keyboard_One);
			rewiredPlayer.controllers.maps.SetMapsEnabled(state: false, SettingsManager.Rewired_MapID_Keyboard_Two);
			if (Singleton<SettingsManager>.Instance.GameOptions.secondKeyboardAllowed)
			{
				if (AssignKeyboardMapIfAvailable(SettingsManager.Rewired_MapID_Keyboard_One))
				{
					inputDevice = InputDevice.Keyboard;
				}
				else if (AssignKeyboardMapIfAvailable(SettingsManager.Rewired_MapID_Keyboard_Two))
				{
					inputDevice = InputDevice.KeyboardTwo;
				}
				else
				{
					rewiredPlayer.controllers.hasKeyboard = false;
					inputDevice = InputDevice.None;
				}
			}
			else if (AssignKeyboardMapIfAvailable(SettingsManager.Rewired_MapID_Keyboard_One))
			{
				AssignKeyboardMapIfAvailable(SettingsManager.Rewired_MapID_Keyboard_Two);
				inputDevice = InputDevice.Keyboard;
			}
			else
			{
				rewiredPlayer.controllers.hasKeyboard = false;
				inputDevice = InputDevice.None;
			}
		}
		rewiredPlayer.controllers.hasKeyboard = inputDevice == InputDevice.Keyboard || inputDevice == InputDevice.KeyboardTwo;
		if (playerType == PlayerType.Human && inputDevice == InputDevice.None)
		{
			SetReadyState(ReadyState.Disabled, skipAnimation: true);
			SelectGridCell(null);
		}
		else if (playerType == PlayerType.Human && (inputDevice == InputDevice.Keyboard || inputDevice == InputDevice.KeyboardTwo) && ReInput.controllers.joystickCount > 3)
		{
			if (uiMenuLobby != null)
			{
				uiMenuLobby.keyboardPlayersAreHidden = true;
			}
			SetReadyState(ReadyState.Disabled, skipAnimation: true);
			SelectGridCell(null);
		}
		else if (inputDevice != InputDevice.None && playerType == PlayerType.Human && readyState == ReadyState.Disabled)
		{
			SetReadyState(ReadyState.Available);
		}
		if (inputDevice != InputDevice.None)
		{
			if (joinPrompt != null)
			{
				joinPrompt.GetComponentInChildren<ButtonPrompt>().Init(rewiredPlayer, Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction);
			}
			if (readyPrompt != null)
			{
				readyPrompt.GetComponentInChildren<ButtonPrompt>().Init(rewiredPlayer, Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction);
			}
		}
	}

	private bool AssignKeyboardMapIfAvailable(int mapID)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC)
		{
			return false;
		}
		for (int i = 0; i < ReInput.players.Players.Count; i++)
		{
			if (ReInput.players.Players[i] == rewiredPlayer || !ReInput.players.Players[i].controllers.hasKeyboard)
			{
				continue;
			}
			foreach (ControllerMap map in ReInput.players.Players[i].controllers.maps.GetMaps(ControllerType.Keyboard, 0))
			{
				if (map.categoryId == mapID && map.enabled)
				{
					return false;
				}
			}
		}
		rewiredPlayer.controllers.maps.SetMapsEnabled(state: true, mapID);
		return true;
	}

	public void SetReadyState(ReadyState newState, bool skipAnimation = false)
	{
		if (newState == ReadyState.Disabled)
		{
			playerIcon.SetActive(setActive: false);
			circle.transform.DOKill();
			if (skipAnimation)
			{
				base.gameObject.SetActive(value: false);
			}
			else
			{
				circle.transform.DOScale(Vector3.one * 0.25f, 0.1f).OnComplete(Disable);
			}
		}
		else if (readyState == ReadyState.Disabled)
		{
			base.gameObject.SetActive(value: true);
			circle.transform.DOKill();
			circle.transform.localScale = Vector3.one;
			circle.transform.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
		}
		else if (newState == ReadyState.Ready)
		{
			circle.transform.DOKill();
			circle.transform.localScale = Vector3.one;
			circle.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 2, 0f);
		}
		if (readyState == newState)
		{
			return;
		}
		switch (newState)
		{
		case ReadyState.Available:
			if (joinPrompt != null)
			{
				joinPrompt.SetActive(value: true);
			}
			if (readyPrompt != null)
			{
				readyPrompt.SetActive(value: false);
			}
			if (lockedPrompt != null)
			{
				lockedPrompt.SetActive(value: false);
			}
			SelectGridCell(null);
			circle.color = circleColorSelecting;
			circle.sprite = circleSpriteDashed;
			playerIcon.SetActive(setActive: false);
			SetCharacterModel(null);
			hatNavigation.Hide();
			break;
		case ReadyState.Selecting:
			if (joinPrompt != null)
			{
				joinPrompt.SetActive(value: false);
			}
			playerIcon.SetActive(setActive: true);
			if (currentGridCell != null)
			{
				SelectGridCell(currentGridCell);
			}
			else if (uiMenuLobby != null)
			{
				UICharacterGridCell randomAvailableGridCell = uiMenuLobby.GetRandomAvailableGridCell();
				if (randomAvailableGridCell != null)
				{
					SelectGridCell(randomAvailableGridCell);
				}
			}
			if (readyState != ReadyState.Ready)
			{
				characterOffset.transform.DOScale(0.75f, 0.1f).SetEase(Ease.OutExpo).From(isRelative: false);
			}
			if (character != null)
			{
				character.anim.SetTrigger("Idle");
				character.SetFacialExpression(character.idleFace);
			}
			break;
		case ReadyState.Ready:
			if (joinPrompt != null)
			{
				joinPrompt.SetActive(value: false);
			}
			if (readyPrompt != null)
			{
				readyPrompt.SetActive(value: false);
			}
			if (lockedPrompt != null)
			{
				lockedPrompt.SetActive(value: false);
			}
			playerIcon.SetActive(setActive: true);
			if (currentGridCell != null)
			{
				currentGridCell.OnReady(this);
				circle.color = currentGridCell.backgroundColorCharacter;
				circle.sprite = circleSpriteFilled;
				playerIcon.SetColor(currentGridCell.borderColor);
				if (character != null)
				{
					character.anim.ResetTrigger("Idle");
					character.anim.SetTrigger("Wave");
					character.SetFacialExpression(character.victoryFace);
				}
			}
			hatNavigation.Hide();
			break;
		}
		readyState = newState;
		if (uiMenuLobby != null)
		{
			UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
			if (currentMenu != null && currentMenu == uiMenuLobby)
			{
				uiMenuLobby.CheckIfAllPlayersAreReady();
			}
		}
	}

	protected override void Disable()
	{
		if (readyState == ReadyState.Disabled)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void SetBot(UICharacterGridCell gridCell)
	{
		base.gameObject.SetActive(value: true);
		playerType = PlayerType.Bot;
		playerIcon.SetPlayer(playerID, playerType);
		if (uiMenuLobby != null)
		{
			if (gridCell == null)
			{
				UICharacterGridCell randomAvailableGridCell = uiMenuLobby.GetRandomAvailableGridCell();
				if (randomAvailableGridCell != null)
				{
					SelectGridCell(randomAvailableGridCell);
				}
			}
			else
			{
				SelectGridCell(gridCell);
			}
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.botsWearHats)
		{
			character.equippedHat = Singleton<GameManager>.Instance.GetRandomUnlockedHatForCharacter(character.characterType);
			int numUnlockedHatsForCharacter = Singleton<GameManager>.Instance.GetNumUnlockedHatsForCharacter(character.characterType);
			for (int i = 0; i < numUnlockedHatsForCharacter + 1; i++)
			{
				if (!IsAnotherPlayingWearingMyHat() && Singleton<GameManager>.Instance.IsHatUnlockedForCharacter(character.characterType, character.equippedHat))
				{
					break;
				}
				if (i == numUnlockedHatsForCharacter)
				{
					character.equippedHat = HatType.None;
				}
				else
				{
					character.equippedHat = Singleton<GameManager>.Instance.GetNextUnlockedHatForCharacter(character.characterType, character.equippedHat);
				}
			}
			character.SetHat();
		}
		SetReadyState(ReadyState.Ready);
	}

	private bool IsAnotherPlayingWearingMyHat()
	{
		for (int i = 0; i < uiMenuLobby.uiPlayers.Length; i++)
		{
			if (!(uiMenuLobby.uiPlayers[i] == this) && uiMenuLobby.uiPlayers[i].readyState == ReadyState.Ready && uiMenuLobby.uiPlayers[i].character.equippedHat == character.equippedHat)
			{
				return true;
			}
		}
		return false;
	}

	public override void SetTeam(int team)
	{
		bool flag = false;
		if (base.team != team)
		{
			flag = true;
			if (team != 0)
			{
				characterOffset.transform.localPosition = Vector3.zero;
				if (team == 1)
				{
					characterOffset.transform.DOLocalMove(new Vector2(16f, 0f), 0.15f).From(isRelative: false);
				}
				else
				{
					characterOffset.transform.DOLocalMove(new Vector2(-16f, 0f), 0.15f).From(isRelative: false);
				}
			}
		}
		base.SetTeam(team);
		if (currentGridCell != null && team == 0)
		{
			if (readyState == ReadyState.Ready)
			{
				circle.color = currentGridCell.backgroundColorCharacter;
			}
			else
			{
				circle.color = circleColorSelecting;
			}
			playerIcon.SetColor(currentGridCell.borderColor);
		}
		else
		{
			switch (team)
			{
			case 1:
				circle.color = Singleton<ColorManager>.Instance.GetTeamBackgroundColor(team);
				playerIcon.SetColor(character.colorTeamOne);
				break;
			case 2:
				circle.color = Singleton<ColorManager>.Instance.GetTeamBackgroundColor(team);
				playerIcon.SetColor(character.colorTeamTwo);
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		UIMenuMatchSettings uIMenuMatchSettings = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.MatchSettings) as UIMenuMatchSettings;
		if (uIMenuMatchSettings != null)
		{
			UIMatchSettingTeamsModule uIMatchSettingTeamsModule = uIMenuMatchSettings.GetCurrentMatchSettingModule() as UIMatchSettingTeamsModule;
			if (uIMatchSettingTeamsModule != null)
			{
				uIMatchSettingTeamsModule.AssignPlayerToTeam(this);
			}
		}
	}

	private void Update()
	{
		if (inputDevice == InputDevice.None || !Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents)
		{
			return;
		}
		UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
		if (playerType == PlayerType.Human && currentMenu != null && currentMenu.MenuID == UIMenuBase.MenuType.MatchSettings)
		{
			UIMenuMatchSettings uIMenuMatchSettings = currentMenu as UIMenuMatchSettings;
			if (uIMenuMatchSettings != null && uIMenuMatchSettings.GetCurrentMatchSettingModuleType() == UIMatchSettingModule.MatchSetting.Teams)
			{
				if (rewiredPlayer.GetAxis("Move Horizontal") < 0f - deadZone && !xAxisInUse && !yAxisInUse)
				{
					SetTeam(1);
				}
				else if (rewiredPlayer.GetAxis("Move Horizontal") > deadZone && !xAxisInUse && !yAxisInUse)
				{
					SetTeam(2);
				}
				UIMatchSettingTeamsModule uIMatchSettingTeamsModule = uIMenuMatchSettings.GetCurrentMatchSettingModule() as UIMatchSettingTeamsModule;
				if (uIMatchSettingTeamsModule != null && rewiredPlayer.GetButtonDown("UIRestore"))
				{
					uIMatchSettingTeamsModule.ToggleBotTeams();
				}
				ChangeMovementAxesIntoButtons();
			}
			return;
		}
		if (currentMenu != null && currentMenu.MenuID == UIMenuBase.MenuType.Lobby)
		{
			if (playerType == PlayerType.Bot && (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction) || rewiredPlayer.GetButtonDown("Start")))
			{
				Reset();
				if (uiMenuLobby != null)
				{
					uiMenuLobby.UpdateBotSliders();
				}
				return;
			}
			if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch && rewiredPlayer.GetButtonDown("Start") && uiMenuLobby != null && Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(uiMenuLobby.switchAppletTimestamp) > UIMenuLobby.switchAppletDelay)
			{
				if (Singleton<UIManager>.Instance.ShowSwitchControllerApplet(6))
				{
					currentMenu.UpdateMasterButtonPrompts();
				}
				uiMenuLobby.switchAppletTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
			}
			switch (readyState)
			{
			case ReadyState.Available:
				if (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction))
				{
					StartVibration(0.5f, 0.2f);
					SetReadyState(ReadyState.Selecting);
					inputDashBlocked = true;
					if (uiMenuLobby != null)
					{
						uiMenuLobby.UpdateMasterButtonPrompts();
					}
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/join", "playerID", playerID);
				}
				if ((rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? backActionSwitch : backAction) || (inputDevice != InputDevice.KeyboardTwo && rewiredPlayer.GetButtonDown("UICancel"))) && !uiMenuLobby.AreAnyPlayersReadyOrSelecting())
				{
					OnBackButtonPressed.Invoke();
				}
				if ((rewiredPlayer.GetAxis("Move Horizontal") < 0f - deadZone || rewiredPlayer.GetAxis("Move Horizontal") > deadZone || rewiredPlayer.GetAxis("Move Vertical") > deadZone || rewiredPlayer.GetAxis("Move Vertical") < 0f - deadZone) && !xAxisInUse && !yAxisInUse)
				{
					circle.transform.DOKill();
					circle.transform.localScale = Vector3.one;
					circle.transform.DOPunchScale(Vector3.one * 0.075f, 0.1f, 2, 0f);
					Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/nope");
				}
				break;
			case ReadyState.Selecting:
			{
				if (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction))
				{
					if (currentGridCell != null && Singleton<GameManager>.Instance.IsCharacterUnlocked(currentGridCell.currentCharacter.characterType))
					{
						StartVibration(0.75f, 0.2f);
						SetReadyState(ReadyState.Ready);
						Singleton<GameManager>.Instance.SaveLastSelectedHat(character.characterType, character.equippedHat);
						inputDashBlocked = true;
						Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/ready", "playerID", playerID);
						if (uiMenuLobby != null)
						{
							uiMenuLobby.UpdateHatPrompt();
							uiMenuLobby.UpdateAltCharacterPrompt();
						}
					}
					else if (currentGridCell != null)
					{
						StartVibration(0.75f, 0.1f);
						lockedPrompt.transform.DOComplete();
						lockedPrompt.transform.DOPunchPosition(new Vector3(-15f, 0f, 0f), 0.4f, 15);
						Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/nope", "playerID", playerID);
					}
				}
				if (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? backActionSwitch : backAction) || (inputDevice != InputDevice.KeyboardTwo && rewiredPlayer.GetButtonDown("UICancel")))
				{
					SetReadyState(ReadyState.Available);
					if (uiMenuLobby != null)
					{
						uiMenuLobby.UpdateMasterButtonPrompts();
					}
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/back", "playerID", playerID);
				}
				if (rewiredPlayer.GetButtonDown(23) && currentGridCell != null && currentGridCell.altCharacter != null && Singleton<GameManager>.Instance.IsCharacterUnlocked(currentGridCell.altCharacter.characterType))
				{
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/alt_character", "playerID", playerID);
					SelectAltGridCell(currentGridCell);
					SlideCharacterModelFromDirection(MoveDirection.Right);
					if (uiMenuLobby != null)
					{
						uiMenuLobby.UpdateHatPrompt();
						uiMenuLobby.UpdateAltCharacterPrompt();
					}
				}
				if (uiMenuLobby != null && currentGridCell != null)
				{
					UICharacterGridCell uICharacterGridCell = null;
					if (rewiredPlayer.GetAxis("Move Horizontal") < 0f - deadZone && !xAxisInUse && !yAxisInUse)
					{
						uICharacterGridCell = uiMenuLobby.GetNextAvailableGridCell(currentGridCell, MoveDirection.Left);
						if (uICharacterGridCell != null)
						{
							SelectGridCell(uICharacterGridCell);
							SlideCharacterModelFromDirection(MoveDirection.Left);
							Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/select", "playerID", playerID);
						}
						else
						{
							currentGridCell.JiggleInDirection(MoveDirection.Left);
						}
					}
					else if (rewiredPlayer.GetAxis("Move Horizontal") > deadZone && !xAxisInUse && !yAxisInUse)
					{
						uICharacterGridCell = uiMenuLobby.GetNextAvailableGridCell(currentGridCell, MoveDirection.Right);
						if (uICharacterGridCell != null)
						{
							SelectGridCell(uICharacterGridCell);
							SlideCharacterModelFromDirection(MoveDirection.Right);
							Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/select", "playerID", playerID);
						}
						else
						{
							currentGridCell.JiggleInDirection(MoveDirection.Right);
						}
					}
					if (rewiredPlayer.GetAxis("Move Vertical") > deadZone && !xAxisInUse && !yAxisInUse)
					{
						uICharacterGridCell = uiMenuLobby.GetNextAvailableGridCell(currentGridCell, MoveDirection.Up);
						if (uICharacterGridCell != null)
						{
							SelectGridCell(uICharacterGridCell);
							SlideCharacterModelFromDirection(MoveDirection.Up);
							Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/select", "playerID", playerID);
						}
						else
						{
							currentGridCell.JiggleInDirection(MoveDirection.Up);
						}
					}
					else if (rewiredPlayer.GetAxis("Move Vertical") < 0f - deadZone && !xAxisInUse && !yAxisInUse)
					{
						uICharacterGridCell = uiMenuLobby.GetNextAvailableGridCell(currentGridCell, MoveDirection.Down);
						if (uICharacterGridCell != null)
						{
							SelectGridCell(uICharacterGridCell);
							SlideCharacterModelFromDirection(MoveDirection.Down);
							Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/select", "playerID", playerID);
						}
						else
						{
							currentGridCell.JiggleInDirection(MoveDirection.Down);
						}
					}
				}
				if ((!rewiredPlayer.GetButtonDown(24) && !rewiredPlayer.GetButtonDown(25)) || !(character != null))
				{
					break;
				}
				int num = Singleton<GameManager>.Instance.NumHatsUnlockedForCharacter(character.characterType);
				int unlockedHatIndex = Singleton<GameManager>.Instance.GetUnlockedHatIndex(character.characterType, character.equippedHat);
				if (num <= 1)
				{
					hatNavigation.FlashCross();
					break;
				}
				HatType hatType = HatType.None;
				bool flag = false;
				if (rewiredPlayer.GetButtonDown(24))
				{
					if (unlockedHatIndex > 0)
					{
						hatType = Singleton<GameManager>.Instance.GetPreviousUnlockedHatForCharacter(character.characterType, character.equippedHat);
						hatNavigation.MoveLeft();
						Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/change_hat_left");
					}
					else
					{
						hatType = Singleton<GameManager>.Instance.GetLastUnlockedHatForCharacter(character.characterType);
						hatNavigation.JumpToEnd(Singleton<GameManager>.Instance.GetUnlockedHatIndex(character.characterType, hatType));
						Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/change_hat_right");
					}
					flag = true;
				}
				else if (rewiredPlayer.GetButton(25))
				{
					if (unlockedHatIndex < num - 1)
					{
						hatType = Singleton<GameManager>.Instance.GetNextUnlockedHatForCharacter(character.characterType, character.equippedHat);
						hatNavigation.MoveRight();
						flag = true;
						Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/change_hat_right");
					}
					else
					{
						hatType = HatType.None;
						hatNavigation.JumpToStart();
						Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/change_hat_left");
					}
					flag = true;
				}
				if (flag)
				{
					character.equippedHat = hatType;
					character.SetHat();
					character.BounceCurrentHat();
					StartVibration(0.75f, 0.1f);
					if (hatType == HatType.None && uiMenuLobby != null)
					{
						uiMenuLobby.PlayChangeHatPFX(character.GetHatInstancePos());
					}
					Singleton<GameManager>.Instance.SaveLastSelectedHat(character.characterType, character.equippedHat);
				}
				break;
			}
			case ReadyState.Ready:
				if (playerType == PlayerType.Bot)
				{
					break;
				}
				if (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction))
				{
					inputDashBlocked = false;
					if (uiMenuLobby != null)
					{
						uiMenuLobby.AreAllPlayersReady();
					}
					if (uiMenuLobby != null && uiMenuLobby.AreMoreBotsRequired())
					{
						StartVibration(0.75f, 0.1f);
						uiMenuLobby.ShakeNumBotsSlider();
					}
				}
				if (!inputDashBlocked && rewiredPlayer.GetButton(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? joinActionSwitch : joinAction) && uiMenuLobby != null && uiMenuLobby.AreAllPlayersReady() && !uiMenuLobby.AreMoreBotsRequired())
				{
					if (uiMenuLobby.ReadyCharge < UIMenuLobby.readyChargeDuration)
					{
						uiMenuLobby.ReadyCharge += Time.unscaledDeltaTime;
						SetVibration(Easing.EaseOut(uiMenuLobby.ReadyCharge / UIMenuLobby.readyChargeDuration) * 0.5f);
					}
					if (uiMenuLobby.ReadyCharge >= UIMenuLobby.readyChargeDuration)
					{
						StopVibration();
					}
				}
				else
				{
					StopVibration();
				}
				if (rewiredPlayer.GetButtonDown(Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(rewiredPlayer) ? backActionSwitch : backAction) || (inputDevice != InputDevice.KeyboardTwo && rewiredPlayer.GetButtonDown("UICancel")))
				{
					SetReadyState(ReadyState.Selecting);
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/back", "playerID", playerID);
					if (uiMenuLobby != null)
					{
						uiMenuLobby.UpdateHatPrompt();
						uiMenuLobby.UpdateAltCharacterPrompt();
					}
				}
				break;
			}
		}
		ChangeMovementAxesIntoButtons();
	}

	private bool IsMasterPlayer()
	{
		Rewired.Player currentMasterPlayer = Singleton<UIManager>.Instance.GetCurrentMasterPlayer();
		if (currentMasterPlayer == null || rewiredPlayer == null)
		{
			return false;
		}
		if (currentMasterPlayer == rewiredPlayer)
		{
			return true;
		}
		return false;
	}

	private void ChangeMovementAxesIntoButtons()
	{
		if (rewiredPlayer != null)
		{
			xAxisInUse = Mathf.Abs(rewiredPlayer.GetAxis("Move Horizontal")) > deadZone;
			yAxisInUse = Mathf.Abs(rewiredPlayer.GetAxis("Move Vertical")) > deadZone;
		}
	}

	private void SelectGridCell(UICharacterGridCell targetGridCell)
	{
		if (currentGridCell != null && targetGridCell != currentGridCell)
		{
			currentGridCell.OnDeselect(this);
		}
		if (targetGridCell != null)
		{
			targetGridCell.OnSelect(this);
			if (character == null || targetGridCell != currentGridCell)
			{
				SetCharacterModel(targetGridCell.currentCharacter);
			}
			currentGridCell = targetGridCell;
			if (playerIcon != null)
			{
				playerIcon.SetColor(targetGridCell.borderColor);
			}
			circle.color = circleColorSelecting;
			if (targetGridCell.lockState == UICharacterGridCell.LockState.Unlocked)
			{
				circle.sprite = circleSpriteDashed;
				lockedOverlay.SetActive(value: false);
			}
			else
			{
				circle.sprite = circleSpriteLocked;
				lockedOverlay.SetActive(value: true);
			}
			playerIcon.SetColor(targetGridCell.borderColor);
			if (readyPrompt != null)
			{
				readyPrompt.SetActive(targetGridCell.lockState == UICharacterGridCell.LockState.Unlocked);
			}
			if (lockedPrompt != null)
			{
				lockedPrompt.SetActive(targetGridCell.lockState == UICharacterGridCell.LockState.Locked);
			}
			if (uiMenuLobby != null)
			{
				uiMenuLobby.UpdateHatPrompt();
				uiMenuLobby.UpdateAltCharacterPrompt();
			}
			hatNavigation.Show(Singleton<GameManager>.Instance.NumHatsUnlockedForCharacter(targetGridCell.currentCharacter.characterType), Singleton<GameManager>.Instance.GetUnlockedHatIndex(character.characterType, character.equippedHat));
		}
		else
		{
			currentGridCell = null;
		}
	}

	private void SelectAltGridCell(UICharacterGridCell targetGridCell)
	{
		if (!(targetGridCell == null))
		{
			targetGridCell.OnSelectAltCharacter();
			SetCharacterModel(targetGridCell.currentCharacter);
			currentGridCell = targetGridCell;
			if (playerIcon != null)
			{
				playerIcon.SetColor(targetGridCell.borderColor);
			}
			circle.color = circleColorSelecting;
			if (targetGridCell.lockState == UICharacterGridCell.LockState.Unlocked)
			{
				circle.sprite = circleSpriteDashed;
				lockedOverlay.SetActive(value: false);
			}
			else
			{
				circle.sprite = circleSpriteLocked;
				lockedOverlay.SetActive(value: true);
			}
			playerIcon.SetColor(targetGridCell.borderColor);
			if (readyPrompt != null)
			{
				readyPrompt.SetActive(targetGridCell.lockState == UICharacterGridCell.LockState.Unlocked);
			}
			if (lockedPrompt != null)
			{
				lockedPrompt.SetActive(targetGridCell.lockState == UICharacterGridCell.LockState.Locked);
			}
			if (uiMenuLobby != null)
			{
				uiMenuLobby.UpdateAltCharacterPrompt();
			}
			hatNavigation.Show(Singleton<GameManager>.Instance.NumHatsUnlockedForCharacter(targetGridCell.currentCharacter.characterType), Singleton<GameManager>.Instance.GetUnlockedHatIndex(character.characterType, character.equippedHat));
			targetGridCell.BootAllOtherPlayersExceptMe(this);
		}
	}

	private void SlideCharacterModelFromDirection(MoveDirection direction)
	{
		Vector2 vector = Vector2.zero;
		switch (direction)
		{
		case MoveDirection.Left:
			vector = new Vector2(-1f, 0f) * 16f;
			break;
		case MoveDirection.Right:
			vector = new Vector2(1f, 0f) * 16f;
			break;
		case MoveDirection.Up:
			vector = new Vector2(0f, 1f) * 8f;
			break;
		case MoveDirection.Down:
			vector = new Vector2(0f, -1f) * 8f;
			break;
		}
		characterOffset.transform.localPosition = Vector3.zero;
		characterOffset.transform.DOLocalMove(vector, 0.15f).From(isRelative: false);
	}

	public void SelectClosestAvailableGridCell(UICharacterGridCell currentGridCell)
	{
		if (!(currentGridCell == null) && !(uiMenuLobby == null))
		{
			UICharacterGridCell closestAvailableGridCell = uiMenuLobby.GetClosestAvailableGridCell(currentGridCell);
			if (closestAvailableGridCell != null)
			{
				SelectGridCell(closestAvailableGridCell);
			}
		}
	}

	public void StartVibration(float intensity, float duration)
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled)
		{
			vibrationRoutine = Vibrate(intensity, duration);
			StartCoroutine(vibrationRoutine);
		}
	}

	private IEnumerator Vibrate(float intensity, float duration)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			intensity *= 0.5f;
			duration *= 0.25f;
		}
		foreach (Joystick j in rewiredPlayer.controllers.Joysticks)
		{
			if (j.supportsVibration)
			{
				j.SetVibration(intensity, intensity);
				if (duration > 0f)
				{
					yield return new WaitForSecondsRealtime(duration);
					j.SetVibration(0f, 0f);
				}
				else
				{
					yield return new WaitForEndOfFrame();
				}
			}
		}
	}

	public void SetVibration(float intensity)
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled)
		{
			return;
		}
		foreach (Joystick joystick in rewiredPlayer.controllers.Joysticks)
		{
			if (joystick.supportsVibration)
			{
				joystick.SetVibration(intensity, intensity);
			}
		}
		controllerIsVibrating = true;
	}

	public void StopVibration()
	{
		if (!controllerIsVibrating)
		{
			return;
		}
		foreach (Joystick joystick in rewiredPlayer.controllers.Joysticks)
		{
			if (joystick.supportsVibration)
			{
				joystick.SetVibration(0f, 0f);
			}
		}
		controllerIsVibrating = false;
	}

	protected override void ScheduleLayoutUpdateForAllPlayers()
	{
		if (uiMenuLobby == null || uiMenuLobby.uiPlayers == null)
		{
			return;
		}
		for (int i = 0; i < uiMenuLobby.uiPlayers.Length; i++)
		{
			if (uiMenuLobby.uiPlayers[i].readyState != ReadyState.Disabled)
			{
				uiMenuLobby.uiPlayers[i].ScheduleLayoutUpdate();
			}
		}
	}

	protected override void UpdateLayout()
	{
		if (!(playerIcon == null) && !(circle == null))
		{
			RectTransform component = circle.GetComponent<RectTransform>();
			playerIcon.SetAnchoredPosition(VectorExt.GetPointOnCircle(135f, component.rect.size.x / 2f));
			base.UpdateLayout();
		}
	}
}
