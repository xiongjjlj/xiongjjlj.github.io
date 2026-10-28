using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using Rewired;
using Rewired.Data.Mapping;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : Singleton<UIManager>
{
	public delegate void OnSelectionChangeDelegate(GameObject selectedGameObject);

	[Header("Menus")]
	public UIMenuBase[] editorMenus;

	private Dictionary<UIMenuBase.MenuType, UIMenuBase> menus = new Dictionary<UIMenuBase.MenuType, UIMenuBase>();

	private Stack<UIMenuBase> menuStack = new Stack<UIMenuBase>();

	private List<UIMenuBase> menuOverlays = new List<UIMenuBase>();

	public UIMenuBase.MenuType initialMenu;

	public RectTransform shakeRoot;

	[Header("Canvases")]
	public Canvas gameplayCanvas;

	public Canvas screenTransitionCanvas;

	public GameObject showcaseOverlay;

	public Camera mainMenuBGCamera;

	[Header("Selection")]
	[SerializeField]
	private GameObject lastSelectedGameObject;

	private static WaitForSeconds buttonClickWait = new WaitForSeconds(0.5f);

	public EventSystem eventSystem;

	public UIEventSystemPlayerSwapper eventSystemPlayerSwapper;

	public bool UserChangedSelection = true;

	private float shakeDuration;

	private bool shakeFinished;

	private bool shakeStarted;

	private Vector2 shakeLerpTarget;

	private Vector2 shakeLerpStart;

	private Vector2 shakeMagnitude;

	private float shakeFrequency;

	private float shakeTimer;

	private float shakeTotalTime;

	private Vector2 shakeOutput;

	[SerializeField]
	private float idleTimer;

	private static float idleTimeBeforeShowcase = 20f;

	private bool skipIdleFrame;

	[Header("Effects")]
	public UILoadingOverlay uiLoadingOverlay;

	public ParticleSystem buttonSubmitPFX;

	private RectTransform buttonSubmitPFXRT;

	private float switchAppletLastShownTime;

	public HardwareJoystickMap[] switchJoysticks;

	[Header("Hide and Seek")]
	public TextMeshProUGUI seekTimerText;

	public RectTransform seekTimerRT;

	public Canvas seekTimerCanvas;

	public ParticleSystem seekTimerPFX;

	private int prevSeekTimerSeconds = -1;

	public static event OnSelectionChangeDelegate OnSelectionChanged;

	private void Start()
	{
		eventSystem = EventSystem.current;
		if (menus.Count == 0)
		{
			SetupAllMenus();
		}
		if (buttonSubmitPFX != null)
		{
			buttonSubmitPFXRT = buttonSubmitPFX.GetComponent<RectTransform>();
		}
		showcaseOverlay.SetActive(value: false);
		if (Singleton<SettingsManager>.Instance.localizationMode)
		{
			LocalizationManager.Sources[0].Import_Google(ForceUpdate: true, justCheck: false);
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.PC)
		{
			Cursor.visible = false;
		}
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	private void SetupAllMenus()
	{
		for (int i = 0; i < editorMenus.Length; i++)
		{
			if (!menus.ContainsKey(editorMenus[i].MenuID))
			{
				menus.Add(editorMenus[i].MenuID, editorMenus[i]);
				editorMenus[i].Setup();
				editorMenus[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void Init()
	{
		if (menus.Count == 0)
		{
			SetupAllMenus();
		}
		UIMenuBase menu = GetMenu(initialMenu);
		if (menu != null)
		{
			PushMenu(menu);
		}
		Singleton<AudioManager>.Instance.SetMusicFilter(0f);
		Singleton<AudioManager>.Instance.ProgressMenuMusic();
	}

	private void Update()
	{
		CheckCurrentSelectedGameObject();
		if (eventSystem != null && eventSystem.currentSelectedGameObject == null && lastSelectedGameObject != null)
		{
			UserChangedSelection = false;
			eventSystem.SetSelectedGameObject(lastSelectedGameObject);
			UserChangedSelection = true;
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.conventionMode)
		{
			if (skipIdleFrame)
			{
				skipIdleFrame = false;
			}
			else
			{
				UIMenuBase currentMenu = GetCurrentMenu();
				if (currentMenu != null && currentMenu.MenuID == UIMenuBase.MenuType.Main)
				{
					if (ReInput.players.GetSystemPlayer().GetAnyButtonDown())
					{
						idleTimer = 0f;
					}
					idleTimer += Time.unscaledDeltaTime;
					if (idleTimer > idleTimeBeforeShowcase)
					{
						StartShowcase();
					}
				}
				else
				{
					idleTimer = 0f;
				}
			}
		}
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Menu && Singleton<GameManager>.Instance.gameState != GameManager.GameState.Paused && Singleton<GameManager>.Instance.gameState != GameManager.GameState.Loading)
		{
			if (Singleton<SettingsManager>.Instance.showcaseMode && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
			{
				ExitShowcase();
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				Rewired.Player player = null;
				foreach (Player player2 in Singleton<GameManager>.Instance.players)
				{
					if (player2.playerInput.rewiredController != null && player2.playerInput.rewiredController.rewiredPlayer.controllers.hasKeyboard)
					{
						player = player2.playerInput.rewiredController.rewiredPlayer;
						break;
					}
				}
				if (player == null)
				{
					foreach (Rewired.Player player3 in ReInput.players.Players)
					{
						if (player3.controllers.hasKeyboard)
						{
							player = player3;
						}
					}
				}
				Pause(player);
			}
		}
		if (!shakeFinished && shakeStarted)
		{
			shakeRoot.anchoredPosition = GetCameraShake();
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			skipIdleFrame = true;
		}
	}

	public void Pause(Rewired.Player drivingPlayer = null)
	{
		Singleton<AudioManager>.Instance.SetMusicFilter(1f);
		Singleton<GameManager>.Instance.Pause();
		Singleton<CameraFX>.Instance.Blur();
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show");
		if (drivingPlayer != null)
		{
			eventSystemPlayerSwapper.ForceSpecificPlayerToMasterPlayer(drivingPlayer);
		}
		UIMenuPause uIMenuPause = GetMenu(UIMenuBase.MenuType.Pause) as UIMenuPause;
		if (uIMenuPause != null)
		{
			PushMenu(uIMenuPause);
		}
	}

	public void Unpause()
	{
		Singleton<AudioManager>.Instance.SetMusicFilter(0f);
		Singleton<GameManager>.Instance.Unpause();
		Singleton<CameraFX>.Instance.UnBlur();
		AssignAllPlayersToEventSystem();
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (!pauseStatus)
		{
			skipIdleFrame = true;
		}
	}

	private void StartShowcase()
	{
		Singleton<SettingsManager>.Instance.showcaseMode = true;
		Singleton<SettingsManager>.Instance.teamMatch = false;
		Singleton<SettingsManager>.Instance.matchType = SettingsManager.MatchType.DeathMatch;
		Singleton<SettingsManager>.Instance.SelectVictoryCondition(0);
		Singleton<SettingsManager>.Instance.numBots = 4;
		Singleton<SettingsManager>.Instance.botDifficulty = SettingsManager.BotDifficulty.medium;
		Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial = false;
		Singleton<SettingsManager>.Instance.matchScoreGoal = 5;
		UIMenuLobby uIMenuLobby = GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			uIMenuLobby.ResetAllPlayers();
			uIMenuLobby.AddBot();
			uIMenuLobby.AddBot();
			uIMenuLobby.AddBot();
			uIMenuLobby.AddBot();
			if (GetMenu(UIMenuBase.MenuType.MatchSettings) as UIMenuMatchSettings != null)
			{
				StartCoroutine(WaitForMatchStartThenClose());
				PopAllMenus();
			}
		}
		idleTimer = 0f;
	}

	public void ExitShowcase()
	{
		showcaseOverlay.SetActive(value: false);
		Singleton<SettingsManager>.Instance.showcaseMode = false;
		Singleton<SettingsManager>.Instance.botDifficulty = SettingsManager.BotDifficulty.easy;
		QuitMatch();
	}

	public IEnumerator WaitForMatchStartThenClose(UIButton button = null, bool rematch = false)
	{
		if (uiLoadingOverlay != null && !rematch)
		{
			yield return StartCoroutine(uiLoadingOverlay.TransitionIn(button, textTransition: true));
		}
		yield return StartCoroutine(Singleton<GameManager>.Instance.StartMatch(rematch));
		if (Singleton<SettingsManager>.Instance.showcaseMode)
		{
			showcaseOverlay.SetActive(value: true);
		}
		PopAllMenus();
		eventSystem.sendNavigationEvents = true;
		uiLoadingOverlay.Close();
	}

	public UIMenuBase GetMenu(UIMenuBase.MenuType menuID)
	{
		if (menus.ContainsKey(menuID))
		{
			return menus[menuID];
		}
		Debug.LogError("Menu " + menuID.ToString() + " could not be found");
		return null;
	}

	public T GetMenu<T>(UIMenuBase.MenuType menuId) where T : UIMenuBase
	{
		UIMenuBase menu = GetMenu(menuId);
		if (menu == null)
		{
			return null;
		}
		T val = menu as T;
		if (val == null)
		{
			Debug.LogError("Cannot cast menu type!");
			return null;
		}
		return val;
	}

	public void PushMenu(UIMenuBase menu, bool disableCurrentMenu = true)
	{
		if (menuStack.Count > 0)
		{
			UIMenuBase uIMenuBase = menuStack.Peek();
			if (uIMenuBase != null)
			{
				uIMenuBase.Suspend();
				if (disableCurrentMenu)
				{
					uIMenuBase.gameObject.SetActive(value: false);
				}
			}
		}
		else
		{
			Singleton<TimeKeeper>.Instance.PauseTime();
		}
		menu.gameObject.SetActive(value: true);
		menu.Init();
		menuStack.Push(menu);
	}

	public void PopMenu(bool resumeLastMenu = true)
	{
		if (menuStack.Count <= 0)
		{
			Debug.Log("No menus in the stack to pop!");
			return;
		}
		UIMenuBase uIMenuBase = menuStack.Peek();
		if (uIMenuBase == null)
		{
			return;
		}
		uIMenuBase.Cleanup();
		uIMenuBase.gameObject.SetActive(value: false);
		menuStack.Pop();
		if (menuStack.Count <= 0)
		{
			Singleton<TimeKeeper>.Instance.UnPauseTime();
		}
		else if (resumeLastMenu)
		{
			UIMenuBase uIMenuBase2 = menuStack.Peek();
			if (uIMenuBase2 != null)
			{
				uIMenuBase2.gameObject.SetActive(value: true);
				uIMenuBase2.Resume();
			}
		}
	}

	public void PopAllMenus()
	{
		if (menuStack.Count > 0)
		{
			while (menuStack.Count > 0)
			{
				PopMenu(resumeLastMenu: false);
			}
		}
	}

	public bool IsOverlayMenu(UIMenuBase menu)
	{
		if (menu == null)
		{
			return false;
		}
		if (!menuOverlays.Contains(menu))
		{
			return false;
		}
		return true;
	}

	public bool IsMenu(UIMenuBase menu)
	{
		if (menu == null)
		{
			return false;
		}
		if (!menuStack.Contains(menu))
		{
			return false;
		}
		return true;
	}

	public void AddOverlayMenu(UIMenuBase menu)
	{
		if (!(menu == null) && !menuOverlays.Contains(menu))
		{
			if (menuOverlays.Count > 0)
			{
				menuOverlays[menuOverlays.Count - 1].Suspend();
			}
			menu.gameObject.SetActive(value: true);
			menu.Init();
			menuOverlays.Add(menu);
		}
	}

	public void RemoveOverlayMenu(UIMenuBase menuController)
	{
		if (!(menuController == null) && menuOverlays.Contains(menuController))
		{
			menuController.Cleanup();
			menuController.gameObject.SetActive(value: false);
			menuOverlays.Remove(menuController);
			UIMenuBase currentMenu = GetCurrentMenu();
			if (currentMenu != null)
			{
				currentMenu.Resume();
			}
		}
	}

	public UIMenuBase GetCurrentMenu()
	{
		if (menuOverlays.Count > 0)
		{
			return menuOverlays[menuOverlays.Count - 1];
		}
		if (menuStack.Count == 0)
		{
			return null;
		}
		return menuStack.Peek();
	}

	public int GetNumMenusInStack()
	{
		return menuStack.Count;
	}

	public void EnableScreenTransitionCanvas()
	{
		screenTransitionCanvas.enabled = true;
	}

	public void DisableScreenTransitionCanvas()
	{
		screenTransitionCanvas.enabled = false;
	}

	public void EnableGameplayCanvas()
	{
		gameplayCanvas.enabled = true;
	}

	public void DisableGameplayCanvas()
	{
		gameplayCanvas.enabled = false;
	}

	private void CheckCurrentSelectedGameObject()
	{
		if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
		{
			if (lastSelectedGameObject != eventSystem.currentSelectedGameObject && OnSelectionChanged != null)
			{
				OnSelectionChanged(eventSystem.currentSelectedGameObject);
			}
			lastSelectedGameObject = eventSystem.currentSelectedGameObject;
		}
	}

	public void SetSelectedGameObject(GameObject go)
	{
		if (!(eventSystem == null))
		{
			CheckCurrentSelectedGameObject();
			UserChangedSelection = false;
			eventSystem.SetSelectedGameObject(go);
			UserChangedSelection = true;
		}
	}

	private void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		UIMenuBase currentMenu = GetCurrentMenu();
		Debug.Log("Controller connected " + args.name);
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch && currentMenu != null && currentMenu.MenuID == UIMenuBase.MenuType.Lobby)
		{
			if (args.controllerType == ControllerType.Joystick && ShowSwitchControllerApplet((ReInput.controllers.joystickCount > 4) ? 6 : 4))
			{
				AssignAllPlayersToEventSystem();
			}
		}
		else if (currentMenu == null || currentMenu.MenuID == UIMenuBase.MenuType.Main || currentMenu.MenuID == UIMenuBase.MenuType.Credits || currentMenu.MenuID == UIMenuBase.MenuType.None || currentMenu.MenuID == UIMenuBase.MenuType.Options)
		{
			AssignAllPlayersToEventSystem();
		}
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		UIMenuBase currentMenu = GetCurrentMenu();
		if (currentMenu == null || currentMenu.MenuID == UIMenuBase.MenuType.Main || currentMenu.MenuID == UIMenuBase.MenuType.Credits || currentMenu.MenuID == UIMenuBase.MenuType.None || currentMenu.MenuID == UIMenuBase.MenuType.Options)
		{
			AssignAllPlayersToEventSystem();
		}
	}

	public void AssignAllPlayersToEventSystem()
	{
		eventSystemPlayerSwapper.AssignAllPlayersToEventSystem();
		if (GetCurrentMenu() != null)
		{
			GetCurrentMenu().UpdateMasterButtonPrompts();
		}
	}

	public void AssignSpecificPlayerToEventSystem(Rewired.Player rwPlayer)
	{
		eventSystemPlayerSwapper.AssignSpecificPlayerToEventSystem(rwPlayer);
	}

	public void RemoveSpecificPlayerFromEventSystem(Rewired.Player rwPlayer)
	{
		eventSystemPlayerSwapper.RemoveSpecificPlayerFromEventSystem(rwPlayer);
	}

	public void ClearAllPlayersFromEventSystem()
	{
		eventSystemPlayerSwapper.ClearAllPlayersFromEventSystem();
	}

	public Rewired.Player GetCurrentMasterPlayer()
	{
		return eventSystemPlayerSwapper.GetCurrentMasterPlayer();
	}

	public void VibrateMasterPlayer(int motorIndex, float intensity, float duration)
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled)
		{
			return;
		}
		Rewired.Player currentMasterPlayer = GetCurrentMasterPlayer();
		if (currentMasterPlayer != null && !currentMasterPlayer.controllers.hasKeyboard)
		{
			if (DoesPlayerHaveASwitchJoystick(currentMasterPlayer))
			{
				intensity *= 0.5f;
				duration *= 0.25f;
			}
			currentMasterPlayer.SetVibration(motorIndex, intensity, duration);
		}
	}

	public bool ShowSwitchControllerApplet(int maxPlayers)
	{
		return false;
	}

	public bool DoesPlayerHaveASwitchJoystick(Rewired.Player rewiredPlayer)
	{
		if (rewiredPlayer.controllers != null && rewiredPlayer.controllers.joystickCount > 0 && rewiredPlayer.controllers.Joysticks[0] != null)
		{
			return IsJoystickASwitchJoystick(rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid);
		}
		return false;
	}

	private bool IsJoystickASwitchJoystick(Guid joystickGuid)
	{
		for (int i = 0; i < switchJoysticks.Length; i++)
		{
			if (switchJoysticks[i].Guid == joystickGuid)
			{
				return true;
			}
		}
		return false;
	}

	public void ShowWishlistPrompt()
	{
		SteamFriends.ActivateGameOverlayToStore(new AppId_t(965680u), EOverlayToStoreFlag.k_EOverlayToStoreFlag_AddToCartAndShow);
	}

	public bool GetButtonDownFromEventSystem(string action)
	{
		if (eventSystem == null || !eventSystem.sendNavigationEvents)
		{
			return false;
		}
		for (int i = 0; i < ReInput.players.allPlayerCount; i++)
		{
			if (ReInput.players.AllPlayers[i].isPlaying && ReInput.players.AllPlayers[i].GetButtonDown(action))
			{
				return true;
			}
		}
		return false;
	}

	public bool GetAnyInputFromEventSystem()
	{
		if (eventSystem == null || !eventSystem.sendNavigationEvents)
		{
			return false;
		}
		for (int i = 0; i < ReInput.players.allPlayerCount; i++)
		{
			if (ReInput.players.AllPlayers[i].isPlaying && eventSystemPlayerSwapper.GetAnyUIButtonOrAxisInput(ReInput.players.AllPlayers[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void CameraShake(Vector2 magnitude, float frequency, float duration)
	{
		shakeMagnitude = magnitude * 10f;
		shakeFrequency = frequency;
		shakeDuration = duration;
		shakeFinished = false;
		shakeStarted = true;
		shakeLerpTarget = Vector2.Scale(UnityEngine.Random.insideUnitCircle.normalized, shakeMagnitude);
		shakeLerpStart = shakeLerpTarget;
		shakeLerpTarget = Vector2.Scale(shakeLerpTarget.normalized * -1f + UnityEngine.Random.insideUnitCircle, shakeMagnitude);
		shakeTimer = 0f;
		shakeTotalTime = 0f;
	}

	private Vector2 GetCameraShake()
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.screenShakeEnabled)
		{
			return Vector2.zero;
		}
		shakeTimer += Time.unscaledDeltaTime * shakeFrequency;
		shakeTotalTime += Time.unscaledDeltaTime;
		shakeOutput = Vector2.Lerp(shakeLerpStart, shakeLerpTarget, Mathf.Min(Easing.SineInOut(shakeTimer), 1f)) * (1f - Mathf.Min(shakeTotalTime / shakeDuration, 1f));
		if (shakeTimer >= 1f)
		{
			shakeLerpStart = shakeLerpTarget;
			if (shakeTotalTime >= shakeDuration)
			{
				shakeFinished = true;
				shakeStarted = false;
				shakeOutput = Vector2.zero;
			}
			else if (shakeTotalTime + 1f / shakeFrequency > shakeDuration)
			{
				shakeLerpTarget = Vector2.zero;
			}
			else
			{
				shakeLerpTarget = Vector2.Scale(shakeLerpTarget.normalized * -1f + UnityEngine.Random.insideUnitCircle, shakeMagnitude);
			}
			shakeTimer = 0f;
		}
		return shakeOutput;
	}

	public void ZoomFrom(float scale, float duration)
	{
		if (!(shakeRoot == null))
		{
			shakeRoot.DOComplete();
			shakeRoot.localScale = Vector3.one;
			shakeRoot.DOScale(scale, duration).From(isRelative: false);
		}
	}

	public void ZoomTo(float scale, float duration)
	{
		if (!(shakeRoot == null))
		{
			shakeRoot.DOComplete();
			shakeRoot.DOScale(scale, duration);
		}
	}

	public void PlayButtonSubmitPFX(RectTransform rt, Color pfxColor)
	{
		if (buttonSubmitPFX != null && buttonSubmitPFXRT != null)
		{
			ParticleSystem.MainModule main = buttonSubmitPFX.main;
			ParticleSystem.EmissionModule emission = buttonSubmitPFX.emission;
			ParticleSystem.ShapeModule shape = buttonSubmitPFX.shape;
			main.startColor = pfxColor;
			Vector2 localPoint = Vector2.zero;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(shakeRoot, RectTransformUtility.WorldToScreenPoint(Singleton<Camera3D>.Instance.uiCamera, rt.transform.position), Singleton<Camera3D>.Instance.uiCamera, out localPoint);
			buttonSubmitPFXRT.anchoredPosition = localPoint;
			shape.scale = new Vector3(rt.rect.width / 42f, shape.scale.y, shape.scale.z);
			ParticleSystem.Burst burst = emission.GetBurst(0);
			burst.minCount = (short)Mathf.RoundToInt(shape.scale.x * 1.3f);
			burst.maxCount = (short)(burst.minCount + 2);
			emission.SetBurst(0, burst);
			buttonSubmitPFX.Play();
		}
	}

	public void SaveScreenshotToReplayScreen()
	{
		StartCoroutine(CaptureScreenshotAtEndOfFrame());
	}

	private IEnumerator CaptureScreenshotAtEndOfFrame()
	{
		yield return new WaitForSeconds(0.1f);
		yield return new WaitForEndOfFrame();
		UIMenuRoundEnd uIMenuRoundEnd = GetMenu(UIMenuBase.MenuType.RoundEnd) as UIMenuRoundEnd;
		if (uIMenuRoundEnd != null && uIMenuRoundEnd.replayScreen != null)
		{
			Texture2D texture2D = new Texture2D(Screen.width, Screen.height);
			texture2D.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
			texture2D.Apply();
			uIMenuRoundEnd.replayScreen.texture = texture2D;
		}
	}

	public void QuitMatch()
	{
		Singleton<GameManager>.Instance.StopAllCoroutines();
		Singleton<AudioManager>.Instance.StopGameplayMusic();
		Singleton<AudioManager>.Instance.PlayMenuMusic();
		Singleton<LevelManager>.Instance.StopAllCoroutines();
		Singleton<GameManager>.Instance.ExitMatch();
		Singleton<GameManager>.Instance.CleanUpPlayers();
		HideSeekTimer();
		PopAllMenus();
		Unpause();
		Singleton<GameManager>.Instance.gameState = GameManager.GameState.Menu;
		UIMenuBase menu = GetMenu(UIMenuBase.MenuType.Main);
		if (menu != null)
		{
			PushMenu(menu);
		}
	}

	public void HideSeekTimer()
	{
		if (seekTimerCanvas.enabled)
		{
			seekTimerRT.DOComplete();
			seekTimerRT.DOScale(0f, 0.4f).SetEase(Ease.InBack).OnComplete(delegate
			{
				seekTimerCanvas.enabled = false;
			});
		}
	}

	public void SetSeekTimer(float value, float maxValue, List<Player> playersToVibrate)
	{
		if (value >= maxValue)
		{
			return;
		}
		if (value < 1f)
		{
			seekTimerCanvas.enabled = false;
			return;
		}
		TimeSpan timeSpan = TimeSpan.FromSeconds(value);
		string text = (seekTimerText.text = $"{timeSpan.Seconds}");
		string text3 = text;
		if (!seekTimerCanvas.enabled)
		{
			seekTimerCanvas.enabled = true;
			seekTimerRT.DOComplete();
			seekTimerRT.localScale = Vector3.one;
			seekTimerRT.DOScale(0f, 0.4f).SetEase(Ease.OutBack).From();
			seekTimerText.text = text3;
			prevSeekTimerSeconds = timeSpan.Seconds;
			seekTimerPFX.Play();
			foreach (Player item in playersToVibrate)
			{
				item.playerInput.StartVibration(0.4f, 0.1f);
			}
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/timer", "counter", timeSpan.Seconds);
		}
		else
		{
			if (prevSeekTimerSeconds == timeSpan.Seconds)
			{
				return;
			}
			seekTimerText.text = text3;
			prevSeekTimerSeconds = timeSpan.Seconds;
			seekTimerRT.DOComplete();
			seekTimerRT.localScale = Vector3.one;
			seekTimerRT.DOScale(0.5f, 0.3f).SetEase(Ease.OutBack).From();
			seekTimerPFX.Play();
			foreach (Player item2 in playersToVibrate)
			{
				item2.playerInput.StartVibration(0.4f, 0.1f);
			}
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/timer", "counter", timeSpan.Seconds);
		}
	}
}
