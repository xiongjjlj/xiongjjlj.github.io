using System.Collections;
using DG.Tweening;
using I2.Loc;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMenuLobby : UIMenuBase
{
	public UILobbyPlayer[] uiPlayers;

	public UICharacterGridCell[] characterGridCells;

	public bool keyboardPlayersAreHidden;

	[Header("Canvases")]
	private RectTransform rt;

	public Canvas headerCanvas;

	public Canvas uiPlayersCanvas;

	public Canvas characterGridCanvas;

	public Canvas fightPromptCanvas;

	public Canvas footerCanvas;

	[Header("Fight prompt")]
	public CanvasGroup fightPromptCanvasGroup;

	public RectTransform fightPromptContainer;

	public RectTransform fightPromptBackground;

	public UISliderButton numBotsSlider;

	public UISliderButton botDifficultySlider;

	private Tweener uiPlayersScaleTweener;

	private Tweener uiPlayersYPosTweener;

	private Tweener fightPromptBackgroundShowTween;

	private Tweener fightPromptContainerShowTween;

	[Header("Button prompts")]
	public Transform headerTitle;

	public ButtonPrompt addBotButtonPrompt;

	public ButtonPrompt removeBotButtonPrompt;

	public UIFillButton continueFillPrompt;

	public MultiButtonPrompt hatPrompt;

	public MultiButtonPrompt altCharacterPrompt;

	private static int numGridColumns = 6;

	private float timeLastCharged;

	[SerializeField]
	private float _readyCharge;

	public static float readyChargeDuration = 1f;

	private bool isReadyFullyCharged;

	private bool wasReadyChargedThisFrame;

	public static float switchAppletDelay = 0.04f;

	public float switchAppletTimestamp;

	[Header("Prefabs")]
	public UICharacterGridPlayerIcon characterGridPlayerIconPrefab;

	public UIPlayerIcon lobbyPlayerPlayerIconPrefab;

	[Header("PFX")]
	public ParticleSystem changeHatPFX;

	public float ReadyCharge
	{
		get
		{
			return _readyCharge;
		}
		set
		{
			if (isReadyFullyCharged)
			{
				return;
			}
			if (value > _readyCharge)
			{
				wasReadyChargedThisFrame = true;
			}
			_readyCharge = value;
			if (continueFillPrompt != null)
			{
				continueFillPrompt.SetFill(Mathf.Clamp01(_readyCharge));
			}
			if (_readyCharge > readyChargeDuration)
			{
				isReadyFullyCharged = true;
				Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
				for (int i = 0; i < uiPlayers.Length; i++)
				{
					uiPlayers[i].StopVibration();
				}
				continueFillPrompt.Flash();
				StartCoroutine(UIResponse_Continue());
			}
		}
	}

	private void OnEnable()
	{
		numBotsSlider.OnValueChanged.AddListener(OnChangeNumBotsSlider);
		botDifficultySlider.OnValueChanged.AddListener(OnChangeBotDifficultySlider);
	}

	private void OnDisable()
	{
	}

	public override void Setup()
	{
		base.Setup();
		rt = GetComponent<RectTransform>();
		for (int i = 0; i < characterGridCells.Length; i++)
		{
			if (i < Singleton<GameManager>.Instance.characters.Length)
			{
				characterGridCells[i].SetupCharacter(Singleton<GameManager>.Instance.characters[i]);
				characterGridCells[i].index = i;
			}
		}
		for (int j = 0; j < uiPlayers.Length; j++)
		{
			uiPlayers[j].SetupUIPlayer(j);
		}
		ResetAllPlayers();
		fightPromptBackgroundShowTween = fightPromptBackground.DOScaleX(0.1f, 0.2f).SetEase(Ease.OutQuad).From(isRelative: false)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
		fightPromptContainerShowTween = fightPromptContainer.transform.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false)
			.SetAutoKill(autoKillOnCompletion: false)
			.Pause();
	}

	public override void Init()
	{
		base.Init();
		Singleton<UIManager>.Instance.mainMenuBGCamera.enabled = false;
		Singleton<Camera3D>.Instance.uiCameraPostProcessing.enabled = false;
		ResumeLobby();
		ZoomInWrapper();
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch && Singleton<UIManager>.Instance.ShowSwitchControllerApplet(4))
		{
			UpdateMasterButtonPrompts();
		}
	}

	public override void UpdateMasterButtonPrompts()
	{
		UpdateHatPrompt(animate: false);
		UpdateAltCharacterPrompt(animate: false);
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if ((uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Ready && uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Selecting) || (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC && uiPlayers[i].inputDevice != UILobbyPlayer.InputDevice.Joystick))
			{
				continue;
			}
			if (uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.KeyboardTwo)
			{
				for (int j = 0; j < masterButtonPrompts.Length; j++)
				{
					string text = string.Empty;
					if (string.Equals(masterButtonPrompts[j].action, "UISubmit"))
					{
						text = "Dash";
					}
					else if (string.Equals(masterButtonPrompts[j].action, "UICancel"))
					{
						text = "Back";
					}
					else if (string.Equals(masterButtonPrompts[j].action, "UITabLeft"))
					{
						text = "Swipe";
					}
					else if (string.Equals(masterButtonPrompts[j].action, "UITabRight"))
					{
						text = "Throw";
					}
					if (string.IsNullOrEmpty(text))
					{
						masterButtonPrompts[j].Init(uiPlayers[i].rewiredPlayer);
					}
					else
					{
						masterButtonPrompts[j].Init(uiPlayers[i].rewiredPlayer, text);
					}
				}
			}
			else
			{
				for (int k = 0; k < masterButtonPrompts.Length; k++)
				{
					masterButtonPrompts[k].Init(uiPlayers[i].rewiredPlayer);
				}
			}
			return;
		}
		if (continueFillPrompt != null && continueFillPrompt.buttonPrompt != null)
		{
			Rewired.Player currentMasterPlayer = Singleton<UIManager>.Instance.GetCurrentMasterPlayer();
			if (currentMasterPlayer != null)
			{
				continueFillPrompt.buttonPrompt.action = (Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(currentMasterPlayer) ? UILobbyPlayer.joinActionSwitch : UILobbyPlayer.joinAction);
			}
		}
		base.UpdateMasterButtonPrompts();
	}

	public void UpdateHatPrompt(bool animate = true)
	{
		hatPrompt.SetButtonPrompts(animate);
	}

	public void UpdateAltCharacterPrompt(bool animate = true)
	{
		altCharacterPrompt.SetButtonPrompts(animate);
	}

	public void PlayChangeHatPFX(Vector3 pos)
	{
		if (!(changeHatPFX == null))
		{
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				position = pos + new Vector3(0f, 0f, -0.5f),
				applyShapeToPosition = true
			};
			changeHatPFX.Emit(emitParams, 5);
		}
	}

	public override void Cleanup()
	{
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
	}

	public override void Resume()
	{
		base.Resume();
		ResumeLobby();
		uiPlayersScaleTweener.PlayBackwards();
		uiPlayersYPosTweener.PlayBackwards();
	}

	private void ResumeLobby()
	{
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
		isReadyFullyCharged = false;
		headerCanvas.enabled = true;
		characterGridCanvas.enabled = true;
		footerCanvas.enabled = true;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Available)
			{
				uiPlayers[i].gameObject.SetActive(value: true);
			}
		}
		ReadyCharge = 0f;
		for (int j = 0; j < characterGridCells.Length; j++)
		{
			characterGridCells[j].CheckAltCharacter();
			characterGridCells[j].CheckLockedState();
		}
		ResetTeams();
		CheckIfAllPlayersAreReady();
	}

	public void ResetAllPlayers()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			uiPlayers[i].Reset();
		}
		headerTitle.gameObject.SetActive(value: true);
	}

	public void UnreadyAllReadyPlayers()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Bot)
			{
				uiPlayers[i].Reset();
			}
			else if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				uiPlayers[i].SetReadyState(UILobbyPlayer.ReadyState.Selecting);
			}
		}
	}

	public void ResetTeams()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			uiPlayers[i].SetTeam(0);
		}
	}

	private void HideExtraKeyboardPlayers()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.Joystick)
			{
				num++;
			}
			else if (num == 0 && (uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.Keyboard || uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.KeyboardTwo) && uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Available)
			{
				uiPlayers[i].gameObject.SetActive(value: false);
			}
		}
	}

	public bool AreAllPlayersReady()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				if (uiPlayers[i].playerType == PlayerType.Human)
				{
					num2++;
				}
				else
				{
					num++;
				}
			}
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Selecting)
			{
				return false;
			}
		}
		if (num2 > 0)
		{
			return true;
		}
		return false;
	}

	public bool AreAllPlayersOnATeam()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready && uiPlayers[i].team == 0)
			{
				return false;
			}
		}
		return true;
	}

	public bool AreMoreBotsRequired()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				num++;
			}
		}
		if (num > 1)
		{
			return false;
		}
		return true;
	}

	public bool AreAnyPlayersReadyOrSelecting()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Human && (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready || uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Selecting))
			{
				return true;
			}
		}
		return false;
	}

	private int GetNumPlayersSelecting()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Selecting)
			{
				num++;
			}
		}
		return num;
	}

	public void CheckIfAllPlayersAreReady()
	{
		if (fightPromptCanvas != null && continueFillPrompt != null)
		{
			if (AreAllPlayersReady())
			{
				UILobbyPlayer firstReadyPlayer = GetFirstReadyPlayer();
				Rewired.Player player = ((firstReadyPlayer == null) ? null : firstReadyPlayer.rewiredPlayer);
				if (firstReadyPlayer == null || player == null)
				{
					continueFillPrompt.InitButtonPrompt(ReInput.players.SystemPlayer, "Dash");
				}
				else if (firstReadyPlayer.inputDevice == UILobbyPlayer.InputDevice.KeyboardTwo)
				{
					continueFillPrompt.InitButtonPrompt(player, "Dash");
				}
				else
				{
					continueFillPrompt.InitButtonPrompt(player, Singleton<UIManager>.Instance.DoesPlayerHaveASwitchJoystick(player) ? UILobbyPlayer.joinActionSwitch : UILobbyPlayer.joinAction);
				}
				ShowFightPrompt();
				headerTitle.gameObject.SetActive(value: false);
			}
			else
			{
				HideFightPrompt();
				if (GetNumPlayersSelecting() == 0)
				{
					headerTitle.gameObject.SetActive(value: true);
				}
				else
				{
					headerTitle.gameObject.SetActive(value: false);
				}
				if (!AreAnyPlayersReadyOrSelecting() && GetNumBotUIPlayers() > 0)
				{
					ResetAllPlayers();
				}
			}
		}
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready && uiPlayers[i].playerType == PlayerType.Human)
			{
				Singleton<UIManager>.Instance.AssignSpecificPlayerToEventSystem(uiPlayers[i].rewiredPlayer);
			}
			else
			{
				Singleton<UIManager>.Instance.RemoveSpecificPlayerFromEventSystem(uiPlayers[i].rewiredPlayer);
			}
		}
	}

	private void ShowFightPrompt()
	{
		fightPromptCanvasGroup.interactable = true;
		if (!fightPromptCanvas.enabled)
		{
			fightPromptCanvas.enabled = true;
			Singleton<UIManager>.Instance.SetSelectedGameObject(numBotsSlider.gameObject);
			if (fightPromptBackground != null)
			{
				fightPromptBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rt.rect.width);
				fightPromptBackground.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, (1920f - rt.rect.width) / 2f, rt.rect.width);
				fightPromptBackgroundShowTween.Goto(0f, andPlay: true);
			}
			fightPromptContainerShowTween.Goto(0f, andPlay: true);
			UpdateBotSliders();
			continueFillPrompt.gameObject.SetActive(value: true);
		}
	}

	private void HideFightPrompt()
	{
		fightPromptCanvasGroup.interactable = false;
		if (fightPromptCanvas.enabled)
		{
			fightPromptCanvas.enabled = false;
			Singleton<UIManager>.Instance.SetSelectedGameObject(null);
			ReadyCharge = 0f;
			fightPromptCanvas.enabled = false;
			continueFillPrompt.gameObject.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
	}

	private void Update()
	{
		if (Singleton<UIManager>.Instance.GetCurrentMenu() != this)
		{
			return;
		}
		if (ReadyCharge > 0f && !wasReadyChargedThisFrame)
		{
			ReadyCharge -= Time.unscaledDeltaTime * 3f;
		}
		wasReadyChargedThisFrame = false;
		if (!keyboardPlayersAreHidden || !ReInput.controllers.GetAnyButtonDown(ControllerType.Keyboard))
		{
			return;
		}
		keyboardPlayersAreHidden = false;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Disabled && (uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.Keyboard || uiPlayers[i].inputDevice == UILobbyPlayer.InputDevice.KeyboardTwo))
			{
				uiPlayers[i].SetReadyState(UILobbyPlayer.ReadyState.Available);
			}
		}
	}

	private void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			uiPlayers[i].AssignInputDevice();
		}
		UpdateBotSliders();
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			uiPlayers[i].AssignInputDevice();
		}
		UpdateBotSliders();
		CheckIfAllPlayersAreReady();
	}

	public UICharacterGridCell GetRandomAvailableGridCell()
	{
		int num = Random.Range(0, characterGridCells.Length);
		int[] array = new int[characterGridCells.Length];
		for (int i = 0; i < characterGridCells.Length; i++)
		{
			array[i] = (i + num) % characterGridCells.Length;
		}
		for (int j = 0; j < characterGridCells.Length; j++)
		{
			int num2 = array[j];
			if (IsGridCellUnlocked(characterGridCells[num2]) && IsGridCellAvailable(characterGridCells[num2]) && characterGridCells[num2].selectionState != UICharacterGridCell.SelectionState.Selected)
			{
				return characterGridCells[num2];
			}
		}
		return null;
	}

	public UICharacterGridCell GetNextAvailableGridCell(UICharacterGridCell currentGridCell, MoveDirection direction)
	{
		if (currentGridCell == null)
		{
			Debug.Log("Cannot find next grid cell because no grid cell is selected");
			return null;
		}
		int num = currentGridCell.index;
		switch (direction)
		{
		case MoveDirection.Up:
			while (Mathf.Floor(num / numGridColumns) > 0f)
			{
				num -= numGridColumns;
				if (IsGridCellAvailable(characterGridCells[num]))
				{
					return characterGridCells[num];
				}
			}
			break;
		case MoveDirection.Down:
			while (Mathf.Floor(num / numGridColumns) < Mathf.Floor(characterGridCells.Length / numGridColumns) - 1f)
			{
				num += numGridColumns;
				if (IsGridCellAvailable(characterGridCells[num]))
				{
					return characterGridCells[num];
				}
			}
			break;
		case MoveDirection.Left:
		{
			for (int j = 0; j < characterGridCells.Length; j++)
			{
				if (num == 0)
				{
					num = characterGridCells.Length;
				}
				num--;
				if (IsGridCellAvailable(characterGridCells[num]))
				{
					return characterGridCells[num];
				}
			}
			break;
		}
		case MoveDirection.Right:
		{
			for (int i = 0; i < characterGridCells.Length; i++)
			{
				if (num == characterGridCells.Length - 1)
				{
					num = -1;
				}
				num++;
				if (IsGridCellAvailable(characterGridCells[num]))
				{
					return characterGridCells[num];
				}
			}
			break;
		}
		}
		return null;
	}

	public UICharacterGridCell GetClosestAvailableGridCell(UICharacterGridCell currentGridCell)
	{
		if (currentGridCell == null)
		{
			Debug.Log("Cannot find closest grid cell because no grid cell is selected");
			return null;
		}
		int num = int.MaxValue;
		UICharacterGridCell result = null;
		for (int i = 0; i < characterGridCells.Length; i++)
		{
			if (!(characterGridCells[i] == currentGridCell) && characterGridCells[i].selectionState != UICharacterGridCell.SelectionState.Selected && Mathf.Abs(characterGridCells[i].index - currentGridCell.index) < num)
			{
				result = characterGridCells[i];
				num = Mathf.Abs(characterGridCells[i].index - currentGridCell.index);
			}
		}
		return result;
	}

	private bool IsGridCellAvailable(UICharacterGridCell characterGridCell)
	{
		if (characterGridCell.selectionState == UICharacterGridCell.SelectionState.Ready)
		{
			return false;
		}
		return true;
	}

	private bool IsGridCellUnlocked(UICharacterGridCell characterGridCell)
	{
		if (characterGridCell.lockState == UICharacterGridCell.LockState.Unlocked)
		{
			return true;
		}
		return false;
	}

	private int GetNumEnabledUIPlayers()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Disabled)
			{
				num++;
			}
		}
		return num;
	}

	private int GetNumBottableUIPlayers()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Disabled || uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Available)
			{
				num++;
			}
		}
		return num;
	}

	public int GetNumBotUIPlayers()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Bot && uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				num++;
			}
		}
		return num;
	}

	private int GetNumHumanUIPlayers()
	{
		int num = 0;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Human && uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				num++;
			}
		}
		return num;
	}

	public UILobbyPlayer GetFirstReadyPlayer()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Human && uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				return uiPlayers[i];
			}
		}
		return null;
	}

	public bool AddBot(UICharacterGridCell gridCell = null)
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Disabled)
			{
				uiPlayers[i].SetBot(gridCell);
				return true;
			}
		}
		for (int num = uiPlayers.Length - 1; num >= 0; num--)
		{
			if (uiPlayers[num].readyState == UILobbyPlayer.ReadyState.Available)
			{
				uiPlayers[num].SetBot(gridCell);
				return true;
			}
		}
		return false;
	}

	public bool RemoveBot()
	{
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Bot && uiPlayers[i].inputDevice != UILobbyPlayer.InputDevice.None)
			{
				uiPlayers[i].Reset();
				return true;
			}
		}
		for (int num = uiPlayers.Length - 1; num >= 0; num--)
		{
			if (uiPlayers[num].readyState == UILobbyPlayer.ReadyState.Ready && uiPlayers[num].playerType == PlayerType.Bot)
			{
				uiPlayers[num].Reset();
				return true;
			}
		}
		return false;
	}

	public void UpdateBotSliders()
	{
		int numBotUIPlayers = GetNumBotUIPlayers();
		GetNumHumanUIPlayers();
		numBotsSlider.SetValue(numBotUIPlayers);
		numBotsSlider.SetMaxValue(GetNumBottableUIPlayers() + numBotUIPlayers);
		if (numBotUIPlayers > 0)
		{
			botDifficultySlider.gameObject.SetActive(value: true);
			botDifficultySlider.SetValue((int)Singleton<SettingsManager>.Instance.botDifficulty, sendCallback: false);
			return;
		}
		if (Singleton<UIManager>.Instance.eventSystem != null && Singleton<UIManager>.Instance.eventSystem.currentSelectedGameObject == botDifficultySlider.gameObject)
		{
			Singleton<UIManager>.Instance.SetSelectedGameObject(numBotsSlider.gameObject);
		}
		botDifficultySlider.gameObject.SetActive(value: false);
	}

	public void OnChangeNumBotsSlider(int value)
	{
		int numBotUIPlayers = GetNumBotUIPlayers();
		if (value < numBotUIPlayers)
		{
			RemoveBot();
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/remove_bot");
		}
		else if (value > numBotUIPlayers)
		{
			AddBot();
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/add_bot");
		}
		if (value == 0 && AreMoreBotsRequired())
		{
			numBotsSlider.text.text = LocalizationManager.GetTranslation("ui_add_bots");
		}
		UpdateBotSliders();
	}

	public void OnChangeBotDifficultySlider(int value)
	{
		Singleton<SettingsManager>.Instance.botDifficulty = (SettingsManager.BotDifficulty)value;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].playerType == PlayerType.Bot && uiPlayers[i].character != null)
			{
				switch (Singleton<SettingsManager>.Instance.botDifficulty)
				{
				case SettingsManager.BotDifficulty.easy:
					uiPlayers[i].character.SetFacialExpression(FacialExpression.happy, Random.Range(0.9f, 1.3f));
					break;
				case SettingsManager.BotDifficulty.medium:
					uiPlayers[i].character.SetFacialExpression(FacialExpression.aggressive, Random.Range(0.9f, 1.3f));
					break;
				case SettingsManager.BotDifficulty.hard:
					uiPlayers[i].character.SetFacialExpression(FacialExpression.retrieving, Random.Range(0.9f, 1.3f));
					break;
				}
			}
		}
	}

	public void ShakeNumBotsSlider()
	{
		if (fightPromptCanvas.enabled)
		{
			numBotsSlider.GetComponent<UIButtonStyle>().Shake();
			if (numBotsSlider.value == 0 && AreMoreBotsRequired())
			{
				numBotsSlider.text.text = LocalizationManager.GetTranslation("ui_add_bots");
			}
		}
	}

	public override void Suspend()
	{
		base.Suspend();
		headerCanvas.enabled = false;
		characterGridCanvas.enabled = false;
		fightPromptCanvas.enabled = false;
		fightPromptCanvasGroup.interactable = false;
		footerCanvas.enabled = false;
		uiPlayersScaleTweener = uiPlayersCanvas.transform.DOScale(0.8f, 0.1f).SetAutoKill(autoKillOnCompletion: false);
		uiPlayersYPosTweener = uiPlayersCanvas.transform.DOLocalMove(Vector2.up * 300f, 0.1f).SetAutoKill(autoKillOnCompletion: false);
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
		for (int i = 0; i < uiPlayers.Length; i++)
		{
			if (uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Available)
			{
				uiPlayers[i].gameObject.SetActive(value: false);
			}
		}
	}

	public IEnumerator UIResponse_Continue()
	{
		float t = 0f;
		while (t < 0.25f)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<SettingsManager>.Instance.numBots = GetNumBotUIPlayers();
		Singleton<SettingsManager>.Instance.numPlayers = GetNumHumanUIPlayers();
		UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(MenuType.MatchSettings);
		if (menu != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menu, disableCurrentMenu: false);
		}
	}

	public override void UIResponse_Close()
	{
		ResetAllPlayers();
		Singleton<UIManager>.Instance.PopMenu();
		if (Singleton<UIManager>.Instance.GetNumMenusInStack() == 0)
		{
			UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(MenuType.Main);
			if (menu != null)
			{
				Singleton<UIManager>.Instance.PushMenu(menu);
			}
		}
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/back");
	}
}
