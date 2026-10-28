using System.Collections;
using FMOD.Studio;
using UnityEngine;

public class Disc : Projectile
{
	public GameObject discPrefab;

	public PowerupType discPowerup;

	private Rigidbody2D discBody;

	public ParticleSystem discTrail;

	public ParticleSystem pfxDiscSpin;

	public Animator discAnimator;

	public Renderer discRenderer;

	public Color discColorPublic = new Color(0.7f, 0.7f, 0.7f);

	private Color discColorPublicPFX = new Color(0.1f, 0.1f, 0.1f);

	public float retrievePFXDefaultSize = 2f;

	public float retrievePFXControlledSize = 4f;

	public ParticleSystem retrievePFX;

	public ParticleSystem controlledPFX;

	public ParticleSystem controlledBoundaryPFX;

	public ParticleSystem floatingPFX;

	public float aimPower;

	public float spinAngle;

	private float spinFactor;

	private Vector2 spinDirection;

	private float spinTimer;

	private float spinDuration;

	private float spinAmt;

	private bool isSpinning;

	private float orbitTimer;

	private float orbitDuration = 1f;

	public bool orbitIsCharged;

	private float returnTimer;

	private Vector2 returnDirection;

	private float returnStrength;

	private GameObject targetEnemy;

	public float homingRadius = 7f;

	private Vector2 homingDirection;

	private float homingStrength;

	private Collider2D[] possibleTargets = new Collider2D[16];

	private Vector2 controlledDirection;

	private float controlledFactor;

	public bool canBeControlled;

	private static float controlledSpeed = 110f;

	private static float controlledRadius = 30f;

	private static float controlledRadiusPadding = 4f;

	public float explosiveTimer = 1.5f;

	public GameObject explosionPrefab;

	private bool canReturn = true;

	private bool canHome;

	public bool isMiniDisc;

	public Sprite shardSprite;

	public Grenade grenadeInstance;

	public float fireDistance;

	private float distanceBetweenFireCells = 0.4f;

	public bool isBurning;

	public float slimeDistance;

	private float distanceBetweenSlimeCells = 0.4f;

	public bool isSliming;

	[SerializeField]
	private bool _isGoldenDisc;

	public GameObject goldenDiscPFX;

	public GameObject goldenDiscSpawnPFX;

	private float outOfBoundsDuration = 2f;

	private float outOfBoundsTimer;

	private float timeScalerCooldown;

	private static float timeScalerDuration = 1f;

	private EventInstance retrieveSoundInstance;

	private EventInstance telekinesisSoundInstance;

	private EventInstance slimeSoundInstance;

	public Player DiscOwner
	{
		get
		{
			return owner as Player;
		}
		set
		{
			owner = value;
		}
	}

	public bool IsGoldenDisc
	{
		get
		{
			return _isGoldenDisc;
		}
		set
		{
			goldenDiscPFX.SetActive(value);
			_isGoldenDisc = value;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		discPowerup = PowerupType.None;
		canReturn = true;
		canHome = false;
		explosiveTimer = 1.5f;
		returnStrength = throwSpeed;
		bounciness = 0.8f;
		retrievePFX.gameObject.SetActive(value: false);
		controlledPFX.gameObject.SetActive(value: false);
		outOfBoundsTimer = 0f;
	}

	protected void OnDisable()
	{
		if (retrieveSoundInstance.isValid())
		{
			retrieveSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
		if (telekinesisSoundInstance.isValid())
		{
			telekinesisSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
		if (slimeSoundInstance.isValid())
		{
			slimeSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
	}

	public void Init(bool isDropped = false, bool isMiniDisc = false, bool isGoldenDisc = false)
	{
		this.isMiniDisc = isMiniDisc;
		IsGoldenDisc = isGoldenDisc;
		targetEnemy = null;
		isSpinning = true;
		canBeControlled = true;
		spinTimer = 0f;
		orbitTimer = 0f;
		isBurning = false;
		isSliming = false;
		fireDistance = 0f;
		float targetInfluence = 0.66f;
		if (IsGoldenDisc)
		{
			targetInfluence = 1.5f;
		}
		else if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
		{
			targetInfluence = 1f;
		}
		Singleton<Camera3D>.Instance.AddCameraTarget(base.transform, targetInfluence);
		base.Init(isDropped);
		if (DiscOwner != null)
		{
			AddTrail();
			if (isGoldenDisc)
			{
				DiscOwner.thrownGoldenDisc = this;
			}
			else
			{
				DiscOwner.thrownDiscs.Add(this);
			}
		}
		if (IsGoldenDisc)
		{
			projectileColor = Singleton<GameManager>.Instance.goldenDiscColor;
			SetDiscColors(projectileColor);
			if (isDropped)
			{
				goldenDiscSpawnPFX.SetActive(value: true);
			}
			else
			{
				goldenDiscSpawnPFX.SetActive(value: false);
			}
		}
		else if (DiscOwner != null)
		{
			CharacterPlayer characterPlayer = owner.character as CharacterPlayer;
			if (characterPlayer != null)
			{
				projectileColor = characterPlayer.DiscColor;
			}
			else
			{
				projectileColor = owner.character.Color;
			}
			SetDiscColors(projectileColor);
			goldenDiscSpawnPFX.SetActive(value: false);
		}
		else
		{
			SetDiscColors(discColorPublic);
			goldenDiscSpawnPFX.SetActive(value: false);
		}
		TogglePFXDiscSpin(value: false);
		if (!isDropped)
		{
			if (IsAnExplosive())
			{
				canReturn = false;
				canBePickedUp = false;
				canCollideWithOwner = false;
			}
			if (Singleton<SettingsManager>.Instance.MatchSettings.homingEnabled)
			{
				canHome = true;
			}
			if (!isMiniDisc)
			{
				StartThrow(reset: false, inheritOwnerVelocity: true);
			}
			else
			{
				StartThrow();
			}
		}
		else
		{
			StartDropped();
		}
		CheckForSquishing();
		collisionsThisFrame.Clear();
		TogglePFXDiscSpin(value: true);
		if (!isMiniDisc)
		{
			grenadeInstance.gameObject.SetActive(value: false);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (movement == Movement.Returning && DiscOwner != null && DiscOwner.actorState == Actor.ActorState.Dead)
		{
			StartFreeform(-10f);
			canBeControlled = false;
		}
		if ((movement == Movement.Throwing || movement == Movement.Returning) && canBeControlled && DiscOwner != null && discPowerup.HasPowerup(PowerupType.ControlledDisc) && DiscOwner.retrieveCharge > 0f)
		{
			movement = Movement.Controlled;
			aimPower = 0f;
			if (IsGoldenDisc)
			{
				SetDiscColors(Singleton<ColorManager>.Instance.goldenDiscColor);
			}
			else
			{
				SetDiscColors(projectileColor);
			}
		}
		if (DiscOwner != null && DiscOwner.activePowerup.HasPowerup(PowerupType.TimeImmune) && timeScalerCooldown > 0f)
		{
			timeScalerCooldown -= Time.deltaTime;
			timeScaler = Mathf.Lerp(1f, (Time.timeScale > 0f) ? (1f / Time.timeScale) : 1f, timeScalerCooldown / timeScalerDuration);
		}
		else
		{
			timeScaler = 1f;
		}
		float num = 1f + aimPower;
		switch (movement)
		{
		case Movement.Free:
			num = velocity.magnitude / speedLimit + aimPower * 0.5f;
			break;
		case Movement.Dropped:
			num = velocity.magnitude / speedLimit * 0.5f;
			TogglePFXDiscSpin(value: false);
			break;
		case Movement.Squished:
			num = 0f;
			break;
		case Movement.Controlled:
			num = 1f;
			break;
		}
		num *= 1.5f * timeScaler;
		discAnimator.SetFloat("SpinSpeed", num);
		if (IsDroppedInHazard())
		{
			if (Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
			{
				if (floatingPFX == null)
				{
					floatingPFX = Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscFloatingOnClouds, base.transform.position, base.transform);
				}
			}
			else if (floatingPFX == null)
			{
				if (Singleton<LevelManager>.Instance.level != null)
				{
					floatingPFX = Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscFloatingOnWater, base.transform.position, Singleton<LevelManager>.Instance.level.waterColor, base.transform);
				}
				else
				{
					floatingPFX = Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscFloatingOnWater, base.transform.position, base.transform);
				}
			}
			discAnimator.SetBool("IsFloating", value: true);
		}
		else
		{
			if (floatingPFX != null)
			{
				floatingPFX.transform.SetParent(null);
				Singleton<ParticleManager>.Instance.StopPFXAndRecycle(floatingPFX);
				floatingPFX = null;
			}
			discAnimator.SetBool("IsFloating", value: false);
		}
		if (movement == Movement.Returning && (DiscOwner.transform.position - base.transform.position).magnitude < 10f)
		{
			if (orbitTimer < orbitDuration && !orbitIsCharged)
			{
				orbitTimer += Time.deltaTime * timeScaler;
			}
			if (orbitTimer > orbitDuration && !orbitIsCharged)
			{
				orbitIsCharged = true;
				if (discTrail != null)
				{
					ParticleSystem.MainModule main = discTrail.main;
					main.startColor = owner.character.Color - new Color(0f, 0f, 0f, 0.3f);
				}
				aimPower = 1f;
				SetDiscColors(IsGoldenDisc ? Singleton<GameManager>.Instance.goldenDiscColor : projectileColor);
			}
			if (orbitIsCharged && (DiscOwner.transform.position - base.transform.position).magnitude < 5f)
			{
				orbitTimer += Time.deltaTime * timeScaler;
				if (orbitTimer > orbitDuration * 2f && !DiscOwner.IsShielded)
				{
					DiscOwner.StartShield(IsGoldenDisc);
				}
			}
		}
		else
		{
			orbitIsCharged = false;
			orbitTimer = 0f;
		}
		if (movement == Movement.Squished)
		{
			int num2 = Physics2D.OverlapCircleNonAlloc(base.transform.position, Projectile.projectileColliderRadius, touchingObstacles, obstacleLayer);
			bool flag = false;
			for (int i = 0; i < num2; i++)
			{
				touchingObstacles[i].Distance(projectileCollider);
				if (touchingObstacles[i].CompareTag("MovingObstacle"))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				aimPower = 0f;
				canBeControlled = false;
				movement = Movement.Dropped;
				orbitIsCharged = false;
				TogglePFXDiscSpin(value: false);
				canCollideWithOwner = true;
			}
		}
		if (movement == Movement.Dropped)
		{
			if (grenadeInstance.gameObject.activeSelf)
			{
				canBePickedUp = false;
			}
			if (IsGoldenDisc)
			{
				if (!Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
				{
					outOfBoundsTimer += Time.deltaTime;
				}
				else if (!Physics2D.OverlapCircle(base.transform.position, Projectile.projectileColliderRadius, Singleton<LevelManager>.Instance.floorLayers))
				{
					outOfBoundsTimer += Time.deltaTime;
				}
				else
				{
					outOfBoundsTimer = 0f;
				}
				if (outOfBoundsTimer >= outOfBoundsDuration)
				{
					GetPickedUp();
					Singleton<GameManager>.Instance.SpawnGoldenDisc();
					if (!Physics2D.OverlapPoint(base.transform.position, Singleton<LevelManager>.Instance.floorLayers))
					{
						if (Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
						{
							Singleton<ParticleManager>.Instance.PlayPFX(PFXType.CloudPuff, base.transform.position + new Vector3(0f, 0f, 8f));
						}
						else
						{
							Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splash, base.transform.position);
						}
					}
					else
					{
						Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Debris, base.transform.position, Singleton<GameManager>.Instance.goldenDiscColor, null, base.transform.rotation);
					}
					outOfBoundsTimer = 0f;
				}
			}
		}
		if (movement != Movement.Squished && DiscOwner != null && DiscOwner.retrieveCharge > 0f)
		{
			if (!retrievePFX.gameObject.activeSelf)
			{
				if (!retrieveSoundInstance.isValid())
				{
					retrieveSoundInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/combat/retrieve", base.transform);
					Singleton<AudioManager>.Instance.SetEventInstanceParameter(retrieveSoundInstance, "pitch", (float)DiscOwner.playerID * 1.5f + (float)DiscOwner.thrownDiscs.Count * 0.25f + (float)(isMiniDisc ? 6 : 0));
				}
				if (retrieveSoundInstance.isValid())
				{
					retrieveSoundInstance.start();
				}
				if (discPowerup.HasPowerup(PowerupType.ControlledDisc) && canBeControlled)
				{
					ParticleSystem.MainModule main2 = retrievePFX.main;
					main2.startSize = retrievePFXControlledSize;
				}
				else
				{
					ParticleSystem.MainModule main3 = retrievePFX.main;
					main3.startSize = retrievePFXDefaultSize;
				}
				retrievePFX.gameObject.SetActive(value: true);
			}
			if (discPowerup.HasPowerup(PowerupType.ControlledDisc) && canBeControlled)
			{
				if (!controlledPFX.gameObject.activeSelf)
				{
					controlledPFX.gameObject.SetActive(value: true);
					if (!telekinesisSoundInstance.isValid())
					{
						telekinesisSoundInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/combat/telekinesis", base.transform);
						Singleton<AudioManager>.Instance.SetEventInstanceParameter(telekinesisSoundInstance, "pitch", (float)DiscOwner.playerID * 1.5f + (float)DiscOwner.thrownDiscs.Count * 0.25f + (float)(isMiniDisc ? 6 : 0));
					}
					telekinesisSoundInstance.start();
				}
				if (telekinesisSoundInstance.isValid())
				{
					Singleton<AudioManager>.Instance.SetEventInstanceParameter(retrieveSoundInstance, "retrieveCharge", DiscOwner.retrieveCharge);
					Singleton<AudioManager>.Instance.SetEventInstanceParameter(telekinesisSoundInstance, "velocity", Mathf.Max(0f, velocity.magnitude / speedLimit));
				}
			}
			else
			{
				controlledPFX.gameObject.SetActive(value: false);
				if (retrieveSoundInstance.isValid())
				{
					Singleton<AudioManager>.Instance.SetEventInstanceParameter(retrieveSoundInstance, "retrieveCharge", DiscOwner.retrieveCharge);
				}
			}
		}
		else
		{
			retrievePFX.gameObject.SetActive(value: false);
			controlledPFX.gameObject.SetActive(value: false);
			if (retrieveSoundInstance.isValid())
			{
				retrieveSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
			}
			if (telekinesisSoundInstance.isValid())
			{
				telekinesisSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
			}
		}
	}

	protected override void FixedUpdate()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		bool active = false;
		switch (movement)
		{
		case Movement.Throwing:
			ApplyHoming();
			break;
		case Movement.Returning:
			velocity += VectorExt.AddForce(CalculateReturn(), ForceMode2D.Force, mass) * timeScaler;
			if (!canBeControlled && !wasJustDeflected && DiscOwner.retrieveCharge > 0.15f)
			{
				canBeControlled = true;
			}
			break;
		case Movement.Free:
			velocity += VectorExt.AddForce(CalculateFreeform(), ForceMode2D.Force, mass) * timeScaler;
			if (velocity.magnitude < 10f * timeScaler)
			{
				if (!IsWayOutOfBounds() && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 1)
				{
					Singleton<Camera3D>.Instance.SetCameraTargetInfluence(base.transform, 0.75f);
				}
				else if (IsWayOutOfBounds())
				{
					Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
				}
				aimPower = 0f;
				movement = Movement.Dropped;
				canBeControlled = false;
				canCollideWithOwner = true;
				TogglePFXDiscSpin(value: false);
				base.gameObject.layer = LayerMask.NameToLayer("Collectables");
				if (DiscOwner == null && !IsGoldenDisc)
				{
					SetDiscColors(discColorPublic);
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, base.transform.position, discColorPublicPFX, base.transform);
				}
				else if (DiscOwner != null && DiscOwner.HasDiscs == 0)
				{
					DiscOwner.playerInput.ConsiderShowingTelekinesisHint(this);
				}
			}
			break;
		case Movement.Dropped:
			if (DiscOwner != null && velocity.magnitude < 8f && !isMiniDisc && (projectileStartedDropped || IsGoldenDisc))
			{
				if (IsGoldenDisc)
				{
					DiscOwner.thrownGoldenDisc = null;
				}
				else
				{
					DiscOwner.thrownDiscs.Remove(this);
				}
				DiscOwner = null;
				if (!IsAnExplosive() && !IsGoldenDisc)
				{
					SetDiscColors(discColorPublic);
				}
				projectileStartedDropped = false;
			}
			if (DiscOwner != null && base.velocityStored.magnitude > 22.5f && DiscOwner.retrieveCharge > 0.1f)
			{
				base.gameObject.layer = LayerMask.NameToLayer("Projectile");
				movement = Movement.Returning;
				TogglePFXDiscSpin(value: true);
				SetDiscColors(projectileColor);
				if (isMiniDisc && !discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
				{
					SetDiscColors(projectileColor);
				}
			}
			if (Singleton<LevelManager>.Instance.level.destroyDiscsOOB && !Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
			{
				Explode(3f);
			}
			break;
		case Movement.Controlled:
		{
			velocity += VectorExt.AddForce(CalculateControlled(), ForceMode2D.Force, mass) * timeScaler;
			Vector2 vector = base.transform.position - DiscOwner.transform.position;
			if (vector.magnitude >= controlledRadius)
			{
				velocity -= vector.normalized * Mathf.Max(0f, Vector2.Dot(velocity, vector.normalized) * Mathf.Clamp01((vector.magnitude - controlledRadius) / controlledRadiusPadding));
				if (Vector2.Dot(vector.normalized, velocity.normalized) > -0.25f)
				{
					active = true;
					float angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
					controlledBoundaryPFX.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
				}
			}
			DiscOwner.metadata.TimeControllingDisc += Time.deltaTime;
			break;
		}
		}
		controlledBoundaryPFX.gameObject.SetActive(active);
		if (isSpinning)
		{
			ApplySpin();
		}
		if (movement != Movement.Squished && DiscOwner != null && DiscOwner.retrieveCharge > 0f && movement != Movement.Controlled && !wasJustDeflected)
		{
			velocity += VectorExt.AddForce(CalculateRetrieve(), ForceMode2D.Force, mass) * timeScaler;
		}
		if (DiscOwner != null && discPowerup.HasPowerup(PowerupType.FireDisc) && !discPowerup.HasPowerup(PowerupType.ExplosiveDisc) && (!discPowerup.HasPowerup(PowerupType.MultiDisc) || isMiniDisc))
		{
			float num = minDistanceFromOwner * 2f + Vector2.Dot(DiscOwner.velocity.normalized, (base.transform.position - DiscOwner.transform.position).normalized) * (DiscOwner.velocity.magnitude / DiscOwner.speedLimit) * 10f;
			if (!Singleton<LevelManager>.Instance.level.isDarkLevel && (movement == Movement.Throwing || movement == Movement.Returning || movement == Movement.Controlled || orbitIsCharged) && (base.transform.position - DiscOwner.transform.position).magnitude > num && velocity.magnitude > 9f)
			{
				isBurning = true;
			}
			else if (movement == Movement.Dropped || (base.transform.position - DiscOwner.transform.position).magnitude <= num)
			{
				isBurning = false;
			}
		}
		if (DiscOwner != null && discPowerup.HasPowerup(PowerupType.SlimeDisc) && (!discPowerup.HasPowerup(PowerupType.MultiDisc) || isMiniDisc))
		{
			float num2 = minDistanceFromOwner * 2f + Vector2.Dot(DiscOwner.velocity.normalized, (base.transform.position - DiscOwner.transform.position).normalized) * (DiscOwner.velocity.magnitude / DiscOwner.speedLimit) * 10f;
			if ((movement == Movement.Throwing || movement == Movement.Returning || movement == Movement.Controlled || orbitIsCharged) && (base.transform.position - DiscOwner.transform.position).magnitude > num2 && velocity.magnitude > 9f)
			{
				if (!isSliming)
				{
					slimeSoundInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/environment/icecell_loop", base.transform);
					slimeSoundInstance.start();
				}
				isSliming = true;
			}
			else if (movement == Movement.Dropped || (base.transform.position - DiscOwner.transform.position).magnitude <= num2)
			{
				if (slimeSoundInstance.isValid())
				{
					slimeSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
					slimeSoundInstance.release();
				}
				isSliming = false;
			}
		}
		base.FixedUpdate();
	}

	protected override void MoveProjectile()
	{
		base.MoveProjectile();
		if (discPowerup.HasPowerup(PowerupType.FireDisc))
		{
			fireDistance += (moveAmount * Time.fixedDeltaTime).magnitude;
		}
		else if (discPowerup.HasPowerup(PowerupType.SlimeDisc))
		{
			slimeDistance += (moveAmount * Time.fixedDeltaTime).magnitude;
		}
		if (isBurning && fireDistance > distanceBetweenFireCells)
		{
			Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, base.transform.position, 1.25f, owner);
			fireDistance = 0f;
			float magnitude = (moveAmount * Time.fixedDeltaTime).magnitude;
			if (magnitude > distanceBetweenFireCells * 2f)
			{
				float num = 0f;
				while (num < magnitude)
				{
					num += distanceBetweenFireCells;
					Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, (Vector2)base.transform.position - moveAmount.normalized * num, 1.25f, owner);
				}
			}
		}
		if (isSliming && slimeSoundInstance.isValid())
		{
			slimeSoundInstance.setParameterByName("velocity", Mathf.Max(0f, velocity.magnitude / speedLimit));
		}
		if (!isSliming || !(slimeDistance > distanceBetweenSlimeCells))
		{
			return;
		}
		Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Ice, base.transform.position, 4f, owner);
		slimeDistance = 0f;
		float magnitude2 = (moveAmount * Time.fixedDeltaTime).magnitude;
		if (magnitude2 > distanceBetweenSlimeCells * 2f)
		{
			float num2 = 0f;
			while (num2 < magnitude2)
			{
				num2 += distanceBetweenSlimeCells;
				Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Ice, (Vector2)base.transform.position - moveAmount.normalized * num2, 4f, owner);
			}
		}
	}

	protected override void GetSquished()
	{
		if (IsGoldenDisc)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Debris, base.transform.position, Singleton<GameManager>.Instance.goldenDiscColor, null, base.transform.rotation);
			GetPickedUp();
			Singleton<GameManager>.Instance.SpawnGoldenDisc();
		}
		else
		{
			base.gameObject.layer = LayerMask.NameToLayer("Collectables");
			movement = Movement.Squished;
			velocity = Vector2.zero;
			discAnimator.SetFloat("SpinSpeed", 0f);
		}
	}

	private void CheckForCollisionsAtCurrentLocation()
	{
		int num = Physics2D.OverlapCircleNonAlloc(layerMask: (movement != Movement.Dropped && !projectileStartedDropped) ? collisionLayers : collisionLayersDropped, point: base.transform.position, radius: Projectile.projectileColliderRadius, results: touchingObstacles);
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			if (touchingObstacles[i].isTrigger && !(touchingObstacles[i] == projectileCollider))
			{
				CollisionBehaviour component = touchingObstacles[i].GetComponent<CollisionBehaviour>();
				if (component != null && (!(DiscOwner != null) || !(component.transform.parent != null) || !(component.transform.parent.GetComponent<Player>() == DiscOwner)) && (canCollideWithOwner || !touchingObstacles[i].CompareTag("MeleeCollider") || !(DiscOwner != null) || !(component.transform.parent.GetComponent<Player>() == DiscOwner)))
				{
					component.OnTriggerEnter2D(projectileCollider);
					OnTriggerEnter2D(touchingObstacles[i]);
				}
			}
		}
	}

	public bool IsWayOutOfBounds()
	{
		if ((base.transform.position - Singleton<LevelManager>.Instance.levelBoundary.bounds.center).magnitude > Singleton<LevelManager>.Instance.levelBoundary.bounds.extents.magnitude)
		{
			return true;
		}
		return false;
	}

	public override void StartThrow(bool reset = false, bool inheritOwnerVelocity = false)
	{
		movement = Movement.Throwing;
		throwTimer = 0f;
		orbitTimer = 0f;
		orbitIsCharged = false;
		throwStrength = throwSpeed * (1f + aimPower);
		homingRadius = 7f;
		if (DiscOwner != null && DiscOwner.playerInput.rewiredController != null && DiscOwner.playerInput.rewiredController.rewiredPlayer.controllers.hasKeyboard)
		{
			homingRadius = 11f;
		}
		throwDuration = 0.45f;
		controlledFactor = 0f;
		if (!reset)
		{
			speedLimit += speedLimit * aimPower * 0.5f;
			throwDuration -= aimPower * 0.15f;
			homingRadius += homingRadius * aimPower;
		}
		if (isMiniDisc)
		{
			throwDuration = 0.3378f;
			if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
			{
				throwDuration *= 0.2f;
			}
		}
		spinDuration = throwDuration * 2f;
		spinFactor = Easing.EaseIn(Mathf.Abs(spinAngle) / 90f);
		velocity = VectorExt.AddForce(throwDirection * throwStrength, ForceMode2D.Force, mass) * 9f * timeScaler;
		base.velocityStored = velocity;
		if (inheritOwnerVelocity)
		{
			velocity += owner.velocity;
		}
		if (DiscOwner != null && discPowerup.HasPowerup(PowerupType.ControlledDisc))
		{
			DiscOwner.playerInput.ConsiderShowingControlledDiscHint(this);
		}
		if (timeScaler != 1f)
		{
			timeScalerCooldown = timeScalerDuration;
			CheckForCollisionsAtCurrentLocation();
		}
	}

	protected override void EndThrow()
	{
		if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc) && !discPowerup.HasPowerup(PowerupType.MultiDisc))
		{
			StartFreeform();
			return;
		}
		if (discPowerup.HasPowerup(PowerupType.MultiDisc))
		{
			MultiDiscBurst();
			return;
		}
		if (canReturn)
		{
			StartReturn();
		}
		else
		{
			freeDirection = velocity.normalized;
			freeStrength = velocity.magnitude * 0.1f;
			StartFreeform();
		}
		throwTimer = throwDuration;
		throwStrength = 0f;
	}

	private void ApplyHoming()
	{
		if (!canHome)
		{
			return;
		}
		targetEnemy = null;
		int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, homingRadius, possibleTargets, playerLayer);
		float magnitude = homingRadius;
		for (int i = 0; i < num; i++)
		{
			Actor component = possibleTargets[i].GetComponent<Actor>();
			if (component != DiscOwner && component.actorState != Actor.ActorState.Dead && !component.IsDashing && Vector2.Dot((possibleTargets[i].transform.position - base.transform.position).normalized, velocity.normalized) > 0.75f && Singleton<GameManager>.Instance.AreOpponents(component, this) && (possibleTargets[i].transform.position - base.transform.position).magnitude < magnitude)
			{
				magnitude = (possibleTargets[i].transform.position - base.transform.position).magnitude;
				targetEnemy = possibleTargets[i].gameObject;
			}
		}
		if (!(targetEnemy != null))
		{
			return;
		}
		Vector2 lhs = targetEnemy.transform.position - base.transform.position;
		float num2 = Vector2.Dot(lhs, throwDirection);
		if (lhs.magnitude <= homingRadius)
		{
			if (num2 < 0.75f)
			{
				lhs = lhs.magnitude * (lhs.normalized * (0.75f + Mathf.Clamp(aimPower * 0.8f, 0f, 1f) * 0.25f));
			}
			if (!Physics2D.Raycast(base.transform.position, lhs.normalized, lhs.magnitude, obstacleLayer))
			{
				float magnitude2 = velocity.magnitude;
				velocity += VectorExt.AddForce(lhs.normalized * (throwSpeed * 1.25f + throwSpeed * aimPower * 3f), ForceMode2D.Force, mass) * timeScaler;
				velocity = Vector2.ClampMagnitude(velocity, magnitude2);
			}
		}
	}

	private void ApplySpin()
	{
		spinTimer += Time.fixedDeltaTime * timeScaler;
		float num = throwDuration * 1.2f;
		float num2 = throwSpeed * (Mathf.Abs(spinAngle) / 90f);
		float magnitude = velocity.magnitude;
		Vector2 vector = Quaternion.AngleAxis(Mathf.Sign(spinAngle) * -90f, Vector3.forward) * throwDirection.normalized * num2;
		if (spinTimer > num)
		{
			vector = Quaternion.AngleAxis(Mathf.Sign(spinAngle) * 90f, Vector3.forward) * returnDirection.normalized * num2;
		}
		if (spinTimer < num)
		{
			velocity += VectorExt.AddForce(vector * Easing.CubicOut(spinTimer / num), ForceMode2D.Force, mass) * timeScaler;
		}
		else if (spinTimer < spinDuration)
		{
			velocity += VectorExt.AddForce(vector, ForceMode2D.Force, mass) * timeScaler;
		}
		else
		{
			velocity += VectorExt.AddForce(vector, ForceMode2D.Force, mass) * timeScaler;
		}
		velocity = Vector2.ClampMagnitude(velocity, magnitude);
	}

	private void StartReturn()
	{
		canCollideWithOwner = true;
		movement = Movement.Returning;
		returnTimer = 0f;
	}

	private Vector2 CalculateReturn()
	{
		returnDirection = (DiscOwner.transform.position - base.transform.position).normalized;
		returnDirection = Vector2.Lerp(velocity.normalized, returnDirection, Time.fixedDeltaTime * 60f);
		float num;
		if (returnTimer < throwDuration)
		{
			num = Mathf.Lerp(0f, returnStrength, Easing.EaseIn(returnTimer / throwDuration));
			returnTimer += Time.fixedDeltaTime * timeScaler;
		}
		else
		{
			returnTimer = throwDuration;
			num = throwSpeed;
		}
		return returnDirection * num;
	}

	private Vector2 CalculateRetrieve(bool fastStart = false)
	{
		returnDirection = (DiscOwner.transform.position - base.transform.position).normalized;
		returnDirection = Vector2.Lerp(velocity.normalized, returnDirection, Time.fixedDeltaTime * 60f);
		float num = Mathf.Clamp01(Easing.QuartOut(DiscOwner.retrieveCharge) - 0.2f);
		if (fastStart)
		{
			num = Mathf.Clamp01(Easing.QuartOut(DiscOwner.retrieveCharge * 3f));
		}
		return returnDirection * returnStrength * num;
	}

	private Vector2 CalculateControlled()
	{
		if (DiscOwner.actorState == Actor.ActorState.Dead || DiscOwner.retrieveCharge == 0f)
		{
			if (velocity.magnitude > 1f)
			{
				movement = Movement.Throwing;
				throwTimer = Easing.EaseOut(1f - Mathf.Clamp01(velocity.magnitude / speedLimit * DiscOwner.retrieveCharge * 4f)) * throwDuration;
				throwDirection = velocity.normalized;
			}
			else if (throwTimer < throwDuration)
			{
				movement = Movement.Throwing;
			}
			else if (canReturn)
			{
				StartReturn();
			}
			else
			{
				freeDirection = velocity.normalized;
				freeStrength = velocity.magnitude * 0.1f;
				StartFreeform();
			}
			controlledFactor = 0f;
			return Vector2.zero;
		}
		if (controlledFactor < 1f)
		{
			controlledFactor += Time.fixedDeltaTime * timeScaler * 4f;
		}
		else
		{
			controlledFactor = 1f;
		}
		controlledDirection = DiscOwner.playerInput.inputMove;
		isSpinning = false;
		return controlledDirection * controlledSpeed * controlledFactor;
	}

	public void TriggerOrbiter()
	{
		canHome = true;
		orbitIsCharged = false;
		spinAngle = 0f;
		throwDirection = velocity.normalized;
		aimPower = 1f;
		speedLimit = 60f;
		velocity *= 2f;
		StartThrow(reset: true);
		Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.identity);
		Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, Mathf.Clamp(velocity.magnitude, 0f, 90f));
		Singleton<TimeKeeper>.Instance.SlowMoEffect(0.1f);
	}

	protected override void StartDropped()
	{
		base.StartDropped();
		canBeControlled = false;
		aimPower = 0f;
		spinAngle = 0f;
		TogglePFXDiscSpin(value: false);
		Singleton<Camera3D>.Instance.SetCameraTargetInfluence(base.transform, 0.75f);
	}

	protected override void StartFreeform(float speedBoost = 0f, bool stayDropped = false)
	{
		base.StartFreeform(speedBoost, stayDropped);
		if (DiscOwner != null)
		{
			DiscOwner.playerInput.ConsiderHidingControlledDiscHint();
		}
		if (DiscOwner != null && DiscOwner.HasDiscs == 0)
		{
			DiscOwner.IsDisarmed = true;
		}
		if (DiscOwner != null && IsGoldenDisc)
		{
			if (DiscOwner.HasGoldenDisc == Player.HoldingGoldenDisc.IsThrown)
			{
				DiscOwner.HasGoldenDisc = Player.HoldingGoldenDisc.IsDropped;
			}
			DiscOwner.thrownGoldenDisc = null;
			DiscOwner = null;
		}
		if (isMiniDisc)
		{
			SetDiscColors(projectileColor, isDroppedMiniDisc: true);
		}
	}

	public override void Bounce(Vector2 collisionVelocity, Vector2 collisionNormal)
	{
		base.Bounce(collisionVelocity, collisionNormal);
		if (movement == Movement.Returning && Vector2.Dot(returnDirection, collisionVelocity.normalized) > 0.01f)
		{
			StartFreeform();
		}
		isSpinning = false;
		controlledFactor = 0f;
		if (DiscOwner != null && DiscOwner.retrieveCharge > 0f)
		{
			velocity -= VectorExt.AddForce(returnDirection * returnStrength * Easing.QuartOut(DiscOwner.retrieveCharge) * ((movement == Movement.Dropped) ? 0.33f : 0.66f), ForceMode2D.Force, mass) * timeScaler;
		}
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		if (collider.CompareTag("InteractiveObstacle") && movement != Movement.Dropped)
		{
			collider.GetComponent<InteractiveObstacle>().Interact((collider.transform.position - base.transform.position).normalized * velocity.magnitude * aimPower * 0.5f);
		}
		base.CollisionEnter2D(normal, collider);
		if (collisionsThisFrame.Contains(collider))
		{
			return;
		}
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			Projectile component = collider.gameObject.GetComponent<Projectile>();
			if (component != null && component.owner != owner)
			{
				canBeControlled = false;
				StartFreeform(2000f);
				wasJustDeflectedTimer = wasJustDeflectedDuration * 3f;
				Singleton<AudioManager>.Instance.PlaySting();
			}
		}
		canHome = false;
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		if (isMiniDisc && collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			return;
		}
		if (DiscOwner != null)
		{
			if (collider.CompareTag("MeleeCollider"))
			{
				if (!Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
				{
					canBeControlled = false;
					canHome = false;
					if (movement != Movement.Dropped && collider.transform.parent.GetComponent<Player>() == DiscOwner)
					{
						aimPower = 1f;
					}
				}
			}
			else if (collider.CompareTag("ShieldCollider") || collider.CompareTag("DamageCollider"))
			{
				canHome = false;
			}
		}
		base.OnTriggerEnter2D(collider);
		if (DiscOwner != null && collider.CompareTag("PickupCollider") && collider.transform.parent.gameObject.GetComponent<Player>() == DiscOwner && canCollideWithOwner && (DiscOwner.HasDiscs >= 2 || (DiscOwner.HasDiscs == 1 && DiscOwner.pendingDiscs > 0)))
		{
			StartFreeform();
		}
	}

	public override void GetPickedUp(bool vanish = false)
	{
		if (grenadeInstance.gameObject.activeSelf)
		{
			grenadeInstance.gameObject.SetActive(value: false);
		}
		Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
		Singleton<GameManager>.Instance.activeDiscs.Remove(this);
		goldenDiscSpawnPFX.SetActive(value: false);
		if (DiscOwner != null)
		{
			if (IsGoldenDisc)
			{
				DiscOwner.thrownGoldenDisc = null;
			}
			else
			{
				DiscOwner.thrownDiscs.Remove(this);
			}
		}
		base.GetPickedUp(vanish);
	}

	protected override void RemoveTrail()
	{
		if (discTrail != null)
		{
			discTrail.transform.SetParent(null);
			Singleton<ParticleManager>.Instance.StopPFXAndRecycle(discTrail);
		}
		if (IsGoldenDisc)
		{
			goldenDiscPFX.GetComponent<ParticleSystem>().Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
	}

	protected override void AddTrail()
	{
		if (discTrail == null)
		{
			if (IsGoldenDisc)
			{
				discTrail = Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscTrail, base.transform.position, Singleton<GameManager>.Instance.goldenDiscColor, base.transform);
			}
			else
			{
				discTrail = Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscTrail, base.transform.position, owner.character.Color - new Color(0f, 0f, 0f, 1f - ((aimPower >= 1f) ? 0.35f : 0f)), base.transform);
			}
		}
		if (IsGoldenDisc)
		{
			goldenDiscPFX.GetComponent<ParticleSystem>().Play();
		}
	}

	public bool IsDroppedInHazard()
	{
		if (movement == Movement.Dropped || (movement == Movement.Free && velocity.magnitude < 15f))
		{
			return !Physics2D.OverlapCircle(base.transform.position, Projectile.projectileColliderRadius * 0.9f, Singleton<LevelManager>.Instance.floorLayers);
		}
		return false;
	}

	public void Explode(float respawnTime)
	{
		if (!isMiniDisc && DiscOwner != null)
		{
			DiscOwner.RespawnDisc(respawnTime);
		}
		GetPickedUp();
	}

	public void MultiDiscBurst()
	{
		if (discPowerup.HasPowerup(PowerupType.MultiDisc))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/multidisc_burst", base.transform.position);
			int num = ((discPowerup.HasPowerup(PowerupType.ExplosiveDisc) || discPowerup.HasPowerup(PowerupType.FireDisc)) ? 4 : 5);
			DiscOwner.miniDiscsRequired = num;
			float num2 = -90f;
			for (int i = 0; i < num; i++)
			{
				num2 = (float)i / ((float)num - 1f) * 180f - 90f;
				SpawnMiniDisc(num2, i);
			}
			isMiniDisc = true;
			GetPickedUp();
			if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
			{
				DiscOwner.RespawnDisc(3.25f);
			}
		}
	}

	public void SpawnMiniDisc(float angle, int index)
	{
		Vector3 euler = new Vector3(0f, 0f, Mathf.Atan2(velocity.normalized.y, velocity.normalized.x) * 57.29578f - 90f);
		euler.z += angle;
		Disc disc = Singleton<GameManager>.Instance.discPrefab.Spawn(base.transform.position, Quaternion.Euler(euler));
		disc.DiscOwner = DiscOwner;
		disc.discPowerup = discPowerup & ~PowerupType.MultiDisc;
		disc.throwDirection = disc.transform.up;
		disc.aimPower = 0f;
		disc.Init(isDropped: false, isMiniDisc: true);
		if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
		{
			disc.grenadeInstance.gameObject.SetActive(value: true);
			disc.grenadeInstance.discOwner = DiscOwner;
			disc.grenadeInstance.parentDisc = disc;
			disc.grenadeInstance.explosiveTimer = 0.931f;
			disc.grenadeInstance.explosiveTimer += (float)index * 0.1f;
		}
		Singleton<GameManager>.Instance.activeDiscs.Add(disc);
	}

	public void OwnerJustDied()
	{
		StartCoroutine(BecomePublic());
	}

	private IEnumerator BecomePublic()
	{
		if (IsAnExplosive())
		{
			yield break;
		}
		if (((Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && !IsGoldenDisc) || (DiscOwner != null && !DiscOwner.hasCompletedTutorial && Singleton<LevelManager>.Instance.level.isTutorialLevel) || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill) && (DiscOwner != null || isMiniDisc))
		{
			yield return new WaitForSeconds(0.5f);
			GetPickedUp(vanish: true);
			yield break;
		}
		yield return null;
		Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
		if (IsGoldenDisc)
		{
			yield return new WaitForSeconds(0.5f);
			DiscOwner.thrownGoldenDisc = null;
			DiscOwner = null;
			yield break;
		}
		if (isMiniDisc)
		{
			yield return new WaitForSeconds(0.5f);
			SetDiscColors(projectileColor, isDroppedMiniDisc: true);
			GetPickedUp(vanish: true);
			yield break;
		}
		yield return new WaitForSeconds(1f);
		if (DiscOwner != null && this != null)
		{
			DiscOwner.thrownDiscs.Remove(this);
			DiscOwner = null;
			SetDiscColors(discColorPublic);
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, base.transform.position, discColorPublicPFX, base.transform);
		}
	}

	private void SetDiscColors(Color targetColor, bool isDroppedMiniDisc = false)
	{
		spinAmt = aimPower;
		if (IsAnExplosive() || movement == Movement.Dropped)
		{
			spinAmt = 0f;
		}
		else if (isMiniDisc)
		{
			spinAmt = 0.5f;
		}
		Singleton<ColorManager>.Instance.SetDiscColor(discRenderer, targetColor, isDroppedMiniDisc, IsGoldenDisc);
		pfxDiscSpin.GetComponent<ParticleSystemRenderer>().material.color = projectileColor;
		ParticleSystem.MainModule main = pfxDiscSpin.main;
		main.startColor = new Color(targetColor.r, targetColor.g, targetColor.b, Mathf.Clamp01(spinAmt) * 0.5f + 0.1f);
		pfxDiscSpin.Clear();
		if (!pfxDiscSpin.isPlaying)
		{
			pfxDiscSpin.Simulate(pfxDiscSpin.main.duration);
		}
		if (discTrail != null)
		{
			main = discTrail.main;
			main.startColor = targetColor - new Color(0f, 0f, 0f, 0.85f - ((aimPower >= 1f) ? 0.4f : 0f));
		}
		main = retrievePFX.main;
		main.startColor = targetColor;
		main = controlledPFX.main;
		main.startColor = targetColor;
		main = controlledBoundaryPFX.main;
		main.startColor = targetColor;
	}

	private void TogglePFXDiscSpin(bool value)
	{
		pfxDiscSpin.gameObject.SetActive(value);
	}

	public override bool IsAnExplosive()
	{
		if (discPowerup.HasPowerup(PowerupType.ExplosiveDisc))
		{
			return !discPowerup.HasPowerup(PowerupType.MultiDisc);
		}
		return false;
	}
}
