using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.UI;

public class Player : Actor
{
	public enum HoldingGoldenDisc
	{
		No,
		IsHolding,
		IsThrown,
		IsDropped
	}

	public enum DisguiseType
	{
		None = 0,
		Rock = 1,
		StoneLantern = 2,
		Crate = 4,
		Pottery = 8,
		PotPlant = 0x10,
		Bamboo = 0x20,
		FlagMast = 0x40,
		PlinthBoomerang = 0x80,
		OnsenBucket = 0x100,
		OnsenStool = 0x200,
		TikiTorch = 0x400,
		RockJungle = 0x1000,
		ShrubGreen = 0x2000,
		ShrubOrange = 0x4000,
		ShrubRed = 0x8000,
		TemplePillar = 0x10000
	}

	[Serializable]
	public class Disguise
	{
		public DisguiseType type;

		public GameObject gameObject;
	}

	public PlayerType playerType;

	[Header("Player analytics")]
	[SerializeField]
	public PlayerMetadata metadata = new PlayerMetadata();

	public List<MatchAward> awards = new List<MatchAward>();

	private Vector3 positionLastFrame;

	public int killsThisRound;

	private int numRecentKills;

	private float timeOfLastKill;

	private static float recentKillMaxTime = 1.2f;

	[Header("Player input")]
	public PlayerInput playerInput;

	[Header("Player colliders")]
	public Collider2D shieldCollider;

	public Collider2D disarmCollider;

	public Collider2D insidesCollider;

	public Collider2D pickupCollider;

	public Collider2D deathCollider;

	private GameObject discMeleeGhost;

	private Animator shieldAnimator;

	[Header("Player appearance")]
	public GameObject shield;

	private GameObject retrievePFX;

	private GameObject controlledPFX;

	private Canvas bamboozledTimeUI;

	private Image bamboozledTimeUIClock;

	protected Canvas slimeTimeUI;

	protected Image slimeTimeUIClock;

	private Canvas goldenTimeUI;

	private Image goldenTimeUIClock;

	private Canvas reviveTimeUI;

	private Image reviveTimeUIClock;

	public ParticleSystem revivingPFX;

	private Animator outOfBoundsEffect;

	public OutOfBoundsArrow outOfBoundsArrow;

	private EventInstance reviveSFXInstance;

	public ParticleSystem meleeTrailRight;

	public ParticleSystem meleeTrailLeft;

	protected float meleeTrailDefaultLifetime;

	protected float meleeTrailDefaultDelay;

	public ParticleSystem seekerPFX;

	public ParticleSystem hiderPFX;

	[Header("Player states")]
	[SerializeField]
	private bool _isHidden;

	private bool _isDisarming;

	[SerializeField]
	private int _hasDiscs;

	private bool _isDisarmed;

	public int disarmsLeft;

	[SerializeField]
	private bool _isShielded;

	public Decoy decoy;

	public bool hasCreatedDecoy;

	private static float discSpawnDistance = 0.75f;

	public List<Disc> thrownDiscs = new List<Disc>();

	protected bool _hasJustSwappedDiscs;

	public bool hasCompletedTutorial = true;

	[Header("Golden boomerang")]
	[SerializeField]
	private HoldingGoldenDisc _hasGoldenDisc;

	public Disc thrownGoldenDisc;

	public float goldenHoldTime;

	public int goldenDiscKills;

	private static float goldenDiscPushbackRadius = 10f;

	public Transform goldenDiscPFX;

	public GameObject goldenPlayerPFX;

	public GameObject goldenWinPFX;

	private EventInstance goldenDiscAudioLoop;

	public ParticleSystem timeImmunePFX;

	public ParticleSystem illuminatePFX;

	public ParticleSystem skullPFX;

	[Header("Player appearance")]
	public bool isInTheLead;

	public AlertBar alertBar;

	public bool dropInAIOnTimeOut;

	private static float combatDelayAtStart = 1.2f;

	public static float outOfBoundsDuration = 3f;

	[Header("Player combat")]
	public float outOfBoundsTimer;

	public bool isOutOfBounds;

	public GameObject discPrefab;

	public Grenade grenadePrefab;

	private Queue<Vector2> rotationQueue = new Queue<Vector2>();

	private IEnumerator respawnDiscRoutine;

	[Header("Powerups")]
	[BitMask(typeof(PowerupType))]
	public PowerupType activePowerup;

	[SerializeField]
	public List<PowerupType> powerupHistory = new List<PowerupType>();

	private int maxActivePowerups = 3;

	private float powerupTimeLimit;

	private float powerupTimer;

	private bool isPowerupTimerOn;

	public int shieldHitsLeft;

	public int pendingDiscs;

	public int miniDiscsCollected;

	public int miniDiscsRequired;

	public float retrieveCharge;

	private float retrieveChargeDuration = 4f;

	public Disguise[] disguises;

	public Animator disguiseAnimator;

	public Transform disguiseObstacle;

	public float disguiseTimer;

	public static float disguiseStartDuration = 0.2f;

	[SerializeField]
	private bool _isDisguised;

	private const float illuminationRadius = 12f;

	private bool _isInTheShadows;

	private float timeImmuneTimer;

	private const float timeImmuneDuration = 1.5f;

	private float extinguishAmount;

	public ParticleSystem moveFasterPFX;

	private static float skullPFXDelay = 0.3f;

	private float timeSinceLastSkullPFX;

	public float lastTeleportTimestamp;

	private float hideFactor = 0.33f;

	public int seekAttemptsRemaining;

	public float timeInCurrentDisguise;

	public override bool IsDashing
	{
		get
		{
			return base.IsDashing;
		}
		set
		{
			base.IsDashing = value;
		}
	}

	public override bool IsAiming
	{
		get
		{
			return base.IsAiming;
		}
		set
		{
			base.IsAiming = value;
		}
	}

	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (_isHidden || value)
			{
				_isHidden = value;
				if (character.anim.isActiveAndEnabled)
				{
					character.anim.SetBool("IsHiddenByPlants", value);
				}
				character.SquashTallHat(value);
				if (value)
				{
					shield.transform.localScale = new Vector3(3f, 3f, 1f);
				}
				else
				{
					shield.transform.localScale = Vector3.one * 3f;
				}
			}
		}
	}

	public override bool IsAttacking
	{
		get
		{
			return base.IsAttacking;
		}
		set
		{
			if (base.IsAttacking && !value)
			{
				character.SetMeleeDiscs(_hasDiscs);
				CheckNumMiniDiscs();
			}
			base.IsAttacking = value;
		}
	}

	public bool IsDisarming
	{
		get
		{
			return _isDisarming;
		}
		set
		{
			disarmCollider.enabled = value;
			if (value)
			{
				character.anim.SetTrigger("AttackDisarm");
				character.SetFacialExpression(FacialExpression.aggressive);
			}
			else if (_isDisarming != value)
			{
				character.SetMeleeDiscs(_hasDiscs);
				CheckNumMiniDiscs();
			}
			_isDisarming = value;
		}
	}

	public int HasDiscs
	{
		get
		{
			return _hasDiscs;
		}
		set
		{
			if (_hasDiscs != value)
			{
				character.anim.SetInteger("HasDiscs", value);
				character.SetMeleeDiscs(value);
				_hasDiscs = value;
			}
		}
	}

	public bool IsDisarmed
	{
		get
		{
			return _isDisarmed;
		}
		set
		{
			if (_isDisarmed != value)
			{
				_isDisarmed = value;
				if (_isDisarmed)
				{
					playerInput.ConsiderShowingTeleportHint();
				}
				else
				{
					playerInput.ConsiderHidingTeleportHint();
				}
			}
		}
	}

	public bool IsShielded
	{
		get
		{
			return _isShielded;
		}
		set
		{
			if (shieldAnimator.isActiveAndEnabled)
			{
				shieldAnimator.SetBool("IsOn", value);
			}
			shield.SetActive(value);
			shieldCollider.enabled = value;
			_isShielded = value;
		}
	}

	public override bool IsSlimed
	{
		get
		{
			return base.IsSlimed;
		}
		set
		{
			base.IsSlimed = value;
			if (_isSlimed)
			{
				slimeTimeUI.enabled = true;
				slimeTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: true);
				BreakDisguise();
			}
			else
			{
				slimeTimeUI.enabled = false;
				slimeTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
			}
		}
	}

	public override bool IsFrozen
	{
		get
		{
			return base.IsFrozen;
		}
		set
		{
			base.IsFrozen = value;
			if (_isFrozen == value)
			{
				return;
			}
			if (value)
			{
				slimeTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
				slimeTimeUIClock.fillAmount = 0f;
				playerInput.ConsiderShowingChipOutHint();
			}
			else if (!value)
			{
				if (!IsSlimed)
				{
					slimeTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
					slimeTimeUIClock.fillAmount = 0f;
				}
				playerInput.ConsiderHidingChipOutHint();
			}
		}
	}

	public bool HasJustSwappedDiscs
	{
		get
		{
			return _hasJustSwappedDiscs;
		}
		set
		{
			if (_hasJustSwappedDiscs != value)
			{
				if (value)
				{
					StartCoroutine(JustSwappedDiscsTimer());
				}
				_hasJustSwappedDiscs = value;
			}
		}
	}

	public HoldingGoldenDisc HasGoldenDisc
	{
		get
		{
			return _hasGoldenDisc;
		}
		set
		{
			if (_hasGoldenDisc != value)
			{
				if (value == HoldingGoldenDisc.IsHolding && _hasGoldenDisc != HoldingGoldenDisc.IsThrown)
				{
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameterAt("event:/powerups/collect_golden_boomerang", "playerID", playerID, base.transform.position);
				}
				_hasGoldenDisc = value;
				ToggleGoldenDiscPFX();
				goldenDiscPFX.gameObject.SetActive(_hasGoldenDisc == HoldingGoldenDisc.IsHolding);
				goldenPlayerPFX.gameObject.SetActive(_hasGoldenDisc == HoldingGoldenDisc.IsHolding || _hasGoldenDisc == HoldingGoldenDisc.IsThrown);
				if (_hasGoldenDisc == HoldingGoldenDisc.IsHolding)
				{
					goldenTimeUI.enabled = true;
					goldenTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: true);
					aimAssistantRenderer.material.color = ColorExt.SetAlpha(Singleton<GameManager>.Instance.goldenDiscColor, 0f);
					goldenDiscAudioLoop = Singleton<AudioManager>.Instance.PlayLooping("event:/powerups/charge_golden_boomerang", base.transform);
				}
				else if (_hasGoldenDisc == HoldingGoldenDisc.No || _hasGoldenDisc == HoldingGoldenDisc.IsDropped)
				{
					goldenHoldTime -= Singleton<GameManager>.Instance.goldenGoalScore * 0.05f;
					goldenTimeUI.enabled = false;
					goldenTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
				}
				if (_hasGoldenDisc != HoldingGoldenDisc.IsHolding && goldenDiscAudioLoop.isValid())
				{
					Singleton<AudioManager>.Instance.StopLooping(goldenDiscAudioLoop);
				}
			}
		}
	}

	public bool IsDisguised
	{
		get
		{
			return _isDisguised;
		}
		set
		{
			if (_isDisguised == value)
			{
				return;
			}
			_isDisguised = value;
			if (_isDisguised)
			{
				int disguiseIndex = UnityEngine.Random.Range(1, disguises.Length);
				if (Singleton<LevelManager>.Instance.level != null)
				{
					DisguiseType disguiseType = Singleton<LevelManager>.Instance.level.GetNearestDisguse(base.transform.position, 15f);
					if (disguiseType == DisguiseType.None && Singleton<LevelManager>.Instance.level.availableDisguiseTypes.Length != 0)
					{
						disguiseType = Singleton<LevelManager>.Instance.level.availableDisguiseTypes[UnityEngine.Random.Range(0, Singleton<LevelManager>.Instance.level.availableDisguiseTypes.Length)];
					}
					if (disguiseType != DisguiseType.None)
					{
						for (int i = 0; i < disguises.Length; i++)
						{
							if (disguises[i].type == disguiseType)
							{
								disguiseIndex = i;
							}
						}
					}
				}
				TurnOnDisguise(disguiseIndex);
				character.IsVisible = false;
				if (IsShielded)
				{
					shield.SetActive(value: false);
				}
				else if (IsFrozen)
				{
					frozenBlockAnimator.SetBool("IsOn", value: false);
				}
			}
			else
			{
				disguiseTimer = 0f;
				character.IsVisible = true;
				if (IsShielded)
				{
					shield.SetActive(value: true);
				}
				else if (IsFrozen)
				{
					frozenBlockAnimator.SetBool("IsOn", value: true);
				}
			}
			if (disguiseAnimator.isActiveAndEnabled)
			{
				if (_isDisguised)
				{
					disguiseAnimator.ResetTrigger("Off");
					disguiseAnimator.SetTrigger("On");
				}
				else
				{
					disguiseAnimator.SetTrigger("Off");
				}
			}
		}
	}

	public bool IsInTheShadows
	{
		get
		{
			return _isInTheShadows;
		}
		set
		{
			if (isOnFire || IsFrozen || IsDashing || IsAiming)
			{
				value = false;
			}
			if (IsShielded)
			{
				shield.SetActive(!value);
			}
			_isInTheShadows = value;
			if (actorState == ActorState.Alive)
			{
				if (value)
				{
					character.IsVisible = false;
				}
				else if (!value)
				{
					character.IsVisible = true;
				}
			}
		}
	}

	public bool IsSeeker => Singleton<GameManager>.Instance.IsPlayerSeeker(this);

	public bool IsHider
	{
		get
		{
			if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
			{
				return false;
			}
			return !IsSeeker;
		}
	}

	private void TurnOnDisguise(int disguiseIndex)
	{
		for (int i = 0; i < disguises.Length; i++)
		{
			if (i == disguiseIndex)
			{
				disguises[i].gameObject.SetActive(value: true);
				if (disguises[i].type == DisguiseType.Rock || disguises[i].type == DisguiseType.RockJungle)
				{
					disguises[i].gameObject.transform.localScale = new Vector3(UnityEngine.Random.Range(0.4f, 0.5f), UnityEngine.Random.Range(0.75f, 1f), UnityEngine.Random.Range(0.4f, 0.5f));
					disguises[i].gameObject.transform.localPosition = new Vector3(0f, 0f, 0f - disguiseObstacle.localScale.y);
				}
				else if (disguises[i].type == DisguiseType.Bamboo)
				{
					float num = UnityEngine.Random.Range(0.8f, 1.25f);
					disguises[i].gameObject.transform.localScale = new Vector3(num, UnityEngine.Random.Range(0.8f, 1.25f), num);
				}
				else if (disguises[i].type == DisguiseType.Pottery)
				{
					float num2 = UnityEngine.Random.Range(0.95f, 1.2f);
					disguises[i].gameObject.transform.localScale = new Vector3(num2, num2, num2);
				}
			}
			else
			{
				disguises[i].gameObject.SetActive(value: false);
			}
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		Physics2D.IgnoreCollision(meleeCollider, shieldCollider);
		Physics2D.IgnoreCollision(disarmCollider, damageCollider);
	}

	public void SetupCharacter(Character setCharacter)
	{
		character = UnityEngine.Object.Instantiate(Singleton<GameManager>.Instance.GetCharacterPrefab(setCharacter.characterType), appearance);
		character.actor = this;
		character.equippedHat = setCharacter.equippedHat;
		character.InitCharacter(Character.CharacterMode.Gameplay);
	}

	public void WipeMetadata()
	{
		metadata = new PlayerMetadata();
	}

	protected override void Start()
	{
		playerInput = GetComponent<PlayerInput>();
		shieldAnimator = base.transform.Find("Appearance/Shield").GetComponent<Animator>();
		retrievePFX = base.transform.Find("Appearance/RetrievePFX").gameObject;
		controlledPFX = retrievePFX.transform.Find("Glow").gameObject;
		retrievePFX.transform.SetParent(character.leftHandJoint);
		retrievePFX.transform.localPosition = Vector3.zero;
		bamboozledTimeUI = base.transform.Find("UITimer").GetComponent<Canvas>();
		bamboozledTimeUIClock = base.transform.Find("UITimer/Time").GetComponent<Image>();
		slimeTimeUI = base.transform.Find("UISlimed").GetComponent<Canvas>();
		slimeTimeUIClock = base.transform.Find("UISlimed/Time").GetComponent<Image>();
		goldenTimeUI = base.transform.Find("UIGoldenTimer").GetComponent<Canvas>();
		goldenTimeUIClock = base.transform.Find("UIGoldenTimer/Time").GetComponent<Image>();
		reviveTimeUI = base.transform.Find("DeathCollider/UIRevive").GetComponent<Canvas>();
		reviveTimeUIClock = base.transform.Find("DeathCollider/UIRevive/Time").GetComponent<Image>();
		base.Start();
		meleeTrailDefaultLifetime = meleeTrailRight.main.startLifetime.constant;
		meleeTrailDefaultDelay = meleeTrailRight.main.startDelay.constant;
	}

	public void Init(int startDiscs = 0, bool resetPowerups = false, bool respawn = false, bool spawnNewDisc = true)
	{
		base.Init();
		insidesCollider.enabled = false;
		deathCollider.enabled = false;
		pickupCollider.enabled = true;
		damageCollider.enabled = true;
		base.gameObject.layer = LayerMask.NameToLayer("Player");
		powerupTimer = 0f;
		isPowerupTimerOn = false;
		goldenDiscPFX.gameObject.SetActive(value: false);
		goldenPlayerPFX.SetActive(value: false);
		goldenWinPFX.SetActive(value: false);
		canWalk = false;
		canRotate = false;
		isSpawningIn = true;
		attacksLeft = HasDiscs;
		disarmsLeft = 1;
		IsDisarmed = false;
		IsDisarming = false;
		pendingDiscs = 0;
		playerInput.inputThrowIsBlocked = false;
		retrieveCharge = 0f;
		timeScaler = 1f;
		timeImmuneTimer = 0f;
		timeImmunePFX.Stop();
		StopTemporaryPowerups();
		StopAiming();
		if (respawnDiscRoutine != null)
		{
			StopCoroutine(respawnDiscRoutine);
		}
		StopAllCoroutines();
		if (Singleton<LevelManager>.Instance.level.isTutorialLevel && !respawn)
		{
			hasCompletedTutorial = false;
			dropInAIOnTimeOut = true;
		}
		HasDiscs = startDiscs;
		character.SetMeleeDiscs(0);
		thrownDiscs.Clear();
		thrownGoldenDisc = null;
		HasGoldenDisc = HoldingGoldenDisc.No;
		SetAppearanceColors(character.Color);
		bamboozledTimeUI.GetComponent<Animator>().SetTrigger("Init");
		slimeTimeUI.GetComponent<Animator>().SetTrigger("Init");
		goldenTimeUI.GetComponent<Animator>().SetTrigger("Init");
		if (!respawn)
		{
			reviveTimeUI.GetComponent<Animator>().SetTrigger("Init");
		}
		if (reviveTimer > 0f)
		{
			reviveTimer = 0f;
			reviveTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
			revivingPFX.Stop();
			if (reviveSFXInstance.isValid())
			{
				reviveSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
				reviveSFXInstance.release();
			}
		}
		if (skullPFX != null)
		{
			skullPFX.Clear();
		}
		character.anim.SetBool("IsRetrieving", value: false);
		retrievePFX.SetActive(value: false);
		controlledPFX.SetActive(value: false);
		disguiseAnimator.SetTrigger("Init");
		if (resetPowerups)
		{
			activePowerup = PowerupType.None;
			StopShield();
			powerupHistory.Clear();
			hasCreatedDecoy = false;
		}
		miniDiscsCollected = 0;
		miniDiscsRequired = 0;
		SetKiller(this);
		character.IsVisible = false;
		IsShielded = false;
		character.SetFacialExpression(character.spawnFace, 1.2f);
		if (!respawn)
		{
			goldenHoldTime = 0f;
			goldenDiscKills = 0;
			killsThisRound = 0;
			hasCreatedDecoy = false;
		}
		else
		{
			if (spawnNewDisc && HasDiscs + pendingDiscs < 2)
			{
				RespawnDisc(1.75f, (!activePowerup.HasPowerup(PowerupType.ExtraDisc)) ? 1 : 2);
			}
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
			{
				hasCreatedDecoy = false;
			}
			character.anim.ResetTrigger("Appear");
			character.anim.SetTrigger("Appear");
			base.IsInvulnerable = true;
			StartCoroutine(SpawnIn(temporaryInvulnerability: true));
		}
		switch (Singleton<SettingsManager>.Instance.MatchSettings.edgeProtection)
		{
		case 0:
			edgeAssist = 0f;
			break;
		case 1:
			edgeAssist = 0.5f;
			break;
		case 2:
			edgeAssist = 1f;
			break;
		}
	}

	protected override Vector2 SlowDownForHazards(Vector2 moveAmount)
	{
		if (IsDashing || IsStunned || playerType == PlayerType.Bot)
		{
			return moveAmount;
		}
		Vector2 vector = base.transform.position;
		if (edgeAssist < 0.95f)
		{
			vector += moveAmount * Time.fixedDeltaTime * 0.5f;
		}
		Vector2 hazardNormal = GetHazardNormal(vector, physicsColliderRadius * 1.5f, physicsColliderRadius * 0.4f, out var hits);
		if (hazardNormal == Vector2.zero || Vector3.Dot(hazardNormal, moveAmount) >= 0f)
		{
			return moveAmount;
		}
		Vector2 vector2 = moveAmount;
		if (edgeAssist >= 0.95f && Singleton<LevelManager>.Instance.IsPointOnHazard(vector))
		{
			vector2 += hazardNormal * GetMoveSpeed() * Time.fixedDeltaTime * 0.25f;
		}
		float num = edgeAssist;
		if (edgeAssist <= 0.95f)
		{
			float num2 = (float)Mathf.Clamp(hits, 1, 5) / 5f;
			num = edgeAssist * num2;
		}
		return vector2 - Vector2.Dot(hazardNormal, moveAmount) * hazardNormal * num;
	}

	public void GetReady()
	{
		if (!Singleton<GameManager>.Instance.testMode)
		{
			character.anim.ResetTrigger("Appear");
			character.anim.SetTrigger("Appear");
			StartCoroutine(SpawnIn());
			playerInput.roundsSinceLastInputReceived++;
		}
	}

	private IEnumerator SpawnIn(bool temporaryInvulnerability = false)
	{
		character.IsVisible = true;
		RotateSnapTo(new Vector2(0f, -1f));
		shadow.SetActive(value: false);
		base.IsInvulnerable = true;
		character.anim.SetFloat("MoveSpeed", 0f);
		character.SetFacialExpression(character.spawnFace, 1f);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SpawnBurst, base.transform.position);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/spawnin_whoosh", base.transform.position);
		yield return new WaitForSeconds(0.2f);
		shadow.SetActive(value: true);
		yield return new WaitForSeconds(character.groundSlamDelayOnSpawn - 0.2f);
		if (activePowerup.HasPowerup(PowerupType.Shield))
		{
			IsShielded = true;
		}
		else if (IsPlayerElligibleForAutobalanceShield())
		{
			StartShield();
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (IsSeeker)
			{
				ClearPowerups();
			}
			else
			{
				seekerPFX.gameObject.SetActive(value: false);
				StartPowerup(PowerupType.Disguise);
			}
		}
		if (Singleton<SettingsManager>.Instance.debugMode && Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC && Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups != Powerup.AllPowerups())
		{
			foreach (PowerupType possiblePowerup in Singleton<LevelManager>.Instance.possiblePowerups)
			{
				StartPowerup(possiblePowerup);
			}
		}
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.GroundSlam, base.transform.position, character.Color, base.transform);
		playerInput.StartVibration(0.5f, 0.2f);
		Singleton<Camera3D>.Instance.Shake(1.2f, 0.2f);
		if (temporaryInvulnerability)
		{
			StartCoroutine(TemporaryInvulnerability(1.5f));
		}
		yield return new WaitForSeconds(0.5f);
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			isSpawningIn = false;
			canWalk = true;
			character.anim.ResetTrigger("StopAppear");
			character.anim.SetTrigger("StopAppear");
		}
		canRotate = true;
		yield return new WaitForSeconds(0.5f);
		if (!temporaryInvulnerability)
		{
			base.IsInvulnerable = false;
		}
	}

	public void ShowSeekerPFX()
	{
		seekerPFX.transform.DOComplete();
		seekerPFX.gameObject.SetActive(value: true);
		seekerPFX.transform.localScale = Vector3.one;
		seekerPFX.transform.DOScale(0f, 0.3f).From().SetEase(Ease.OutBack);
	}

	public void HideSeekerPFX()
	{
		if (seekerPFX.gameObject.activeSelf)
		{
			seekerPFX.transform.DOComplete();
			seekerPFX.transform.DOScale(0f, 0.3f).SetEase(Ease.OutSine).OnComplete(delegate
			{
				seekerPFX.gameObject.SetActive(value: false);
			});
		}
	}

	public bool IsPlayerElligibleForAutobalanceShield()
	{
		if (activePowerup.HasPowerup(PowerupType.Shield))
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.MatchSettings.autoBalance && Singleton<GameManager>.Instance.IsALosingPlayer(this))
		{
			return powerupHistory.Count < maxActivePowerups;
		}
		return false;
	}

	public void Go()
	{
		isSpawningIn = false;
		canWalk = true;
		canRotate = true;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (IsSeeker)
			{
				character.SetFacialExpression(FacialExpression.content);
			}
			else
			{
				character.SetFacialExpression(FacialExpression.worried);
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (IsSeeker)
			{
				RespawnDisc(Singleton<SettingsManager>.Instance.trailerMode ? 0f : combatDelayAtStart);
				physicsCollider.enabled = true;
			}
		}
		else
		{
			RespawnDisc(Singleton<SettingsManager>.Instance.trailerMode ? 0f : combatDelayAtStart, (!activePowerup.HasPowerup(PowerupType.ExtraDisc)) ? 1 : 2);
		}
		if (!Singleton<GameManager>.Instance.testMode)
		{
			InitLeaderIcon();
			if (Singleton<LevelManager>.Instance.level.isTutorialLevel && !hasCompletedTutorial && metadata.Dashes == 0)
			{
				StartCoroutine(playerInput.ShowDashHint(3f, onlyShowHintIfNeverDashed: true));
			}
			else if (activePowerup.HasPowerup(PowerupType.Decoy))
			{
				playerInput.ConsiderShowingDecoyHint();
			}
		}
	}

	protected override void FixedUpdate()
	{
		inputMove = playerInput.inputMove;
		base.FixedUpdate();
		if (gettingRevivedThisFrame)
		{
			gettingRevivedThisFrame = false;
			if (reviveTimer >= Actor.reviveDuration)
			{
				Revive();
				reviveTimer = 0f;
			}
		}
		else if (reviveTimer > 0f)
		{
			if (revivingPFX.isPlaying)
			{
				revivingPFX.Stop();
			}
			reviveTimer -= Time.fixedDeltaTime * 4f;
			if (reviveTimer <= 0f)
			{
				reviveTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
				revivingPFX.Stop();
				if (reviveSFXInstance.isValid())
				{
					reviveSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
					reviveSFXInstance.release();
				}
			}
		}
		else
		{
			reviveTimer = 0f;
		}
		if (aimTimer > aimStartTime * 0.5f)
		{
			if (rotationQueue.Count >= 10)
			{
				rotationQueue.Dequeue();
			}
			rotationQueue.Enqueue(lastInputMove.normalized);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (dropInAIOnTimeOut && Singleton<SettingsManager>.Instance.GameOptions.conventionMode && (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionOut || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionIn))
		{
			playerInput.timeSinceLastInputReceived += Time.deltaTime;
			if (Singleton<LevelManager>.Instance.level.isTutorialLevel && !hasCompletedTutorial && playerInput.timeSinceLastInputReceived > 20f)
			{
				Debug.Log("Spent too long in tutorial");
				DropInAI();
			}
		}
		if (activePowerup.HasPowerup(PowerupType.TimeImmune) && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			timeScaler = ((Time.timeScale > 0f) ? (1f / Time.timeScale) : 1f);
			if (timeImmuneTimer > 0f)
			{
				timeImmuneTimer -= Time.deltaTime;
			}
			else if (Singleton<TimeKeeper>.Instance.targetScale - Time.timeScale < 0.4f)
			{
				timeImmuneTimer = 0f;
				activePowerup &= ~PowerupType.TimeImmune;
				timeScaler = 1f;
				timeImmunePFX.Stop();
			}
		}
		else
		{
			timeScaler = 1f;
		}
		if (actorState == ActorState.Alive && !IsDisguised && activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			moveFasterPFX.gameObject.SetActive(value: true);
		}
		else
		{
			moveFasterPFX.gameObject.SetActive(value: false);
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && actorState != ActorState.Dead)
		{
			CheckIfPlayerIsInBounds();
		}
		else if ((Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu || actorState == ActorState.Dead) && isOutOfBounds)
		{
			isOutOfBounds = false;
			StopOutOfBoundsEffect();
		}
		if (isWading && velocity.magnitude < 12f && !IsAttacking)
		{
			IsHidden = true;
			metadata.TimeInPlants += Time.deltaTime;
		}
		else
		{
			IsHidden = false;
		}
		if (HasDiscs > 0)
		{
			IsDisarmed = false;
		}
		if (!isCoolingAttack)
		{
			attacksLeft = HasDiscs;
			disarmsLeft = 1;
		}
		RunReverseInputsTimer();
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			RunGoldenDiscTimer();
		}
		if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
		{
			goldenDiscPFX.position = character.rightDisc.transform.position;
		}
		if (isOnFire && burnTimer < burnDuration)
		{
			metadata.TimeOnFire += Time.deltaTime;
			if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
			{
				playerInput.StartVibration(burnTimer / burnDuration * 0.5f + 0.25f, 0f);
			}
			if (extinguishAmount >= 1f || base.IsOnWetFloor)
			{
				ToggleFire(onFire: false, extinguishPFX: true);
				metadata.FiresExtinguished++;
			}
		}
		if (isSlimeTiming && slimeTimer < Actor.slimeDuration)
		{
			if (!IsFrozen && IsSlimed)
			{
				slimeTimeUIClock.fillAmount = Mathf.Lerp(slimeTimeUIClock.fillAmount, 1f - Mathf.Clamp01(slimeTimer / Actor.slimeDuration), Time.deltaTime * 30f);
			}
			metadata.TimeSlimed += Time.deltaTime;
		}
		if (IsFrozen && freezeTimer < Actor.freezeDuration)
		{
			slimeTimeUIClock.fillAmount = 0f;
			metadata.TimeSlimed += Time.deltaTime;
		}
		if (reviveTimer > 0f)
		{
			reviveTimeUIClock.fillAmount = reviveTimer / Actor.reviveDuration;
			if (reviveSFXInstance.isValid())
			{
				reviveSFXInstance.setParameterByName("retrieveCharge", reviveTimeUIClock.fillAmount);
			}
		}
		if (retrieveCharge > 0f && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			retrievePFX.SetActive(value: true);
			character.anim.SetBool("IsRetrieving", value: true);
			if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
			{
				if (activePowerup.HasPowerup(PowerupType.ControlledDisc))
				{
					playerInput.StartVibration(retrieveCharge * 0.1f + 0.15f, 0f);
					controlledPFX.SetActive(value: true);
				}
				else
				{
					playerInput.StartVibration(retrieveCharge * 0.333f + 0.15f, 0f);
				}
			}
		}
		else
		{
			StopRetrieve();
		}
		float num = 5.5f;
		if (Singleton<GameManager>.Instance.IsInSeekingPhase)
		{
			num = speedLimit;
		}
		if (activePowerup.HasPowerup(PowerupType.Disguise) && velocity.magnitude < num && !IsStunned && !isShowingOff && !IsDashing && !isLunging && !IsFrozen && canWalk && !isSpawningIn && !IsHidden && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && actorState == ActorState.Alive)
		{
			if (!IsDisguised && velocity.magnitude < 0.5f)
			{
				disguiseTimer += Time.deltaTime * timeScaler;
				if (disguiseTimer >= disguiseStartDuration && !Singleton<GameManager>.Instance.IsInRevealPhase)
				{
					IsDisguised = true;
				}
			}
		}
		else if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && (!IsHider || actorState != ActorState.Alive || Singleton<GameManager>.Instance.IsInHidingPhase))
		{
			BreakDisguise();
		}
		if (IsDisguised)
		{
			metadata.TimeDisguised += Time.deltaTime;
			timeInCurrentDisguise += Time.deltaTime;
		}
		character.anim.speed = timeScaler;
		shieldAnimator.speed = timeScaler;
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu || Singleton<GameManager>.Instance.gameState == GameManager.GameState.Paused || Singleton<GameManager>.Instance.gameState == GameManager.GameState.Loading)
		{
			playerInput.StopVibration();
		}
		metadata.MoveDistance += (base.transform.position - positionLastFrame).magnitude;
		if (isOnFire && Singleton<TimeKeeper>.Instance.TimeSince(lastTeleportTimestamp) > Time.deltaTime && canUseTeleportal)
		{
			extinguishAmount += (base.transform.position - positionLastFrame).magnitude / dashDistance * 0.24f;
		}
		positionLastFrame = base.transform.position;
		if (IsDisarmed)
		{
			metadata.TimeDisarmed += Time.deltaTime;
		}
		UpdateLeaderIcon();
	}

	protected override Vector2 CalculateWalk()
	{
		if (activePowerup.HasPowerup(PowerupType.ReverseInputs))
		{
			inputMove *= -1f;
			metadata.TimeBamboozled += Time.fixedDeltaTime;
		}
		return base.CalculateWalk();
	}

	protected override float GetMoveSpeed()
	{
		float num = walkSpeed;
		num = ((!IsDisarmed) ? walkSpeed : walkSpeedUnarmed);
		if (HasGoldenDisc == HoldingGoldenDisc.IsHolding && !activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			num *= 0.8f;
		}
		if (activePowerup.HasPowerup(PowerupType.MoveFaster) && HasGoldenDisc != HoldingGoldenDisc.IsHolding)
		{
			num *= 1.4f;
		}
		if (IsAiming)
		{
			num = aimSpeed;
		}
		else if (aimMoveLag > 0f)
		{
			aimMoveLag -= Time.fixedDeltaTime * timeScaler * (1f / aimMoveLagDuration);
			num = Mathf.Lerp(num, aimSpeed, Easing.EaseOut(aimMoveLag));
		}
		if (retrieveCharge > 0f)
		{
			num = ((!activePowerup.HasPowerup(PowerupType.ControlledDisc)) ? retrieveSpeed : controlledSpeed);
		}
		if (isWading)
		{
			num *= wadeFactor;
		}
		if (IsSlimed)
		{
			num *= slimedFactor;
		}
		if (IsHider && Singleton<GameManager>.Instance.IsInSeekingPhase)
		{
			num *= hideFactor;
		}
		return num * Mathf.Clamp(timeScaler, 1f, 1.75f);
	}

	public override bool Dash()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && ((IsHider && !Singleton<GameManager>.Instance.IsInHidingPhase) || (IsSeeker && !Singleton<GameManager>.Instance.IsInSeekingPhase)))
		{
			return false;
		}
		if (actorState == ActorState.Falling && fallTimer > fallAffordance)
		{
			return false;
		}
		if (!IsStunned && !isShowingOff && !isLunging && Teleport())
		{
			if (actorState == ActorState.Falling)
			{
				FallSaved();
			}
			character.SetFacialExpression(FacialExpression.aggressive);
			if (!hasCreatedDecoy && activePowerup.HasPowerup(PowerupType.Decoy))
			{
				CreateDecoy();
			}
			return true;
		}
		if (base.Dash())
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/dash", base.transform.position);
			character.SetFacialExpression(FacialExpression.aggressive);
			if (activePowerup.HasPowerup(PowerupType.MoveFaster))
			{
				dashForce *= 1.25f;
				dashTimer = dashDuration * 0.2f;
				dashCooldownTimer = dashCooldown * 0.6f;
			}
			if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
			{
				dashTimer = dashDuration * 0.25f;
			}
			if (activePowerup.HasPowerup(PowerupType.DashThroughWalls))
			{
				insidesCollider.enabled = true;
				collisionLayer = (int)collisionLayer ^ (int)obstacleLayer;
			}
			if (!hasCreatedDecoy && activePowerup.HasPowerup(PowerupType.Decoy))
			{
				CreateDecoy();
			}
			metadata.Dashes++;
			return true;
		}
		return false;
	}

	protected override Vector2 CalculateDash()
	{
		if (IsDashing && dashTimer >= dashDuration)
		{
			if (activePowerup.HasPowerup(PowerupType.DashThroughWalls))
			{
				if ((bool)Physics2D.OverlapCircle(base.transform.position, 0.3f, obstacleLayer))
				{
					dashTimer = dashDuration * 0.4f;
					if (dashCooldownTimer < dashTimer)
					{
						dashCooldownTimer = dashDuration;
					}
					return dashDirection * currentDashAmt;
				}
				collisionLayer = (int)collisionLayer | (int)obstacleLayer;
			}
			insidesCollider.enabled = false;
		}
		return base.CalculateDash();
	}

	protected override void StopDashOnHazards()
	{
		if (!stopDashOnHazards || canWalk || IsStunned || !activePowerup.HasPowerup(PowerupType.DashThroughWalls) || !Physics2D.OverlapCircle(base.transform.position, 0.3f, obstacleLayer))
		{
			base.StopDashOnHazards();
		}
	}

	public override void CheckForSquishing(ref Vector3 newPosition, int recursionDepth = 0)
	{
		if (!IsDashing || !activePowerup.HasPowerup(PowerupType.DashThroughWalls))
		{
			base.CheckForSquishing(ref newPosition, recursionDepth);
		}
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		base.CollisionEnter2D(normal, collider);
		if (actorState == ActorState.Dead || !collider.CompareTag("Player"))
		{
			return;
		}
		Actor component = collider.GetComponent<Actor>();
		Player player = component as Player;
		SwapDiscsWithOtherPlayer(player);
		if (component.actorState == ActorState.Dead)
		{
			return;
		}
		if (player != null && player.HasGoldenDisc == HoldingGoldenDisc.IsHolding && killer != null && (killer == this || killer == component))
		{
			SetKiller(component, KillerType.GoldenDisc, IsFrozen ? 3f : 1.5f);
		}
		if (component.IsDashing)
		{
			playerInput.StartVibration(0.3f, 0.3f);
		}
		if (!IsDashing)
		{
			if (component.IsDashing && Singleton<SettingsManager>.Instance.MatchSettings.dashDisarmsOpponents && HasDiscs > 0)
			{
				Vector3 vector = -(collider.transform.position - base.transform.position).normalized;
				if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || HasGoldenDisc == HoldingGoldenDisc.IsHolding)
				{
					Singleton<TimeKeeper>.Instance.SlowMoEffect(0.25f);
				}
				DropDisc(vector * 2f, dropAllDiscs: false);
				metadata.DisarmedByPlayers++;
			}
		}
		else
		{
			metadata.Pushes++;
		}
	}

	public override void StartPushback(Vector2 direction, float duration, float stunDuration = 0f)
	{
		base.StartPushback(direction, duration, stunDuration);
		if (stunDuration > 0f && !IsFrozen)
		{
			character.SetFacialExpression(FacialExpression.hurt);
		}
	}

	protected override void CooldownStun()
	{
		base.CooldownStun();
		if (IsStunned && stunTimer >= pushStunDuration)
		{
			character.facialExpressionTimer = 0.1f;
		}
	}

	private void CheckIfPlayerIsInBounds()
	{
		if (!Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
		{
			if (!isOutOfBounds)
			{
				isOutOfBounds = true;
				if (Singleton<LevelManager>.Instance.isLevelBoundaryShrinking)
				{
					outOfBoundsDuration = 3.75f;
				}
				else
				{
					outOfBoundsDuration = 3f;
				}
				outOfBoundsTimer = outOfBoundsDuration;
			}
		}
		else if (isOutOfBounds)
		{
			isOutOfBounds = false;
			playerInput.StopVibration();
		}
		if (isOutOfBounds)
		{
			if (outOfBoundsTimer > 0f && actorState == ActorState.Alive)
			{
				float num = Easing.EaseIn(1f - outOfBoundsTimer / outOfBoundsDuration);
				outOfBoundsTimer -= Time.deltaTime;
				if (outOfBoundsTimer <= outOfBoundsDuration * 0.8f)
				{
					StartOutOfBoundsEffect();
					float num2 = 0.5f + Mathf.Sin(4.712389f + (outOfBoundsDuration * 0.8f - outOfBoundsTimer) * (3f + num * 17f)) * 0.5f;
					if (num2 > 0.8f && character.DangerEffect <= 0.8f)
					{
						Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/death/outofbounds_warning", "pitch", num, base.transform);
					}
					character.DangerEffect = num2;
					outOfBoundsArrow.alpha = num2;
				}
				else
				{
					character.DangerEffect = 0f;
				}
				playerInput.StartVibration(num * 0.5f, 0f);
				metadata.TimeOutOfBounds += Time.fixedDeltaTime;
			}
			else if (outOfBoundsTimer <= 0f)
			{
				playerInput.StopVibration();
				StopOutOfBoundsEffect();
				if (!Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
				{
					Die(DeathType.outofbounds);
				}
			}
		}
		else
		{
			StopOutOfBoundsEffect();
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		if (Singleton<SettingsManager>.Instance.MatchSettings.teamRevives && actorState == ActorState.Alive && canWalk && !IsFrozen && !IsStunned && collider.gameObject.layer == LayerMask.NameToLayer("PlayerDeath"))
		{
			Player component = collider.transform.parent.GetComponent<Player>();
			if (component != null && component != this && component.actorState == ActorState.Dead && !Singleton<GameManager>.Instance.AreOpponents(component, this))
			{
				playerInput.StartVibration(component.reviveTimer / Actor.reviveDuration, 0.1f);
				component.AddReviveTime();
			}
		}
		base.OnTriggerStay2D(collider);
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		if (canUseTeleportal && collider.CompareTag("Teleportal"))
		{
			moveFasterPFX.gameObject.SetActive(value: false);
			if (activePowerup.HasPowerup(PowerupType.DashThroughWalls) && (Vector2.Dot((base.transform.position - collider.transform.position).normalized, collider.transform.up) < 0f || Vector2.Dot(velocity.normalized, collider.transform.up) >= 0f))
			{
				return;
			}
		}
		base.OnTriggerEnter2D(collider);
		if (collider.gameObject.layer == LayerMask.NameToLayer("PlayerDeath"))
		{
			Player component = collider.transform.parent.GetComponent<Player>();
			if (component != null && component != this)
			{
				SwapDiscsWithOtherPlayer(component);
			}
		}
	}

	public override void OnTriggerExit2D(Collider2D collider)
	{
		base.OnTriggerExit2D(collider);
		if (IsDashing && IsPositionOverHazard(base.transform.position))
		{
			metadata.DashesOverHazards++;
		}
	}

	public bool ThrowDisc()
	{
		if (!Singleton<SettingsManager>.Instance.MatchSettings.canThrowDiscs)
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && (IsHider || !Singleton<GameManager>.Instance.IsInSeekingPhase))
		{
			return false;
		}
		BreakDisguise();
		if (!IsStunned && !isShowingOff)
		{
			TriggerDisc();
		}
		if (attacksLeft > 0 && !IsStunned && !isShowingOff && !IsFrozen && HasDiscs > 0 && retrieveCharge == 0f)
		{
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<SettingsManager>.Instance.limitSeekAttempts)
			{
				if (seekAttemptsRemaining <= 0)
				{
					return false;
				}
				seekAttemptsRemaining--;
				Singleton<GameManager>.Instance.OnSeekAttempted(seekAttemptsRemaining);
			}
			attacksLeft--;
			Vector2 vector = base.transform.position + base.transform.up * discSpawnDistance;
			RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position - base.transform.up * 0.1f, discPrefab.GetComponent<CircleCollider2D>().radius * discPrefab.transform.lossyScale.x, base.transform.up, discSpawnDistance * 2f, discPrefab.GetComponent<Disc>().obstacleLayer);
			if ((bool)raycastHit2D)
			{
				vector = raycastHit2D.centroid - (raycastHit2D.point - (Vector2)base.transform.position).normalized * 0.1f;
			}
			Disc disc = Singleton<GameManager>.Instance.discPrefab.Spawn((Vector3)vector + new Vector3(0f, 0f, -0.75f), base.transform.rotation);
			disc.DiscOwner = this;
			disc.throwDirection = ((lastInputMove.magnitude > 0f) ? lastInputMove.normalized : ((Vector2)base.transform.up));
			if (!canWalk)
			{
				disc.throwDirection = base.transform.up;
			}
			disc.aimPower = aimPower;
			disc.timeScaler = timeScaler;
			float num = 0f;
			if (rotationQueue.Count > 0)
			{
				num = Vector2.Angle(lastInputMove.normalized, rotationQueue.Peek());
				if (Vector3.Cross(lastInputMove.normalized, rotationQueue.Peek()).z < 0f)
				{
					num = 0f - num;
				}
				num = Mathf.Clamp(num, -90f, 90f);
			}
			disc.spinAngle = num;
			metadata.SpinAngle += Mathf.Abs(num);
			if (HasGoldenDisc != HoldingGoldenDisc.IsHolding)
			{
				disc.discPowerup = activePowerup;
			}
			Singleton<GameManager>.Instance.activeDiscs.Add(disc);
			disc.Init(isDropped: false, isMiniDisc: false, HasGoldenDisc == HoldingGoldenDisc.IsHolding);
			character.anim.SetFloat("AttackSpeed", 1.75f);
			if (activePowerup.HasPowerup(PowerupType.ExplosiveDisc) && !activePowerup.HasPowerup(PowerupType.MultiDisc) && HasGoldenDisc != HoldingGoldenDisc.IsHolding)
			{
				disc.grenadeInstance.gameObject.SetActive(value: true);
				disc.grenadeInstance.parentDisc = disc;
				disc.grenadeInstance.discOwner = this;
			}
			if (HasDiscs == 1)
			{
				character.anim.SetTrigger("ThrowRight");
			}
			else if (HasDiscs == 2)
			{
				character.anim.SetTrigger("ThrowLeft");
			}
			else
			{
				character.anim.SetTrigger("ThrowRight");
			}
			character.SetFacialExpression(FacialExpression.idle);
			if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
			{
				HasGoldenDisc = HoldingGoldenDisc.IsThrown;
			}
			HasDiscs--;
			if (!Singleton<LevelManager>.Instance.level.isTutorialLevel)
			{
				metadata.Throws++;
			}
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/combat/throw", "aimPower", aimPower, disc.transform);
			return true;
		}
		return false;
	}

	private void DropDisc(Vector3 dropDirection, bool dropAllDiscs = true)
	{
		if (HasDiscs == 0 && HasGoldenDisc != HoldingGoldenDisc.IsHolding)
		{
			return;
		}
		if (HasDiscs == 0 && HasGoldenDisc == HoldingGoldenDisc.IsHolding)
		{
			Debug.Log(character.name + " should not drop golden disc because they are not holding any discs! How did this happen?");
		}
		float num = 1f;
		if (dropAllDiscs && HasDiscs > 1)
		{
			dropDirection = VectorExt.RotatePointAroundPivot(dropDirection, Vector3.zero, new Vector3(0f, 0f, -15f));
			num = HasDiscs;
		}
		for (int i = 0; (float)i < num; i++)
		{
			float num2 = discSpawnDistance;
			RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position - dropDirection.normalized * 0.1f, discPrefab.GetComponent<CircleCollider2D>().radius * discPrefab.transform.lossyScale.x, dropDirection.normalized, discSpawnDistance, discPrefab.GetComponent<Disc>().obstacleLayer);
			if ((bool)raycastHit2D)
			{
				num2 = (raycastHit2D.centroid - (Vector2)base.transform.position).magnitude - 0.25f;
			}
			Disc disc = Singleton<GameManager>.Instance.discPrefab.Spawn(base.transform.position + new Vector3(0f, 0f, -0.75f) + dropDirection.normalized * num2, Quaternion.Euler(dropDirection.normalized));
			disc.DiscOwner = this;
			disc.throwDirection = dropDirection;
			disc.aimPower = 0f;
			disc.discPowerup = activePowerup;
			disc.Init(isDropped: true, isMiniDisc: false, HasGoldenDisc == HoldingGoldenDisc.IsHolding);
			Singleton<GameManager>.Instance.activeDiscs.Add(disc);
			character.anim.SetFloat("AttackSpeed", 0.75f);
			if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
			{
				HasGoldenDisc = HoldingGoldenDisc.IsDropped;
			}
			HasDiscs--;
			dropDirection = VectorExt.RotatePointAroundPivot(dropDirection, Vector3.zero, new Vector3(0f, 0f, 30f));
		}
	}

	public void TriggerDisc()
	{
		if (thrownDiscs.Count == 0 && thrownGoldenDisc == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < thrownDiscs.Count; i++)
		{
			if (thrownDiscs[i].orbitIsCharged)
			{
				metadata.OrbitalThrows++;
				thrownDiscs[i].TriggerOrbiter();
				num++;
			}
		}
		if (HasGoldenDisc == HoldingGoldenDisc.IsThrown && thrownGoldenDisc != null && thrownGoldenDisc.orbitIsCharged)
		{
			metadata.OrbitalThrows++;
			thrownGoldenDisc.TriggerOrbiter();
			num++;
		}
		if (num > 0)
		{
			character.anim.SetTrigger("AttackDisarm");
			character.SetFacialExpression(FacialExpression.aggressive);
		}
	}

	public override void Aim()
	{
		if (!Singleton<SettingsManager>.Instance.MatchSettings.canThrowDiscs || (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && (IsHider || !Singleton<GameManager>.Instance.IsInSeekingPhase)))
		{
			return;
		}
		BreakDisguise();
		if (HasDiscs > 0 && retrieveCharge == 0f && !IsFrozen)
		{
			aimTimer += Time.deltaTime * timeScaler;
		}
		base.Aim();
		if (aimTimer >= aimStartTime)
		{
			metadata.TimeAiming += Time.deltaTime;
			if (!IsAiming)
			{
				character.SetFacialExpression(FacialExpression.focus);
				if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
				{
					aimAssistantRenderer.material.color = ColorExt.SetAlpha(Singleton<GameManager>.Instance.goldenDiscColor, 0f);
				}
			}
			if (!(aimTimer < aimDuration))
			{
				playerInput.StartVibration(0.1f, 0f);
			}
		}
		if (aimTimer == 0f && !IsDashing && !isLunging && (Singleton<SettingsManager>.Instance.MatchSettings.canRetrieveDiscs || activePowerup.HasPowerup(PowerupType.ControlledDisc)) && thrownDiscs.Count > 0 && HasDiscs + pendingDiscs < 2)
		{
			if ((double)retrieveCharge < 1.4)
			{
				retrieveCharge += Time.deltaTime / retrieveChargeDuration * timeScaler;
			}
			metadata.TimeRetrieving += Time.deltaTime;
			character.SetFacialExpression(FacialExpression.retrieving);
		}
		else if ((Singleton<SettingsManager>.Instance.MatchSettings.canRetrieveDiscs || activePowerup.HasPowerup(PowerupType.ControlledDisc)) && (thrownDiscs.Count == 0 || HasDiscs + pendingDiscs > 1) && retrieveCharge > 0f)
		{
			retrieveCharge = 0f;
			if (playerInput.inputThrowHeld)
			{
				playerInput.inputThrowIsBlocked = true;
				playerInput.inputThrowHeld = false;
			}
			character.SetFacialExpression(FacialExpression.idle);
		}
	}

	public override void StopAiming()
	{
		if (IsAiming)
		{
			playerInput.StopVibration();
		}
		base.StopAiming();
		rotationQueue.Clear();
		if (playerInput.inputThrowHeld)
		{
			playerInput.inputThrowIsBlocked = true;
			playerInput.inputThrowHeld = false;
		}
		if (retrieveCharge > 0f)
		{
			if (IsDisarmed)
			{
				character.SetFacialExpression(FacialExpression.worried);
			}
			else
			{
				character.SetFacialExpression(FacialExpression.idle);
			}
			retrieveCharge = 0f;
		}
	}

	private void StopRetrieve()
	{
		if (retrievePFX.activeSelf)
		{
			character.anim.SetBool("IsRetrieving", value: false);
			retrievePFX.SetActive(value: false);
			controlledPFX.SetActive(value: false);
			playerInput.StopVibration();
		}
		if (retrieveCharge > 0f)
		{
			character.anim.SetBool("IsRetrieving", value: false);
			playerInput.StopVibration();
			retrieveCharge = 0f;
		}
	}

	public bool Teleport()
	{
		if (thrownDiscs.Count == 0 || !activePowerup.HasPowerup(PowerupType.TeleportDisc))
		{
			return false;
		}
		Disc disc = thrownDiscs[UnityEngine.Random.Range(0, thrownDiscs.Count - 1)];
		if ((base.transform.position - disc.transform.position).magnitude < physicsColliderRadius * 3f && IsInLineOfSight(disc.transform.position))
		{
			return true;
		}
		if (IsPositionOverHazard(disc.transform.position))
		{
			if (thrownDiscs.Count == 1)
			{
				return false;
			}
			bool flag = false;
			for (int i = 0; i < thrownDiscs.Count; i++)
			{
				if (!IsPositionOverHazard(thrownDiscs[i].transform.position) && thrownDiscs[i].movement != Projectile.Movement.Squished)
				{
					disc = thrownDiscs[i];
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		int num = Physics2D.OverlapCircleNonAlloc(disc.transform.position, physicsColliderRadius + 0.01f, teleportDestColliders, playerLayer);
		for (int j = 0; j < num; j++)
		{
			if (!(teleportDestColliders[j].gameObject == base.gameObject))
			{
				teleportDestColliders[j].GetComponent<Player>().SetKiller(this, disc.IsGoldenDisc ? KillerType.GoldenDisc : KillerType.Telefrag);
				teleportDestColliders[j].GetComponent<Player>().Die(DeathType.telefrag);
			}
		}
		if (!disc.discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
		{
			disc.canCollideWithOwner = true;
			disc.canBePickedUp = true;
		}
		else
		{
			disc.Explode(0.25f);
			if (disc.isMiniDisc && pendingDiscs > 0)
			{
				StopCoroutine(respawnDiscRoutine);
				pendingDiscs--;
				RespawnDisc(1.5f);
			}
		}
		_ = (Vector2)base.transform.position;
		dustTrail.Stop();
		if (moveFasterPFX.gameObject.activeSelf)
		{
			moveFasterPFX.Stop();
		}
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Teleport, base.transform.position, character.Color);
		base.transform.SetParent(null, worldPositionStays: true);
		interpolatedTransform.ForgetPreviousTransforms();
		base.transform.position = new Vector3(disc.transform.position.x, disc.transform.position.y, base.transform.position.z);
		interpolatedTransform.ForgetPreviousTransforms();
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.TeleportExit, base.transform.position, character.Color);
		dustTrail.Play();
		if (moveFasterPFX.gameObject.activeSelf)
		{
			moveFasterPFX.Play();
		}
		if (activePowerup.HasPowerup(PowerupType.FireDisc))
		{
			Singleton<CellManager>.Instance.ExtinguishCellsAt(base.transform.position, CellManager.CellType.Fire, checkMovingFloors: true, 10);
		}
		if (activePowerup.HasPowerup(PowerupType.SlimeDisc))
		{
			Singleton<CellManager>.Instance.ExtinguishCellsAt(base.transform.position, CellManager.CellType.Ice, checkMovingFloors: true, 10);
		}
		if (disc.canBePickedUp)
		{
			if (disc.isMiniDisc)
			{
				PickUpMiniDisc(disc.GetComponent<Collider2D>());
			}
			else
			{
				PickUpDisc(disc.GetComponent<Collider2D>());
			}
		}
		BreakDisguise();
		StopRetrieve();
		character.anim.SetTrigger("Teleport");
		if (IsDashing)
		{
			dashTrail.Stop();
			StopDash(rechargeDash: true);
		}
		illuminatePFX.Play();
		attackCooldownTimer = 0f;
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/teleport", base.transform.position);
		metadata.Teleports++;
		if (isWading)
		{
			NotifyPlantsOfDeath();
		}
		lastTeleportTimestamp = Singleton<TimeKeeper>.Instance.CurrentTime;
		return true;
	}

	public bool IsItSafeToTeleport()
	{
		if (thrownDiscs.Count > 0)
		{
			for (int i = 0; i < thrownDiscs.Count; i++)
			{
				if (!IsPositionOverHazard(thrownDiscs[i].transform.position))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public override bool Attack()
	{
		if (IsHider)
		{
			SwitchDisguise();
			return true;
		}
		if (!IsStunned && !isShowingOff && !IsAiming && (attacksLeft > 0 || (!HasWeapon() && CanDisarm())))
		{
			BreakDisguise();
			if (IsFrozen)
			{
				ChipOutOfFrozen();
				return true;
			}
			if (activePowerup.HasPowerup(PowerupType.DashThroughWalls) && (bool)Physics2D.OverlapCircle(base.transform.position, 0.3f, obstacleLayer))
			{
				return false;
			}
			if (HasWeapon())
			{
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<SettingsManager>.Instance.limitSeekAttempts)
				{
					if (seekAttemptsRemaining <= 0)
					{
						return false;
					}
					seekAttemptsRemaining--;
					Singleton<GameManager>.Instance.OnSeekAttempted(seekAttemptsRemaining);
				}
				StopRetrieve();
				ParticleSystem.MainModule main = meleeTrailRight.main;
				main.startLifetime = meleeTrailDefaultLifetime * 1f / timeScaler;
				main.startDelay = meleeTrailDefaultDelay * 1f / timeScaler;
				if (attacksLeft == 1)
				{
					meleeTrailRight.Play();
				}
				else if (attacksLeft == 2)
				{
					meleeTrailLeft.Play();
				}
				disarmsLeft = 0;
				base.Attack();
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/melee", base.transform.position);
				return true;
			}
			if (!IsFrozen)
			{
				base.Attack();
				lungeCooldownTimer *= 0.85f;
				IsDisarming = true;
				disarmsLeft--;
				attacksLeft = 0;
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/disarm", base.transform.position);
				return true;
			}
			if (activePowerup.HasPowerup(PowerupType.MoveFaster))
			{
				attackCooldownTimer = attackCooldownDuration * 0.66f;
				lungeCooldownTimer = lungeCooldownDuration * 0.66f;
				character.anim.SetFloat("AttackSpeed", 1.5f);
			}
		}
		return false;
	}

	protected override bool HasWeapon()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && (IsHider || !Singleton<GameManager>.Instance.IsInSeekingPhase))
		{
			return false;
		}
		return HasDiscs > 0;
	}

	protected override bool CanDisarm()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && (IsHider || !Singleton<GameManager>.Instance.IsInSeekingPhase))
		{
			return false;
		}
		return disarmsLeft > 0;
	}

	protected override void GetNotifiedOfSuccessfulMeleeKill()
	{
		playerInput.StartVibration(0.7f, 0.3f);
		base.GetNotifiedOfSuccessfulMeleeKill();
		RecordKill();
	}

	public void RecordKill()
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			if (Singleton<TimeKeeper>.Instance.TimeSince(timeOfLastKill) > recentKillMaxTime)
			{
				numRecentKills = 0;
			}
			timeOfLastKill = Singleton<TimeKeeper>.Instance.CurrentTime;
			numRecentKills++;
			if (numRecentKills == 2)
			{
				metadata.DoubleKills++;
				alertBar.ShowAlert(AlertBar.AlertType.DoubleKill);
			}
			else if (numRecentKills == 3)
			{
				metadata.DoubleKills--;
				metadata.TripleKills++;
				alertBar.ShowAlert(AlertBar.AlertType.TripleKill);
				Singleton<TimeKeeper>.Instance.FreezeFrame(0.2f);
			}
			else if (numRecentKills > 3)
			{
				metadata.TripleKills--;
				metadata.MultiKills++;
				alertBar.ShowAlert(AlertBar.AlertType.MultiKill);
				Singleton<TimeKeeper>.Instance.FreezeFrame(0.2f);
			}
		}
	}

	protected override void CooldownAttack()
	{
		base.CooldownAttack();
		if (meleeTimer <= 0f)
		{
			IsDisarming = false;
		}
	}

	protected override void InterruptAttack()
	{
		base.InterruptAttack();
		IsDisarming = false;
	}

	public override void GetDisarmed(Vector3 disarmDirection)
	{
		base.GetDisarmed(disarmDirection);
		StopMeleeTrail();
		if (HasWeapon() && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || HasGoldenDisc == HoldingGoldenDisc.IsHolding))
		{
			Singleton<TimeKeeper>.Instance.SlowMoEffect(0.25f);
		}
		if (HasWeapon() && !IsFrozen)
		{
			DropDisc(disarmDirection.normalized * 1.5f, dropAllDiscs: false);
			metadata.DisarmedByPlayers++;
		}
	}

	protected override float GetDisarmedStunDuration()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && (HasGoldenDisc == HoldingGoldenDisc.IsHolding || HasGoldenDisc == HoldingGoldenDisc.IsThrown))
		{
			return 1.125f;
		}
		return base.GetDisarmedStunDuration();
	}

	protected override void GetStunned(Vector3 disarmDirection, float stunDuration = 0.66f)
	{
		base.GetStunned(disarmDirection, stunDuration);
		BreakDisguise();
		playerInput.StartVibration(0.5f, 0.5f);
	}

	public override void OnDamageColliderTriggered(Collider2D collider)
	{
		if (IsDashing)
		{
			metadata.Dodges++;
		}
		if (!IsShielded)
		{
			base.OnDamageColliderTriggered(collider);
		}
	}

	public override void OnDamageColliderStay(Collider2D collider)
	{
		if (IsDashing && (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFreeze") || collider.CompareTag("ExplosionFire")))
		{
			metadata.Dodges++;
		}
		if (!IsShielded)
		{
			base.OnDamageColliderStay(collider);
		}
	}

	public override void OnMeleeColliderTriggered(Collider2D collider)
	{
		if (actorState == ActorState.Dead)
		{
			return;
		}
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") && !deflectedColliders.Contains(collider))
		{
			FastForwardMeleeTrail();
			Projectile component = collider.GetComponent<Projectile>();
			if (component.movement != Projectile.Movement.Dropped && !component.projectileStartedDropped)
			{
				metadata.Blocks++;
				Singleton<AudioManager>.Instance.PlaySting();
				Disc disc = component as Disc;
				if (disc != null && disc.aimPower >= 1f)
				{
					playerInput.StartVibration(0.2f, 0.1f);
				}
				else
				{
					playerInput.StartVibration(0.1f, 0.05f);
				}
			}
		}
		else if (collider.CompareTag("MeleeCollider"))
		{
			if ((bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				return;
			}
			metadata.Blocks++;
			Singleton<AudioManager>.Instance.PlaySting();
			playerInput.StartVibration(0.4f, 0.15f);
			FastForwardMeleeTrail();
			Player player = collider.transform.parent.GetComponent<Actor>() as Player;
			if (player == null || player.playerID < playerID)
			{
				CreateImpactLine(collider.transform.position);
				Singleton<Camera3D>.Instance.Shake(0.5f, 0.4f);
				if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() < 3)
				{
					Singleton<TimeKeeper>.Instance.FreezeFrame(0.2f);
				}
			}
		}
		else if (collider.CompareTag("ShieldCollider"))
		{
			FastForwardMeleeTrail();
		}
		else if (collider.CompareTag("Switch"))
		{
			FlipSwitch(collider);
		}
		base.OnMeleeColliderTriggered(collider);
	}

	private void FastForwardMeleeTrail()
	{
		if (meleeTrailRight.isPlaying)
		{
			_ = meleeTrailRight.main;
			if (meleeTrailRight.time < 0.125f)
			{
				meleeTrailRight.Simulate(0.125f, withChildren: true, restart: true);
				meleeTrailRight.Play();
			}
		}
		else if (meleeTrailLeft.isPlaying)
		{
			_ = meleeTrailLeft.main;
			if (meleeTrailLeft.time < 0.125f)
			{
				meleeTrailLeft.Simulate(0.125f, withChildren: true, restart: true);
				meleeTrailLeft.Play();
			}
		}
	}

	protected void StopMeleeTrail()
	{
		meleeTrailLeft.Stop();
		meleeTrailRight.Stop();
	}

	public void OnDisarmColliderTriggered(Collider2D collider)
	{
		if (collider.CompareTag("Switch"))
		{
			FlipSwitch(collider);
		}
		else
		{
			if (!collider.CompareTag("Player") || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				return;
			}
			Actor component = collider.GetComponent<Actor>();
			if (component != null && !component.IsDashing && !component.IsInvulnerable && actorState != ActorState.Dead)
			{
				Player player = component as Player;
				if (!(player != null) || !player.IsShielded)
				{
					StartCoroutine(InterruptAttackDelayed());
					meleeTimer = 0f;
					isCoolingAttack = true;
					attackCooldownTimer = attackCooldownDuration;
					velocity *= 0.5f;
					playerInput.StartVibration(0.5f, 0.3f);
					metadata.PlayersDisarmed++;
				}
			}
		}
	}

	private void FlipSwitch(Collider2D collider)
	{
		playerInput.StartVibration(0.4f, 0.15f);
		StopLunge();
		velocity += Vector2.Reflect(velocity * 1.5f, (collider.transform.position - base.transform.position).normalized) * slideFactor;
	}

	public void OnShieldColliderTriggered(Collider2D collider)
	{
		if (IsDashing)
		{
			return;
		}
		if (IsAttacking && Vector3.Dot((collider.transform.position - base.transform.position).normalized, base.transform.up) > 0f)
		{
			Debug.Log("Disc must have been deflected by melee collider, rejecting collision");
		}
		else
		{
			if ((bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				return;
			}
			if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
			{
				Projectile component = collider.GetComponent<Projectile>();
				if (!(component.owner != this) || (!Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire && !Singleton<GameManager>.Instance.AreOpponents(component.owner, this)))
				{
					return;
				}
				Disc disc = component as Disc;
				if (disc != null)
				{
					SetKiller(disc.owner, (!disc.IsGoldenDisc) ? KillerType.Disc : KillerType.GoldenDisc, 1.5f);
				}
				ShieldTakesDamage();
				Vector2 vector = (collider.transform.position - base.transform.position).normalized;
				float angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
				Vector2 vector2 = (Vector2)base.transform.position + vector * 2.5f;
				Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(vector2, Quaternion.AngleAxis(angle, Vector3.forward));
				Singleton<GameManager>.Instance.newVFXRico.Emit(character.Color, 90f);
				StopAiming();
				velocity = Vector3.zero;
				float num = Mathf.Max(1f / timeScaler * component.timeScaler, 1f / component.timeScaler * timeScaler);
				if (disc != null && disc.aimPower >= 1f)
				{
					Singleton<Camera3D>.Instance.Shake(0.25f, 0.25f);
					velocity = Vector2.Reflect(velocityStored, (collider.transform.position - base.transform.position).normalized) * bounciness;
					Vector2 direction = -(velocityStored - component.velocityStored) * 1.2f * num;
					StartPushback(direction, 0.2f, 0.75f);
					playerInput.StartVibration(0.4f, 0.15f);
					if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || HasGoldenDisc == HoldingGoldenDisc.IsHolding)
					{
						Singleton<TimeKeeper>.Instance.SlowMoEffect(0.25f);
					}
				}
				else
				{
					StartPushback(component.velocityStored * num, 0.4f, IsStunned ? 0f : 0.2f);
					playerInput.StartVibration(0.2f, 0.1f);
				}
			}
			else if (collider.CompareTag("MeleeCollider") && !Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				StopAiming();
				velocity = Vector3.zero;
				Actor component2 = collider.transform.parent.GetComponent<Actor>();
				float num2 = Mathf.Max(1f / timeScaler * component2.timeScaler, 1f / component2.timeScaler * timeScaler);
				Player player = component2 as Player;
				if (player != null)
				{
					SetKiller(player, KillerType.Melee, 1.5f);
				}
				ShieldTakesDamage();
				StartPushback(-(collider.transform.position - base.transform.position).normalized * 60f * num2, 0.3f, IsStunned ? 0f : 0.325f);
				playerInput.StartVibration(0.2f, 0.1f);
			}
		}
	}

	public void OnShieldColliderStay(Collider2D collider)
	{
		if (!IsDashing && !base.IsInvulnerable && IsShielded && (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFreeze") || collider.CompareTag("ExplosionFire")))
		{
			velocity = Vector2.zero;
			StartPushback((base.transform.position - collider.transform.position).normalized * 40f * timeScaler, 0.5f, 0.6f);
			ShieldTakesDamage();
			SetKiller(collider.GetComponent<Explosion>().owner, KillerType.Explosion, 1.5f);
		}
	}

	public void ShieldTakesDamage()
	{
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ShieldPop, base.transform.position, character.Color);
		StartCoroutine(TemporaryInvulnerability(0.85f));
		shieldHitsLeft--;
		if (shieldHitsLeft <= 0)
		{
			Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/shield_off", base.gameObject);
			StopShield();
		}
	}

	private IEnumerator ShieldTakesDamageDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		ShieldTakesDamage();
	}

	public override void OnPickupColliderTriggered(Collider2D collider)
	{
		base.OnPickupColliderTriggered(collider);
		if (!collider.CompareTag("Disc"))
		{
			return;
		}
		Disc component = collider.GetComponent<Disc>();
		if (component.DiscOwner == this)
		{
			if ((HasDiscs + pendingDiscs > 1 && !component.IsGoldenDisc) || !component.canCollideWithOwner)
			{
				return;
			}
			if (component.isMiniDisc)
			{
				PickUpMiniDisc(collider);
				return;
			}
			PickUpDisc(collider);
			if (component.movement != Projectile.Movement.Dropped)
			{
				metadata.DiscsCaught++;
			}
		}
		else if (!Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire && Singleton<SettingsManager>.Instance.MatchSettings.canCatchTeammatesDiscs && !Singleton<GameManager>.Instance.AreOpponents(component.DiscOwner, this))
		{
			if (!(component.DiscOwner != this) || !Singleton<SettingsManager>.Instance.MatchSettings.canCatchTeammatesDiscs || Singleton<GameManager>.Instance.AreOpponents(component.DiscOwner, this))
			{
				return;
			}
			if (component.movement != Projectile.Movement.Dropped && component.movement != Projectile.Movement.Squished)
			{
				float num = Mathf.Max(1f / timeScaler * component.timeScaler, 1f / component.timeScaler * timeScaler);
				Vector2 direction = component.velocityStored * num;
				if (direction.magnitude < 1f)
				{
					direction = (base.transform.position - collider.transform.position).normalized * 50f;
				}
				SetKiller(component.owner, KillerType.Disc, 1f);
				StartPushback(direction, 0.2f);
			}
			if (HasDiscs + pendingDiscs <= 1 && !component.isMiniDisc)
			{
				HSBColor hsbColor = HSBColor.FromColor(character.Color);
				hsbColor.b -= 0.2f;
				Color color = HSBColor.ToColor(hsbColor);
				Transform transform = ((HasDiscs + pendingDiscs == 1) ? character.leftDisc.transform : character.rightDisc.transform);
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, transform.position, color, transform);
				PickUpDisc(collider);
				if (component.movement != Projectile.Movement.Dropped)
				{
					metadata.DiscsCaught++;
				}
			}
		}
		else if ((Singleton<SettingsManager>.Instance.MatchSettings.canStealOpponentDiscs || component.DiscOwner == null || (!Singleton<GameManager>.Instance.AreOpponents(component.DiscOwner, this) && Singleton<SettingsManager>.Instance.teamMatch)) && component.movement == Projectile.Movement.Dropped && !component.isMiniDisc)
		{
			PickUpDisc(collider);
			metadata.DiscsStolen++;
		}
	}

	public void OnPickupColliderStay(Collider2D hit)
	{
		if (hit.CompareTag("Powerup"))
		{
			if (actorState != ActorState.Dead)
			{
				Powerup component = hit.GetComponent<Powerup>();
				if (component.powerupState != Powerup.PowerupState.Collected)
				{
					CollectPowerup(component);
				}
			}
		}
		else if (hit.CompareTag("Disc"))
		{
			OnPickupColliderTriggered(hit);
		}
	}

	public void PickUpDisc(Collider2D collider)
	{
		if (actorState == ActorState.Dead || (actorState == ActorState.Falling && fallTimer > fallAffordance))
		{
			return;
		}
		Disc disc = null;
		if (collider != null)
		{
			disc = collider.GetComponent<Disc>();
			JiggleDisguise();
		}
		if (disc != null)
		{
			if (!disc.canBePickedUp)
			{
				return;
			}
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && disc.IsGoldenDisc)
			{
				if (HasGoldenDisc == HoldingGoldenDisc.No || HasGoldenDisc == HoldingGoldenDisc.IsDropped)
				{
					PushBackNearbyPlayersAndDiscs();
				}
				for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
				{
					if (Singleton<GameManager>.Instance.players[i] != this)
					{
						Singleton<GameManager>.Instance.players[i].HasGoldenDisc = HoldingGoldenDisc.No;
					}
				}
				if (disc.DiscOwner != this || HasDiscs > 1)
				{
					HasDiscs = Mathf.Max(0, HasDiscs - 1);
				}
				HasGoldenDisc = HoldingGoldenDisc.IsHolding;
				character.SetFacialExpression(FacialExpression.happy);
			}
			disc.GetPickedUp();
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/catch", base.transform.position);
		}
		isCoolingAttack = true;
		attackCooldownTimer = ((activePowerup.HasPowerup(PowerupType.MoveFaster) || HasDiscs > 0) ? 0.05f : 0.1f);
		if (disc != null && disc.wasJustDeflected && disc.DiscOwner == this)
		{
			float num = Mathf.Max(1f / timeScaler * disc.timeScaler, 1f / disc.timeScaler * timeScaler);
			Vector2 direction = -(velocityStored - disc.velocityStored) * 1.2f * num;
			if (direction.magnitude < 1f)
			{
				direction = (base.transform.position - collider.transform.position).normalized * 50f;
			}
			StartPushback(direction, 0.1f, 0.15f);
		}
		else if (Singleton<SettingsManager>.Instance.MatchSettings.discsHaveKnockback && disc != null && disc.aimPower > 0.15f && disc.DiscOwner == this)
		{
			float num2 = Mathf.Max(1f / timeScaler * disc.timeScaler, 1f / disc.timeScaler * timeScaler);
			Vector2 direction2 = -(velocityStored - disc.velocityStored) * num2;
			if (direction2.magnitude < 1f)
			{
				direction2 = (base.transform.position - collider.transform.position).normalized * 50f;
			}
			StartPushback(direction2, 0.02f);
		}
		HSBColor hsbColor = HSBColor.FromColor(character.Color);
		if (disc == null)
		{
			hsbColor.b -= 0.2f;
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/disc_spawn", base.transform.position);
		}
		else
		{
			hsbColor.b = 0.2f;
		}
		Color color = HSBColor.ToColor(hsbColor);
		Transform transform = ((HasDiscs + pendingDiscs == 1) ? character.leftDisc.transform : character.rightDisc.transform);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, transform.position, color, transform);
		if (HasDiscs < 2)
		{
			HasDiscs++;
			if (pendingDiscs > 0)
			{
				pendingDiscs = Mathf.Min(pendingDiscs, 2 - HasDiscs);
			}
		}
		CheckNumMiniDiscs();
		playerInput.StartVibration(0.7f, 0.1f);
	}

	public void PickUpMiniDisc(Collider2D disc)
	{
		if (disc.GetComponent<Disc>().canBePickedUp)
		{
			if (miniDiscsCollected >= miniDiscsRequired)
			{
				character.SetMiniDiscs(0);
			}
			else if (miniDiscsCollected == miniDiscsRequired - 1)
			{
				miniDiscsCollected = 0;
				PickUpDisc(disc);
				Transform transform = ((HasDiscs == 2) ? character.leftDisc.transform : character.rightDisc.transform);
				HSBColor hsbColor = HSBColor.FromColor(character.Color);
				hsbColor.b -= 0.2f;
				Color color = HSBColor.ToColor(hsbColor);
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, transform.position, color, transform);
			}
			else if (miniDiscsCollected < miniDiscsRequired - 1)
			{
				character.SetMiniDiscs(_hasDiscs + 1);
				miniDiscsCollected++;
				disc.GetComponent<Disc>().GetPickedUp();
				playerInput.StartVibration(0.1f, 0.1f);
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/catch", base.transform.position);
			}
		}
	}

	private void CheckNumMiniDiscs()
	{
		if (miniDiscsCollected >= miniDiscsRequired || miniDiscsCollected == 0)
		{
			character.SetMiniDiscs(0);
		}
		else
		{
			character.SetMiniDiscs(_hasDiscs + 1);
		}
	}

	public void SwapDiscsWithOtherPlayer(Player otherPlayer)
	{
		if (Singleton<SettingsManager>.Instance.MatchSettings.canSwapDiscs && Singleton<SettingsManager>.Instance.teamMatch && !Singleton<GameManager>.Instance.AreOpponents(otherPlayer, this) && !HasJustSwappedDiscs && !otherPlayer.HasJustSwappedDiscs && !otherPlayer.IsInvulnerable && !base.IsInvulnerable)
		{
			if ((otherPlayer.actorState == ActorState.Alive && otherPlayer.HasDiscs > HasDiscs && otherPlayer.HasDiscs > 1) || (otherPlayer.actorState == ActorState.Dead && HasDiscs + pendingDiscs < 2 && Singleton<SettingsManager>.Instance.MatchSettings.teamRevives && otherPlayer.HasDiscs == 2))
			{
				HasJustSwappedDiscs = true;
				otherPlayer.HasJustSwappedDiscs = true;
				PickUpDisc(null);
				otherPlayer.HasDiscs--;
				otherPlayer.playerInput.StartVibration(0.7f, 0.1f);
				Debug.Log(character.name + " stole " + otherPlayer.character.name + "'s disc");
			}
			else if (otherPlayer.HasDiscs < HasDiscs && HasDiscs > 1 && otherPlayer.actorState == ActorState.Alive)
			{
				Debug.Log(character.name + " donated disc to " + otherPlayer.character.name);
				HasJustSwappedDiscs = true;
				otherPlayer.HasJustSwappedDiscs = true;
				otherPlayer.PickUpDisc(null);
				HasDiscs--;
				playerInput.StartVibration(0.7f, 0.1f);
			}
		}
	}

	private IEnumerator JustSwappedDiscsTimer(float duration = 0.5f)
	{
		yield return new WaitForSeconds(duration);
		HasJustSwappedDiscs = false;
	}

	public void EmitSkullParticle()
	{
		if (!(Singleton<TimeKeeper>.Instance.TimeSince(timeSinceLastSkullPFX) < skullPFXDelay) && skullPFX != null)
		{
			skullPFX.Clear();
			skullPFX.transform.position = shadow.transform.position;
			ParticleSystem.MainModule main = skullPFX.main;
			main.startColor = character.Color;
			skullPFX.Emit(1);
			timeSinceLastSkullPFX = Singleton<TimeKeeper>.Instance.CurrentTime;
		}
	}

	private void PushBackNearbyPlayers()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (Singleton<GameManager>.Instance.players[i] != this)
			{
				Vector2 vector = Singleton<GameManager>.Instance.players[i].transform.position - base.transform.position;
				if (vector.magnitude < goldenDiscPushbackRadius && !Physics2D.Raycast(base.transform.position, vector.normalized, vector.magnitude, obstacleLayer))
				{
					Singleton<GameManager>.Instance.players[i].StartPushback(vector.normalized * Mathf.Clamp01((goldenDiscPushbackRadius - vector.magnitude * 0.5f) / goldenDiscPushbackRadius) * 25f, 0.2f);
					Singleton<GameManager>.Instance.players[i].SetKiller(this, KillerType.Push, 2f);
				}
			}
		}
	}

	private void PushBackNearbyPlayersAndDiscs()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.activeDiscs.Count; i++)
		{
			Vector2 vector = Singleton<GameManager>.Instance.activeDiscs[i].transform.position - base.transform.position;
			if (vector.magnitude < goldenDiscPushbackRadius && !Physics2D.Raycast(base.transform.position, vector.normalized, vector.magnitude, obstacleLayer))
			{
				Singleton<GameManager>.Instance.activeDiscs[i].velocity += vector.normalized * 80f;
			}
		}
		for (int j = 0; j < Singleton<GameManager>.Instance.players.Count; j++)
		{
			if (Singleton<GameManager>.Instance.players[j] != this)
			{
				Vector2 vector2 = Singleton<GameManager>.Instance.players[j].transform.position - base.transform.position;
				if (vector2.magnitude < goldenDiscPushbackRadius && !Physics2D.Raycast(base.transform.position, vector2.normalized, vector2.magnitude, obstacleLayer))
				{
					Singleton<GameManager>.Instance.players[j].StartPushback(vector2.normalized * Mathf.Clamp01((goldenDiscPushbackRadius - vector2.magnitude * 0.5f) / goldenDiscPushbackRadius) * 100f, 0.4f, 0.4f);
					Singleton<GameManager>.Instance.players[j].SetKiller(this, (HasGoldenDisc == HoldingGoldenDisc.IsHolding) ? KillerType.GoldenDisc : KillerType.Push, 1.5f);
				}
			}
		}
	}

	public void AddReviveTime()
	{
		if (reviveTimer == 0f && actorState == ActorState.Dead)
		{
			reviveTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: true);
			revivingPFX.Play();
			reviveSFXInstance = Singleton<AudioManager>.Instance.PlayLooping("event:/combat/revive_charge", base.transform);
		}
		reviveTimer += Time.fixedDeltaTime;
		gettingRevivedThisFrame = true;
		playerInput.StartVibration(reviveTimer / Actor.reviveDuration, 0.1f);
	}

	private void Revive()
	{
		playerInput.StartVibration(1f, 0.5f);
		base.transform.position = deathCollider.transform.position;
		Init(Mathf.Max(HasDiscs, 1), resetPowerups: false, respawn: true, spawnNewDisc: false);
		if ((bool)GetComponent<PlayerAI>())
		{
			GetComponent<PlayerAI>().Init();
		}
		reviveTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/revive", base.transform.position);
		ParticleSystem.ShapeModule shape = revivingPFX.shape;
		shape.angle = 30f;
		revivingPFX.Emit(15);
		shape.angle = 0f;
		revivingPFX.Stop();
		if (reviveSFXInstance.isValid())
		{
			reviveSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
			reviveSFXInstance.release();
		}
		PushBackNearbyPlayers();
		Singleton<Camera3D>.Instance.AddCameraTarget(base.transform, 1f, 0f);
	}

	public void Wave(bool forceWave = false)
	{
		if (forceWave || (actorState == ActorState.Alive && !IsStunned && !IsFrozen && !isShowingOff && !IsDashing && !isLunging && canWalk && !isSpawningIn && !Singleton<GameManager>.Instance.IsInRevealPhase && !IsHider))
		{
			if (Vector2.Dot(lastInputMove, new Vector2(0f, -1f)) < 0.2f)
			{
				showOffDirection = new Vector2(0f, -1f);
			}
			else
			{
				showOffDirection = lastInputMove;
			}
			if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 1 || !AttemptHeroPose())
			{
				character.anim.SetFloat("MoveSpeed", 0f);
				character.anim.ResetTrigger("Wave");
				character.anim.SetTrigger("Wave");
				showOffTimer = 0f;
				showOffDuration = 0.5f;
				isShowingOff = true;
				character.SetFacialExpression(FacialExpression.happy, 1.3f);
			}
		}
	}

	public bool AttemptHeroPose()
	{
		if (actorState != ActorState.Alive || inputMove.magnitude > 0.4f || IsStunned || IsFrozen || isShowingOff || IsDashing || !Singleton<LevelManager>.Instance.IsInBounds(base.transform.position) || IsPositionOverHazard(base.transform.position) || Singleton<LevelManager>.Instance.level.isFallingApart)
		{
			return false;
		}
		IsAiming = false;
		showOffDirection = new Vector2(0f, -1f);
		character.anim.SetFloat("MoveSpeed", 0f);
		character.anim.ResetTrigger("Hero");
		character.anim.SetTrigger("Hero");
		isShowingOff = true;
		showOffTimer = 0f;
		showOffDuration = 2f;
		return true;
	}

	private void CollectPowerup(Powerup powerup)
	{
		if (alertBar != null && alertBar.currentAlert != AlertBar.AlertType.None)
		{
			alertBar.HideAlert();
		}
		PowerupType collected = powerup.GetCollected(this);
		Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/collect_power", base.gameObject);
		StartPowerup(collected);
		playerInput.StartVibration(0.5f, 0.25f);
		metadata.PowerupsCollected++;
	}

	public void StartPowerup(PowerupType newPowerup)
	{
		playerInput.StartVibration(0.2f, 0.1f);
		bool flag = activePowerup.HasPowerup(newPowerup);
		switch (newPowerup)
		{
		case PowerupType.MoveFaster:
		case PowerupType.DashThroughWalls:
		case PowerupType.ExplosiveDisc:
		case PowerupType.MultiDisc:
		case PowerupType.TeleportDisc:
		case PowerupType.Disguise:
		case PowerupType.ControlledDisc:
			if (!flag)
			{
				activePowerup |= newPowerup;
				powerupHistory.Add(newPowerup);
			}
			break;
		case PowerupType.Decoy:
			if (!flag)
			{
				activePowerup |= newPowerup;
				powerupHistory.Add(newPowerup);
			}
			if (!Singleton<GameManager>.Instance.decoysInMatch)
			{
				Singleton<GameManager>.Instance.decoysInMatch = true;
			}
			playerInput.ConsiderShowingDecoyHint();
			break;
		case PowerupType.FireDisc:
			if (!flag)
			{
				activePowerup |= newPowerup;
				powerupHistory.Add(newPowerup);
				if (activePowerup.HasPowerup(PowerupType.SlimeDisc))
				{
					powerupHistory.Remove(PowerupType.SlimeDisc);
					activePowerup &= ~PowerupType.SlimeDisc;
				}
			}
			break;
		case PowerupType.SlimeDisc:
			if (!flag)
			{
				activePowerup |= newPowerup;
				if (activePowerup.HasPowerup(PowerupType.FireDisc))
				{
					powerupHistory.Remove(PowerupType.FireDisc);
					activePowerup &= ~PowerupType.FireDisc;
				}
				powerupHistory.Add(newPowerup);
			}
			break;
		case PowerupType.ReverseInputs:
			StartReverseInputsTimer(6f);
			break;
		case PowerupType.Shield:
			if (!flag)
			{
				powerupHistory.Add(newPowerup);
				StartShield();
			}
			break;
		case PowerupType.ExtraDisc:
			if (pendingDiscs + HasDiscs < 2)
			{
				PickUpDisc(null);
			}
			if (!flag)
			{
				activePowerup |= newPowerup;
				powerupHistory.Add(newPowerup);
			}
			break;
		case PowerupType.TimeImmune:
			activePowerup |= newPowerup;
			timeImmuneTimer = 1.5f;
			timeImmunePFX.Play();
			Singleton<TimeKeeper>.Instance.SlowMoTimeWarp(6.3f, 0.2f, 1f, 0.4f);
			break;
		case PowerupType.ShrinkingBounds:
			Singleton<LevelManager>.Instance.StartShrinkingLevelBoundary(base.transform.position, showMarker: true, this);
			break;
		}
		if (!flag && powerupHistory.Count > maxActivePowerups && powerupHistory.Count > 0)
		{
			activePowerup &= ~powerupHistory[0];
			powerupHistory.RemoveAt(0);
		}
	}

	private void StopTemporaryPowerups()
	{
		activePowerup &= ~PowerupType.ReverseInputs;
		activePowerup &= ~PowerupType.TimeImmune;
		powerupTimer = 0f;
		if (IsSlimed || IsFrozen)
		{
			slimeTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
		}
		goldenTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
		BreakDisguise();
	}

	public void ClearPowerups()
	{
		StopTemporaryPowerups();
		activePowerup = PowerupType.None;
		powerupHistory.Clear();
	}

	private void StartReverseInputsTimer(float duration)
	{
		activePowerup |= PowerupType.ReverseInputs;
		bamboozledTimeUI.enabled = true;
		if (powerupTimer > 0f)
		{
			bamboozledTimeUI.GetComponent<Animator>().SetTrigger("ResetTimer");
		}
		else
		{
			bamboozledTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: true);
		}
		powerupTimeLimit = duration;
		powerupTimer = duration;
		isPowerupTimerOn = true;
	}

	private void RunReverseInputsTimer()
	{
		if (isPowerupTimerOn)
		{
			if (powerupTimer > 0f)
			{
				bamboozledTimeUIClock.fillAmount = powerupTimer / powerupTimeLimit;
				powerupTimer -= Time.deltaTime * timeScaler;
				return;
			}
			bamboozledTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
			bamboozledTimeUIClock.fillAmount = 0f;
			powerupTimer = 0f;
			isPowerupTimerOn = false;
			activePowerup &= ~PowerupType.ReverseInputs;
		}
		else
		{
			bamboozledTimeUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
			bamboozledTimeUIClock.fillAmount = 0f;
			powerupTimer = 0f;
		}
	}

	private void StartShieldHitCounter(int count)
	{
		shieldHitsLeft = count;
	}

	public void StartShield(bool goldenShield = false)
	{
		activePowerup |= PowerupType.Shield;
		StartShieldHitCounter(1);
		IsShielded = true;
		Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/shield_on", base.gameObject);
		if (isOnFire)
		{
			ToggleFire(onFire: false, extinguishPFX: true);
			metadata.FiresExtinguished++;
		}
	}

	private void StopShield()
	{
		activePowerup &= ~PowerupType.Shield;
		if (powerupHistory.Contains(PowerupType.Shield))
		{
			powerupHistory.Remove(PowerupType.Shield);
		}
		shieldHitsLeft = 0;
		IsShielded = false;
		if (actorState != ActorState.Dead)
		{
			damageCollider.enabled = true;
		}
	}

	protected override void ToggleFire(bool onFire, bool extinguishPFX = false)
	{
		if (isOnFire && !onFire)
		{
			playerInput.StopVibration();
			extinguishAmount = 0f;
		}
		if (!isOnFire & onFire)
		{
			character.SetFacialExpression(FacialExpression.panic);
			isShowingOff = false;
			BreakDisguise();
			extinguishAmount = 0f;
		}
		base.ToggleFire(onFire, extinguishPFX);
	}

	public void RespawnDisc(float waitTime, int numDiscs = 1)
	{
		if (actorState != ActorState.Dead)
		{
			respawnDiscRoutine = RespawnDiscRoutine(waitTime, numDiscs);
			StartCoroutine(respawnDiscRoutine);
		}
	}

	private IEnumerator RespawnDiscRoutine(float waitTime, int numDiscs = 1)
	{
		if (HasDiscs + pendingDiscs >= 2)
		{
			yield break;
		}
		for (int i = 0; i < numDiscs; i++)
		{
			if (HasDiscs + pendingDiscs < 2)
			{
				pendingDiscs++;
			}
		}
		yield return new WaitForSeconds(waitTime);
		if (actorState == ActorState.Dead || (actorState == ActorState.Falling && fallTimer > fallAffordance))
		{
			yield break;
		}
		for (int j = 0; j < numDiscs; j++)
		{
			if (pendingDiscs > 0 && HasDiscs < 2)
			{
				pendingDiscs--;
				PickUpDisc(null);
			}
		}
	}

	protected override IEnumerator TemporaryInvulnerability(float duration)
	{
		base.IsInvulnerable = true;
		float t = 0f;
		while (t < duration)
		{
			if (actorState != ActorState.Alive)
			{
				character.IsVisible = true;
				character.SetMeleeDiscs(HasDiscs);
				yield break;
			}
			if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Paused)
			{
				t += Time.unscaledDeltaTime;
			}
			if (Mathf.Round(t * 40f) % 2f == 1f)
			{
				character.IsVisible = true;
			}
			else
			{
				character.IsVisible = false;
			}
			BreakDisguise();
			yield return null;
		}
		character.IsVisible = true;
		character.SetMeleeDiscs(HasDiscs);
		base.IsInvulnerable = false;
		justBecomeVulernable = true;
	}

	public void Illuminate(float duration = 0.3f)
	{
	}

	private IEnumerator TemporaryIllumination(float duration = 0.16f)
	{
		if (!(Singleton<LevelManager>.Instance.level == null) && Singleton<LevelManager>.Instance.level.isDarkLevel)
		{
			if (actorState == ActorState.Alive)
			{
				illuminatePFX.Play();
			}
			yield return new WaitForSeconds(0.0333f * timeScaler);
			yield return new WaitForSeconds(duration * timeScaler);
			if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() <= 1)
			{
				_ = Singleton<SettingsManager>.Instance.matchType;
			}
		}
	}

	protected override void Freeze()
	{
		base.Freeze();
		if (isOnFire)
		{
			metadata.FiresExtinguished++;
		}
		if (IsShielded)
		{
			Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/shield_off", base.gameObject);
			StopShield();
		}
		character.SetFacialExpression(FacialExpression.retrieving);
	}

	protected override void Unfreeze()
	{
		base.Unfreeze();
	}

	private void BreakDisguise()
	{
		IsDisguised = false;
		disguiseTimer = 0f;
		timeInCurrentDisguise = 0f;
	}

	private void SwitchDisguise()
	{
		if (!IsDisguised)
		{
			return;
		}
		timeInCurrentDisguise = 0f;
		int num = 0;
		for (int i = 0; i < disguises.Length; i++)
		{
			if (disguises[i].gameObject.activeSelf)
			{
				num = (i + 1) % disguises.Length;
				break;
			}
		}
		for (int j = 0; j < disguises.Length; j++)
		{
			if (Singleton<LevelManager>.Instance.level.availableDisguiseTypes.Contains(disguises[num].type))
			{
				break;
			}
			num = (num + 1) % disguises.Length;
		}
		TurnOnDisguise(num);
		if (disguiseAnimator.isActiveAndEnabled)
		{
			disguiseAnimator.ResetTrigger("Off");
			disguiseAnimator.SetTrigger("On");
		}
	}

	public void RevealHider()
	{
		if (isWading)
		{
			NotifyPlantsOfDeath();
		}
		BreakDisguise();
		Wave();
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/decoy_die", base.transform.position);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/spawn_golden_boomerang", base.transform.position);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DecoyPuff, base.transform.position, character.Color);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.RevealHider, base.transform.position, character.Color);
		playerInput.StartVibration(0.3f, 0.1f);
		HideSeekerPFX();
	}

	private void JiggleDisguise()
	{
		if (IsDisguised)
		{
			disguiseAnimator.SetTrigger("Jiggle");
		}
	}

	private void CreateDecoy()
	{
		Singleton<GameManager>.Instance.CreateDecoy(this);
		hasCreatedDecoy = true;
		metadata.DecoysSpawned++;
	}

	private void RunGoldenDiscTimer()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		if (HasGoldenDisc == HoldingGoldenDisc.IsHolding && actorState == ActorState.Alive)
		{
			if (goldenHoldTime < Singleton<GameManager>.Instance.goldenGoalScore)
			{
				if (isOutOfBounds)
				{
					goldenHoldTime += Time.deltaTime * 0.333f;
				}
				else
				{
					goldenHoldTime += Time.deltaTime;
				}
			}
			metadata.TimeHoldingGoldenDisc += Time.deltaTime;
		}
		else if ((HasGoldenDisc != HoldingGoldenDisc.IsThrown || actorState != ActorState.Alive) && ((HasGoldenDisc != HoldingGoldenDisc.IsDropped && HasGoldenDisc != HoldingGoldenDisc.No) || !(goldenHoldTime > 0f)) && HasGoldenDisc == HoldingGoldenDisc.IsDropped && goldenHoldTime < 0f)
		{
			HasGoldenDisc = HoldingGoldenDisc.No;
		}
		if (goldenDiscAudioLoop.isValid())
		{
			if (goldenHoldTime < Singleton<GameManager>.Instance.goldenGoalScore)
			{
				Singleton<AudioManager>.Instance.SetEventInstanceParameter(goldenDiscAudioLoop, "retrieveCharge", goldenHoldTime / Singleton<GameManager>.Instance.goldenGoalScore);
			}
			else
			{
				Singleton<AudioManager>.Instance.StopLooping(goldenDiscAudioLoop);
			}
		}
		if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Survival)
		{
			Singleton<GameManager>.Instance.UpdateGoldenDiscScore(this, goldenHoldTime, HasGoldenDisc == HoldingGoldenDisc.IsHolding);
			goldenTimeUIClock.fillAmount = goldenHoldTime / Singleton<GameManager>.Instance.goldenGoalScore;
		}
		else if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills)
		{
			Singleton<GameManager>.Instance.UpdateGoldenDiscScore(this, goldenDiscKills, HasGoldenDisc != HoldingGoldenDisc.No);
			goldenTimeUIClock.fillAmount = (float)goldenDiscKills / Singleton<GameManager>.Instance.goldenGoalScore;
		}
	}

	public void WinMatch()
	{
		base.IsInvulnerable = true;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			HasGoldenDisc = HoldingGoldenDisc.No;
			goldenWinPFX.SetActive(value: true);
		}
		playerInput.StartVibration(1f, 0.5f);
		character.SetFacialExpression(FacialExpression.happy);
		NotifyPlantsOfDeath();
	}

	public override void Die(DeathType deathType)
	{
		if (actorState == ActorState.Dead)
		{
			if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
			{
				Debug.Log("Golden disc player just got killed, but is already dead, so we are not dropping the golden disc");
			}
		}
		else
		{
			if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing && Singleton<GameManager>.Instance.gameState != GameManager.GameState.TransitionIn)
			{
				return;
			}
			if (!IsFrozen)
			{
				switch (deathType)
				{
				case DeathType.disc:
				case DeathType.melee:
					StartCoroutine(EnableDeathColliderAfterDelay());
					deathCollider.transform.localPosition = new Vector3(0f, -1f, 0f);
					break;
				case DeathType.fire:
					StartCoroutine(EnableDeathColliderAfterDelay());
					deathCollider.transform.localPosition = new Vector3(0f, 1.25f, 0f);
					break;
				case DeathType.squished:
				case DeathType.explosion:
				case DeathType.telefrag:
					StartCoroutine(EnableDeathColliderAfterDelay(0.5f));
					deathCollider.transform.localPosition = Vector3.zero;
					break;
				}
			}
			base.Die(deathType);
			StopMeleeTrail();
			playerInput.StartVibration(1f, 0.5f);
			bool flag = HasGoldenDisc == HoldingGoldenDisc.IsHolding || HasGoldenDisc == HoldingGoldenDisc.IsThrown;
			if (killer != null && killer is Player)
			{
				if (killer == this && Singleton<LevelManager>.Instance.levelBoundaryOwner != null)
				{
					killer = Singleton<LevelManager>.Instance.levelBoundaryOwner;
				}
				Player player = killer as Player;
				if (player != this)
				{
					Singleton<GameManager>.Instance.AddPlayerKill(player, Singleton<GameManager>.Instance.AreOpponents(player, this));
					player.metadata.TotalKills++;
					player.killsThisRound++;
				}
				else if (deathType == DeathType.fall)
				{
					Singleton<GameManager>.Instance.AddPlayerDeath(this);
				}
				else if (hasCompletedTutorial || !Singleton<LevelManager>.Instance.level.isTutorialLevel)
				{
					Singleton<GameManager>.Instance.AddPlayerSuicide(this);
				}
				if (killerType == KillerType.Melee && player.HasGoldenDisc == HoldingGoldenDisc.IsHolding)
				{
					killerType = KillerType.GoldenDisc;
				}
				if (killerType == KillerType.GoldenDisc)
				{
					player.goldenDiscKills++;
				}
				if (!flag && player != null && (player.HasGoldenDisc == HoldingGoldenDisc.IsHolding || player.HasGoldenDisc == HoldingGoldenDisc.IsThrown))
				{
					flag = true;
				}
			}
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
			{
				Singleton<GameManager>.Instance.RespawnPlayer(this, (HasGoldenDisc == HoldingGoldenDisc.IsHolding || HasGoldenDisc == HoldingGoldenDisc.IsThrown) ? 1f : 0.5f);
			}
			else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
			{
				Singleton<GameManager>.Instance.RespawnPlayer(this, 0.4f);
			}
			else if (!hasCompletedTutorial && Singleton<LevelManager>.Instance.level.isTutorialLevel)
			{
				Singleton<GameManager>.Instance.RespawnPlayer(this);
				playerInput.StartCoroutine(playerInput.ShowDashHint(1.5f));
			}
			else if (Singleton<GameManager>.Instance.IsInHidingPhase || (IsSeeker && !Singleton<GameManager>.Instance.IsInRevealPhase))
			{
				Singleton<GameManager>.Instance.RespawnPlayer(this);
			}
			if (Singleton<SettingsManager>.Instance.MatchSettings.deadDiscsArePublic || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
			{
				for (int i = 0; i < thrownDiscs.Count; i++)
				{
					thrownDiscs[i].OwnerJustDied();
				}
			}
			if (HasGoldenDisc == HoldingGoldenDisc.IsHolding)
			{
				switch (deathType)
				{
				case DeathType.disc:
				case DeathType.melee:
				case DeathType.explosion:
				case DeathType.teamkill:
				case DeathType.telefrag:
				case DeathType.fire:
					DropDisc(velocityStored.normalized * 0.1f, dropAllDiscs: false);
					break;
				case DeathType.squished:
				case DeathType.fall:
				case DeathType.outofbounds:
					Singleton<GameManager>.Instance.SpawnGoldenDisc();
					break;
				}
				HasGoldenDisc = HoldingGoldenDisc.No;
			}
			if (respawnDiscRoutine != null)
			{
				StopCoroutine(respawnDiscRoutine);
				pendingDiscs = 0;
			}
			if (activePowerup.HasPowerup(PowerupType.TimeImmune) && Singleton<TimeKeeper>.Instance.slowMo)
			{
				bool flag2 = true;
				for (int j = 0; j < Singleton<GameManager>.Instance.players.Count; j++)
				{
					if (!(Singleton<GameManager>.Instance.players[j] == this) && Singleton<GameManager>.Instance.players[j].GetComponent<Player>().activePowerup.HasPowerup(PowerupType.TimeImmune))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					Singleton<TimeKeeper>.Instance.ResetTimeScale();
				}
			}
			if ((!Singleton<LevelManager>.Instance.level.isTutorialLevel || hasCompletedTutorial) && ((Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc) | flag))
			{
				Singleton<TimeKeeper>.Instance.SlowMoEffect(0.5f, 0.5f);
			}
			StopTemporaryPowerups();
			if (IsShielded)
			{
				Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/shield_off", base.gameObject);
			}
			StopShield();
			if (hasCreatedDecoy)
			{
				Singleton<GameManager>.Instance.KillDecoy(this);
			}
			switch (deathType)
			{
			case DeathType.disc:
			case DeathType.melee:
				character.facialExpressionTimer = 0.2f;
				ToggleFire(onFire: false);
				break;
			case DeathType.fire:
				character.facialExpressionTimer = 0.2f;
				character.SetFacialExpression(FacialExpression.dead);
				ToggleFire(onFire: false);
				break;
			case DeathType.squished:
			case DeathType.explosion:
			case DeathType.telefrag:
				ToggleFire(onFire: false);
				character.SetFacialExpression(FacialExpression.dead);
				character.IsVisible = true;
				break;
			default:
				ToggleFire(onFire: false, extinguishPFX: true);
				physicsCollider.enabled = false;
				character.IsVisible = false;
				break;
			}
			if (killer != this && killer is Player && !Singleton<GameManager>.Instance.AreOpponents(killer as Player, this) && Singleton<SettingsManager>.Instance.teamMatch)
			{
				deathType = DeathType.teamkill;
			}
			if (killer != null)
			{
				if (deathType == DeathType.squished)
				{
					metadata.SquishDeaths++;
				}
				Player player2 = killer as Player;
				if (player2 == null || player2 != this)
				{
					metadata.Deaths++;
				}
				if (player2 != null)
				{
					switch (deathType)
					{
					case DeathType.disc:
						player2.metadata.ThrowKills++;
						player2.RecordKill();
						break;
					case DeathType.explosion:
						if (player2 == this)
						{
							metadata.ExplosiveSuicides++;
							break;
						}
						player2.metadata.ExplosiveKills++;
						player2.RecordKill();
						break;
					case DeathType.fall:
						if (killer == this)
						{
							metadata.FallSuicides++;
							break;
						}
						if (killerType == KillerType.Push)
						{
							player2.metadata.PushKills++;
							player2.RecordKill();
						}
						if (player2.IsDisarmed)
						{
							player2.metadata.KillsWhileDisarmed++;
						}
						break;
					case DeathType.teamkill:
						metadata.TeamDeaths++;
						player2.metadata.TeamKills++;
						break;
					case DeathType.melee:
						player2.metadata.MeleeKills++;
						break;
					case DeathType.outofbounds:
						if (killer == this)
						{
							if (Singleton<LevelManager>.Instance.levelBoundaryOwner == null)
							{
								metadata.OutOfBoundsSuicides++;
							}
						}
						else
						{
							if (!IsFrozen)
							{
								player2.metadata.PushKills++;
							}
							if (player2.IsDisarmed)
							{
								player2.metadata.KillsWhileDisarmed++;
							}
							player2.RecordKill();
						}
						character.anim.SetFloat("MoveSpeed", 0f);
						break;
					case DeathType.squished:
						if (killer != this)
						{
							player2.metadata.SquishKills++;
							player2.RecordKill();
						}
						break;
					case DeathType.telefrag:
						metadata.TeleportDeaths++;
						player2.metadata.TeleportKills++;
						player2.RecordKill();
						break;
					case DeathType.fire:
						if (fireOwner != null && fireOwner is Player && fireOwner as Player != this)
						{
							player2.metadata.FireKills++;
						}
						break;
					}
					if (IsFrozen)
					{
						player2.metadata.FrozenKills++;
						Unfreeze();
					}
					if (killer != this)
					{
						if (deathType == DeathType.disc && player2.metadata.LongestKillDistance < (player2.transform.position - base.transform.position).magnitude)
						{
							player2.metadata.LongestKillDistance = (killer.transform.position - base.transform.position).magnitude;
						}
						if (player2.actorState == ActorState.Dead)
						{
							player2.metadata.KillsAfterDeath++;
						}
						if (player2.activePowerup.HasPowerup(PowerupType.ReverseInputs))
						{
							player2.metadata.KillsWhileBamboozled++;
						}
						if (activePowerup.HasPowerup(PowerupType.TimeImmune))
						{
							player2.metadata.TimeImmuneKills++;
						}
						if (player2.activePowerup.HasPowerup(PowerupType.TimeImmune))
						{
							player2.metadata.KillsWhileTimeImmune++;
						}
					}
				}
			}
			timeSinceLastSkullPFX = Singleton<TimeKeeper>.Instance.CurrentTime;
			if (deathType == DeathType.disc || deathType == DeathType.melee || deathType == DeathType.explosion || deathType == DeathType.teamkill)
			{
				Singleton<TimeKeeper>.Instance.FreezeFrame(0.1f);
			}
			if (!Singleton<LevelManager>.Instance.level.isTutorialLevel || hasCompletedTutorial)
			{
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc || (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 1))
				{
					Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
				}
				else if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() == 1)
				{
					Singleton<Camera3D>.Instance.ZoomInOnKill(base.transform);
				}
			}
			if (!Singleton<SettingsManager>.Instance.GameOptions.recordReplays && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() == 1)
			{
				Singleton<UIManager>.Instance.SaveScreenshotToReplayScreen();
			}
		}
	}

	protected override void RunKillerTimer()
	{
		if (killer != this)
		{
			if (killerTimer > 0f)
			{
				killerTimer -= Time.deltaTime * timeScaler;
				return;
			}
			SetKiller(this);
			killerTimer = 0f;
		}
	}

	private IEnumerator EnableDeathColliderAfterDelay(float delay = 1f)
	{
		yield return new WaitForSeconds(delay);
		deathCollider.enabled = true;
	}

	public override void DisableColliders()
	{
		base.DisableColliders();
		pickupCollider.enabled = false;
		IsDisarming = false;
		shieldCollider.enabled = false;
	}

	public override void DisableActor()
	{
		base.DisableActor();
		deathCollider.enabled = false;
	}

	public override void StartFall()
	{
		base.StartFall();
		StopAiming();
		character.SetFacialExpression(FacialExpression.panic);
		character.IsVisible = true;
	}

	protected override void FallSaved()
	{
		base.FallSaved();
		character.SetFacialExpression(FacialExpression.idle);
	}

	private void SetAppearanceColors(Color targetColor)
	{
		Renderer component = shield.GetComponent<Renderer>();
		HSBColor hsbColor = HSBColor.FromColor(targetColor);
		hsbColor.b -= 0.4f;
		hsbColor.s += 0.2f;
		component.material.SetColor("_RimColor", HSBColor.ToColor(hsbColor));
		hsbColor.b = 0.1f;
		component.material.SetColor("_InnerColor", HSBColor.ToColor(hsbColor));
		(character as CharacterPlayer).SetTeam(team);
		ParticleSystem.MainModule main = dashTrail.main;
		main.startColor = targetColor;
		main = illuminatePFX.main;
		main.startColor = targetColor;
		main = retrievePFX.GetComponent<ParticleSystem>().main;
		main.startColor = targetColor;
		hsbColor = HSBColor.FromColor(targetColor);
		hsbColor.b = 1f;
		main = moveFasterPFX.main;
		main.startColor = HSBColor.ToColor(hsbColor);
		main = seekerPFX.main;
		main.startColor = HSBColor.ToColor(hsbColor);
		ToggleGoldenDiscPFX();
	}

	private void ToggleGoldenDiscPFX()
	{
		character.IsHoldingGoldenDisc = HasGoldenDisc == HoldingGoldenDisc.IsHolding;
		Color color = ((HasGoldenDisc == HoldingGoldenDisc.IsHolding) ? Singleton<GameManager>.Instance.goldenDiscColor : character.Color);
		ParticleSystem.MainModule main = meleeTrailRight.main;
		main.startColor = color - new Color(0f, 0f, 0f, 0.2f);
		main = meleeTrailLeft.main;
		main.startColor = color - new Color(0f, 0f, 0f, 0.2f);
		main = controlledPFX.GetComponent<ParticleSystem>().main;
		main.startColor = color;
		main = retrievePFX.GetComponent<ParticleSystem>().main;
		main.startColor = color;
	}

	private void ResetOutOfBoundsEffect()
	{
	}

	private void StartOutOfBoundsEffect()
	{
		outOfBoundsArrow.Enable();
	}

	private void StopOutOfBoundsEffect()
	{
		outOfBoundsArrow.Disable();
		character.DangerEffect = 0f;
	}

	private void InitLeaderIcon()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && Singleton<SettingsManager>.Instance.numBots + Singleton<SettingsManager>.Instance.numPlayers > 2)
		{
			isInTheLead = Singleton<GameManager>.Instance.IsLeadingPlayer(this);
		}
		else
		{
			isInTheLead = false;
		}
		UpdateLeaderIcon();
	}

	private void UpdateLeaderIcon()
	{
		if (isInTheLead)
		{
			_ = actorState;
		}
	}

	public void DropInAI()
	{
		if (!(playerInput.rewiredController == null))
		{
			UnityEngine.Object.Destroy(playerInput.rewiredController);
			PlayerAI playerAI = base.gameObject.AddComponent<PlayerAI>();
			playerAI.InitPathSettings();
			playerAI.Init();
			Singleton<SettingsManager>.Instance.numPlayers--;
			Singleton<SettingsManager>.Instance.numBots++;
			if (Singleton<SettingsManager>.Instance.numBots == 1)
			{
				Singleton<LevelManager>.Instance.LoadPathfindingGraph();
			}
			Wave();
			playerType = PlayerType.Bot;
			if (Singleton<SettingsManager>.Instance.GameOptions.xinput)
			{
				alertBar.ShowAlert(AlertBar.AlertType.Disconnected, 2f, 0f);
			}
		}
	}

	public void DropInPlayer()
	{
		if (actorState == ActorState.Alive)
		{
			if (Singleton<SettingsManager>.Instance.GameOptions.conventionMode)
			{
				UIMenuInstructions uIMenuInstructions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Instructions) as UIMenuInstructions;
				if (uIMenuInstructions != null)
				{
					Singleton<UIManager>.Instance.PushMenu(uIMenuInstructions);
					uIMenuInstructions.ShowInstructionPanels(this);
				}
			}
			PushBackNearbyPlayersAndDiscs();
			Wave();
			StartCoroutine(TemporaryInvulnerability(1.5f));
		}
		playerType = PlayerType.Human;
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.GroundSlam, base.transform.position, character.Color, base.transform);
		playerInput.StartVibration(0.5f, 0.25f);
		Singleton<TimeKeeper>.Instance.SlowMoEffect(0.25f);
		Singleton<CameraFX>.Instance.UpdateVignette(0.75f);
		if (Singleton<SettingsManager>.Instance.GameOptions.xinput)
		{
			alertBar.ShowAlert(AlertBar.AlertType.Joined, 3f, Singleton<SettingsManager>.Instance.GameOptions.conventionMode ? 0.5f : 0f);
		}
	}

	protected override void StopAllSoundLoops()
	{
		base.StopAllSoundLoops();
		if (reviveSFXInstance.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(reviveSFXInstance);
		}
		if (goldenDiscAudioLoop.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(goldenDiscAudioLoop);
		}
	}
}
