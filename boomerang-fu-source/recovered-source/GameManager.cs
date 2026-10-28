using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rewired;
using TKOU_DLC_System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
	public enum GameState
	{
		Menu,
		Playing,
		Paused,
		TransitionOut,
		TransitionIn,
		Loading
	}

	public bool canPlatformSaveGifs;

	public GameState gameState;

	public bool testMode;

	public bool abortLoading;

	[Header("Characters")]
	public Character[] characters;

	public List<CharacterType> demoCharacters;

	public List<CharacterType> dlcOneCharacters;

	public List<CharacterType> dlcTwoCharacters;

	public Dictionary<int, Color[]> characterColors = new Dictionary<int, Color[]>();

	public Color[] char0colours = new Color[3]
	{
		new Color(1f, 0.42f, 0.42f),
		new Color(1f, 0.42f, 0.42f),
		new Color(0.7f, 0.42f, 1f)
	};

	public Color[] char1colours = new Color[3]
	{
		new Color(1f, 0.81f, 0.42f),
		new Color(1f, 0.81f, 0.42f),
		new Color(0.53f, 0.4f, 0.95f)
	};

	public Color[] char2colours = new Color[3]
	{
		new Color(0.64f, 0.9f, 0.38f),
		new Color(1f, 0.71f, 0.42f),
		new Color(0.52f, 0.7f, 0.95f)
	};

	public Color[] char3colours = new Color[3]
	{
		new Color(0.51f, 0.53f, 1f),
		new Color(1f, 0.58f, 0.48f),
		new Color(0.51f, 0.53f, 1f)
	};

	public Color[] char4colours = new Color[3]
	{
		new Color(1f, 0.42f, 0.42f),
		new Color(1f, 0.42f, 0.42f),
		new Color(0.7f, 0.42f, 1f)
	};

	public Color[] char5colours = new Color[3]
	{
		new Color(1f, 0.81f, 0.42f),
		new Color(1f, 0.81f, 0.42f),
		new Color(0.53f, 0.4f, 0.95f)
	};

	public Dictionary<CharacterType, string> characterNames = new Dictionary<CharacterType, string>
	{
		{
			CharacterType.None,
			"character_none"
		},
		{
			CharacterType.Eggplant,
			"character_eggplant"
		},
		{
			CharacterType.Avocado,
			"character_avocado"
		},
		{
			CharacterType.Banana,
			"character_banana"
		},
		{
			CharacterType.Milk,
			"character_milk"
		},
		{
			CharacterType.Watermelon,
			"character_watermelon"
		},
		{
			CharacterType.Donut,
			"character_donut"
		},
		{
			CharacterType.Coffee,
			"character_coffee"
		},
		{
			CharacterType.PaddlePop,
			"character_icecream"
		},
		{
			CharacterType.Bacon,
			"character_bacon"
		},
		{
			CharacterType.Bread,
			"character_bread"
		},
		{
			CharacterType.Carrot,
			"character_carrot"
		},
		{
			CharacterType.Sushi,
			"character_sushi"
		},
		{
			CharacterType.Ramen,
			"character_ramen"
		},
		{
			CharacterType.HotSauce,
			"character_hotsauce"
		},
		{
			CharacterType.Burger,
			"character_burger"
		},
		{
			CharacterType.Pineapple,
			"character_pineapple"
		},
		{
			CharacterType.BubbleTea,
			"character_bubbletea"
		},
		{
			CharacterType.Chips,
			"character_chips"
		},
		{
			CharacterType.Jelly,
			"character_jelly"
		},
		{
			CharacterType.SaltAndPepper,
			"character_saltandpepper"
		},
		{
			CharacterType.Grapes,
			"character_grapes"
		},
		{
			CharacterType.Chocolate,
			"character_chocolate"
		}
	};

	[SerializeField]
	private Hat[] hatLibrary;

	public Color goldenDiscColor;

	public Color hazardColor;

	private IEnumerator playersLeftStandingRoutine;

	private IEnumerator killAllDecoysRoutine;

	private bool isKillingAllDecoys;

	[Header("Round")]
	public Player playerPrefab;

	public Disc discPrefab;

	public Decoy decoyPrefab;

	public Javelin javelinPrefab;

	public List<Player> players = new List<Player>();

	public List<Disc> activeDiscs = new List<Disc>();

	public List<GameObject> activeExplosives = new List<GameObject>(128);

	public List<GameObject> activeFires = new List<GameObject>();

	public List<GameObject> activeSlimes = new List<GameObject>();

	public List<Decoy> activeDecoys = new List<Decoy>();

	public PowerupBar[] powerupBars = new PowerupBar[6];

	public HintBar[] hintBars = new HintBar[6];

	public AlertBar[] alertBars = new AlertBar[6];

	public SeekAttemptBar seekAttemptBar;

	public List<Enemy> enemies = new List<Enemy>();

	public int roundNumber;

	public int spawnPointOffset;

	public GIFRecorder gifRecorder;

	public float roundTimer;

	public float matchTimer;

	public bool suddenDeathStarted;

	public float goldenGoalScore;

	private Player lastGoldenPlayer;

	public bool decoysInMatch;

	private int seekerIndex;

	private float hideTimer;

	private float seekTimer;

	public Player lastHiderKilled;

	[Header("Powerups")]
	public GameObject powerupPrefab;

	public List<GameObject> spawnedPowerups = new List<GameObject>();

	public LayerMask layersToAvoidWhenSpawningClose;

	public LayerMask layersToAvoidWhenSpawningFar;

	[Header("Spawnable objects")]
	public GameObject discTrailPrefab;

	public Explosion explosionPrefab;

	public Explosion explosionFreezePrefab;

	public Explosion explosionFirePrefab;

	[Header("Particles")]
	public VFXRico VFXRico;

	public VFXRico newVFXRico;

	public GameObject impactLine;

	public GameObject impactLineMini;

	[Header("Scoring")]
	public Player lastPlayerStanding;

	public Dictionary<int, MatchScore> matchScores = new Dictionary<int, MatchScore>();

	public List<int> matchWinners = new List<int>();

	public Dictionary<int, int> crowns = new Dictionary<int, int>();

	public List<CharacterType> nextCharactersToUnlock = new List<CharacterType>();

	public List<KeyValuePair<CharacterType, HatType>> nextHatsToUnlock = new List<KeyValuePair<CharacterType, HatType>>();

	[Header("UI")]
	private Text UIHelperText;

	public GameObject[] controllers = new GameObject[6];

	public GameObject UIGameplay;

	public GameObject UIPowerupCanvasPrefab;

	public GameObject UIOffscreenPointer;

	public UICircleWipe transitionCircleWipe;

	public UICircleWipe victoryCircleWipe;

	private bool loadLevelCheatPressed;

	private bool loadLevelCheatFirstNumberPressed;

	private List<int> loadLevelNumbers = new List<int>(2);

	public Canvas levelNumberCanvas;

	public Text levelNumberText;

	private AchievementManager achievementManager;

	private float disconnectedControllerTimestamp;

	private static float disconnectedControllerCheckDelay = 1f;

	private bool checkForDisconnectedControllers;

	private IEnumerator spawnGoldenDiscRoutine;

	private GameState lastState;

	private IEnumerator pauseRecordingRoutine;

	private float HideDuration => Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekHideTime switch
	{
		HideAndSeekHideTime.Short => 10f, 
		HideAndSeekHideTime.Medium => 15f, 
		HideAndSeekHideTime.Long => 20f, 
		_ => 10f, 
	};

	private float SeekDuration => Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekTimeLimit switch
	{
		HideAndSeekTimeLimit.Short => 30f, 
		HideAndSeekTimeLimit.Medium => 55f, 
		HideAndSeekTimeLimit.Long => 130f, 
		_ => 50f, 
	};

	public bool IsInHidingPhase
	{
		get
		{
			if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
			{
				return false;
			}
			return hideTimer < HideDuration;
		}
	}

	public bool IsInSeekingPhase
	{
		get
		{
			if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
			{
				return false;
			}
			if (hideTimer >= HideDuration)
			{
				return seekTimer < SeekDuration;
			}
			return false;
		}
	}

	public bool IsInRevealPhase
	{
		get
		{
			if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
			{
				return false;
			}
			if (hideTimer >= HideDuration)
			{
				return seekTimer >= SeekDuration;
			}
			return false;
		}
	}

	private bool GetButtonDownAllPlayers(string name)
	{
		IList<Rewired.Player> list = ReInput.players.Players;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].GetButtonDown(name))
			{
				return true;
			}
		}
		return false;
	}

	private float GetAxisAllPlayers(string name)
	{
		IList<Rewired.Player> list = ReInput.players.Players;
		for (int i = 0; i < list.Count; i++)
		{
			if (Mathf.Abs(list[i].GetAxis(name)) > 0.1f)
			{
				return list[i].GetAxis(name);
			}
		}
		return 0f;
	}

	private void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		Debug.Log(args.controller.hardwareName);
		if (gameState == GameState.Playing || gameState == GameState.TransitionIn || gameState == GameState.TransitionOut)
		{
			Singleton<UIManager>.Instance.Pause();
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch || Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Xbox)
		{
			return;
		}
		UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
		if (currentMenu != null && currentMenu.MenuID == UIMenuBase.MenuType.Alert && currentMenu is UIMenuAlert)
		{
			currentMenu.Init();
			(currentMenu as UIMenuAlert).ControllerConnected();
			return;
		}
		UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Alert);
		Singleton<UIManager>.Instance.AddOverlayMenu(menu);
		if (menu is UIMenuAlert)
		{
			(menu as UIMenuAlert).ControllerConnected();
		}
	}

	private void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			if (!checkForDisconnectedControllers)
			{
				disconnectedControllerTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
				checkForDisconnectedControllers = true;
			}
		}
		else
		{
			if (gameState == GameState.Loading)
			{
				return;
			}
			if (gameState == GameState.Playing || gameState == GameState.TransitionIn || gameState == GameState.TransitionOut)
			{
				Singleton<UIManager>.Instance.Pause();
			}
			if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Xbox)
			{
				UIMenuBase menu = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Alert);
				Singleton<UIManager>.Instance.AddOverlayMenu(menu);
				if (menu is UIMenuAlert)
				{
					(menu as UIMenuAlert).ControllerDisconnected();
				}
			}
		}
	}

	public void OnGameOverlayActivated()
	{
		if (gameState == GameState.Playing || gameState == GameState.TransitionIn || gameState == GameState.TransitionOut)
		{
			Singleton<UIManager>.Instance.Pause();
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
	}

	public void OnGameOverlayClosed()
	{
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
	}

	public bool DoesPlayerHaveValidJoystick(Rewired.Player rewiredPlayer)
	{
		if (rewiredPlayer == null)
		{
			return false;
		}
		if (rewiredPlayer.controllers.joystickCount > 0)
		{
			if (rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid == Guid.Empty)
			{
				Debug.Log("Unknown controller detected");
				rewiredPlayer.controllers.ClearControllersOfType(ControllerType.Joystick);
				return false;
			}
			return true;
		}
		return false;
	}

	private void Start()
	{
		if (!SceneManager.GetSceneByName("LoadingScene").isLoaded)
		{
			if (Singleton<SettingsManager>.Instance == null)
			{
				base.gameObject.AddComponent<SettingsManager>();
			}
			if (Singleton<DataManager>.Instance == null)
			{
				base.gameObject.AddComponent<DataManager>();
			}
			StartCoroutine(DelayedStart());
		}
	}

	private IEnumerator DelayedStart()
	{
		Singleton<DataManager>.Instance.LoadPlatformData();
		Singleton<SettingsManager>.Instance.LoadOptions(null);
		Setup();
		yield return null;
		Singleton<UIManager>.Instance.Init();
	}

	public void Setup()
	{
		Physics2D.autoSyncTransforms = false;
		ResetRewired();
		Singleton<DecalManager>.Instance.Setup();
		Singleton<ParticleManager>.Instance.Setup();
		playersLeftStandingRoutine = CheckPlayersLeftStanding();
		if (!testMode)
		{
			gifRecorder = Singleton<Camera3D>.Instance.GetComponent<GIFRecorder>();
		}
		if (!Singleton<SettingsManager>.Instance.trailerMode)
		{
			levelNumberCanvas.enabled = false;
		}
		characterColors.Add(0, char0colours);
		characterColors.Add(1, char1colours);
		characterColors.Add(2, char2colours);
		characterColors.Add(3, char3colours);
		characterColors.Add(4, char4colours);
		characterColors.Add(5, char5colours);
		powerupBars = UIGameplay.GetComponentsInChildren<PowerupBar>();
		for (int i = 0; i < powerupBars.Length; i++)
		{
			powerupBars[i].IsDisplayed = false;
		}
		hintBars = UIGameplay.GetComponentsInChildren<HintBar>();
		alertBars = UIGameplay.GetComponentsInChildren<AlertBar>();
		Singleton<SettingsManager>.Instance.ApplySoundOptions();
		Singleton<SettingsManager>.Instance.ApplyControllerOptions();
		Singleton<SettingsManager>.Instance.ApplyKeyBindings();
		VFXRico.CreatePool(36);
		impactLine.CreatePool(12);
		impactLineMini.CreatePool(12);
		discPrefab.CreatePool(72);
		discTrailPrefab.CreatePool(72);
		explosionPrefab.CreatePool(72);
		explosionFirePrefab.CreatePool(72);
		explosionFreezePrefab.CreatePool(72);
		decoyPrefab.CreatePool(6);
		for (int j = 0; j < characters.Length; j++)
		{
			characters[j].CreatePool(6);
		}
		victoryCircleWipe.Reset();
		transitionCircleWipe.Reset();
		canPlatformSaveGifs = true;
		achievementManager = base.gameObject.AddComponent<SteamAchievementManager>();
		base.gameObject.AddComponent<SteamUpdater>();
		GC.Collect();
	}

	public void ResetRewired()
	{
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	public void ResetGame()
	{
		if (gameState == GameState.Loading)
		{
			abortLoading = true;
			StartCoroutine(ResetGameAfterLoadComplete());
		}
		else
		{
			CompleteResetGame();
		}
	}

	private IEnumerator ResetGameAfterLoadComplete()
	{
		while (abortLoading && gameState == GameState.Loading)
		{
			yield return null;
		}
		abortLoading = false;
		CompleteResetGame();
	}

	private void CompleteResetGame()
	{
		Singleton<LevelManager>.Instance.StopAllCoroutines();
		ExitMatch();
		CleanUpPlayers();
		Singleton<UIManager>.Instance.PopAllMenus();
		Singleton<UIManager>.Instance.uiLoadingOverlay.Close();
		Unpause();
		gameState = GameState.Menu;
		Singleton<DataManager>.Instance.LoadPlatformData();
		Singleton<SettingsManager>.Instance.LoadSystemLanguage();
		Singleton<SettingsManager>.Instance.ApplySoundOptions();
		Singleton<SettingsManager>.Instance.ApplyGraphicsOptions();
		Singleton<SettingsManager>.Instance.ApplyControllerOptions();
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<UIManager>.Instance.Init();
		StopAllCoroutines();
	}

	private void Update()
	{
		if (Singleton<SettingsManager>.Instance.debugMode && Input.GetKeyDown(KeyCode.Alpha8))
		{
			StartCoroutine(TakeAllMapScreenshots());
		}
		if (Singleton<SettingsManager>.Instance.debugMode)
		{
			if (Input.GetKeyDown(KeyCode.M))
			{
				StartCoroutine(SkipToNextLevel());
			}
			if ((Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.H))
			{
				Singleton<SettingsManager>.Instance.limitSeekAttempts = !Singleton<SettingsManager>.Instance.limitSeekAttempts;
				Debug.Log($"Limit seek attempts: {Singleton<SettingsManager>.Instance.limitSeekAttempts}");
			}
			if (Input.GetKey(KeyCode.BackQuote))
			{
				if (!levelNumberCanvas.enabled)
				{
					levelNumberCanvas.enabled = true;
					levelNumberText.text = Singleton<LevelManager>.Instance.GetCurrentLevelIndex().ToString();
				}
			}
			else
			{
				levelNumberCanvas.enabled = false;
			}
			if (Input.GetKeyDown(KeyCode.Insert))
			{
				UIMenuUnlocked uIMenuUnlocked = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Unlocked) as UIMenuUnlocked;
				uIMenuUnlocked.UnlockHat(new KeyValuePair<CharacterType, HatType>(CharacterType.Bacon, HatType.Glasses));
				Singleton<UIManager>.Instance.AddOverlayMenu(uIMenuUnlocked);
			}
			if (Input.GetKeyDown(KeyCode.H))
			{
				Debug.Log("Unlocking all hats...");
				int num = 0;
				while (UnlockHat())
				{
					num++;
				}
				Debug.Log($"Unlocked {num} hats");
			}
			if (Input.GetKeyDown(KeyCode.PageDown))
			{
				Debug.Log("Saving unlocked items to disk...");
				Singleton<SettingsManager>.Instance.SaveUnlockedItems(null);
			}
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch && checkForDisconnectedControllers && gameState != GameState.Loading && Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(disconnectedControllerTimestamp) >= disconnectedControllerCheckDelay)
		{
			checkForDisconnectedControllers = false;
			for (int i = 0; i < ReInput.players.playerCount; i++)
			{
				if (ReInput.players.GetPlayer(i).controllers.joystickCount == 0)
				{
					UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
					if (gameState == GameState.Playing || gameState == GameState.TransitionIn || gameState == GameState.TransitionOut)
					{
						Singleton<UIManager>.Instance.Pause();
					}
					else if (currentMenu != null && currentMenu.MenuID != UIMenuBase.MenuType.Instructions && currentMenu.MenuID != UIMenuBase.MenuType.Pause && currentMenu.MenuID != UIMenuBase.MenuType.RoundEnd && currentMenu.MenuID != UIMenuBase.MenuType.RoundTitles)
					{
						break;
					}
					if (Singleton<UIManager>.Instance.ShowSwitchControllerApplet((ReInput.controllers.joystickCount > 4) ? 6 : 4) && currentMenu != null)
					{
						currentMenu.UpdateMasterButtonPrompts();
					}
					break;
				}
			}
		}
		if (gameState == GameState.Playing)
		{
			roundTimer += Time.deltaTime;
			if (roundTimer > Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime && !suddenDeathStarted && Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && Singleton<LevelManager>.Instance.level != null && !Singleton<LevelManager>.Instance.level.isTutorialLevel && !Singleton<LevelManager>.Instance.isLevelBoundaryShrinking && OpponentsLeftStandingNow() > 1)
			{
				suddenDeathStarted = true;
				Singleton<LevelManager>.Instance.StartShrinkingLevelBoundary(Singleton<LevelManager>.Instance.GetRandomPointInLevelBounds(), showMarker: true);
			}
			if (Singleton<LevelManager>.Instance.isLevelBoundaryShrinking && OpponentsLeftStandingNow() < 2)
			{
				Singleton<LevelManager>.Instance.StopShrinkingLevelBoundary();
			}
			if (IsInHidingPhase)
			{
				hideTimer += Time.deltaTime;
				Singleton<UIManager>.Instance.SetSeekTimer(HideDuration - hideTimer + 1f, 6f, players.Where((Player x) => x.IsHider).ToList());
				if (hideTimer >= HideDuration)
				{
					foreach (Player player in players)
					{
						if (player.IsHider && !player.IsHidden && player.actorState == Actor.ActorState.Alive)
						{
							player.velocity = Vector2.zero;
							player.IsDisguised = true;
							player.timeInCurrentDisguise = 100f;
						}
					}
					StartCoroutine(StartSeekingPhase());
				}
			}
			if (IsInSeekingPhase)
			{
				seekTimer += Time.deltaTime;
				Singleton<UIManager>.Instance.SetSeekTimer(SeekDuration - seekTimer + 1f, 11f, players.Where((Player x) => x.IsSeeker).ToList());
				if (seekTimer >= SeekDuration)
				{
					HidersWin();
					if (Singleton<SettingsManager>.Instance.limitSeekAttempts)
					{
						seekAttemptBar.HideSeekAttemptBar();
					}
				}
			}
			if (IsInRevealPhase)
			{
				Singleton<UIManager>.Instance.HideSeekTimer();
			}
		}
		if (gameState != GameState.Playing && gameState != GameState.TransitionOut && gameState != GameState.TransitionIn)
		{
			return;
		}
		if (Singleton<SettingsManager>.Instance.showcaseMode)
		{
			for (int num2 = 0; num2 < ReInput.players.playerCount; num2++)
			{
				if (ReInput.players.GetPlayer(num2).GetButtonDown(6) || ReInput.players.GetPlayer(num2).GetButtonDown(2) || ReInput.players.GetPlayer(num2).GetButtonDown(3) || ReInput.players.GetPlayer(num2).GetButtonDown(4))
				{
					Singleton<UIManager>.Instance.ExitShowcase();
					break;
				}
			}
		}
		else
		{
			if (Singleton<SettingsManager>.Instance.numBots <= 0 || gameState != GameState.Playing)
			{
				return;
			}
			for (int num3 = 0; num3 < ReInput.players.playerCount; num3++)
			{
				if (ReInput.players.GetPlayer(num3).GetButtonDown("Start") && !IsRewiredPlayerAlreadyAssigned(ReInput.players.GetPlayer(num3)))
				{
					PlayerAI playerAI = FindAvailableAI();
					if (playerAI != null)
					{
						StartCoroutine(TakeOverAI(playerAI, ReInput.players.GetPlayer(num3)));
						break;
					}
				}
			}
		}
		IEnumerator TakeAllMapScreenshots()
		{
			for (int j = 0; j < Singleton<LevelManager>.Instance.levelAssets.Count; j++)
			{
				yield return new WaitForSecondsRealtime(1f);
				yield return StartCoroutine(TakeMapScreenShot());
				yield return StartCoroutine(SkipToNextLevel());
			}
		}
		IEnumerator TakeMapScreenShot()
		{
			foreach (Player player2 in players)
			{
				player2.gameObject.SetActive(value: false);
			}
			Singleton<UIManager>.Instance.gameObject.SetActive(value: false);
			yield return null;
			int currentLevelIndex = Singleton<LevelManager>.Instance.GetCurrentLevelIndex();
			Debug.Log($"Level index is {currentLevelIndex}");
			string text = Singleton<LevelManager>.Instance.GetLevelAssetByIndex(currentLevelIndex).name;
			ScreenCapture.CaptureScreenshot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), text + ".png"));
			yield return null;
			Singleton<UIManager>.Instance.gameObject.SetActive(value: true);
			foreach (Player player3 in players)
			{
				player3.gameObject.SetActive(value: true);
			}
		}
	}

	private bool IsRewiredPlayerAlreadyAssigned(Rewired.Player rewiredPlayer)
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			RewiredController component = Singleton<GameManager>.Instance.players[i].GetComponent<RewiredController>();
			if (component != null && component.rewiredPlayer == rewiredPlayer)
			{
				return true;
			}
		}
		return false;
	}

	private PlayerAI FindAvailableAI()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			PlayerAI component = Singleton<GameManager>.Instance.players[i].GetComponent<PlayerAI>();
			if (component != null && Singleton<GameManager>.Instance.players[i].actorState == Actor.ActorState.Alive)
			{
				return component;
			}
		}
		for (int j = 0; j < Singleton<GameManager>.Instance.players.Count; j++)
		{
			PlayerAI component = Singleton<GameManager>.Instance.players[j].GetComponent<PlayerAI>();
			if (component != null)
			{
				return component;
			}
		}
		return null;
	}

	private IEnumerator TakeOverAI(PlayerAI aiController, Rewired.Player rewiredPlayer)
	{
		yield return null;
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		aiController.gameObject.AddComponent<RewiredController>();
		aiController.gameObject.GetComponent<RewiredController>().rewiredID = rewiredPlayer.id;
		aiController.GetComponent<Player>().DropInPlayer();
		aiController.RemoveComponent();
		Singleton<SettingsManager>.Instance.numBots--;
		Singleton<SettingsManager>.Instance.numPlayers++;
	}

	public void UpdateGoldenDiscScore(Player player, float playerScore, bool isHoldingGoldenDisc)
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && gameState == GameState.Playing)
		{
			if (playerScore >= goldenGoalScore)
			{
				AddGoldenDiscPoint(player);
			}
			else if (isHoldingGoldenDisc && lastGoldenPlayer != null && player != lastGoldenPlayer && goldenGoalScore > SettingsManager.goalTimeMin)
			{
				goldenGoalScore = Mathf.Max(goldenGoalScore * 0.98f, SettingsManager.goalTimeMin);
				lastGoldenPlayer = player;
			}
		}
	}

	public void ExitMatch()
	{
		CleanUpRound();
		Singleton<LevelManager>.Instance.UnloadCurrentLevel();
		Singleton<TimeKeeper>.Instance.ResetTimeScale();
	}

	public Character GetCharacterPrefab(CharacterType type)
	{
		for (int i = 0; i < characters.Length; i++)
		{
			if (characters[i].characterType == type)
			{
				return characters[i];
			}
		}
		Debug.LogError("Character " + type.ToString() + " prefab not found");
		return null;
	}

	public bool IsDLCUnlocked(DownloadableContentID dlcID, bool debugLog = false)
	{
		if (dlcID == DownloadableContentID.None)
		{
			return true;
		}
		string text = Enum.GetName(typeof(DownloadableContentID), dlcID);
		if (text == null)
		{
			return false;
		}
		return DlcPlatformManager.Instance.IsInstalled(text, debugLog);
	}

	public void UnlockDLCItems()
	{
		if (IsDLCUnlocked(DownloadableContentID.FreshFlavorsPack, debugLog: true))
		{
			for (int i = 0; i < hatLibrary.Length; i++)
			{
				if (hatLibrary[i].dlcID != DownloadableContentID.FreshFlavorsPack)
				{
					continue;
				}
				for (int j = 0; j < characters.Length; j++)
				{
					if (CanCharacterWearHat(characters[j].characterType, hatLibrary[i].hatType))
					{
						UnlockHatForCharacter(characters[j].characterType, hatLibrary[i].hatType, saveData: false);
					}
				}
			}
			for (int k = 0; k < characters.Length; k++)
			{
				if (!Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Contains(characters[k].characterType) && dlcOneCharacters.Contains(characters[k].characterType))
				{
					Debug.Log("Unlocking DLC character " + characters[k].characterType);
					Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Add(characters[k].characterType);
					Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Add(characters[k].characterType);
				}
			}
		}
		else
		{
			for (int l = 0; l < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; l++)
			{
				for (int num = Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[l].unlockedHats.Count - 1; num >= 0; num--)
				{
					if (!CanCharacterWearHat(Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[l].characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[l].unlockedHats[num]))
					{
						Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[l].unlockedHats.RemoveAt(num);
					}
				}
			}
		}
		if (!IsDLCUnlocked(DownloadableContentID.JustDessertsPack, debugLog: true))
		{
			return;
		}
		for (int m = 0; m < characters.Length; m++)
		{
			if (!Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Contains(characters[m].characterType) && dlcTwoCharacters.Contains(characters[m].characterType))
			{
				Debug.Log("Unlocking DLC character " + characters[m].characterType);
				Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Add(characters[m].characterType);
				Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Add(characters[m].characterType);
			}
		}
	}

	public bool IsCharacterUnlocked(CharacterType type)
	{
		if (Singleton<SettingsManager>.Instance.demoMode)
		{
			return demoCharacters.Contains(type);
		}
		if (dlcOneCharacters.Contains(type))
		{
			if (IsDLCUnlocked(DownloadableContentID.FreshFlavorsPack))
			{
				return true;
			}
			return false;
		}
		if (dlcTwoCharacters.Contains(type))
		{
			if (IsDLCUnlocked(DownloadableContentID.JustDessertsPack))
			{
				return true;
			}
			return false;
		}
		return Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Contains(type);
	}

	private bool IsCharacterInDLC(CharacterType type)
	{
		if (dlcOneCharacters.Contains(type))
		{
			return true;
		}
		if (dlcTwoCharacters.Contains(type))
		{
			return true;
		}
		return false;
	}

	public Hat GetHat(HatType targetHatType)
	{
		for (int i = 0; i < hatLibrary.Length; i++)
		{
			if (hatLibrary[i].hatType == targetHatType)
			{
				return hatLibrary[i];
			}
		}
		return null;
	}

	public bool IsHatUnlockedForCharacter(CharacterType characterType, HatType hatType)
	{
		return Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.FirstOrDefault((UnlockedCharacterHats x) => x.characterType == characterType)?.unlockedHats.Contains((int)hatType) ?? false;
	}

	public void UnlockHatForCharacter(CharacterType characterType, HatType hatType, bool saveData = true)
	{
		bool flag = false;
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			flag = true;
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Contains((int)hatType))
			{
				return;
			}
			Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Add((int)hatType);
			if (!Singleton<SettingsManager>.Instance.UnlockedItems.hatTypesUnlockedSoFar.Contains((int)hatType))
			{
				Singleton<SettingsManager>.Instance.UnlockedItems.hatTypesUnlockedSoFar.Add((int)hatType);
			}
			if (saveData)
			{
				Singleton<SettingsManager>.Instance.SaveUnlockedItems(delegate(bool success)
				{
					Debug.Log("WARNING - Finished saving unlocked items data with result: " + success + " ");
				});
			}
			break;
		}
		if (!flag)
		{
			Debug.Log("Character type " + characterType.ToString() + " not found in save file, adding character type");
			Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Add(new UnlockedCharacterHats(characterType));
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count - 1].characterType == characterType)
			{
				Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count - 1].unlockedHats.Add((int)hatType);
			}
		}
	}

	public int NumHatsUnlockedForCharacter(CharacterType characterType)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.PC)
		{
			int num = 0;
			for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
			{
				if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
				{
					continue;
				}
				for (int j = 0; j < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count; j++)
				{
					if (CanCharacterWearHat(characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j]))
					{
						num++;
					}
				}
				break;
			}
			return num;
		}
		UnlockedCharacterHats unlockedCharacterHats = Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.FirstOrDefault((UnlockedCharacterHats x) => x.characterType == characterType);
		if (unlockedCharacterHats != null && unlockedCharacterHats.unlockedHats != null)
		{
			return unlockedCharacterHats.unlockedHats.Count;
		}
		return 0;
	}

	public bool CanCharacterWearHat(CharacterType characterType, HatType hatType)
	{
		switch (hatType)
		{
		case HatType.Crown:
			Debug.LogWarning("Don't equip crowns using CanCharacterWearHat() check!");
			return false;
		case HatType.None:
			return true;
		default:
		{
			Hat hat = GetHat(hatType);
			if (hat == null)
			{
				return false;
			}
			if (hat.bannedCharacters.Contains(characterType))
			{
				return false;
			}
			if (!IsDLCUnlocked(hat.dlcID))
			{
				return false;
			}
			return true;
		}
		}
	}

	public HatType GetNextUnlockedHatForCharacter(CharacterType characterType, HatType currentHat)
	{
		bool flag = false;
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			for (int j = 0; j < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count; j++)
			{
				if (flag && CanCharacterWearHat(characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j]))
				{
					return (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j];
				}
				if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j] == (int)currentHat)
				{
					flag = true;
				}
			}
			break;
		}
		return HatType.None;
	}

	public HatType GetPreviousUnlockedHatForCharacter(CharacterType characterType, HatType currentHat)
	{
		bool flag = false;
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			for (int num = Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count - 1; num >= 0; num--)
			{
				if (flag && CanCharacterWearHat(characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num]))
				{
					return (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num];
				}
				if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num] == (int)currentHat)
				{
					flag = true;
				}
			}
			break;
		}
		return HatType.None;
	}

	public HatType GetLastUnlockedHatForCharacter(CharacterType characterType)
	{
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			for (int num = Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count - 1; num >= 0; num--)
			{
				if (CanCharacterWearHat(characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num]))
				{
					return (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num];
				}
			}
			break;
		}
		return HatType.None;
	}

	public int GetUnlockedHatIndex(CharacterType characterType, HatType targetHat)
	{
		int num = 0;
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			for (int j = 0; j < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count; j++)
			{
				if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j] == (int)targetHat)
				{
					return num;
				}
				if (CanCharacterWearHat(characterType, (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j]))
				{
					num++;
				}
			}
			break;
		}
		return num;
	}

	public void SaveLastSelectedHat(CharacterType characterType, HatType targetHat)
	{
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType != characterType)
			{
				continue;
			}
			for (int j = 0; j < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count; j++)
			{
				if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[j] == (int)targetHat)
				{
					Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].lastSelectedHat = targetHat;
					return;
				}
			}
			Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].lastSelectedHat = HatType.None;
			break;
		}
	}

	public HatType GetLastSelectedHatForCharacter(CharacterType characterType)
	{
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType == characterType)
			{
				return Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].lastSelectedHat;
			}
		}
		return HatType.None;
	}

	public int GetNumUnlockedHatsForCharacter(CharacterType characterType)
	{
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType == characterType)
			{
				return Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count;
			}
		}
		return 0;
	}

	private float GetRatioOfHatsUnlockedForCharacter(CharacterType characterType)
	{
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType == characterType)
			{
				return (float)(Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count - 1) / ((float)hatLibrary.Length - 1f);
			}
		}
		return 0f;
	}

	public bool AreAnyCharactersLeftToUnlock()
	{
		int num = Enum.GetNames(typeof(CharacterType)).Length - 1;
		if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Count < num)
		{
			Debug.Log("There are still characters left to unlock...");
			return true;
		}
		Debug.Log("All characters have been unlocked");
		return false;
	}

	public CharacterType GetRandomLockedCharacter()
	{
		List<CharacterType> list = new List<CharacterType>();
		for (int i = 0; i < characters.Length; i++)
		{
			if (!Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Contains(characters[i].characterType) && !IsCharacterInDLC(characters[i].characterType))
			{
				list.Add(characters[i].characterType);
			}
		}
		if (list.Count > 0)
		{
			return list[UnityEngine.Random.Range(0, list.Count)];
		}
		return CharacterType.None;
	}

	public HatType GetRandomLockedHatForCharacter(CharacterType characterType)
	{
		HatType result = HatType.None;
		int num = UnityEngine.Random.Range(0, hatLibrary.Length);
		for (int i = 0; i < hatLibrary.Length; i++)
		{
			int num2 = (i + num) % hatLibrary.Length;
			if (hatLibrary[num2].hatType != HatType.None && hatLibrary[num2].hatType != HatType.Crown && !IsHatUnlockedForCharacter(characterType, hatLibrary[num2].hatType) && hatLibrary[num2].dlcID == DownloadableContentID.None)
			{
				if (Singleton<SettingsManager>.Instance.UnlockedItems.hatTypesUnlockedSoFar.Count < 10 && Singleton<SettingsManager>.Instance.UnlockedItems.hatTypesUnlockedSoFar.Contains((int)hatLibrary[num2].hatType))
				{
					Debug.Log($"Skipping {hatLibrary[num2].hatType} because it has already been unlocked by another character");
				}
				else if (CanCharacterWearHat(characterType, hatLibrary[num2].hatType))
				{
					return hatLibrary[num2].hatType;
				}
			}
		}
		return result;
	}

	public HatType GetRandomUnlockedHatForCharacter(CharacterType characterType)
	{
		HatType result = HatType.None;
		int num = UnityEngine.Random.Range(1, GetNumUnlockedHatsForCharacter(characterType));
		for (int i = 0; i < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].characterType == characterType)
			{
				num %= Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats.Count;
				result = (HatType)Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacterHats[i].unlockedHats[num];
			}
		}
		return result;
	}

	public bool UnlockCharacter(bool elementOfChance = true)
	{
		int num = 2;
		float probability = 100f;
		if (elementOfChance)
		{
			probability = 1f / (float)num * (float)(Singleton<SettingsManager>.Instance.UnlockedItems.matchesPlayedSinceLastCharacterUnlock % (num + 1)) * 100f;
		}
		if (!AreAnyCharactersLeftToUnlock())
		{
			Debug.Log("No characters left to unlock");
		}
		Debug.Log("Chance of unlocking new character: " + probability);
		if (Chance.PercentageTrue(probability))
		{
			CharacterType randomLockedCharacter = GetRandomLockedCharacter();
			if (randomLockedCharacter != CharacterType.None)
			{
				nextCharactersToUnlock.Add(randomLockedCharacter);
				Singleton<SettingsManager>.Instance.UnlockedItems.matchesPlayedSinceLastCharacterUnlock = 0;
				if (!Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Contains(randomLockedCharacter))
				{
					Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Add(randomLockedCharacter);
					Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Add(randomLockedCharacter);
					Debug.Log("Next character to unlock is: " + randomLockedCharacter);
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public bool UnlockHat()
	{
		Debug.Log("-------- Unlocking hat logic follows --------");
		List<CharacterType> list = new List<CharacterType>(16);
		int num = UnityEngine.Random.Range(0, Singleton<SettingsManager>.Instance.numPlayers);
		for (int i = 0; i < players.Count; i++)
		{
			int index = (num + i) % players.Count;
			if ((Singleton<SettingsManager>.Instance.numPlayers > 1 && players[index].playerType == PlayerType.Human) || matchWinners.Contains(GetRowID(players[index])))
			{
				if (Chance.PercentageTrue(Easing.EaseOut(GetRatioOfHatsUnlockedForCharacter(players[index].character.characterType)) * 70f))
				{
					Debug.Log("Skipped " + players[index].character.characterType.ToString() + " based on " + Easing.EaseOut(GetRatioOfHatsUnlockedForCharacter(players[index].character.characterType)) * 70f + "% chance");
				}
				else
				{
					list.Add(players[index].character.characterType);
				}
			}
		}
		if (Chance.PercentageTrue(50f) && list.Count > 1)
		{
			Debug.Log("Human players sorted by least number of hats");
			int num2 = int.MaxValue;
			int num3 = 0;
			for (int j = 0; j < list.Count; j++)
			{
				int num4 = NumHatsUnlockedForCharacter(list[j]);
				Debug.Log($"{list[j]} has {num4} hats");
				if (num4 < num2)
				{
					num2 = num4;
					num3 = j;
				}
			}
			if (num3 > 0)
			{
				CharacterType characterType = list[num3];
				list.RemoveAt(num3);
				list.Insert(0, characterType);
				Debug.Log($"{characterType} moved to the front of the list!");
			}
		}
		else
		{
			Debug.Log("Human players sorted randomly");
		}
		num = UnityEngine.Random.Range(0, players.Count);
		for (int k = 0; k < players.Count; k++)
		{
			int index2 = (num + k) % players.Count;
			if (players[index2].character.characterType != CharacterType.None && !list.Contains(players[index2].character.characterType) && !Chance.PercentageTrue(Easing.EaseOut(GetRatioOfHatsUnlockedForCharacter(players[index2].character.characterType)) * 75f))
			{
				list.Add(players[index2].character.characterType);
			}
		}
		num = UnityEngine.Random.Range(0, Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Count);
		for (int l = 0; l < Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Count; l++)
		{
			int index3 = (num + l) % Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters.Count;
			if (!list.Contains(Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters[index3]))
			{
				list.Add(Singleton<SettingsManager>.Instance.UnlockedItems.unlockedCharacters[index3]);
			}
		}
		bool flag = false;
		HatType hatType = HatType.None;
		CharacterType characterType2 = CharacterType.None;
		for (int m = 0; m < list.Count; m++)
		{
			hatType = GetRandomLockedHatForCharacter(list[m]);
			if (hatType != HatType.None)
			{
				characterType2 = list[m];
				flag = true;
				break;
			}
		}
		if (flag && characterType2 != CharacterType.None && hatType != HatType.None)
		{
			nextHatsToUnlock.Add(new KeyValuePair<CharacterType, HatType>(characterType2, hatType));
			UnlockHatForCharacter(characterType2, hatType);
			Debug.Log($"Unlocked {hatType} for {characterType2}");
			return true;
		}
		return false;
	}

	public void ClearNextItemsToUnlock()
	{
		nextHatsToUnlock.Clear();
		nextCharactersToUnlock.Clear();
	}

	public void AddLobbyBots()
	{
	}

	public void RemoveLobbyBots()
	{
	}

	public IEnumerator StartMatch(bool rematch)
	{
		Singleton<TimeKeeper>.Instance.ResetTimeScale();
		matchWinners.Clear();
		Singleton<SettingsManager>.Instance.tieBreaker = false;
		matchTimer = 0f;
		decoysInMatch = false;
		if (rematch)
		{
			for (int i = 0; i < players.Count; i++)
			{
				players[i].gameObject.SetActive(value: true);
				players[i].Init(0, resetPowerups: true);
				players[i].WipeMetadata();
			}
		}
		else
		{
			UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
			if (uIMenuLobby == null)
			{
				Debug.LogError("Could not find UIMenuLobby! Aborting game start");
			}
			for (int j = 0; j < uIMenuLobby.uiPlayers.Length; j++)
			{
				if (uIMenuLobby.uiPlayers[j].readyState == UILobbyPlayer.ReadyState.Ready)
				{
					CreatePlayer(uIMenuLobby.uiPlayers[j]);
				}
			}
		}
		if (players.Count == 0)
		{
			Singleton<UIManager>.Instance.QuitMatch();
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
			Singleton<UIManager>.Instance.uiLoadingOverlay.Close();
			yield break;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			Singleton<SettingsManager>.Instance.SetupGoldenDisc();
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
		{
			Singleton<SettingsManager>.Instance.SetupKingOfTheHill();
		}
		InitMatchScores();
		if (!rematch)
		{
			Singleton<LevelManager>.Instance.BuildMatchPlaylist(rematch);
		}
		yield return StartCoroutine(LoadNextRound());
		Singleton<AudioManager>.Instance.PlayGameplayMusic();
		StartRound(rematch);
	}

	private void CreatePlayer(UILobbyPlayer lobbyPlayer)
	{
		Player player = UnityEngine.Object.Instantiate(playerPrefab, new Vector3(1000f + (float)(lobbyPlayer.playerID * 10), 1000f + (float)(lobbyPlayer.playerID * 10), Actor.zPos), Quaternion.identity);
		players.Add(player);
		player.interpolatedTransform.ForgetPreviousTransforms();
		if (lobbyPlayer.playerType == PlayerType.Human)
		{
			player.gameObject.AddComponent<RewiredController>();
			if (lobbyPlayer.rewiredPlayer == null)
			{
				player.GetComponent<RewiredController>().rewiredID = 0;
			}
			else
			{
				player.GetComponent<RewiredController>().rewiredID = lobbyPlayer.playerID;
			}
		}
		else
		{
			player.gameObject.AddComponent<PlayerAI>().InitPathSettings();
		}
		player.playerType = lobbyPlayer.playerType;
		player.playerID = lobbyPlayer.playerID;
		player.team = lobbyPlayer.team;
		player.SetupCharacter(lobbyPlayer.character);
		GameObject obj = UnityEngine.Object.Instantiate(Singleton<GameManager>.Instance.UIOffscreenPointer, new Vector3(0f, 0f, -2f), Quaternion.identity);
		obj.transform.SetParent(Singleton<GameManager>.Instance.UIGameplay.transform, worldPositionStays: false);
		obj.GetComponent<OffscreenPointer>().AssignTarget(player);
	}

	public void CreateDecoy(Player player)
	{
		Decoy decoy = decoyPrefab.Spawn(null, player.transform.position, Quaternion.identity);
		Physics2D.IgnoreCollision(decoy.physicsCollider, player.physicsCollider);
		activeDecoys.Add(decoy);
		decoy.interpolatedTransform.ForgetPreviousTransforms();
		decoy.owner = player;
		decoy.activePowerup = player.activePowerup;
		decoy.playerType = PlayerType.Bot;
		decoy.playerID = player.playerID;
		decoy.team = player.team;
		decoy.SetupCharacter(player.character);
		decoy.Init(Mathf.Max(1, player.HasDiscs), resetPowerups: true);
		DecoyAI component = decoy.gameObject.GetComponent<DecoyAI>();
		if (component != null)
		{
			component.InitPathSettings();
			component.Init();
			Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
			if (Vector2.Dot(normalized, player.transform.up) > 0.5f)
			{
				normalized *= -1f;
			}
			component.ForceDash(normalized);
		}
		if (!decoysInMatch)
		{
			decoysInMatch = true;
		}
		Singleton<Camera3D>.Instance.AddCameraTarget(decoy.transform, 1f, 1f);
	}

	public IEnumerator LoadNextRound()
	{
		gameState = GameState.Loading;
		yield return StartCoroutine(Singleton<LevelManager>.Instance.LoadNewLevel());
		if (abortLoading)
		{
			gameState = GameState.Menu;
			Debug.Log("Aborted loading here");
		}
		else
		{
			yield return StartCoroutine(PrepareRound());
		}
	}

	private IEnumerator PrepareRound()
	{
		suddenDeathStarted = false;
		roundTimer = 0f;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			goldenGoalScore = Singleton<SettingsManager>.Instance.goalTimeStart;
			lastGoldenPlayer = null;
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
		{
			crowns.Clear();
			crowns.Add(1, 0);
			crowns.Add(2, 0);
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (roundNumber == 0)
			{
				seekerIndex = UnityEngine.Random.Range(0, players.Count);
			}
			else
			{
				seekerIndex = (seekerIndex + 1) % players.Count;
			}
		}
		if (abortLoading)
		{
			Debug.Log("Aborted loading here");
			yield break;
		}
		spawnPointOffset = UnityEngine.Random.Range(0, 2);
		StartCoroutine(Singleton<LevelManager>.Instance.InitNewLevel(spawnPointOffset));
		int num = 0;
		int num2 = 0;
		int num3 = UnityEngine.Random.Range(1, 3);
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < players.Count; i++)
		{
			if (Singleton<SettingsManager>.Instance.tieBreaker && !matchWinners.Contains(GetRowID(players[i])))
			{
				players[i].actorState = Actor.ActorState.Dead;
				players[i].DisableColliders();
				players[i].gameObject.SetActive(value: false);
				continue;
			}
			if ((players.Count == 2 || (Singleton<SettingsManager>.Instance.tieBreaker && matchWinners.Count == 2)) && Singleton<LevelManager>.Instance.level.playerSpawnPoints.Count > 3)
			{
				num = spawnPointOffset + num2 * 2;
			}
			if (Singleton<SettingsManager>.Instance.teamMatch && Singleton<LevelManager>.Instance.level.playerSpawnPoints.Count > 5)
			{
				if (players[i].team == num3)
				{
					switch (num4)
					{
					case 0:
						num = 0;
						break;
					case 1:
						num = 1;
						break;
					case 2:
						num = 4;
						break;
					case 3:
						num = 5;
						break;
					case 4:
						num = 3;
						break;
					case 5:
						num = 2;
						break;
					}
					num4++;
				}
				else
				{
					switch (num5)
					{
					case 0:
						num = 2;
						break;
					case 1:
						num = 3;
						break;
					case 2:
						num = 5;
						break;
					case 3:
						num = 4;
						break;
					case 4:
						num = 1;
						break;
					case 5:
						num = 0;
						break;
					}
					num5++;
				}
			}
			if (num > Singleton<LevelManager>.Instance.level.playerSpawnPoints.Count)
			{
				Debug.LogError("spawnIndex is greater than spawn point count!");
				num = 0;
			}
			players[i].transform.SetParent(null);
			players[i].transform.position = new Vector3(Singleton<LevelManager>.Instance.level.playerSpawnPoints[num].position.x, Singleton<LevelManager>.Instance.level.playerSpawnPoints[num].position.y, Actor.zPos);
			players[i].GetComponent<InterpolatedTransform>().ForgetPreviousTransforms();
			num2++;
			num++;
			players[i].Init(0, roundNumber == 0);
			hintBars[i].Init(players[i]);
			players[i].GetComponent<PlayerInput>().Init(hintBars[i]);
			alertBars[i].Init(players[i]);
			if ((bool)players[i].GetComponent<PlayerAI>())
			{
				players[i].GetComponent<PlayerAI>().Init();
			}
			if (players[i].IsSeeker)
			{
				seekAttemptBar.Init(players[i]);
			}
		}
		Singleton<LevelManager>.Instance.StartMovingObjects();
		roundNumber++;
		Singleton<CameraFX>.Instance.ResetCamera();
		Singleton<UIManager>.Instance.EnableGameplayCanvas();
		victoryCircleWipe.Reset();
		isKillingAllDecoys = false;
	}

	public void StartRound(bool rematch = false)
	{
		gameState = GameState.TransitionIn;
		StartCoroutine(StartRoundSequence(rematch));
	}

	private IEnumerator StartRoundSequence(bool rematch)
	{
		Singleton<UIManager>.Instance.HideSeekTimer();
		StartCoroutine(transitionCircleWipe.TransitionOut());
		Singleton<AudioManager>.Instance.SetGameplayMusicState(AudioManager.GameplayMusicState.Match);
		yield return new WaitForSecondsRealtime(0.1f);
		List<int> alreadySpawnedIndicies = new List<int>();
		Singleton<Camera3D>.Instance.AddAllSceneryToCamera();
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			lastHiderKilled = null;
			hideTimer = 0f;
			seekTimer = 0f;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			for (int i = 0; i < players.Count; i++)
			{
				if (players[i].IsSeeker)
				{
					alreadySpawnedIndicies.Add(i);
					yield return StartCoroutine(SpawnPlayer(players[i]));
					yield return ShowSeeker(GetSeeker());
					break;
				}
			}
		}
		else if (!Singleton<SettingsManager>.Instance.teamMatch)
		{
			for (int j = 0; j < players.Count; j++)
			{
				if (IsLeadingPlayer(players[j]))
				{
					alreadySpawnedIndicies.Add(j);
					yield return StartCoroutine(SpawnPlayer(players[j]));
					break;
				}
			}
		}
		for (int k = 0; k < players.Count; k++)
		{
			if (!alreadySpawnedIndicies.Contains(k))
			{
				yield return StartCoroutine(SpawnPlayer(players[k]));
			}
		}
		if (!Singleton<SettingsManager>.Instance.showcaseMode && !Singleton<SettingsManager>.Instance.trailerMode && (Singleton<LevelManager>.Instance.level.isTutorialLevel || (!rematch && !Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial && Singleton<LevelManager>.Instance.matchIndex == 1)))
		{
			yield return new WaitForSecondsRealtime(0.3f);
			UIMenuInstructions uIMenuInstructions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Instructions) as UIMenuInstructions;
			if (uIMenuInstructions != null)
			{
				Singleton<UIManager>.Instance.PushMenu(uIMenuInstructions);
				uIMenuInstructions.ShowInstructionPanels();
				while (Singleton<UIManager>.Instance.GetCurrentMenu() != null && Singleton<UIManager>.Instance.GetCurrentMenu().MenuID == UIMenuBase.MenuType.Instructions)
				{
					yield return null;
				}
				yield return new WaitForSecondsRealtime(0.2f);
			}
		}
		if (!Singleton<SettingsManager>.Instance.showcaseMode && !Singleton<SettingsManager>.Instance.trailerMode)
		{
			UIMenuRoundTitles menuRoundTitles = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundTitles) as UIMenuRoundTitles;
			if (menuRoundTitles != null)
			{
				Singleton<UIManager>.Instance.PushMenu(menuRoundTitles);
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
				{
					yield return new WaitForSecondsRealtime(0.3f);
					menuRoundTitles.ShowHideTitle();
				}
				else
				{
					menuRoundTitles.ShowRoundTitles();
				}
				while (menuRoundTitles.titlesShowing)
				{
					yield return null;
				}
				while (gameState == GameState.Paused)
				{
					yield return null;
				}
			}
		}
		else
		{
			Singleton<TimeKeeper>.Instance.PauseTime();
			yield return new WaitForSecondsRealtime(1f);
			Singleton<TimeKeeper>.Instance.UnPauseTime();
		}
		gameState = GameState.Playing;
		for (int l = 0; l < players.Count; l++)
		{
			if (!Singleton<SettingsManager>.Instance.tieBreaker || matchWinners.Contains(GetRowID(players[l])))
			{
				if (players[l].IsSeeker)
				{
					players[l].gameObject.layer = LayerMask.NameToLayer("PlayerInsides");
					players[l].seekAttemptsRemaining = GetNumSeekAttemptsForPlayer(players[l]);
					Singleton<SettingsManager>.Instance.limitSeekAttempts = Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekAttempts != HideAndSeekAttempts.Unlimited;
				}
				else
				{
					players[l].Go();
				}
			}
		}
		for (int m = 0; m < powerupBars.Length; m++)
		{
			powerupBars[m].HideBar(0.2f);
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
		{
			gifRecorder.StartRecording();
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			SpawnGoldenDisc(0.25f);
		}
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			Singleton<LevelManager>.Instance.StartSpawningPowerups();
		}
	}

	public int GetNumSeekAttempts()
	{
		if (Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekAttempts == HideAndSeekAttempts.Few)
		{
			return 3 + (players.Count - 1);
		}
		return 6 + (players.Count - 1);
	}

	public int GetNumSeekAttemptsForPlayer(Player player)
	{
		int num = 0;
		if (IsALosingPlayer(player))
		{
			num += 2;
		}
		return GetNumSeekAttempts() + num;
	}

	private IEnumerator SpawnPlayer(Player player)
	{
		if (Singleton<SettingsManager>.Instance.tieBreaker && !matchWinners.Contains(Singleton<SettingsManager>.Instance.teamMatch ? player.team : player.playerID))
		{
			yield break;
		}
		Vector3 playerPos = player.transform.position;
		player.GetReady();
		yield return new WaitForSeconds((Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots > 4) ? 0.35f : 0.4f);
		int playerIndex = players.IndexOf(player);
		if (player.playerType == PlayerType.Bot && !Singleton<SettingsManager>.Instance.showcaseMode && !Singleton<SettingsManager>.Instance.trailerMode)
		{
			player.alertBar.ShowAlert(AlertBar.AlertType.PlayerID, 0f, 0f);
		}
		if (player.GetComponent<PlayerInput>().roundsSinceLastInputReceived > 2 && !Singleton<SettingsManager>.Instance.trailerMode && Singleton<SettingsManager>.Instance.GameOptions.conventionMode)
		{
			Debug.Log("too many rounds since last input");
			player.DropInAI();
			yield return new WaitForSeconds(2f);
		}
		else
		{
			if (IsLeadingPlayer(player))
			{
				player.character.SetFacialExpression(FacialExpression.happy);
			}
			if (player.alertBar.currentAlert != AlertBar.AlertType.None)
			{
				if (powerupBars[playerIndex].GetNumIcons(player.activePowerup) > 0 || player.IsPlayerElligibleForAutobalanceShield())
				{
					yield return new WaitForSeconds((Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots > 4) ? 0.4f : 0.5f);
					player.alertBar.HideAlert();
				}
				else
				{
					player.alertBar.HideAlert(0.7f);
					yield return new WaitForSeconds(0.2f);
				}
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			powerupBars[playerIndex].gameObject.SetActive(value: true);
			powerupBars[playerIndex].transform.position = playerPos;
			powerupBars[playerIndex].Init(player, player.IsPlayerElligibleForAutobalanceShield());
			yield return new WaitForSeconds((float)powerupBars[playerIndex].iconsShowing * 0.1f);
		}
	}

	public void SpawnGoldenDisc(float delay = 0.4f)
	{
		if (spawnGoldenDiscRoutine != null)
		{
			StopCoroutine(spawnGoldenDiscRoutine);
		}
		spawnGoldenDiscRoutine = SpawnGoldenDiscAfterDelay(delay);
		StartCoroutine(spawnGoldenDiscRoutine);
	}

	private IEnumerator SpawnGoldenDiscAfterDelay(float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		int i = 0;
		Vector3 point;
		while (!Singleton<LevelManager>.Instance.FindSuitableDiscSpawnPoint(out point) && i < 180)
		{
			if (i == 179)
			{
				if (Singleton<LevelManager>.Instance.level != null && Singleton<LevelManager>.Instance.level.playerSpawnPoints.Count > 0)
				{
					_ = Singleton<LevelManager>.Instance.level.playerSpawnPoints[UnityEngine.Random.Range(0, Singleton<LevelManager>.Instance.level.playerSpawnPoints.Count - 1)].position;
				}
				else
				{
					_ = Vector3.zero;
				}
			}
			i++;
			yield return null;
		}
		Disc disc = Singleton<GameManager>.Instance.discPrefab.Spawn(point, Quaternion.identity);
		disc.throwDirection = Vector2.zero;
		disc.aimPower = 0f;
		disc.Init(isDropped: true, isMiniDisc: false, isGoldenDisc: true);
		activeDiscs.Add(disc);
		Singleton<Camera3D>.Instance.Shake(0.2f, 0.2f);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/spawn_golden_boomerang", point);
	}

	public void CleanUpRound()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			lastHiderKilled = null;
			hideTimer = 0f;
			seekTimer = 0f;
		}
		foreach (Player player in players)
		{
			player.DisableActor();
			SceneManager.MoveGameObjectToScene(player.gameObject, SceneManager.GetActiveScene());
		}
		CleanUpDiscs();
		CleanUpExplosives();
		CleanUpFires();
		CleanUpSlimes();
		CleanUpPowerups();
		CleanUpDecoys();
		Singleton<ParticleManager>.Instance.CleanUpParticles();
		Singleton<DecalManager>.Instance.CleanUpDecals();
		Singleton<CellManager>.Instance.CleanUp();
		for (int i = 0; i < powerupBars.Length; i++)
		{
			powerupBars[i].IsDisplayed = false;
		}
		for (int j = 0; j < alertBars.Length; j++)
		{
			alertBars[j].HideAlert();
		}
		seekAttemptBar.HideSeekAttemptBar();
	}

	private void CleanUpDiscs()
	{
		foreach (Disc activeDisc in activeDiscs)
		{
			Singleton<Camera3D>.Instance.RemoveCameraTarget(activeDisc.transform);
			UnityEngine.Object.Destroy(activeDisc.gameObject);
		}
		activeDiscs.Clear();
	}

	private void CleanUpExplosives()
	{
		foreach (GameObject activeExplosive in activeExplosives)
		{
			Singleton<Camera3D>.Instance.RemoveCameraTarget(activeExplosive.transform);
			activeExplosive.Recycle();
		}
		activeExplosives.Clear();
	}

	private void CleanUpPowerups()
	{
		foreach (GameObject spawnedPowerup in spawnedPowerups)
		{
			UnityEngine.Object.Destroy(spawnedPowerup);
		}
		spawnedPowerups.Clear();
	}

	private void CleanUpDecoys()
	{
		foreach (Decoy activeDecoy in activeDecoys)
		{
			Singleton<Camera3D>.Instance.RemoveCameraTarget(activeDecoy.transform);
			activeDecoy.gameObject.Recycle();
		}
		activeDecoys.Clear();
	}

	public void CleanUpPlayers()
	{
		foreach (Player player in players)
		{
			Singleton<Camera3D>.Instance.RemoveCameraTarget(player.transform);
			UnityEngine.Object.Destroy(player.gameObject);
		}
		players.Clear();
	}

	private void CleanUpFires()
	{
		foreach (GameObject activeFire in activeFires)
		{
			activeFire.Recycle();
		}
		activeFires.Clear();
	}

	private void CleanUpSlimes()
	{
		foreach (GameObject activeSlime in activeSlimes)
		{
			activeSlime.Recycle();
		}
		activeSlimes.Clear();
	}

	public void RespawnPlayer(Player candidate, float delay = 0.5f)
	{
		StartCoroutine(RespawnPlayerAfterDelay(candidate, delay + (float)Singleton<GameManager>.Instance.players.Count * delay * 0.25f));
	}

	private IEnumerator RespawnPlayerAfterDelay(Player player, float delay)
	{
		if (delay > 0.3f)
		{
			yield return new WaitForSeconds(delay - 0.3f);
			player.Evaporate();
			yield return new WaitForSeconds(0.3f);
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
			if (gameState != GameState.Playing)
			{
				yield break;
			}
		}
		else
		{
			yield return new WaitForSeconds(delay);
		}
		if (!Singleton<LevelManager>.Instance.level.isTutorialLevel || player.hasCompletedTutorial)
		{
			player.transform.position = Singleton<LevelManager>.Instance.FindRandomPlayerRespawnPoint();
			player.interpolatedTransform.ForgetPreviousTransforms();
		}
		else
		{
			player.transform.position = Singleton<LevelManager>.Instance.FindNearestRespawnPoint(player.transform.position);
			player.interpolatedTransform.ForgetPreviousTransforms();
		}
		player.Init(0, resetPowerups: false, respawn: true);
		if ((bool)player.GetComponent<PlayerAI>())
		{
			player.GetComponent<PlayerAI>().Init();
		}
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			int num = players.IndexOf(player);
			powerupBars[num].transform.position = player.transform.position;
			powerupBars[num].Init(player);
			powerupBars[num].HideBar(1f);
		}
		Singleton<Camera3D>.Instance.AddCameraTarget(player.transform, 1f, 0f);
	}

	public bool AreOpponents(GameObject object1, GameObject object2)
	{
		if (object1.CompareTag("Player") && object2.CompareTag("Player"))
		{
			return AreOpponents(object1.GetComponent<Player>(), object2.GetComponent<Player>());
		}
		if (object1.CompareTag("Player") && object2.CompareTag("Disc"))
		{
			return AreOpponents(object1.GetComponent<Player>(), object2.GetComponent<Disc>());
		}
		if (object1.CompareTag("Disc") && object2.CompareTag("Player"))
		{
			return AreOpponents(object2.GetComponent<Player>(), object1.GetComponent<Disc>());
		}
		return false;
	}

	public bool AreOpponents(Actor actor, Disc disc)
	{
		if (disc.DiscOwner == null)
		{
			return false;
		}
		return AreOpponents(disc.DiscOwner, actor);
	}

	public bool AreOpponents(Actor actor, Projectile projectile)
	{
		if (projectile.owner == null)
		{
			return false;
		}
		return AreOpponents(projectile.owner, actor);
	}

	public bool AreOpponents(Actor actor1, Actor actor2)
	{
		Player player = actor1 as Player;
		Player player2 = actor2 as Player;
		if (player == null && player2 == null)
		{
			return false;
		}
		if (player == null || player2 == null)
		{
			return true;
		}
		if (player.team == 0 || player2.team == 0)
		{
			return true;
		}
		if (player.team == player2.team)
		{
			return false;
		}
		return true;
	}

	public void Pause()
	{
		PauseRecording();
		lastState = gameState;
		gameState = GameState.Paused;
		Singleton<TimeKeeper>.Instance.PauseTime();
		Singleton<Camera3D>.Instance.xrayCamera.gameObject.SetActive(value: false);
	}

	public void Unpause()
	{
		gameState = lastState;
		Singleton<TimeKeeper>.Instance.UnPauseTime();
		UnPauseRecording();
		Singleton<Camera3D>.Instance.xrayCamera.gameObject.SetActive(value: true);
		if (IsInHidingPhase)
		{
			Player seeker = GetSeeker();
			if (seeker != null)
			{
				seeker.playerInput.SetVibration(0.1f);
			}
		}
	}

	public void PauseRecording(float delay = 0f)
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
		{
			if (pauseRecordingRoutine != null)
			{
				StopCoroutine(pauseRecordingRoutine);
			}
			if (delay > 0f)
			{
				pauseRecordingRoutine = PauseRecordingIn(delay);
				StartCoroutine(pauseRecordingRoutine);
			}
			else
			{
				gifRecorder.PauseRecording();
			}
		}
	}

	public void UnPauseRecording()
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
		{
			if (pauseRecordingRoutine != null)
			{
				StopCoroutine(pauseRecordingRoutine);
			}
			gifRecorder.UnPauseRecording();
		}
	}

	private IEnumerator PauseRecordingIn(float delay)
	{
		Debug.Log($"PK: pause recording in {delay} seconds");
		yield return new WaitForSecondsRealtime(delay);
		gifRecorder.PauseRecording();
	}

	public void PauseAndBakeRecording()
	{
		gifRecorder.PauseRecording();
		gifRecorder.SaveRecordingToDisk();
	}

	private void InitMatchScores()
	{
		matchScores.Clear();
		roundNumber = 0;
		if (Singleton<SettingsManager>.Instance.teamMatch)
		{
			for (int i = 1; i < 3; i++)
			{
				matchScores.Add(i, new MatchScore
				{
					Points = 0,
					Suicides = 0,
					TeamKills = 0,
					Total = 0,
					Rank = 0
				});
			}
		}
		else
		{
			foreach (Player player in players)
			{
				matchScores.Add(player.playerID, new MatchScore
				{
					Points = 0,
					Suicides = 0,
					TeamKills = 0,
					Total = 0,
					Rank = 0
				});
			}
		}
		UIMenuRoundEnd uIMenuRoundEnd = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundEnd) as UIMenuRoundEnd;
		if (uIMenuRoundEnd != null)
		{
			uIMenuRoundEnd.ResetScores();
		}
	}

	public void AddPlayerKill(Player player, bool killerWasOpponent = true)
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && player.IsHider)
		{
			return;
		}
		if (OpponentsLeftStandingNow() <= 1)
		{
			Singleton<AudioManager>.Instance.PlaySting();
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills && !Singleton<LevelManager>.Instance.level.isTutorialLevel)
		{
			int rowID = GetRowID(player);
			if (killerWasOpponent)
			{
				matchScores[rowID].Points++;
				if (Singleton<SettingsManager>.Instance.tieBreaker)
				{
					matchWinners.Clear();
					matchWinners.Add(rowID);
					Singleton<AudioManager>.Instance.WinMatch();
					StartCoroutine(EndMatch());
					return;
				}
				if (PlayerJustWonDeathMatch(player) && !matchWinners.Contains(rowID))
				{
					matchWinners.Add(rowID);
					if (matchWinners.Count == 1 && !AnotherPlayerCouldWinDeathMatchThisRound(player) && !player.isOnFire && !player.IsFrozen && player.actorState == Actor.ActorState.Alive)
					{
						Debug.Log("end match early");
						StartCoroutine(EndMatch());
						return;
					}
				}
			}
			else
			{
				matchScores[rowID].TeamKills++;
			}
		}
		StopCoroutine(playersLeftStandingRoutine);
		playersLeftStandingRoutine = CheckPlayersLeftStanding();
		StartCoroutine(playersLeftStandingRoutine);
		if (Singleton<LevelManager>.Instance.level != null)
		{
			Singleton<LevelManager>.Instance.level.MakeStatuesReactToKill();
		}
	}

	public void AddGoldenDiscPoint(Player winningPlayer)
	{
		int playerID = winningPlayer.playerID;
		matchScores[playerID].Points++;
		if (matchScores[playerID].Points >= Singleton<SettingsManager>.Instance.matchScoreGoal && !matchWinners.Contains(playerID))
		{
			matchWinners.Add(playerID);
		}
		gameState = GameState.TransitionOut;
		StartCoroutine(EndGoldenDiscRound(winningPlayer));
		winningPlayer.WinMatch();
	}

	public void AddHideAndSeekPoint(Player player)
	{
		int playerID = player.playerID;
		matchScores[playerID].Points++;
		if (matchScores[playerID].Points >= Singleton<SettingsManager>.Instance.matchScoreGoal && !matchWinners.Contains(playerID))
		{
			matchWinners.Add(playerID);
		}
	}

	public void AddPlayerSuicide(Player player)
	{
		if (OpponentsLeftStandingNow() <= 1)
		{
			Singleton<AudioManager>.Instance.PlaySting();
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && !Singleton<LevelManager>.Instance.level.isTutorialLevel && Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills && !Singleton<SettingsManager>.Instance.tieBreaker)
		{
			int rowID = GetRowID(player);
			matchScores[rowID].Suicides++;
			if (matchWinners.Contains(rowID) && matchScores[rowID].Points - matchScores[rowID].Suicides - matchScores[rowID].TeamKills < Singleton<SettingsManager>.Instance.matchScoreGoal)
			{
				matchWinners.Remove(rowID);
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek || !player.IsSeeker)
		{
			StopCoroutine(playersLeftStandingRoutine);
			playersLeftStandingRoutine = CheckPlayersLeftStanding();
			StartCoroutine(playersLeftStandingRoutine);
			if (Singleton<LevelManager>.Instance.level != null)
			{
				Singleton<LevelManager>.Instance.level.MakeStatuesReactToKill();
			}
		}
	}

	public void AddPlayerDeath(Player player)
	{
		if (OpponentsLeftStandingNow() <= 1)
		{
			Singleton<AudioManager>.Instance.PlaySting();
		}
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek || !player.IsSeeker)
		{
			StopCoroutine(playersLeftStandingRoutine);
			playersLeftStandingRoutine = CheckPlayersLeftStanding();
			StartCoroutine(playersLeftStandingRoutine);
		}
	}

	public void PreventNegativeScores()
	{
		if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Survival)
		{
			return;
		}
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			while (matchScores[matchScore.Key].Points < matchScores[matchScore.Key].Suicides + matchScores[matchScore.Key].TeamKills)
			{
				if (matchScores[matchScore.Key].TeamKills > 0)
				{
					matchScores[matchScore.Key].TeamKills--;
				}
				else if (matchScores[matchScore.Key].Suicides > 0)
				{
					matchScores[matchScore.Key].Suicides--;
				}
			}
		}
	}

	private void RemoveLosingMatchWinners()
	{
		int num = 0;
		for (int i = 0; i < players.Count; i++)
		{
			int rowID = GetRowID(players[i]);
			if (matchWinners.Contains(rowID))
			{
				int num2 = matchScores[rowID].Points - matchScores[rowID].Suicides - matchScores[rowID].TeamKills;
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		for (int j = 0; j < players.Count; j++)
		{
			int rowID2 = GetRowID(players[j]);
			if (matchWinners.Contains(rowID2) && matchScores[rowID2].Points - matchScores[rowID2].Suicides - matchScores[rowID2].TeamKills < num)
			{
				matchWinners.Remove(rowID2);
			}
		}
	}

	public int GetRowID(Player player)
	{
		if (!Singleton<SettingsManager>.Instance.teamMatch)
		{
			return player.playerID;
		}
		return player.team;
	}

	private void EndRound(bool transitionWipe = true)
	{
		Debug.Log("End round");
		UIMenuRoundTitles uIMenuRoundTitles = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundTitles) as UIMenuRoundTitles;
		if (uIMenuRoundTitles != null)
		{
			uIMenuRoundTitles.UIResponse_Close();
		}
		Singleton<TimeKeeper>.Instance.SetTimeScale(0f);
		gameState = GameState.Menu;
		Singleton<UIManager>.Instance.DisableGameplayCanvas();
		StopAllCoroutines();
		PreventNegativeScores();
		matchTimer += roundTimer;
		if (matchWinners.Count > 1)
		{
			RemoveLosingMatchWinners();
		}
		StartCoroutine(DisplayRoundScores(transitionWipe));
	}

	private IEnumerator DisplayRoundScores(bool transitionWipe = true)
	{
		Singleton<TimeKeeper>.Instance.SetTimeScale(0f);
		Singleton<AudioManager>.Instance.StopAllWorldSounds();
		if (transitionWipe)
		{
			Transform target = null;
			if (OpponentsLeftStandingNow() == 1 && lastPlayerStanding != null)
			{
				target = lastPlayerStanding.transform;
			}
			yield return StartCoroutine(transitionCircleWipe.TransitionIn(target));
		}
		if (killAllDecoysRoutine != null)
		{
			StopCoroutine(killAllDecoysRoutine);
		}
		gameState = GameState.Menu;
		Singleton<TimeKeeper>.Instance.SetTimeScale(1f);
		if (!Singleton<SettingsManager>.Instance.showcaseMode && !Singleton<SettingsManager>.Instance.trailerMode)
		{
			UIMenuRoundEnd uIMenuRoundEnd = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundEnd) as UIMenuRoundEnd;
			if (uIMenuRoundEnd != null)
			{
				Singleton<UIManager>.Instance.PushMenu(uIMenuRoundEnd);
			}
		}
		victoryCircleWipe.Reset();
		CleanUpRound();
		if (Singleton<SettingsManager>.Instance.showcaseMode || Singleton<SettingsManager>.Instance.trailerMode)
		{
			if (matchWinners.Count > 0)
			{
				StartCoroutine(EndMatch());
				yield break;
			}
			yield return StartCoroutine(LoadNextRound());
			StartRound();
		}
	}

	public IEnumerator JumpToLevel(int levelID)
	{
		LevelAsset levelAssetByIndex = Singleton<LevelManager>.Instance.GetLevelAssetByIndex(levelID);
		if (!(levelAssetByIndex == null))
		{
			Singleton<LevelManager>.Instance.InsertLevelNextInPlaylist(levelAssetByIndex);
			Singleton<TimeKeeper>.Instance.SetTimeScale(0f);
			yield return StartCoroutine(transitionCircleWipe.TransitionIn());
			gameState = GameState.Menu;
			Singleton<TimeKeeper>.Instance.SetTimeScale(1f);
			victoryCircleWipe.Reset();
			CleanUpRound();
			yield return StartCoroutine(LoadNextRound());
			StartRound();
		}
	}

	public IEnumerator SkipToNextLevel()
	{
		Singleton<TimeKeeper>.Instance.SetTimeScale(0f);
		yield return StartCoroutine(transitionCircleWipe.TransitionIn());
		gameState = GameState.Menu;
		Singleton<TimeKeeper>.Instance.SetTimeScale(1f);
		victoryCircleWipe.Reset();
		CleanUpRound();
		yield return StartCoroutine(LoadNextRound());
		StartRound();
	}

	private bool PlayerJustWonDeathMatch(Player player)
	{
		int rowID = GetRowID(player);
		if (matchScores[rowID].Points - matchScores[rowID].Suicides - matchScores[rowID].TeamKills >= Singleton<SettingsManager>.Instance.matchScoreGoal)
		{
			return true;
		}
		return false;
	}

	private bool PlayerCouldWinDeathMatchThisRound(Player player)
	{
		if (player.actorState == Actor.ActorState.Dead)
		{
			return false;
		}
		int key = (Singleton<SettingsManager>.Instance.teamMatch ? player.team : player.playerID);
		int num = matchScores[key].Points - matchScores[key].Suicides - matchScores[key].TeamKills;
		int num2 = 0;
		for (int i = 0; i < players.Count; i++)
		{
			if (!(players[i] == player) && players[i].actorState != Actor.ActorState.Dead && AreOpponents(player, players[i]))
			{
				num2++;
			}
		}
		if (num + num2 >= Singleton<SettingsManager>.Instance.matchScoreGoal)
		{
			return true;
		}
		return false;
	}

	private bool AnotherPlayerCouldWinDeathMatchThisRound(Player player)
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (!(players[i] == player) && PlayerCouldWinDeathMatchThisRound(players[i]))
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerator CheckPlayersLeftStanding()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
		{
			yield break;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			int num = HidersLeftStandingNow();
			if (IsInRevealPhase)
			{
				Player seeker = GetSeeker();
				foreach (Player player2 in players)
				{
					if (player2.IsHider)
					{
						if (player2.actorState == Actor.ActorState.Alive)
						{
							AddHideAndSeekPoint(player2);
						}
						else
						{
							AddHideAndSeekPoint(seeker);
						}
					}
				}
				if (num > 0)
				{
					UIMenuRoundTitles uIMenuRoundTitles = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundTitles) as UIMenuRoundTitles;
					if (uIMenuRoundTitles != null)
					{
						Singleton<UIManager>.Instance.PushMenu(uIMenuRoundTitles);
						uIMenuRoundTitles.ShowHidersWinTitle();
					}
					foreach (Player player3 in players)
					{
						player3.canWalk = false;
						player3.playerInput.StartVibration(0.7f, 0.2f);
					}
					foreach (Player player in players)
					{
						if (player.IsHider && player.actorState == Actor.ActorState.Alive)
						{
							player.ShowSeekerPFX();
							yield return new WaitForSecondsRealtime(1f);
							player.RevealHider();
							yield return new WaitForSecondsRealtime(0.5f);
						}
					}
					PauseRecording();
				}
			}
			else if (IsInSeekingPhase && num == 0)
			{
				seekTimer = SeekDuration;
				Player seeker2 = GetSeeker();
				foreach (Player player4 in players)
				{
					if (player4.IsHider)
					{
						AddHideAndSeekPoint(seeker2);
					}
				}
			}
		}
		yield return new WaitForSeconds(0.15f);
		if (OpponentsLeftStandingNow() <= 1 && !isKillingAllDecoys)
		{
			if (killAllDecoysRoutine != null)
			{
				StopCoroutine(killAllDecoysRoutine);
			}
			killAllDecoysRoutine = KillAllDecoys();
			StartCoroutine(killAllDecoysRoutine);
		}
		if (IsInRevealPhase && HidersLeftStandingNow() > 0)
		{
			yield return new WaitForSeconds(0.65f);
		}
		else if (Singleton<SettingsManager>.Instance.trailerMode)
		{
			yield return new WaitForSeconds(3f);
		}
		else
		{
			yield return new WaitForSeconds(1.3f);
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays && OpponentsLeftStandingNow() <= 1)
		{
			PauseRecording();
		}
		if (OpponentsLeftStandingNow() > 1)
		{
			yield break;
		}
		Singleton<AudioManager>.Instance.SetGameplayMusicState(AudioManager.GameplayMusicState.RoundEnd);
		if (Singleton<SettingsManager>.Instance.showcaseMode || Singleton<SettingsManager>.Instance.trailerMode)
		{
			EndRound();
			yield break;
		}
		if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Survival && !Singleton<LevelManager>.Instance.level.isTutorialLevel && lastPlayerStanding != null)
		{
			int rowID = GetRowID(lastPlayerStanding);
			matchScores[rowID].Points++;
			if (matchScores[rowID].Points >= Singleton<SettingsManager>.Instance.matchScoreGoal && !matchWinners.Contains(rowID))
			{
				matchWinners.Add(rowID);
			}
		}
		if (Singleton<SettingsManager>.Instance.tieBreaker && lastPlayerStanding != null)
		{
			int rowID2 = GetRowID(lastPlayerStanding);
			if (matchWinners.Contains(rowID2))
			{
				matchWinners.Clear();
				matchWinners.Add(rowID2);
			}
		}
		if (!Singleton<SettingsManager>.Instance.teamMatch && OpponentsLeftStandingNow() == 1)
		{
			lastPlayerStanding.metadata.LastPlayerStanding++;
		}
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		EndRound();
	}

	private IEnumerator KillAllDecoys()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		isKillingAllDecoys = true;
		for (int i = 0; i < activeDecoys.Count; i++)
		{
			if (activeDecoys[i] != null)
			{
				activeDecoys[i].Die(Actor.DeathType.outofbounds);
				yield return new WaitForSecondsRealtime(0.1f);
			}
		}
	}

	public void KillDecoy(Player player)
	{
		for (int i = 0; i < activeDecoys.Count; i++)
		{
			if (!(activeDecoys[i] == null) && activeDecoys[i].actorState == Actor.ActorState.Alive && activeDecoys[i].owner == player)
			{
				StartCoroutine(KillDecoyAfterDelay(activeDecoys[i]));
			}
		}
	}

	private IEnumerator KillDecoyAfterDelay(Decoy decoy)
	{
		yield return new WaitForSecondsRealtime(0.333f);
		if (decoy != null && decoy.gameObject.activeSelf && decoy.actorState == Actor.ActorState.Alive)
		{
			decoy.Die(Actor.DeathType.outofbounds);
		}
	}

	public bool AreDangersPresentForPlayer(Player player)
	{
		if (player.actorState == Actor.ActorState.Falling)
		{
			return true;
		}
		if (player.IsDashing)
		{
			return true;
		}
		if (Singleton<LevelManager>.Instance.level != null && Singleton<LevelManager>.Instance.level.isFallingApart)
		{
			return true;
		}
		if (!Singleton<LevelManager>.Instance.IsInBounds(player.transform.position))
		{
			return true;
		}
		if (activeExplosives.Count > 0)
		{
			return true;
		}
		if (AreAnyObstaclesMoving())
		{
			return true;
		}
		for (int i = 0; i < activeDiscs.Count; i++)
		{
			if (activeDiscs[i].DiscOwner != null && activeDiscs[i].DiscOwner != player && activeDiscs[i].movement != Projectile.Movement.Dropped && activeDiscs[i].movement != Projectile.Movement.Squished)
			{
				return true;
			}
		}
		return false;
	}

	public int OpponentsLeftStandingNow()
	{
		int num = 0;
		lastPlayerStanding = null;
		if (IsInRevealPhase)
		{
			return 0;
		}
		if (Singleton<SettingsManager>.Instance.teamMatch)
		{
			int[] array = new int[3];
			int num2 = 0;
			foreach (Player player in players)
			{
				if (player.actorState != Actor.ActorState.Dead || (!player.hasCompletedTutorial && Singleton<LevelManager>.Instance.level.isTutorialLevel))
				{
					lastPlayerStanding = player;
					array[player.team]++;
					num++;
				}
			}
			for (int i = 1; i < 3; i++)
			{
				num2 += ((array[i] > 0) ? 1 : 0);
			}
			return num2;
		}
		foreach (Player player2 in players)
		{
			if (player2.actorState != Actor.ActorState.Dead || (!player2.hasCompletedTutorial && Singleton<LevelManager>.Instance.level.isTutorialLevel))
			{
				lastPlayerStanding = player2;
				num++;
			}
		}
		return num;
	}

	public int HidersLeftStandingNow()
	{
		int num = 0;
		foreach (Player player in players)
		{
			if (player.actorState != Actor.ActorState.Dead && player.IsHider)
			{
				num++;
			}
		}
		if (num == 1)
		{
			foreach (Player player2 in players)
			{
				if (player2.actorState != Actor.ActorState.Dead && player2.IsHider)
				{
					lastHiderKilled = player2;
					break;
				}
			}
		}
		return num;
	}

	public bool AreAnyObstaclesMoving()
	{
		for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingObstacles.Length; i++)
		{
			if (Singleton<LevelManager>.Instance.level.allMovingObstacles[i].movementState == MovingObstacle.MovementState.Moving)
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerator ShowSeeker(Player seeker)
	{
		gameState = GameState.TransitionIn;
		victoryCircleWipe.startColor = ColorExt.SetAlpha(seeker.character.Color, 0.33f);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/transition_in");
		StartCoroutine(victoryCircleWipe.TransitionIn(seeker.transform, 0.8f));
		yield return StartCoroutine(MakePlayerWaveAfterDelay(seeker));
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		UIMenuRoundTitles menuRoundTitles = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundTitles) as UIMenuRoundTitles;
		if (menuRoundTitles != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menuRoundTitles);
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_seeker");
			menuRoundTitles.ShowSeekerTitle();
			while (menuRoundTitles.titlesShowing)
			{
				yield return null;
			}
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
		}
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
	}

	private IEnumerator StartSeekingPhase()
	{
		Player seeker = GetSeeker();
		seeker.HideSeekerPFX();
		seeker.playerInput.StopVibration();
		seeker.gameObject.layer = LayerMask.NameToLayer("Player");
		gameState = GameState.TransitionIn;
		victoryCircleWipe.startColor = ColorExt.SetAlpha(seeker.character.Color, 0.33f);
		foreach (Player player in players)
		{
			player.canWalk = false;
			if (player.IsHider)
			{
				player.canRotate = false;
				player.RotateSnapToLastInputMove();
			}
		}
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/transition_in");
		StartCoroutine(victoryCircleWipe.TransitionIn(seeker.transform, 0.8f));
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/gong");
		yield return new WaitForSecondsRealtime(0.4f);
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		UIMenuRoundTitles menuRoundTitles = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundTitles) as UIMenuRoundTitles;
		if (menuRoundTitles != null)
		{
			Singleton<UIManager>.Instance.PushMenu(menuRoundTitles);
			menuRoundTitles.ShowSeekTitle();
			while (menuRoundTitles.titlesShowing)
			{
				yield return null;
			}
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
		}
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/transition_out");
		yield return StartCoroutine(victoryCircleWipe.TransitionOut(resetFocusPosition: false, 0.8f));
		yield return new WaitForSeconds(0.4f);
		foreach (Player player2 in players)
		{
			if (player2.actorState == Actor.ActorState.Alive)
			{
				player2.Go();
			}
		}
		gameState = GameState.Playing;
	}

	private IEnumerator MakePlayerWaveAfterDelay(Player player, float delay = 0.7f)
	{
		yield return new WaitForSecondsRealtime(delay);
		player.Wave(forceWave: true);
	}

	public void OnSeekAttempted(int seekAttemptsRemaining)
	{
		seekAttemptBar.PulseSeekBar();
		if (seekAttemptsRemaining == 0)
		{
			StartCoroutine(CheckLastSeekAttempt());
		}
	}

	private IEnumerator CheckLastSeekAttempt()
	{
		bool discsWereStillInTheAir = false;
		while (AreAnyActiveDiscsStillInTheAir())
		{
			discsWereStillInTheAir = true;
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		while (AreAnyActiveDiscsStillInTheAir())
		{
			discsWereStillInTheAir = true;
			yield return null;
		}
		if (discsWereStillInTheAir)
		{
			yield return new WaitForSeconds(0.2f);
		}
		if (IsInSeekingPhase)
		{
			HidersWin();
		}
	}

	private void HidersWin()
	{
		seekTimer = SeekDuration;
		Singleton<TimeKeeper>.Instance.EndGoldenDiscRound();
		CleanUpDiscs();
		foreach (Player player in players)
		{
			player.HasDiscs = 0;
		}
		StopCoroutine(playersLeftStandingRoutine);
		playersLeftStandingRoutine = CheckPlayersLeftStanding();
		StartCoroutine(playersLeftStandingRoutine);
	}

	private bool AreAnyActiveDiscsStillInTheAir()
	{
		if (activeDiscs.Count == 0)
		{
			return false;
		}
		foreach (Disc activeDisc in activeDiscs)
		{
			Projectile.Movement movement = activeDisc.movement;
			if ((uint)movement <= 2u)
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerator EndGoldenDiscRound(Player winningPlayer)
	{
		gameState = GameState.TransitionOut;
		Singleton<TimeKeeper>.Instance.EndGoldenDiscRound();
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/win_golden_boomerang");
		yield return new WaitForSecondsRealtime(0.5f);
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		victoryCircleWipe.startColor = ColorExt.SetAlpha(winningPlayer.character.Color, 0.33f);
		StartCoroutine(victoryCircleWipe.TransitionIn(winningPlayer.transform));
		yield return new WaitForSecondsRealtime(2f);
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
		{
			gifRecorder.PauseRecording();
		}
		if (gameState != GameState.Paused)
		{
			yield return new WaitForSecondsRealtime(0.5f);
		}
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		Singleton<TimeKeeper>.Instance.SetTimeScale(0f);
		gameState = GameState.Menu;
		StopAllCoroutines();
		PreventNegativeScores();
		StartCoroutine(DisplayRoundScores(transitionWipe: false));
	}

	private IEnumerator EndMatch()
	{
		gameState = GameState.TransitionOut;
		Player winningPlayer = null;
		if (matchWinners.Count > 0 && Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills && !Singleton<SettingsManager>.Instance.teamMatch)
		{
			for (int i = 0; i < players.Count; i++)
			{
				if (matchWinners.Contains(players[i].playerID) && players[i].actorState == Actor.ActorState.Alive)
				{
					players[i].WinMatch();
					winningPlayer = players[i];
				}
			}
		}
		Singleton<TimeKeeper>.Instance.EndMatch();
		while (gameState == GameState.Paused)
		{
			yield return null;
		}
		if (winningPlayer == null)
		{
			yield return new WaitForSecondsRealtime(2f);
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
			if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
			{
				gifRecorder.PauseRecording();
			}
			if (gameState != GameState.Paused)
			{
				yield return new WaitForSecondsRealtime(0.5f);
			}
		}
		else
		{
			yield return new WaitForSecondsRealtime(0.5f);
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
			victoryCircleWipe.startColor = ColorExt.SetAlpha(winningPlayer.character.Color, 0.25f);
			if (matchWinners.Count == 1)
			{
				Singleton<AudioManager>.Instance.WinMatch();
			}
			StartCoroutine(victoryCircleWipe.TransitionIn(winningPlayer.transform));
			yield return new WaitForSecondsRealtime(1.5f);
			if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
			{
				gifRecorder.PauseRecording();
			}
			if (gameState != GameState.Paused)
			{
				yield return new WaitForSecondsRealtime(0.5f);
			}
		}
		if (Singleton<SettingsManager>.Instance.showcaseMode)
		{
			gameState = GameState.Menu;
			Singleton<UIManager>.Instance.DisableGameplayCanvas();
			Singleton<UIManager>.Instance.ExitShowcase();
		}
		else
		{
			while (gameState == GameState.Paused)
			{
				yield return null;
			}
			EndRound(!(winningPlayer != null));
		}
	}

	public void GiveAwardsToPlayers()
	{
		for (int i = 0; i < players.Count; i++)
		{
			int rowID = GetRowID(players[i]);
			players[i].metadata.FinalScore = matchScores[rowID].Points - matchScores[rowID].Suicides;
			players[i].awards.Clear();
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			GiveEveryoneARank();
			return;
		}
		GiveEveryoneAwardsFor(MatchAward.DoubleKill, "DoubleKills");
		GiveEveryoneAwardsFor(MatchAward.TripleKill, "TripleKills");
		GiveEveryoneAwardsFor(MatchAward.NoDashes, "Dashes", highestScore: false);
		GiveAwardsForTotalKills(0f, 7f);
		GiveAwardFor(MatchAward.GoodCatch, "DiscsCaught", highestScore: true, 10f, 2f);
		GiveAwardFor(MatchAward.LastPlayerStanding, "LastPlayerStanding", highestScore: true, Mathf.Round((float)roundNumber / 3f));
		GiveAwardFor(MatchAward.MostUnarmed, "TimeDisarmed", highestScore: true, 45f);
		GiveAwardFor(MatchAward.LeastDisarmed, "TimeDisarmed", highestScore: false, 5f, 2f);
		GiveAwardFor(MatchAward.MostUnitsMoved, "MoveDistance", highestScore: true, 500f, 100f);
		GiveAwardFor(MatchAward.LeastUnitsMoved, "MoveDistance", highestScore: false, 1000f, 100f);
		GiveAwardFor(MatchAward.LongestKillDistance, "LongestKillDistance", highestScore: true, 15f);
		GiveAwardFor(MatchAward.ShortestKillDistance, "LongestKillDistance", highestScore: false, 8f, 1f, zeroIsNotAllowed: true);
		GiveAwardFor(MatchAward.MostBamboozled, "TimeBamboozled", highestScore: true, 11f);
		GiveAwardFor(MatchAward.MostControlledKills, "ControlledKills", highestScore: true, 1f);
		GiveAwardFor(MatchAward.MostDashes, "Dashes", highestScore: true, 10f, 5f);
		GiveAwardFor(MatchAward.MostDashesOverHazards, "DashesOverHazards", highestScore: true, 5f);
		GiveAwardFor(MatchAward.MostDeaths, "Deaths", highestScore: true, roundNumber, 9f);
		GiveAwardFor(MatchAward.MostDisarmedByPlayers, "DisarmedByPlayers", highestScore: true, 2f);
		GiveAwardFor(MatchAward.MostDiscsStolen, "DiscsStolen", highestScore: true, 1f);
		GiveAwardFor(MatchAward.MostDodges, "Dodges", highestScore: true, 2f);
		GiveAwardFor(MatchAward.MostExplosiveKills, "ExplosiveKills", highestScore: true, 1f);
		GiveAwardFor(MatchAward.MostExplosiveSuicides, "ExplosiveSuicides");
		GiveAwardFor(MatchAward.MostFallSuicides, "FallSuicides", highestScore: true, 2f);
		GiveAwardFor(MatchAward.MostFireKills, "FireKills");
		GiveAwardFor(MatchAward.MostFiresExtinguished, "FiresExtinguished");
		GiveAwardFor(MatchAward.MostFlippedSwitches, "FlippedSwitches", highestScore: true, 8f);
		GiveAwardFor(MatchAward.MostFrozenKills, "FrozenKills");
		GiveAwardFor(MatchAward.MostKillsAfterDeath, "KillsAfterDeath");
		GiveAwardFor(MatchAward.MostKillsWhileBamboozled, "KillsWhileBamboozled");
		GiveAwardFor(MatchAward.MostKillsWhileDisarmed, "KillsWhileDisarmed");
		GiveAwardFor(MatchAward.MostOnFire, "TimeOnFire", highestScore: true, 3f);
		GiveAwardFor(MatchAward.MostOrbitalThrows, "OrbitalThrows");
		GiveAwardFor(MatchAward.MostOutOfBoundsSuicides, "OutOfBoundsSuicides", highestScore: true, 2f);
		GiveAwardFor(MatchAward.MostPlayersDisarmed, "PlayersDisarmed");
		GiveAwardFor(MatchAward.MostPowerupsCollected, "PowerupsCollected", highestScore: true, 0f, 2f);
		GiveAwardFor(MatchAward.MostPushes, "Pushes", highestScore: true, 1f);
		GiveAwardFor(MatchAward.MostPushKills, "PushKills");
		GiveAwardFor(MatchAward.MostSpin, "SpinAngle", highestScore: true, 540f);
		GiveAwardFor(MatchAward.MostSquishDeaths, "SquishDeaths");
		GiveAwardFor(MatchAward.MostSquishKills, "SquishKills");
		GiveAwardFor(MatchAward.MostTeamDeaths, "TeamDeaths");
		GiveAwardFor(MatchAward.MostTeamKills, "TeamKills");
		GiveAwardFor(MatchAward.MostTeleportKills, "TeleportKills");
		GiveAwardFor(MatchAward.MostTeleportDeaths, "TeleportDeaths");
		GiveAwardFor(MatchAward.MostThrowKills, "ThrowKills", highestScore: true, 5f);
		GiveAwardFor(MatchAward.MostTimeAiming, "TimeAiming", highestScore: true, 5f);
		GiveAwardFor(MatchAward.MostTimeDisguised, "TimeDisguised", highestScore: true, 9f);
		GiveAwardFor(MatchAward.MostTimeInPlants, "TimeInPlants", highestScore: true, 6f);
		GiveAwardFor(MatchAward.MostTimeOutOfBounds, "TimeOutOfBounds", highestScore: true, 9f);
		GiveAwardFor(MatchAward.MostTimeSlimed, "TimeSlimed");
		GiveAwardFor(MatchAward.MostTimeHoldingGoldenDisc, "TimeHoldingGoldenDisc");
		GiveAwardFor(MatchAward.NeedlessViolence, "FinalScore", highestScore: true, Singleton<SettingsManager>.Instance.matchScoreGoal);
		GiveEveryoneThrowAccuracyAwards(0.1f, 0.66f);
		GiveSpecialAwardFor(MatchAward.Swordsmanship);
		GiveSpecialAwardFor(MatchAward.OnlyMeleeKills);
		GiveSpecialAwardFor(MatchAward.MoreTeamKillsThanKills);
		GiveSpecialAwardFor(MatchAward.MostBlocks);
		GiveSpecialAwardFor(MatchAward.MostEfficient);
		GiveEveryonePacifistAwards();
		GiveEveryoneConsolationAwards();
		GiveCloseSecondAward();
		if (!Singleton<SettingsManager>.Instance.teamMatch)
		{
			GiveEveryoneARank();
		}
		for (int j = 0; j < players.Count; j++)
		{
			for (int k = 0; k < players[j].awards.Count; k++)
			{
				MatchAward value = players[j].awards[k];
				int index = UnityEngine.Random.Range(k, players[j].awards.Count);
				players[j].awards[k] = players[j].awards[index];
				players[j].awards[index] = value;
			}
			for (int l = 0; l < players[j].awards.Count; l++)
			{
				if (players[j].awards[l].Priority)
				{
					MatchAward item = players[j].awards[l];
					players[j].awards.RemoveAt(l);
					players[j].awards.Insert(0, item);
				}
			}
		}
	}

	private void GiveAwardFor(MatchAward award, string awardCondition, bool highestScore = true, float minimumScore = 0f, float winningMargin = 1f, bool zeroIsNotAllowed = false)
	{
		Player player = null;
		float num = minimumScore;
		for (int i = 0; i < players.Count; i++)
		{
			float num2 = Convert.ToSingle(players[i].metadata.GetType().GetField(awardCondition).GetValue(players[i].metadata));
			if (highestScore && num2 >= num)
			{
				player = ((!(num2 > num + ((num > minimumScore) ? (winningMargin - 1f) : 0f))) ? null : players[i]);
				num = num2;
			}
			else if (!highestScore && num2 < num && (!zeroIsNotAllowed || num2 != 0f))
			{
				player = ((!(num2 < num - ((num < minimumScore) ? (winningMargin + 1f) : 0f))) ? null : players[i]);
				num = num2;
			}
		}
		if (player != null)
		{
			player.awards.Add(award);
		}
	}

	private void GiveEveryoneAwardsFor(MatchAward award, string awardCondition, bool highestScore = true, float minimumScore = 0f)
	{
		for (int i = 0; i < players.Count; i++)
		{
			float num = Convert.ToSingle(players[i].metadata.GetType().GetField(awardCondition).GetValue(players[i].metadata));
			if (highestScore && num > minimumScore)
			{
				players[i].awards.Add(award);
				if (players[i].awards.Contains(MatchAward.TripleKill) && players[i].awards.Contains(MatchAward.DoubleKill))
				{
					players[i].awards.Remove(MatchAward.DoubleKill);
				}
			}
			else if (!highestScore && num <= minimumScore)
			{
				players[i].awards.Add(award);
			}
		}
	}

	private void GiveEveryoneThrowAccuracyAwards(float minRatio = 0.5f, float maxRatio = 0.5f)
	{
		for (int i = 0; i < players.Count; i++)
		{
			float num = ((players[i].metadata.Throws <= 0) ? 0f : ((float)(players[i].metadata.ThrowKills + players[i].metadata.Deflections) / (float)players[i].metadata.Throws));
			if (num > 0f && num < minRatio)
			{
				players[i].awards.Add(MatchAward.InaccurateThrows);
			}
			else if (num > maxRatio)
			{
				players[i].awards.Add(MatchAward.AccurateThrows);
			}
		}
	}

	private void GiveEveryonePacifistAwards()
	{
		for (int i = 0; i < players.Count; i++)
		{
			if ((float)(players[i].metadata.Attacks + players[i].metadata.Throws + players[i].metadata.PushKills + players[i].metadata.SquishKills) == 0f)
			{
				players[i].awards.Add(MatchAward.Pacifist);
			}
		}
	}

	private void GiveAwardsForTotalKills(float minimumScore = 0f, float winningMargin = 1f)
	{
		Player player = null;
		float num = minimumScore;
		for (int i = 0; i < players.Count; i++)
		{
			float num2 = players[i].metadata.MeleeKills + players[i].metadata.PushKills + players[i].metadata.ThrowKills + players[i].metadata.TeamKills;
			if (num2 == 0f)
			{
				players[i].awards.Add(MatchAward.NoKills);
			}
			else if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Survival && players.Count > 2 && num2 >= num)
			{
				player = ((!(num2 > num + ((num > minimumScore) ? (winningMargin - 1f) : 0f))) ? null : players[i]);
				num = num2;
			}
		}
		if (player != null)
		{
			player.awards.Add(MatchAward.MostKills);
		}
	}

	private void GiveSpecialAwardFor(MatchAward award)
	{
		Player player = null;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < players.Count; i++)
		{
			if (award == MatchAward.Swordsmanship && players[i].metadata.Attacks > 4)
			{
				num2 = (players[i].metadata.MeleeKills + players[i].metadata.Blocks) / players[i].metadata.Attacks;
			}
			else if (award == MatchAward.OnlyMeleeKills)
			{
				num2 = ((players[i].metadata.ThrowKills == 0 && players[i].metadata.ExplosiveKills == 0 && players[i].metadata.PushKills == 0) ? ((float)players[i].metadata.MeleeKills) : 0f);
			}
			else if (award == MatchAward.OnlyThrowKills)
			{
				num2 = ((players[i].metadata.MeleeKills == 0 && players[i].metadata.ExplosiveKills == 0 && players[i].metadata.PushKills == 0) ? ((float)players[i].metadata.ThrowKills) : 0f);
			}
			else if (award == MatchAward.MoreTeamKillsThanKills)
			{
				num2 = players[i].metadata.TeamKills - players[i].metadata.MeleeKills - players[i].metadata.PushKills - players[i].metadata.ThrowKills;
			}
			else if (award == MatchAward.MostBlocks)
			{
				num2 = players[i].metadata.Blocks + players[i].metadata.Deflections;
			}
			else if (award == MatchAward.MostEfficient && players[i].metadata.Throws + players[i].metadata.Attacks > 0)
			{
				num2 = (players[i].metadata.TotalKills + players[i].metadata.Deflections + players[i].metadata.Blocks) / (players[i].metadata.Throws + players[i].metadata.Attacks);
				if (num2 < 0.4f)
				{
					num2 = 0f;
				}
			}
			if (num2 > num)
			{
				player = ((!(num2 > num)) ? null : players[i]);
				num = num2;
			}
		}
		if (player != null)
		{
			player.awards.Add(award);
		}
	}

	private void GiveCloseSecondAward()
	{
		if (Singleton<SettingsManager>.Instance.teamMatch || players.Count < 3)
		{
			return;
		}
		int num = 0;
		int num2 = -1;
		int num3 = 0;
		int num4 = -1;
		bool flag = false;
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			int num5 = matchScores[matchScore.Key].Points - matchScores[matchScore.Key].Suicides - matchScores[matchScore.Key].TeamKills;
			if (num5 > num)
			{
				num4 = num2;
				num3 = num;
				num = num5;
				num2 = matchScore.Key;
				flag = false;
			}
			else if (num5 == num)
			{
				flag = true;
			}
			else if (num5 > num3)
			{
				num3 = num5;
				num4 = matchScore.Key;
				flag = false;
			}
			else if (num5 == num3)
			{
				flag = true;
			}
		}
		if (flag || num4 == -1 || num - num3 >= 2)
		{
			return;
		}
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].playerID == num4)
			{
				players[i].awards.Add(MatchAward.CloseSecond);
				break;
			}
		}
	}

	private void GiveEveryoneConsolationAwards()
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].awards.Count == 0)
			{
				players[i].awards.Add(MatchAward.NoAward);
			}
		}
	}

	public GameObject GetPlayerWithLeastPowerups(bool livingPlayersOnly = false)
	{
		int num = 999999;
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < players.Count; i++)
		{
			if (!livingPlayersOnly || players[i].actorState == Actor.ActorState.Alive)
			{
				int num2 = EnumExt.CountSetFlags((ulong)players[i].activePowerup);
				if (num2 == num)
				{
					list.Add(players[i].gameObject);
				}
				else if (num2 < num)
				{
					list.Clear();
					list.Add(players[i].gameObject);
					num = num2;
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(players[UnityEngine.Random.Range(0, players.Count)].gameObject);
		}
		return list[UnityEngine.Random.Range(0, list.Count)].gameObject;
	}

	public GameObject GetPlayerWithMostPowerups(bool livingPlayersOnly = false)
	{
		int num = 0;
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < players.Count; i++)
		{
			if (!livingPlayersOnly || players[i].actorState == Actor.ActorState.Alive)
			{
				int num2 = EnumExt.CountSetFlags((ulong)players[i].activePowerup);
				if (num2 == num)
				{
					list.Add(players[i].gameObject);
				}
				else if (num2 > num)
				{
					list.Clear();
					list.Add(players[i].gameObject);
					num = num2;
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(players[UnityEngine.Random.Range(0, players.Count)].gameObject);
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void GiveEveryoneARank()
	{
		UpdateMatchScoreTotals();
		int num = 1;
		int num2 = 0;
		int rank = 0;
		foreach (KeyValuePair<int, MatchScore> item in matchScores.OrderByDescending((KeyValuePair<int, MatchScore> key) => key.Value.Total))
		{
			if (item.Value.Total == num2)
			{
				item.Value.Rank = rank;
			}
			else
			{
				item.Value.Rank = num;
			}
			num2 = item.Value.Total;
			rank = item.Value.Rank;
			num++;
		}
	}

	public bool IsLeadingPlayer(Player player)
	{
		if (Singleton<SettingsManager>.Instance.teamMatch)
		{
			return false;
		}
		if (players.Count < 3)
		{
			return false;
		}
		UpdateMatchScoreTotals();
		List<int> list = new List<int>(6);
		int num = 0;
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			if (matchScores[matchScore.Key].Total > num)
			{
				list.Clear();
				num = matchScores[matchScore.Key].Total;
			}
			if (matchScores[matchScore.Key].Total >= num)
			{
				list.Add(matchScore.Key);
			}
		}
		if ((float)list.Count > (float)matchScores.Count / 2f)
		{
			return false;
		}
		if (list.Contains(player.playerID))
		{
			return true;
		}
		return false;
	}

	private void UpdateMatchScoreTotals()
	{
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			matchScore.Value.Total = matchScore.Value.Points - matchScore.Value.Suicides - matchScore.Value.TeamKills;
		}
	}

	public int PointsLeftToWin(Player candidate)
	{
		UpdateMatchScoreTotals();
		int rowID = GetRowID(candidate);
		return Singleton<SettingsManager>.Instance.matchScoreGoal - matchScores[rowID].Total;
	}

	public bool IsALosingPlayer(Player candidate, int pointDifference = 4)
	{
		UpdateMatchScoreTotals();
		int num = 0;
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			if (matchScore.Value.Total > num)
			{
				num = matchScore.Value.Total;
			}
		}
		int key = (Singleton<SettingsManager>.Instance.teamMatch ? candidate.team : candidate.playerID);
		if (matchScores[key].Total <= num - pointDifference)
		{
			return true;
		}
		return false;
	}

	public List<GameObject> GetLosingPlayers(int pointDifference = 3)
	{
		UpdateMatchScoreTotals();
		List<GameObject> list = new List<GameObject>();
		int num = 0;
		foreach (KeyValuePair<int, MatchScore> matchScore in matchScores)
		{
			if (matchScore.Value.Total > num)
			{
				num = matchScore.Value.Total;
			}
		}
		for (int i = 0; i < players.Count; i++)
		{
			int rowID = GetRowID(players[i]);
			if (matchScores[rowID].Total <= num - pointDifference)
			{
				list.Add(players[i].gameObject);
			}
		}
		return list;
	}

	private void OnDisable()
	{
		ReInput.ControllerConnectedEvent -= OnControllerConnected;
		ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
	}

	public bool IsPlayerSeeker(Player player)
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			return false;
		}
		if (players.IndexOf(player) == seekerIndex)
		{
			return true;
		}
		return false;
	}

	public Player GetSeeker()
	{
		if (seekerIndex >= players.Count)
		{
			return null;
		}
		return players[seekerIndex];
	}

	public bool IsNextSeekerABot()
	{
		return players[(seekerIndex + 1) % players.Count].GetComponent<AI>();
	}
}
