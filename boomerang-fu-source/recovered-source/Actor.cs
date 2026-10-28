using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

public class Actor : CollisionBehaviour
{
	public enum ActorState
	{
		Alive,
		Dead,
		Falling
	}

	public enum DeathType
	{
		disc,
		melee,
		squished,
		explosion,
		fall,
		outofbounds,
		teamkill,
		telefrag,
		fire
	}

	public enum KillerType
	{
		Suicide,
		Disc,
		Melee,
		Push,
		MovingObstacle,
		GoldenDisc,
		Explosion,
		Fire,
		Freeze,
		Telefrag
	}

	[Header("Identity")]
	public int characterID;

	public int playerID;

	public int team;

	[Header("Collision")]
	public Collider2D physicsCollider;

	public Collider2D meleeCollider;

	public Collider2D damageCollider;

	public LayerMask obstacleLayer;

	public float physicsColliderRadius;

	protected float damageColliderRadius;

	protected LayerMask collisionLayer;

	public LayerMask playerLayer;

	public LayerMask hazardLayers;

	public LayerMask floorLayers;

	public LayerMask destructablesLayer;

	public LayerMask triggerLayers;

	public InterpolatedTransform interpolatedTransform;

	public Vector2 velocity = Vector2.zero;

	[Range(0f, 1f)]
	protected float edgeAssist = 0.5f;

	public static float zPos = -0.25f;

	protected Vector2 moveAmount;

	protected float mass = 1f;

	protected float drag = 11f;

	protected float bounciness = 1f;

	[Header("State")]
	public ActorState actorState;

	private bool _isStunned;

	public bool canWalk = true;

	public bool canDash = true;

	public bool canRotate = true;

	protected bool _isDashing;

	public bool isShowingOff;

	public bool isSpawningIn;

	public bool isWading;

	private bool _isOnWetFloor;

	private EventInstance wetFloorAudioLoop;

	protected bool isFalling;

	protected int fallsSaved;

	[SerializeField]
	protected bool _isAiming;

	protected bool _isAttacking;

	public bool isCoolingAttack;

	public bool isLunging;

	public int attacksLeft;

	protected bool _isInvlunerable;

	public bool justBecomeVulernable;

	[Header("Movement settings")]
	public float speedLimit = 40f;

	protected float turningSpeed = 12f;

	protected float walkSpeed = 200f;

	protected float walkSpeedUnarmed = 230f;

	protected float dashSpeed = 660f;

	protected float dashDuration = 0.2f;

	protected float dashCooldown = 0.55f;

	protected float dashCooldownMoveThreshold;

	protected float aimSpeed;

	public float slideFactor = 1f;

	protected const float slideFactorMin = 0.25f;

	protected float wadeFactor = 1f;

	protected float slimedFactor = 0.33f;

	protected float retrieveSpeed;

	protected float controlledSpeed = 0.1f;

	protected float pushSpeed = 500f;

	protected float lungeSpeed = 800f;

	public Vector2 inputMove = Vector2.zero;

	[SerializeField]
	protected Vector2 lastInputMove;

	public Vector2 lastRotation;

	protected float dashTimer;

	protected float dashCooldownTimer;

	protected float dashForce;

	protected Vector2 dashDirection;

	protected float currentDashAmt;

	public Vector2 velocityStored;

	public float dashDistanceInvulnerable = 9.45f;

	public float dashDistance = 11.06f;

	public float attackDistance = 4.4f;

	public float walkVelocity = 14.25f;

	protected bool stopDashOnHazards;

	protected float pushDuration;

	protected float pushStunDuration;

	protected Vector2 pushDirection;

	protected float pushTimer;

	protected float pushStrength;

	protected float stunTimer;

	protected Vector2 stunnedDirection;

	protected float showOffDuration;

	protected float showOffTimer;

	protected Vector2 showOffDirection;

	public MovingFloor movingFloor;

	protected Vector3 measuringTape;

	protected float measuringTime;

	[Header("Combat settings")]
	protected float attackDuration = 0.2f;

	protected float attackCooldownDuration = 0.66f;

	protected float lungeCooldownDuration = 0.5f;

	protected float lungeDuration = 0.1125f;

	protected float aimStartTime = 0.1f;

	public float attackCooldownTimer;

	public float meleeTimer;

	public float lungeCooldownTimer;

	public float lungeTimer;

	protected Vector2 lungeDirection;

	protected bool stopLungeOnHazards;

	protected float fallDuration = 0.66f;

	protected float fallTimer;

	protected float fallAffordance = 0.33f;

	protected Collider2D[] touchingObstacles = new Collider2D[16];

	protected RaycastHit2D[] touchingTriggers = new RaycastHit2D[16];

	public float aimTimer;

	public float aimPower;

	public float aimMaxPower = 1f;

	public float aimDuration = 0.9f;

	protected Vector3 aimAssistantStartPos = new Vector3(0f, 1.25f, 0f);

	protected Vector3 aimAssistantEndPos = new Vector3(0f, 5f, 0f);

	protected float aimMoveLag;

	protected float aimMoveLagDuration = 0.5f;

	protected Collider2D[] teleportDestColliders = new Collider2D[16];

	protected bool canUseTeleportal = true;

	[Header("Death and debuffs")]
	public Actor killer;

	protected float killerTimer;

	protected KillerType killerType;

	public HashSet<Collider2D> deflectedColliders = new HashSet<Collider2D>();

	protected ParticleSystem onFirePFX;

	public bool isOnFire;

	public Actor fireOwner;

	protected float burnTimer;

	protected float burnDuration = 2.5f;

	public float fireDistance;

	protected bool isBeingExtinguished;

	private EventInstance fireAudioLoop;

	[SerializeField]
	protected bool _isSlimed;

	[SerializeField]
	public float slimeTimer;

	[SerializeField]
	protected static float slimeDuration = 0.5f;

	protected static float slimeBuffer = 0.1f;

	[SerializeField]
	protected bool isSlimeTiming;

	protected bool _isFrozen;

	[SerializeField]
	protected float freezeTimer;

	[SerializeField]
	protected static float freezeDuration = 7f;

	protected float chipTimer;

	protected ParticleSystem frozenParticles;

	protected Animator frozenBlockAnimator;

	public float timeScaler = 1f;

	public float reviveTimer;

	public static float reviveDuration = 1.68f;

	public bool gettingRevivedThisFrame;

	[Header("Appearance")]
	public Character character;

	public Transform appearance;

	protected Vector3 characterOffset = new Vector3(0.4f, 0f, 0f);

	public Transform actorPFX;

	public GameObject shadow;

	protected GameObject aimAssistant;

	protected SpriteRenderer aimAssistantRenderer;

	protected bool walkOffset;

	protected bool isStandingStill = true;

	protected float standingStillTimer;

	protected float standingStillDuration = 0.2f;

	protected float tiltFactor = 1f;

	protected float tiltSpeed = 1f;

	[Header("Particle FX")]
	public ParticleSystem onFirePFXPrefab;

	public Animator frozenBlockPrefab;

	public ParticleSystem frozenParticlesPrefab;

	public ParticleSystem dustTrailPrefab;

	public ParticleSystem slidingPFXPrefab;

	protected ParticleSystem dustTrail;

	protected ParticleSystem.MainModule dustTrailMain;

	protected ParticleSystem slidingPFX;

	protected ParticleSystem.MainModule slidingPFXMain;

	protected float timeBetweenSlidingPFX = 1f;

	protected float timeOfLastSlidingPFX;

	public ParticleSystem dashTrail;

	public virtual bool IsStunned
	{
		get
		{
			return _isStunned;
		}
		set
		{
			if (_isStunned != value)
			{
				if (((!IsFrozen && !IsAttacking) & value) || !value)
				{
					character.anim.SetBool("IsStunned", value);
				}
				_isStunned = value;
			}
		}
	}

	public virtual bool IsDashing
	{
		get
		{
			return _isDashing;
		}
		set
		{
			if (value != _isDashing)
			{
				_isDashing = value;
				if (_isDashing)
				{
					character.anim.SetBool("IsDashing", value: true);
					return;
				}
				character.anim.SetBool("IsDashing", value: false);
				EmitFootstep(3, 2f);
				dashTrail.Stop();
			}
		}
	}

	public bool IsOnWetFloor
	{
		get
		{
			return _isOnWetFloor;
		}
		set
		{
			if (_isOnWetFloor == value)
			{
				return;
			}
			if (value && actorState == ActorState.Alive)
			{
				wetFloorAudioLoop = Singleton<AudioManager>.Instance.PlayLooping("event:/environment/wet_floor", base.transform);
			}
			else if (wetFloorAudioLoop.isValid())
			{
				Singleton<AudioManager>.Instance.StopLooping(wetFloorAudioLoop);
			}
			if (value)
			{
				if (Singleton<LevelManager>.InstanceExists && Singleton<LevelManager>.Instance.level != null)
				{
					slidingPFXMain.startColor = Singleton<LevelManager>.Instance.level.slidingColor;
				}
				slidingPFX.Emit(UnityEngine.Random.Range(3, 4));
			}
			_isOnWetFloor = value;
		}
	}

	public virtual bool IsAiming
	{
		get
		{
			return _isAiming;
		}
		set
		{
			if (character.anim.isActiveAndEnabled)
			{
				if (value)
				{
					character.anim.SetFloat("AttackSpeed", 1f);
				}
				if (_isAiming && !value)
				{
					character.anim.ResetTrigger("AimStart");
					character.anim.SetTrigger("AimStop");
				}
				else if (!_isAiming & value)
				{
					character.anim.ResetTrigger("AimStop");
					character.anim.SetTrigger("AimStart");
				}
			}
			_isAiming = value;
		}
	}

	public virtual bool IsAttacking
	{
		get
		{
			return _isAttacking;
		}
		set
		{
			_isAttacking = value;
			meleeCollider.enabled = _isAttacking;
		}
	}

	public bool IsInvulnerable
	{
		get
		{
			return _isInvlunerable;
		}
		set
		{
			if (value != _isInvlunerable)
			{
				_isInvlunerable = value;
			}
		}
	}

	public virtual bool IsSlimed
	{
		get
		{
			return _isSlimed;
		}
		set
		{
			_isSlimed = value;
			if (!IsFrozen)
			{
				character.IsSlimed = value;
			}
		}
	}

	[SerializeField]
	public virtual bool IsFrozen
	{
		get
		{
			return _isFrozen;
		}
		set
		{
			if (_isFrozen != value)
			{
				if (value)
				{
					character.anim.SetFloat("MoveSpeed", 0f);
				}
				character.IsSlimed = value;
				frozenBlockAnimator.SetBool("IsOn", value);
				character.anim.SetBool("IsFrozen", value);
				_isFrozen = value;
			}
		}
	}

	protected virtual void OnEnable()
	{
		Physics2D.IgnoreCollision(meleeCollider, damageCollider);
	}

	protected virtual void Start()
	{
		damageColliderRadius = damageCollider.GetComponent<CircleCollider2D>().radius * base.transform.lossyScale.x;
		physicsColliderRadius = physicsCollider.GetComponent<CircleCollider2D>().radius * base.transform.lossyScale.x;
		aimAssistant = base.transform.Find("Appearance/AimAssistant").gameObject;
		aimAssistantRenderer = aimAssistant.GetComponent<SpriteRenderer>();
		actorPFX = base.transform.Find("Appearance/ActorPFX").transform;
		onFirePFX = UnityEngine.Object.Instantiate(onFirePFXPrefab, base.transform.position + new Vector3(0f, 0f, -1f * base.transform.localScale.z), onFirePFXPrefab.transform.rotation, actorPFX);
		frozenParticles = UnityEngine.Object.Instantiate(frozenParticlesPrefab, base.transform.position + new Vector3(0f, 0f, -0.5f * base.transform.localScale.z), frozenParticlesPrefab.transform.rotation, base.transform);
		dustTrail = UnityEngine.Object.Instantiate(dustTrailPrefab, base.transform.position, dustTrailPrefab.transform.rotation, base.transform);
		dustTrailMain = dustTrail.main;
		slidingPFX = UnityEngine.Object.Instantiate(slidingPFXPrefab, base.transform.position, slidingPFXPrefab.transform.rotation, base.transform);
		slidingPFXMain = slidingPFX.main;
		frozenBlockAnimator = UnityEngine.Object.Instantiate(frozenBlockPrefab, base.transform.position + new Vector3(0f, -0.2f, -0.5f * base.transform.localScale.z), frozenBlockPrefab.transform.rotation, base.transform);
		TiltActorTowardsCamera();
	}

	public virtual void Init()
	{
		interpolatedTransform.ForgetPreviousTransforms();
		physicsCollider.enabled = true;
		collisionLayer = (int)playerLayer | (int)obstacleLayer;
		deflectedColliders.Clear();
		dashTimer = dashDuration;
		meleeTimer = 0f;
		attackCooldownTimer = 0f;
		lungeCooldownTimer = 0f;
		burnTimer = 0f;
		fireDistance = 0f;
		actorState = ActorState.Alive;
		IsStunned = false;
		isShowingOff = false;
		isSpawningIn = false;
		canWalk = true;
		canDash = true;
		canRotate = true;
		IsDashing = false;
		slideFactor = 1f;
		isWading = false;
		IsOnWetFloor = false;
		IsAiming = false;
		isFalling = false;
		fallsSaved = 0;
		IsFrozen = false;
		attacksLeft = 1;
		IsAttacking = false;
		isCoolingAttack = false;
		isLunging = false;
		IsInvulnerable = false;
		canUseTeleportal = true;
		velocity = Vector2.zero;
		pushTimer = 0f;
		ToggleFire(onFire: false);
		character.Reset();
		if (dustTrail != null)
		{
			dustTrail.Clear();
		}
		UpdateDustTrailColor();
		character.anim.SetFloat("MoveSpeed", 0f);
		character.anim.SetBool("IsFrozen", value: false);
		character.anim.SetBool("IsFalling", value: false);
		if (frozenBlockAnimator != null)
		{
			frozenBlockAnimator.SetBool("IsOn", value: false);
			frozenBlockAnimator.SetTrigger("Init");
		}
	}

	protected virtual void FixedUpdate()
	{
		if (!physicsCollider.enabled)
		{
			if (character.anim.isActiveAndEnabled)
			{
				character.anim.SetFloat("MoveSpeed", 0f);
			}
			return;
		}
		if (!IsDashing && !isFalling && actorState != ActorState.Falling)
		{
			if (actorState == ActorState.Alive && IsPositionOverHazard(base.transform.position) && velocity.magnitude < speedLimit * 0.8f)
			{
				StartFall();
			}
			else if (actorState == ActorState.Dead && IsPositionOverHazard(shadow.transform.position))
			{
				FallInHazard();
			}
		}
		IsOnWetFloor = Physics2D.OverlapPoint(base.transform.position, 1 << LayerMask.NameToLayer("WetFloor"));
		if ((!IsDashing || dashTimer > dashDuration * 0.8f) && IsOnWetFloor)
		{
			slideFactor = 0.25f;
			drag = 1f;
			float num = velocity.magnitude * 10f;
			num *= Mathf.Clamp(1f - (Vector2.Dot(velocity.normalized, base.transform.up) * 0.5f + 0.5f), 0.25f, 1f);
			timeBetweenSlidingPFX = Mathf.Max(1f / num, 0.0166f);
			if (Singleton<TimeKeeper>.Instance.TimeSince(timeOfLastSlidingPFX) > timeBetweenSlidingPFX)
			{
				EmitSlidingPFX();
			}
		}
		else if (1f - slideFactor < 0.1f)
		{
			slideFactor = 1f;
			drag = 11f;
		}
		else
		{
			float t = Time.fixedDeltaTime * 7f;
			slideFactor = Mathf.Lerp(slideFactor, 1f, t);
			drag = Mathf.Lerp(drag, 11f, t);
		}
		if (IsFrozen && !isFalling)
		{
			drag = 2f;
		}
		if (actorState == ActorState.Dead && physicsCollider.enabled)
		{
			MoveActor();
			return;
		}
		Collider2D collider2D = Physics2D.OverlapCircle(base.transform.position, physicsColliderRadius, 1 << LayerMask.NameToLayer("Plant"));
		if (!IsDashing && collider2D != null && collider2D.CompareTag("Plant"))
		{
			isWading = true;
		}
		else
		{
			isWading = false;
		}
		velocity += VectorExt.AddForce(CalculatePushback()) * slideFactor;
		Actor owner = null;
		if (!isOnFire && !isSlimeTiming && Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Fire, out owner))
		{
			if (IsDashing || IsInvulnerable || IsOnWetFloor)
			{
				Singleton<CellManager>.Instance.ExtinguishCellsAt(base.transform.position, CellManager.CellType.Fire);
			}
			else if (this is Player)
			{
				Player player = this as Player;
				if (!isOnFire && player.IsShielded)
				{
					StopAiming();
					velocity = Vector3.zero;
					player.ShieldTakesDamage();
					StartPushback(Vector2.zero, 0.15f);
					player.playerInput.StartVibration(0.2f, 0.1f);
					Actor owner2 = null;
					if (Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Fire, out owner2))
					{
						Singleton<CellManager>.Instance.ExtinguishCellsAt(base.transform.position, CellManager.CellType.Fire);
						if (owner2 != null)
						{
							SetKiller(owner2, KillerType.Fire, 1.5f);
						}
					}
				}
				else
				{
					ToggleFire(onFire: true);
					if (owner != null)
					{
						fireOwner = owner;
					}
				}
			}
			else
			{
				ToggleFire(onFire: true);
				if (owner != null)
				{
					fireOwner = owner;
				}
			}
		}
		if (isOnFire && fireDistance > 0.5f)
		{
			fireDistance = 0f;
			Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, base.transform.position, 0.66f, this);
		}
		Actor owner3 = null;
		if (!IsDashing && !IsInvulnerable && !IsFrozen && Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Ice, out owner3))
		{
			if (!isSlimeTiming)
			{
				isSlimeTiming = true;
				slimeTimer = slimeDuration;
				if (isOnFire)
				{
					ToggleFire(onFire: false, extinguishPFX: true);
					if (this is Player)
					{
						(this as Player).metadata.FiresExtinguished++;
					}
				}
			}
			else if (slimeTimer > 0f)
			{
				slimeTimer -= Time.fixedDeltaTime * ((owner3 != null && owner3 == this) ? 0.2f : 1f);
			}
			else
			{
				Singleton<CellManager>.Instance.ExtinguishCellsAt(base.transform.position, CellManager.CellType.Ice);
				Freeze();
				SetKiller(owner3, KillerType.Freeze, 1f);
			}
		}
		else if (isSlimeTiming)
		{
			if (slimeTimer < slimeDuration)
			{
				slimeTimer += Time.fixedDeltaTime * 0.5f;
			}
			else
			{
				isSlimeTiming = false;
				slimeTimer = 0f;
				if (IsSlimed)
				{
					IsSlimed = false;
				}
			}
		}
		if (isSlimeTiming && !IsSlimed && slimeTimer < slimeDuration - slimeBuffer)
		{
			IsSlimed = true;
		}
		ApplyForces();
		MoveActor();
		if ((!IsDashing || dashTimer > dashDuration * 0.8f) && IsOnWetFloor && wetFloorAudioLoop.isValid())
		{
			Singleton<AudioManager>.Instance.SetEventInstanceParameter(wetFloorAudioLoop, "velocity", (Vector2.Dot(velocity.normalized, base.transform.up) * 0.5f + 0.5f) * Easing.EaseOut(Mathf.Clamp01(velocityStored.magnitude / speedLimit)));
		}
	}

	protected void MoveActor()
	{
		velocity *= timeScaler;
		if (velocity.magnitude > 0f)
		{
			velocity *= Mathf.Clamp01(1f - drag * Time.fixedDeltaTime * Mathf.Clamp(timeScaler, 1f, 4f));
			if (isOnFire || isBeingExtinguished)
			{
				fireDistance += (moveAmount * Time.fixedDeltaTime).magnitude;
			}
		}
		else
		{
			velocity = Vector2.zero;
		}
		Physics2D.SyncTransforms();
		Vector3 newPosition = base.transform.position;
		CheckForSquishing(ref newPosition);
		base.transform.position = newPosition;
		velocity = Vector2.ClampMagnitude(velocity, speedLimit * (IsDashing ? 2f : 1f) * timeScaler);
		moveAmount = velocity;
		moveAmount = CheckForCollisions(moveAmount);
		base.transform.position += (Vector3)moveAmount * Time.fixedDeltaTime;
		velocityStored = velocity;
		velocity /= timeScaler;
	}

	protected void ApplyForces()
	{
		if (actorState == ActorState.Dead)
		{
			return;
		}
		velocity += VectorExt.AddForce(CalculateWalk()) * timeScaler * slideFactor;
		velocity += VectorExt.AddForce(CalculateDash()) * timeScaler * slideFactor;
		StopDashOnHazards();
		CooldownDash();
		velocity += VectorExt.AddForce(CalculateLunge()) * timeScaler * slideFactor;
		StopLungeOnHazards();
		if (canRotate && !isLunging && !IsFrozen && actorState == ActorState.Alive)
		{
			if (IsStunned)
			{
				RotateSmoothlyTo(stunnedDirection);
			}
			else if (isShowingOff)
			{
				RotateSmoothlyTo(showOffDirection);
			}
			else
			{
				RotateSmoothlyTo(lastInputMove);
			}
		}
		if (!character.anim.isActiveAndEnabled || IsStunned || isShowingOff || IsFrozen || IsDashing)
		{
			return;
		}
		float num = Mathf.Min(velocity.magnitude / 11f, 2f);
		if (num > 0.25f && character.characterType == CharacterType.PaddlePop)
		{
			num = Mathf.Max(num, 1f);
		}
		if (slideFactor < 1f)
		{
			num *= Mathf.Clamp01(slideFactor * 2f);
		}
		else if (isLunging)
		{
			num = 0f;
		}
		if (num <= 0.25f)
		{
			num = 0f;
			if (standingStillTimer < standingStillDuration)
			{
				standingStillTimer += Time.fixedDeltaTime;
			}
			else if (!isStandingStill)
			{
				isStandingStill = true;
				walkOffset = !walkOffset;
				character.anim.SetFloat("WalkOffset", walkOffset ? 0f : 0.5f);
			}
		}
		else
		{
			isStandingStill = false;
			standingStillTimer = 0f;
		}
		character.anim.SetFloat("MoveSpeed", num);
	}

	protected Vector2 CheckForCollisions(Vector2 moveAmount)
	{
		if (actorState != ActorState.Alive)
		{
			collisionLayer = (int)collisionLayer ^ (int)playerLayer;
		}
		if (moveAmount.magnitude > 0f)
		{
			RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, physicsColliderRadius, moveAmount.normalized, moveAmount.magnitude * Time.fixedDeltaTime, 1 << LayerMask.NameToLayer("Teleportal"));
			if ((bool)raycastHit2D)
			{
				OnTriggerEnter2D(raycastHit2D.collider);
			}
			moveAmount = CollideWithObstacles(moveAmount);
			int num = Physics2D.CircleCastNonAlloc(base.transform.position, physicsColliderRadius, moveAmount.normalized, touchingTriggers, moveAmount.magnitude * Time.fixedDeltaTime, triggerLayers);
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					CollisionBehaviour component = touchingTriggers[i].collider.GetComponent<CollisionBehaviour>();
					if (component != null)
					{
						if (component as Plant != null)
						{
							component.OnTriggerEnter2D(damageCollider);
						}
						else if (component as Debris3D != null)
						{
							component.OnTriggerEnter2D(physicsCollider);
						}
					}
				}
			}
			moveAmount = SlowDownForHazards(moveAmount);
		}
		return moveAmount;
	}

	protected virtual Vector2 SlowDownForHazards(Vector2 moveAmount)
	{
		return moveAmount;
	}

	protected Vector2 CollideWithObstacles(Vector2 moveAmount, int recursionDepth = 0)
	{
		recursionDepth++;
		RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, physicsColliderRadius, moveAmount.normalized, moveAmount.magnitude * Time.fixedDeltaTime, (int)collisionLayer | (int)destructablesLayer);
		bool num = raycastHit2D;
		Vector2 normal = raycastHit2D.normal;
		Collider2D collider = raycastHit2D.collider;
		Vector2 centroid = raycastHit2D.centroid;
		if (num)
		{
			CollisionEnter2D(normal, collider);
			CollisionBehaviour component = collider.GetComponent<CollisionBehaviour>();
			if (component != null)
			{
				component.CollisionEnter2D((collider.transform.position - base.transform.position).normalized, physicsCollider);
			}
			if (component != null && component is Decoy)
			{
				this.moveAmount = moveAmount;
				return this.moveAmount;
			}
			if (component != null && component is Player { IsHider: false })
			{
				this.moveAmount = centroid - (Vector2)base.transform.position - moveAmount.normalized * 0.01f;
			}
			else
			{
				if (recursionDepth < 2)
				{
					Vector2 vector = CollideWithObstacles(moveAmount - normal * Vector3.Dot(moveAmount, normal) - moveAmount.normalized * 0.01f, recursionDepth);
					this.moveAmount = vector;
				}
				else
				{
					this.moveAmount = Vector2.zero;
				}
				velocity = this.moveAmount;
			}
		}
		else
		{
			this.moveAmount = moveAmount;
		}
		return this.moveAmount;
	}

	protected virtual void PenetrateActors()
	{
		if (actorState != ActorState.Alive || ((this as Player).IsHider && Singleton<GameManager>.Instance.IsInSeekingPhase) || (Singleton<GameManager>.Instance.IsInHidingPhase && (this as Player).IsSeeker))
		{
			return;
		}
		int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, physicsColliderRadius, touchingObstacles, playerLayer);
		for (int i = 0; i < num; i++)
		{
			if (touchingObstacles[i].transform.root != base.transform && (Singleton<GameManager>.Instance.activeDecoys.Count <= 0 || !touchingObstacles[i].transform.root.GetComponent<Decoy>()))
			{
				velocity += (Vector2)(base.transform.position - touchingObstacles[i].transform.position) * slideFactor;
				break;
			}
		}
	}

	public virtual void CheckForSquishing(ref Vector3 newPosition, int recursionDepth = 0)
	{
		if (!physicsCollider.enabled || recursionDepth > 5)
		{
			return;
		}
		float num = -0.01f;
		int num2 = Physics2D.OverlapCircleNonAlloc(newPosition, physicsColliderRadius + num, touchingObstacles, obstacleLayer);
		MovingObstacle movingObstacle = null;
		for (int i = 0; i < num2; i++)
		{
			ColliderDistance2D colliderDistance2D = touchingObstacles[i].Distance(physicsCollider);
			if (touchingObstacles[i].CompareTag("MovingObstacle"))
			{
				movingObstacle = touchingObstacles[i].GetComponent<MovingObstacle>();
				if (movingObstacle.movementState == MovingObstacle.MovementState.Moving)
				{
					if (movingObstacle.moveOwner != null)
					{
						SetKiller(movingObstacle.moveOwner, KillerType.MovingObstacle, 1.5f);
					}
					if (recursionDepth == 0)
					{
						if (movingObstacle.isRotatingObstacle)
						{
							Vector2 vector = Vector2.Lerp(colliderDistance2D.pointA, colliderDistance2D.pointB, 0.5f) - (Vector2)movingObstacle.transform.position;
							float num3 = vector.magnitude * movingObstacle.angularVelocity;
							Vector2 vector2 = new Vector2(vector.y, 0f - vector.x);
							if (!movingObstacle.isRotatingClockwise)
							{
								vector2 *= -1f;
							}
							velocity += vector2.normalized * num3;
						}
						else
						{
							velocity += movingObstacle.velocity * Mathf.Clamp01(Vector3.Dot(movingObstacle.velocity.normalized, colliderDistance2D.normal));
						}
					}
					if (colliderDistance2D.isOverlapped && recursionDepth > 3)
					{
						if (actorState == ActorState.Alive)
						{
							Die(DeathType.squished);
						}
						else
						{
							GetSquished();
						}
						break;
					}
				}
			}
			if (colliderDistance2D.isOverlapped)
			{
				Vector2 vector3 = -colliderDistance2D.normal * colliderDistance2D.distance;
				RaycastHit2D raycastHit2D = Physics2D.CircleCast(newPosition, physicsColliderRadius + num, vector3.normalized, vector3.magnitude, obstacleLayer);
				Vector2 vector4 = vector3 - raycastHit2D.normal * Vector3.Dot(vector3, raycastHit2D.normal);
				newPosition += (Vector3)vector4;
				CheckForSquishing(ref newPosition, recursionDepth + 1);
				break;
			}
		}
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		if (actorState == ActorState.Dead)
		{
			return;
		}
		if (collider.CompareTag("InteractiveObstacle") && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			InteractiveObstacle component = collider.GetComponent<InteractiveObstacle>();
			if (IsDashing && !(component is DestructableObstacle))
			{
				component.Interact((collider.transform.position - base.transform.position).normalized * velocity.magnitude);
			}
			else if (IsStunned && velocity.magnitude > speedLimit * 0.66f)
			{
				component.Interact((collider.transform.position - base.transform.position).normalized * velocity.magnitude);
				velocity *= 0.5f;
			}
		}
		if (collider.CompareTag("Player"))
		{
			Actor component2 = collider.GetComponent<Actor>();
			if (component2 is Decoy)
			{
				return;
			}
			if (killer != null && (killer == this || killer == component2))
			{
				SetKiller(component2, KillerType.Push, 2.5f);
			}
			if (component2.isOnFire && !isOnFire && !isSlimeTiming)
			{
				ToggleFire(onFire: true);
				fireOwner = component2;
			}
			if (Singleton<GameManager>.Instance.IsInSeekingPhase && ((this as Player).IsHider || (component2 as Player).IsHider))
			{
				return;
			}
			if (!isLunging)
			{
				velocity = Vector2.Reflect(velocityStored, normal) * bounciness * slideFactor;
			}
			Vector2 vector = Vector2.Reflect(velocityStored - component2.velocityStored, normal);
			float num = Mathf.Max(1f / timeScaler * component2.timeScaler, 1f / component2.timeScaler * timeScaler);
			if (!IsDashing)
			{
				if (component2.IsDashing)
				{
					StopAiming();
					StopLunge();
					StartPushback(component2.velocityStored.normalized * num * dashSpeed * 0.5f, 0.19f, 0.4f);
					Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/dash_slam", base.transform.position);
				}
				else
				{
					velocity += component2.velocityStored * num * slideFactor;
					StartPushback(vector, 0.2f);
				}
			}
			else if (component2.IsDashing)
			{
				StartPushback(vector * 0.75f, 0.2f, 0.66f);
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/dash_slam", base.transform.position);
			}
			else
			{
				if (dashTimer < dashDuration * 0.66f)
				{
					dashTimer = dashDuration * 0.66f;
				}
				velocity = Vector3.zero;
			}
		}
		else if (IsFrozen && velocityStored.magnitude > speedLimit * 0.4f)
		{
			Die(DeathType.melee);
			physicsCollider.enabled = false;
			character.IsVisible = false;
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && base.transform.parent == null && (Singleton<LevelManager>.Instance.HazardContactPoints(base.transform.position, physicsColliderRadius * 0.95f) > 4 || collider.OverlapPoint(base.transform.position)))
		{
			Transform parent = base.transform.parent;
			base.transform.SetParent(collider.transform, worldPositionStays: true);
			interpolatedTransform.ReparentPreviousTransforms(parent, collider.transform);
			movingFloor = collider.transform.GetComponent<MovingFloor>();
			if (movingFloor != null)
			{
				AI component = GetComponent<AI>();
				if (component != null)
				{
					component.OnEnterMovingFloor(movingFloor);
				}
			}
		}
		if (collider.CompareTag("TrapFloor") && !IsDashing && !Singleton<LevelManager>.Instance.IsPointOnSolidGround(base.transform.position, physicsColliderRadius * 0.95f))
		{
			collider.GetComponent<TrapFloor>().StartFalling();
		}
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		if (canUseTeleportal && collider.CompareTag("Teleportal"))
		{
			Teleportal component = collider.GetComponent<Teleportal>();
			Vector2 vector = component.exit.transform.position + component.exit.transform.up * (physicsColliderRadius + component.exit.thickness);
			base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
			if (IsDashing)
			{
				dashTrail.Stop();
			}
			interpolatedTransform.ForgetPreviousTransforms();
			component.Flash(character.Color);
			component.exit.Flash(character.Color);
			canUseTeleportal = false;
			StartCoroutine(EnableTeleportalCollisionAfterFrames());
			if (IsDashing)
			{
				dashTrail.Play();
			}
			else
			{
				character.anim.SetTrigger("Teleport");
			}
			AI component2 = GetComponent<AI>();
			if (component2 != null)
			{
				component2.ReactToTeleportal();
			}
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/teleportal", base.transform.position);
			Singleton<Camera3D>.Instance.Shake(0.25f, 0.1f);
		}
	}

	public override void OnTriggerExit2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && (actorState != ActorState.Dead || !character.IsSquished))
		{
			UnparentFromMovingFloor();
		}
	}

	private void UnparentFromMovingFloor()
	{
		Transform parent = base.transform.parent;
		if (!(parent == null))
		{
			if (base.gameObject.activeSelf)
			{
				base.transform.SetParent(null, worldPositionStays: true);
			}
			interpolatedTransform.ReparentPreviousTransforms(parent, null);
			AI component = GetComponent<AI>();
			if (component != null)
			{
				component.OnExitMovingFloor();
			}
			movingFloor = null;
		}
	}

	private IEnumerator EnableTeleportalCollisionAfterFrames(int frames = 2)
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		canUseTeleportal = true;
	}

	protected virtual Vector2 CalculateWalk()
	{
		if (canRotate)
		{
			if (inputMove.magnitude > 0f)
			{
				lastInputMove = inputMove;
			}
			else
			{
				aimMoveLag = 0f;
			}
		}
		if (!canWalk || IsStunned || isShowingOff || IsFrozen || isLunging || actorState != ActorState.Alive || isSpawningIn)
		{
			return Vector2.zero;
		}
		return inputMove * GetMoveSpeed();
	}

	protected virtual float GetMoveSpeed()
	{
		float num = walkSpeed;
		if (IsAiming)
		{
			num = aimSpeed;
		}
		else if (aimMoveLag > 0f)
		{
			aimMoveLag -= Time.fixedDeltaTime * timeScaler * (1f / aimMoveLagDuration);
			num = Mathf.Lerp(num, aimSpeed, Easing.EaseOut(aimMoveLag));
		}
		if (slideFactor < 1f)
		{
			num *= slideFactor;
		}
		if (isWading)
		{
			num *= wadeFactor;
		}
		if (IsSlimed)
		{
			num *= slimedFactor;
		}
		return num * Mathf.Clamp(timeScaler, 1f, 1.75f);
	}

	public virtual void StartPushback(Vector2 direction, float duration, float stunDuration = 0f)
	{
		if (actorState == ActorState.Alive)
		{
			velocity += VectorExt.AddForce(direction * 2.5f * timeScaler) * slideFactor;
			pushDuration = duration;
			pushTimer = pushDuration;
			pushDirection = direction.normalized;
			pushStrength = pushSpeed * direction.magnitude;
			EmitFootstep(3, 2f);
			if (stunDuration > 0f)
			{
				stunnedDirection = -direction;
				character.anim.SetFloat("MoveSpeed", 0f);
				pushStunDuration = stunDuration;
				IsStunned = true;
				stunTimer = 0f;
				isShowingOff = false;
			}
		}
	}

	protected Vector2 CalculatePushback()
	{
		if (pushTimer > 0f && !isFalling)
		{
			float t = pushTimer / pushDuration;
			float num = Mathf.Lerp(0f, pushStrength, t);
			pushTimer -= Time.fixedDeltaTime * timeScaler;
			return pushDirection * num * Time.fixedDeltaTime;
		}
		pushTimer = 0f;
		pushStrength = 0f;
		return Vector2.zero;
	}

	protected void RotateSmoothlyTo(Vector2 direction)
	{
		lastRotation = direction;
		Quaternion b = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * 57.29578f - 90f, Vector3.forward);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.fixedDeltaTime * timeScaler * turningSpeed);
	}

	public void RotateSnapTo(Vector2 direction)
	{
		lastRotation = direction;
		float angle = Mathf.Atan2(direction.y, direction.x) * 57.29578f - 90f;
		base.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		interpolatedTransform.ForgetPreviousTransforms();
		lastInputMove = direction;
	}

	public void RotateSnapToLastInputMove()
	{
		RotateSnapTo(lastInputMove);
	}

	protected void TiltActorTowardsCamera()
	{
		if (actorState != ActorState.Dead)
		{
			tiltFactor = 1f;
		}
		else if (tiltFactor > 0f && !character.IsSquished)
		{
			tiltFactor -= Time.deltaTime * tiltSpeed;
		}
		else
		{
			tiltFactor = 0f;
		}
		if (actorState != ActorState.Dead || character.hipJoint == null)
		{
			shadow.transform.position = base.transform.position + character.characterOffset + character.shadowOffset;
		}
		else
		{
			Vector3 a = new Vector3(character.hipJoint.transform.position.x, character.hipJoint.transform.position.y, shadow.transform.position.z);
			shadow.transform.position = Vector3.Lerp(a, shadow.transform.position, 0.2f) + character.characterOffset * tiltFactor + character.shadowOffset * tiltFactor;
		}
		if (character.hipJoint != null)
		{
			float num = 8f;
			float num2 = 1.5f * (1f - Mathf.Clamp01((0f - character.hipJoint.position.z + character.hipJointPosZ) / num));
			if (actorState == ActorState.Falling)
			{
				num2 = Mathf.Max(shadow.transform.localScale.x / character.shadowScale - Time.deltaTime * 5f, 0f);
			}
			else if (actorState == ActorState.Dead && (character.characterType == CharacterType.Coffee || character.characterType == CharacterType.Ramen || character.characterType == CharacterType.HotSauce || character.characterType == CharacterType.BubbleTea || character.characterType == CharacterType.SaltAndPepper))
			{
				num2 = 0f;
			}
			shadow.transform.localScale = Vector3.one * num2 * character.shadowScale;
		}
		character.transform.localPosition = base.transform.InverseTransformPoint(base.transform.position + character.characterOffset * tiltFactor);
		character.transform.rotation = Quaternion.Euler(25f * tiltFactor, 0f, base.transform.eulerAngles.z - 90f);
		if (actorState == ActorState.Falling && Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
		{
			character.transform.localPosition += new Vector3(0f, 0.25f, 1f) * Mathf.Max(0f, fallTimer - 0.35f) * 7f;
		}
		actorPFX.rotation = character.transform.rotation;
	}

	public Vector2 GetHazardNormal(Vector2 position, float radius, float circleRadius, out int hits)
	{
		hits = 0;
		int num = 16;
		float num2 = 360 / num;
		Vector2[] array = new Vector2[num];
		Vector2 zero = Vector2.zero;
		for (int i = 0; i < num; i++)
		{
			float num3 = -90f + num2 * (float)i;
			array[i] = position + radius * new Vector2(Mathf.Cos(MathF.PI / 180f * num3), Mathf.Sin(MathF.PI / 180f * num3));
			if (!Physics2D.OverlapCircle(array[i], circleRadius, floorLayers))
			{
				zero += position - array[i];
				hits++;
			}
		}
		if (zero == Vector2.zero)
		{
			return Vector2.zero;
		}
		return zero.normalized;
	}

	public bool IsPositionOverHazard(Vector2 position)
	{
		return Singleton<LevelManager>.Instance.IsPositionOverHazard(position, physicsColliderRadius * 1.15f);
	}

	public bool IsPositionTouchingHazard(Vector2 position, int minContactPoints = 0)
	{
		return Singleton<LevelManager>.Instance.IsPositionTouchingHazard(position, physicsColliderRadius * 1.15f, minContactPoints);
	}

	public bool ActorWillMoveIntoHazard()
	{
		if (Singleton<LevelManager>.Instance.HazardContactPoints((Vector2)base.transform.position + velocity * Time.fixedDeltaTime, physicsColliderRadius * 0.95f, includeObstacles: false) > 0 && Singleton<LevelManager>.Instance.HazardContactPoints(base.transform.position, physicsColliderRadius * 0.95f) < 8 && IsPositionOverHazard((Vector2)base.transform.position + velocity.normalized * physicsColliderRadius * 2f))
		{
			return true;
		}
		return false;
	}

	private IEnumerator FallOffCliff()
	{
		float t = 0f;
		while (t < 0.3f)
		{
			t += Time.deltaTime;
			yield return null;
		}
		t = 0f;
		float dur = 0.45f;
		while (t < dur)
		{
			t += Time.deltaTime;
			character.anim.transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, 5f, t / dur));
			yield return null;
		}
		character.anim.transform.localPosition = Vector3.zero;
	}

	public virtual void OnDamageColliderTriggered(Collider2D collider)
	{
		if (actorState == ActorState.Dead || IsDashing || IsInvulnerable || deflectedColliders.Contains(collider))
		{
			return;
		}
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			Projectile component = collider.GetComponent<Projectile>();
			if (component == null || !(component.owner != this))
			{
				return;
			}
			if (IsAttacking && Vector3.Dot((component.transform.position - base.transform.position).normalized, base.transform.up) > 0f)
			{
				if ((base.transform.position - component.transform.position).magnitude < (component.transform.position - component.owner.transform.position).magnitude)
				{
					component.owner.DeflectColliderImmediately(meleeCollider);
				}
			}
			else if (Singleton<GameManager>.Instance.AreOpponents(this, component) || Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire)
			{
				Disc disc = component as Disc;
				if (disc != null && disc.IsGoldenDisc)
				{
					SetKiller(component.owner, KillerType.GoldenDisc);
				}
				else
				{
					SetKiller(component.owner, KillerType.Disc);
				}
				if (component.movement == Projectile.Movement.Controlled)
				{
					Player player = killer as Player;
					if (player != null)
					{
						player.metadata.ControlledKills++;
					}
				}
				if (!IsFrozen)
				{
					character.Slice((base.transform.position - disc.transform.position).normalized);
				}
				Die(DeathType.disc);
			}
			else if (IsFrozen)
			{
				Die(DeathType.disc);
			}
		}
		else if (collider.CompareTag("MeleeCollider"))
		{
			if ((bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				return;
			}
			if (collider.transform.parent == null)
			{
				Debug.LogWarning("MeleeCollider was missing parent transform");
				return;
			}
			Actor component2 = collider.transform.parent.gameObject.GetComponent<Actor>();
			if (component2 == null)
			{
				Debug.LogWarning("Attacker could not be found on MeleeCollider");
			}
			else
			{
				if ((IsAttacking && Vector3.Dot((component2.transform.position - base.transform.position).normalized, base.transform.up) > 0f) || !component2.HasWeapon())
				{
					return;
				}
				if (Singleton<GameManager>.Instance.AreOpponents(component2, this) || Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire)
				{
					if (!IsFrozen)
					{
						character.Slice(base.transform.position - component2.transform.position);
					}
					SetKiller(component2, KillerType.Melee);
					Die(DeathType.melee);
					component2.GetNotifiedOfSuccessfulMeleeKill();
				}
				else
				{
					SetKiller(component2, KillerType.Push, 2f);
					if (IsFrozen)
					{
						SetKiller(component2, KillerType.Melee);
						Die(DeathType.melee);
						component2.GetNotifiedOfSuccessfulMeleeKill();
					}
				}
			}
		}
		else
		{
			if (!collider.CompareTag("DisarmCollider") || collider.transform.parent == null || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				return;
			}
			Actor component3 = collider.transform.parent.GetComponent<Actor>();
			if (component3 == null || component3.actorState == ActorState.Dead || (bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				return;
			}
			SetKiller(component3, KillerType.Push, 2f);
			Vector3 disarmDirection = -(collider.transform.position - base.transform.position).normalized * 30f * timeScaler;
			if (Singleton<GameManager>.Instance.AreOpponents(component3, this) || Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire)
			{
				if (HasWeapon() && !IsFrozen)
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Disarm, base.transform.position - disarmDirection.normalized, component3.character.Color);
				}
				GetDisarmed(disarmDirection);
			}
			else
			{
				GetStunned(disarmDirection);
			}
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/disarm_hit", base.transform.position);
		}
	}

	public virtual void OnDamageColliderStay(Collider2D collider)
	{
		if (!IsDashing && !IsInvulnerable && (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFire")))
		{
			Explosion component = collider.GetComponent<Explosion>();
			SetKiller(component.owner, KillerType.Explosion);
			Die(DeathType.explosion);
			if (character != null)
			{
				character.LoseCurrentHat((base.transform.position - component.transform.position).normalized);
			}
			character.IsBurnt = true;
			if (IsPositionOverHazard(base.transform.position))
			{
				StartCoroutine(StartFallAfterDeath());
			}
			else if (!isFalling && !character.IsSliced)
			{
				character.anim.SetBool("IsFalling", value: false);
				character.anim.SetBool("IsBurnt", value: true);
			}
		}
		else if (!IsDashing && !IsInvulnerable && collider.CompareTag("ExplosionFreeze"))
		{
			Freeze();
			SetKiller(collider.GetComponent<Explosion>().owner, KillerType.Freeze, freezeDuration + 0.01f);
		}
		if (justBecomeVulernable)
		{
			justBecomeVulernable = false;
			OnDamageColliderTriggered(collider);
		}
	}

	public virtual void OnMeleeColliderTriggered(Collider2D collider)
	{
		if (actorState == ActorState.Dead)
		{
			return;
		}
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer);
		if (collider.CompareTag("InteractiveObstacle"))
		{
			if (raycastHit2D.collider != collider)
			{
				return;
			}
			InteractiveObstacle component = collider.GetComponent<InteractiveObstacle>();
			if (component != null)
			{
				component.Interact((collider.transform.position - base.transform.position).normalized * speedLimit);
				if (component is WobblyObstacle)
				{
					string eventName = "event:/environment/disc_rico_stone";
					switch (component.obstacleMaterial)
					{
					case InteractiveObstacle.ObstacleMaterial.Bamboo:
						eventName = "event:/environment/disc_rico_bamboo";
						break;
					case InteractiveObstacle.ObstacleMaterial.Tree:
						eventName = "event:/environment/disc_rico_tree";
						break;
					case InteractiveObstacle.ObstacleMaterial.Wood:
						eventName = "event:/environment/disc_rico_wood";
						break;
					case InteractiveObstacle.ObstacleMaterial.Shrub:
						eventName = "event:/environment/disc_rico_shrub";
						break;
					}
					Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(eventName, "velocity", 1f, base.transform);
				}
			}
			StopLunge(reduceVelocity: false);
			StartPushback((base.transform.position - collider.transform.position).normalized * 12f, 0.1f);
		}
		if ((bool)raycastHit2D)
		{
			return;
		}
		if (collider.CompareTag("DamageCollider"))
		{
			Actor component2 = collider.transform.parent.GetComponent<Actor>();
			if (component2.IsInvulnerable || !Singleton<GameManager>.Instance.AreOpponents(component2, this))
			{
				StopLunge(reduceVelocity: false);
			}
		}
		else if (collider.CompareTag("Disc") && !deflectedColliders.Contains(collider))
		{
			Disc component3 = collider.GetComponent<Disc>();
			if (component3.DiscOwner == this && !component3.canCollideWithOwner)
			{
				return;
			}
			DeflectCollider(collider);
			if (component3.movement == Projectile.Movement.Dropped || component3.projectileStartedDropped)
			{
				return;
			}
			meleeTimer = 0f;
			attackCooldownTimer = attackCooldownDuration * 1.2f;
			if (attacksLeft > 0)
			{
				attacksLeft = 0;
				isCoolingAttack = true;
				attackCooldownTimer = 0.15f;
			}
			StopLunge();
			CreateImpactLine(collider.transform.position);
			Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(collider.transform.position, Quaternion.LookRotation(Vector3.forward, (collider.transform.position - base.transform.position).normalized));
			Singleton<GameManager>.Instance.newVFXRico.Emit(character.Color, 90f);
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/block_disc", base.transform.position);
			if (component3.aimPower >= 1f)
			{
				Singleton<Camera3D>.Instance.Shake(1f, 0.5f);
				Singleton<TimeKeeper>.Instance.FreezeFrame(0.25f);
				Singleton<TimeKeeper>.Instance.SlowMoEffect(0.3f);
				velocity = Vector2.Reflect(velocityStored, (collider.transform.position - base.transform.position).normalized) * bounciness;
				float num = Mathf.Max(1f / timeScaler * component3.timeScaler, 1f / component3.timeScaler * timeScaler);
				Vector2 direction = -(velocityStored - component3.velocityStored) * 1.2f * num;
				if (direction.magnitude < 1f)
				{
					direction = (base.transform.position - collider.transform.position).normalized * 80f;
				}
				StartPushback(direction, 0.3f, 0.8f);
			}
			else
			{
				Singleton<Camera3D>.Instance.Shake(0.5f, 1f);
				Singleton<TimeKeeper>.Instance.FreezeFrame(0.25f);
				velocity = Vector2.Reflect(velocityStored, (collider.transform.position - base.transform.position).normalized) * bounciness;
			}
		}
		else if ((collider.CompareTag("MeleeCollider") || collider.CompareTag("ShieldCollider")) && (!collider.CompareTag("MeleeCollider") || !(Vector2.Dot((base.transform.position - collider.transform.position).normalized, collider.transform.up) < -0.25f)))
		{
			DeflectCollider(collider);
			meleeTimer = 0f;
			attackCooldownTimer = attackCooldownDuration * 1.2f;
			if (attacksLeft > 0)
			{
				attacksLeft = 0;
				isCoolingAttack = true;
				attackCooldownTimer = 0.15f;
			}
			StopLunge();
			Vector2 direction2 = -(Vector2)((collider.transform.position - base.transform.position).normalized * 24f) * timeScaler;
			velocity = Vector2.zero;
			if (collider.CompareTag("MeleeCollider"))
			{
				direction2 *= 1.5f;
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/block_melee", base.transform.position);
			}
			else if (collider.CompareTag("ShieldCollider"))
			{
				Vector2 vector = (collider.transform.position - base.transform.position).normalized;
				float angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
				Vector2 vector2 = (Vector2)base.transform.position + vector * 2.5f;
				Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(vector2, Quaternion.AngleAxis(angle, Vector3.forward));
				Singleton<GameManager>.Instance.newVFXRico.Emit(character.Color, 90f);
			}
			StartPushback(direction2, 0.4f, 0.4f);
		}
	}

	public virtual void DeflectCollider(Collider2D col, int physicsFrameDelay = 10)
	{
		StartCoroutine(AllowCollisionAfterDelay(col, physicsFrameDelay));
	}

	public virtual void DeflectColliderImmediately(Collider2D col, int physicsFrameDelay = 10)
	{
		deflectedColliders.Add(col);
		DeflectCollider(col, physicsFrameDelay);
	}

	protected IEnumerator AllowCollisionAfterDelay(Collider2D col, int physicsFrameDelay)
	{
		yield return new WaitForFixedUpdate();
		if (!deflectedColliders.Contains(col))
		{
			deflectedColliders.Add(col);
		}
		for (int i = 0; i < physicsFrameDelay - 1; i++)
		{
			yield return new WaitForFixedUpdate();
		}
		if (deflectedColliders.Contains(col))
		{
			deflectedColliders.Remove(col);
		}
	}

	protected void CreateImpactLine(Vector3 location)
	{
		Vector2 vector = (location - base.transform.position).normalized;
		float angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
		Vector2 vector2 = (Vector2)base.transform.position + vector * 2.5f;
		Singleton<GameManager>.Instance.impactLine.Spawn(vector2, Quaternion.AngleAxis(angle, Vector3.forward)).GetComponent<ImpactLine>().Play(character.Color);
	}

	public virtual void OnPickupColliderTriggered(Collider2D collider)
	{
	}

	protected virtual void Update()
	{
		CooldownAttack();
		CooldownStun();
		CooldownShowOff();
		RunKillerTimer();
		if (isFalling)
		{
			if (fallTimer < fallDuration)
			{
				if (timeScaler == 1f && fallTimer < fallAffordance && !IsPositionOverHazard(base.transform.position) && actorState == ActorState.Falling)
				{
					FallSaved();
					Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/footstep", base.transform.position);
				}
				if (fallTimer < fallDuration * 0.15f && fallTimer + Time.deltaTime * timeScaler > fallDuration * 0.15f)
				{
					Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/hop", base.transform.position);
				}
				fallTimer += Time.deltaTime * timeScaler;
			}
			else
			{
				isFalling = false;
				if (actorState == ActorState.Falling)
				{
					Die(DeathType.fall);
				}
				else
				{
					FallInHazard();
				}
			}
		}
		if (isOnFire && burnTimer < burnDuration)
		{
			burnTimer += Time.deltaTime * timeScaler;
			if (IsOnWetFloor || isSlimeTiming)
			{
				ToggleFire(onFire: false, extinguishPFX: true);
			}
		}
		else if (isOnFire)
		{
			if (actorState != ActorState.Dead)
			{
				if (fireOwner != null)
				{
					SetKiller(fireOwner, KillerType.Fire);
				}
				Die(DeathType.fire);
			}
			if (IsPositionOverHazard(base.transform.position))
			{
				StartCoroutine(StartFallAfterDeath());
			}
			else if (!isFalling && !character.IsSliced)
			{
				character.anim.SetBool("IsFalling", value: false);
				character.anim.SetBool("IsBurnt", value: true);
			}
			character.IsBurnt = true;
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/burnt", base.transform.position);
			burnTimer = 0f;
			ToggleFire(onFire: false);
		}
		if (IsFrozen && freezeTimer < freezeDuration)
		{
			freezeTimer += Time.deltaTime;
		}
		else if (IsFrozen)
		{
			Unfreeze();
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/unfreeze", base.transform.position);
		}
		if (IsFrozen && chipTimer > 0f)
		{
			chipTimer -= Time.deltaTime;
		}
		TiltActorTowardsCamera();
	}

	protected virtual void CooldownStun()
	{
		if (IsStunned && stunTimer < pushStunDuration)
		{
			stunTimer += Time.deltaTime * timeScaler;
		}
		else if (IsStunned)
		{
			IsStunned = false;
		}
	}

	protected virtual void CooldownShowOff()
	{
		if (isShowingOff && showOffTimer < showOffDuration)
		{
			showOffTimer += Time.deltaTime * timeScaler;
		}
		else
		{
			isShowingOff = false;
		}
	}

	public virtual void SetKiller(Actor killer, KillerType type = KillerType.Suicide, float duration = 0f)
	{
		this.killer = killer;
		killerType = type;
		killerTimer = duration;
	}

	protected virtual void RunKillerTimer()
	{
		if (killer != null)
		{
			if (killerTimer > 0f)
			{
				killerTimer -= Time.deltaTime * timeScaler;
				return;
			}
			SetKiller(null);
			killerTimer = 0f;
		}
	}

	public virtual bool Dash()
	{
		if (actorState == ActorState.Falling && fallTimer > fallAffordance)
		{
			return false;
		}
		if (IsFrozen)
		{
			ChipOutOfFrozen();
			return false;
		}
		if (!canDash || IsStunned || isLunging || isShowingOff)
		{
			return false;
		}
		if (actorState == ActorState.Falling)
		{
			if (fallsSaved != 0)
			{
				return false;
			}
			FallSaved();
		}
		measuringTape = base.transform.position;
		if (inputMove.magnitude > 0f)
		{
			dashDirection = inputMove.normalized;
		}
		else
		{
			dashDirection = lastRotation.normalized;
		}
		canWalk = false;
		canRotate = false;
		canDash = false;
		StopAiming();
		dashTimer = 0f;
		dashCooldownTimer = dashCooldown;
		dashForce = dashSpeed;
		if (IsSlimed)
		{
			dashCooldownTimer *= 1.5f;
		}
		stopDashOnHazards = !IsDashSafe(dashDirection, andNoObstaclesInTheWay: false, allowOutOfBounds: true, allowHazardInFrontOfEndPoint: true, debugGizmos: false);
		IsDashing = true;
		velocity += speedLimit * 1.5f * dashDirection * timeScaler;
		velocityStored = velocity * Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);
		RotateSnapTo(dashDirection);
		dashTrail.Play();
		return true;
	}

	protected virtual Vector2 CalculateDash()
	{
		if (dashTimer < dashDuration)
		{
			ParticleSystem.MainModule main = dashTrail.main;
			main.startRotation = (360f - base.transform.rotation.eulerAngles.z) * (MathF.PI / 180f);
			currentDashAmt = Mathf.Lerp(dashForce, 0f, Easing.EaseIn(dashTimer / dashDuration));
			dashTimer += Time.fixedDeltaTime * timeScaler;
			return dashDirection * currentDashAmt;
		}
		if (IsDashing)
		{
			if (!IsPositionOverHazard(base.transform.position))
			{
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/dash_end", base.transform.position);
			}
			IsDashing = false;
			dashTimer = dashDuration;
			if (fallsSaved > 0 && !IsPositionOverHazard(base.transform.position))
			{
				fallsSaved = 0;
			}
		}
		return Vector2.zero;
	}

	protected void CooldownDash()
	{
		if (dashCooldownTimer > 0f)
		{
			dashCooldownTimer -= Time.fixedDeltaTime * timeScaler;
			if (dashCooldownTimer > dashCooldownMoveThreshold * dashCooldown)
			{
				canWalk = false;
				canRotate = false;
			}
			else if (dashCooldownTimer < dashDuration)
			{
				canWalk = true;
				canRotate = true;
				canDash = true;
			}
		}
		else
		{
			dashCooldownTimer = 0f;
		}
	}

	protected void StopDash(bool rechargeDash = false, bool reduceVelocity = false)
	{
		character.anim.SetBool("IsDashing", value: false);
		if (dashTimer < dashDuration)
		{
			dashCooldownTimer -= dashDuration - dashTimer;
			dashTimer = dashDuration;
		}
		if (reduceVelocity)
		{
			velocity = -0.5f * Vector2.ClampMagnitude(velocity, speedLimit * 0.5f);
		}
		if (rechargeDash)
		{
			dashCooldownTimer = 0f;
			if (!isLunging)
			{
				canWalk = true;
				canRotate = true;
				canDash = true;
			}
		}
	}

	protected virtual void StopDashOnHazards()
	{
		if (stopDashOnHazards && !canWalk && !IsStunned && dashTimer > dashDuration * 0.5f && ActorWillMoveIntoHazard())
		{
			StopDash(rechargeDash: false, reduceVelocity: true);
		}
	}

	public void ReverseDash()
	{
		dashDirection = -dashDirection;
		IsDashing = true;
		velocity = speedLimit * 1.5f * dashDirection * timeScaler;
		velocityStored = velocity * Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);
		RotateSnapTo(dashDirection);
		if (dashTimer > dashDuration * 0.5f)
		{
			dashTimer = dashDuration * 0.5f;
		}
	}

	public virtual bool IsDashSafe(Vector2 dashDirection, bool andNoObstaclesInTheWay = false, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool debugGizmos = true)
	{
		return IsDashSafeFromPosition(base.transform.position, dashDirection, andNoObstaclesInTheWay, allowOutOfBounds, allowHazardInFrontOfEndPoint, debugGizmos);
	}

	public virtual bool IsDashSafeFromPosition(Vector2 startingPosition, Vector2 dashDirection, bool andNoObstaclesInTheWay = false, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool debugGizmos = true)
	{
		dashDirection = dashDirection.normalized;
		if (slideFactor < 1f)
		{
			return false;
		}
		Vector2 vector = velocity * Mathf.Pow(Mathf.Clamp01(1f - drag * Time.fixedDeltaTime), dashDuration / Time.fixedDeltaTime) * 0.8f;
		Vector2 vector2 = startingPosition + dashDirection * dashDistanceInvulnerable + vector;
		Vector2 vector3 = startingPosition + dashDirection * dashDistance + vector;
		MovingFloor movingFloor = null;
		if ((IsPointOnMovingFloor(vector2, ref movingFloor) || IsPointOnMovingFloor(vector3, ref movingFloor)) && movingFloor != null && movingFloor.transform != base.transform.parent)
		{
			vector2 -= movingFloor.velocity * dashDuration;
			vector3 -= movingFloor.velocity * dashDuration;
		}
		if (base.transform.parent != null)
		{
			movingFloor = base.transform.parent.GetComponent<MovingFloor>();
			if (movingFloor != null)
			{
				float num = 1f;
				RaycastHit2D raycastHit2D = Physics2D.Raycast(vector2, (startingPosition - vector2).normalized, (startingPosition - vector2).magnitude, floorLayers);
				if ((bool)raycastHit2D && raycastHit2D.collider == movingFloor.collider)
				{
					num = (raycastHit2D.point - startingPosition).magnitude / (vector2 - startingPosition).magnitude;
				}
				vector2 += movingFloor.velocity * dashDuration * num;
				vector3 += movingFloor.velocity * dashDuration * num;
			}
		}
		RaycastHit2D raycastHit2D2 = Physics2D.CircleCast(startingPosition, physicsColliderRadius, dashDirection, (vector3 - startingPosition).magnitude, obstacleLayer);
		if ((bool)raycastHit2D2)
		{
			Player player = this as Player;
			if (player != null && player.activePowerup.HasPowerup(PowerupType.DashThroughWalls))
			{
				if (andNoObstaclesInTheWay)
				{
					if (debugGizmos)
					{
						Debug.DrawLine(vector2, vector3, Color.red, 3f);
					}
					return false;
				}
			}
			else
			{
				if (andNoObstaclesInTheWay && (raycastHit2D2.point - vector3).magnitude > dashDistance * 0.3f)
				{
					if (debugGizmos)
					{
						Debug.DrawLine(vector2, vector3, Color.red, 3f);
					}
					return false;
				}
				if (!Physics2D.OverlapPoint(raycastHit2D2.point, floorLayers) || !Physics2D.OverlapPoint(raycastHit2D2.point - dashDirection.normalized, floorLayers))
				{
					if (debugGizmos)
					{
						Debug.DrawLine(vector2, vector3, Color.red, 3f);
					}
					return false;
				}
				Vector2 vector4 = vector2 - startingPosition;
				Vector2 vector5 = raycastHit2D2.point - Vector2.Lerp(-raycastHit2D2.normal, velocity.normalized, 0.5f) * physicsColliderRadius * 1.05f;
				Vector2 vector6 = vector5 - startingPosition;
				Vector2 vector7 = vector4 - vector6;
				Vector2 vector8 = vector7 - raycastHit2D2.normal * Vector3.Dot(vector7, raycastHit2D2.normal);
				vector2 = vector5 + vector8;
				vector3 = vector2 + vector8.normalized * (dashDistance - dashDistanceInvulnerable);
			}
		}
		else
		{
			raycastHit2D2 = Physics2D.CircleCast(startingPosition, physicsColliderRadius, dashDirection, (vector3 - startingPosition).magnitude, obstacleLayer);
			if ((bool)raycastHit2D2)
			{
				vector3 = raycastHit2D2.point + Vector2.Reflect(dashDirection, raycastHit2D2.normal) * physicsColliderRadius * 1.5f;
			}
		}
		Vector2 position = (vector2 + vector3) * 0.5f;
		if (!allowOutOfBounds && (!Singleton<LevelManager>.Instance.IsInBounds(vector2) || !Singleton<LevelManager>.Instance.IsInBounds(vector3)))
		{
			return false;
		}
		if (!allowHazardInFrontOfEndPoint && IsPositionOverHazard(vector3 + dashDirection))
		{
			if (debugGizmos)
			{
				Debug.DrawLine(vector2, vector3, Color.yellow, 3f);
			}
			return false;
		}
		if (!IsPositionOverHazard(vector2) && !IsPositionOverHazard(position) && !IsPositionOverHazard(vector3))
		{
			if (debugGizmos)
			{
				Debug.DrawLine(vector2, vector3, Color.green, 3f);
			}
			return true;
		}
		if (debugGizmos)
		{
			Debug.DrawLine(vector2, vector3, Color.red, 3f);
		}
		return false;
	}

	protected bool IsPointOnMovingFloor(Vector2 position, ref MovingFloor movingFloor)
	{
		Collider2D collider2D = Physics2D.OverlapCircle(position, physicsColliderRadius, floorLayers);
		if ((bool)collider2D && collider2D.CompareTag("MovingFloor"))
		{
			movingFloor = collider2D.GetComponent<MovingFloor>();
			if (movingFloor == null)
			{
				return false;
			}
			return true;
		}
		movingFloor = null;
		return false;
	}

	protected virtual void InterruptAttack()
	{
		IsAttacking = false;
	}

	protected IEnumerator InterruptAttackDelayed()
	{
		yield return null;
		yield return null;
		InterruptAttack();
	}

	protected virtual void InterruptAttackAnimation()
	{
	}

	protected virtual void CooldownAttack()
	{
		if (attackCooldownTimer > 0f)
		{
			attackCooldownTimer -= Time.deltaTime * timeScaler;
		}
		else
		{
			attackCooldownTimer = 0f;
			if (isCoolingAttack)
			{
				isCoolingAttack = false;
			}
		}
		if (meleeTimer <= 0f)
		{
			meleeTimer = 0f;
			IsAttacking = false;
		}
		else
		{
			meleeTimer -= Time.deltaTime * timeScaler;
		}
		if (lungeCooldownTimer > 0f)
		{
			lungeCooldownTimer -= Time.deltaTime * timeScaler;
			canWalk = false;
			canRotate = false;
			return;
		}
		lungeCooldownTimer = 0f;
		if (isLunging)
		{
			canWalk = true;
			canRotate = true;
			dashTrail.Stop();
		}
		isLunging = false;
	}

	protected virtual Vector2 CalculateLunge()
	{
		if (lungeTimer > 0f)
		{
			float num = Mathf.Lerp(0f, lungeSpeed, Easing.CubicIn(lungeTimer / lungeDuration));
			if (IsSlimed)
			{
				num *= slimedFactor;
			}
			lungeTimer -= Time.fixedDeltaTime * timeScaler;
			return lungeDirection * num;
		}
		return Vector2.zero;
	}

	protected virtual void StopLunge(bool reduceVelocity = true)
	{
		if (lungeTimer > 0f)
		{
			lungeTimer = 0f;
			if (reduceVelocity)
			{
				velocity = Vector2.ClampMagnitude(velocity, speedLimit * timeScaler) * 0.5f;
				velocity *= 0.3f;
			}
		}
	}

	protected void StopLungeOnHazards()
	{
		if (isLunging && stopLungeOnHazards && ActorWillMoveIntoHazard())
		{
			StopLunge();
		}
	}

	public bool IsLungeSafe(Vector2 attackDirection)
	{
		Vector2 position = (Vector2)base.transform.position + attackDirection * attackDistance;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, attackDirection, attackDistance, 1 << LayerMask.NameToLayer("Obstacle"));
		if ((bool)raycastHit2D)
		{
			position = raycastHit2D.point;
		}
		if (!IsPositionOverHazard(position))
		{
			return true;
		}
		return false;
	}

	public virtual void Aim()
	{
		if (aimTimer >= aimStartTime)
		{
			if (!IsAiming)
			{
				IsAiming = true;
				aimAssistant.SetActive(value: true);
				aimAssistantRenderer.material.color = ColorExt.SetAlpha(character.Color, 0f);
			}
			float t = Mathf.Clamp01((aimTimer - aimStartTime) / aimDuration);
			aimPower = Mathf.Clamp01(Mathf.Lerp(0f, aimMaxPower, Easing.EaseIn(t)));
			aimAssistant.transform.localPosition = Vector2.Lerp(aimAssistant.transform.localPosition, Vector2.Lerp(aimAssistantStartPos, aimAssistantEndPos, Easing.EaseIn(t)), Time.deltaTime * timeScaler * 20f);
			aimAssistant.transform.localPosition += new Vector3(UnityEngine.Random.Range(-0.075f, 0.075f), UnityEngine.Random.Range(-0.1f, 0.1f), 0f) * Easing.EaseIn(t);
			aimAssistant.transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 1.5f, t);
			if (aimTimer < aimDuration)
			{
				ColorExt.SetAlpha(aimAssistantRenderer.material, Mathf.Lerp(0.4f, 1f, t));
			}
			else
			{
				ColorExt.SetAlpha(aimAssistantRenderer.material, 1f);
			}
			RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, Projectile.projectileColliderRadius, base.transform.up, (aimAssistant.transform.position - base.transform.position).magnitude + 0.5f, obstacleLayer);
			if ((bool)raycastHit2D)
			{
				if (base.transform.InverseTransformPoint(raycastHit2D.point).magnitude > aimAssistantStartPos.magnitude)
				{
					aimAssistant.transform.position = raycastHit2D.point - (Vector2)base.transform.up * 0.7f;
				}
				else
				{
					aimAssistant.transform.localPosition = aimAssistantStartPos - aimAssistantStartPos.normalized * 0.6f;
				}
			}
		}
		else
		{
			ColorExt.SetAlpha(aimAssistantRenderer.material, 0f);
		}
	}

	public virtual void StopAiming()
	{
		if (IsAiming && inputMove.magnitude > 0f)
		{
			aimMoveLag = 1f;
		}
		IsAiming = false;
		aimTimer = 0f;
		aimPower = 0f;
		if (aimAssistant != null)
		{
			aimAssistant.transform.localPosition = aimAssistantStartPos;
			aimAssistantRenderer.material.color = new Color(0f, 0f, 0f, 0f);
			aimAssistant.SetActive(value: false);
		}
	}

	public virtual bool Attack()
	{
		if (IsStunned || isShowingOff || IsAiming)
		{
			return false;
		}
		if (IsFrozen)
		{
			ChipOutOfFrozen();
			return true;
		}
		if (attacksLeft > 0 || (!HasWeapon() && CanDisarm()))
		{
			if (IsDashing)
			{
				StopDash();
			}
			isCoolingAttack = true;
			attackCooldownTimer = attackCooldownDuration;
			meleeTimer = attackDuration;
			isLunging = true;
			lungeCooldownTimer = lungeCooldownDuration;
			lungeTimer = lungeDuration;
			lungeDirection = (canWalk ? lastInputMove.normalized : lastRotation.normalized);
			RotateSnapTo(lungeDirection);
			stopLungeOnHazards = !IsLungeSafe(lungeDirection);
			velocity += speedLimit * 0.66f * lungeDirection * timeScaler * slideFactor;
			velocityStored = velocity * Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);
			character.anim.SetFloat("AttackSpeed", 1f);
			if (HasWeapon())
			{
				IsAttacking = true;
				UpdateDustTrailColor();
				dustTrailMain.startSizeMultiplier = 2f;
				dustTrailMain.startSpeedMultiplier = 2f;
				dustTrail.Emit(3);
				dustTrailMain.startSizeMultiplier = 1f;
				dustTrailMain.startSpeedMultiplier = 1f;
				if (attacksLeft == 1)
				{
					character.anim.SetTrigger("AttackRight");
				}
				else if (attacksLeft == 2)
				{
					character.anim.SetTrigger("AttackLeft");
				}
				attacksLeft--;
			}
			return true;
		}
		return false;
	}

	protected virtual bool HasWeapon()
	{
		return false;
	}

	protected virtual bool CanDisarm()
	{
		return false;
	}

	protected virtual void GetNotifiedOfSuccessfulMeleeKill()
	{
	}

	public virtual void GetDisarmed(Vector3 disarmDirection)
	{
		GetStunned(disarmDirection, GetDisarmedStunDuration());
	}

	protected virtual float GetDisarmedStunDuration()
	{
		if (IsFrozen || HasWeapon())
		{
			return 0.8f;
		}
		return 0.5f;
	}

	protected virtual void GetStunned(Vector3 disarmDirection, float stunDuration = 0.66f)
	{
		InterruptAttack();
		InterruptAttackAnimation();
		Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.LookRotation(Vector3.forward, disarmDirection.normalized));
		Singleton<GameManager>.Instance.newVFXRico.Emit(character.Color, 80f);
		StopAiming();
		velocity = Vector3.zero;
		StartPushback(disarmDirection, 0.5f, IsStunned ? 0f : stunDuration);
	}

	protected virtual void ToggleFire(bool onFire, bool extinguishPFX = false)
	{
		if (extinguishPFX && isOnFire && !onFire)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SteamBurst, base.transform.position, base.transform);
			if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
			{
				Debug.Log("EXTINGUISH");
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/fire_extinguish", base.transform.position);
			}
		}
		if (onFire && IsOnWetFloor)
		{
			return;
		}
		if (onFire)
		{
			if (onFirePFX != null)
			{
				onFirePFX.gameObject.SetActive(value: true);
				onFirePFX.Play();
			}
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/fire_ignite", base.transform.position);
			fireAudioLoop = Singleton<AudioManager>.Instance.PlayLooping("event:/combat/fire_loop", base.transform);
		}
		else
		{
			if (onFirePFX != null && !extinguishPFX)
			{
				onFirePFX.gameObject.SetActive(value: false);
			}
			if (onFirePFX != null)
			{
				onFirePFX.Stop();
			}
			if (fireAudioLoop.isValid())
			{
				Singleton<AudioManager>.Instance.StopLooping(fireAudioLoop);
			}
		}
		isOnFire = onFire;
		burnTimer = 0f;
	}

	protected virtual void Freeze()
	{
		if (Vector2.Dot(base.transform.up, new Vector2(0f, -1f)) < 0.2f)
		{
			RotateSnapTo(new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), -1f).normalized);
		}
		IsFrozen = true;
		freezeTimer = 0f;
		chipTimer = 0f;
		ToggleFire(onFire: false, extinguishPFX: true);
		Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/ice_freeze", base.gameObject);
	}

	protected virtual void Unfreeze()
	{
		frozenParticles.Play();
		IsFrozen = false;
		freezeTimer = 0f;
		IsSlimed = false;
		isSlimeTiming = false;
		slimeTimer = 0f;
	}

	protected virtual void ChipOutOfFrozen()
	{
		if (!(chipTimer > 0f))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/hop", base.transform.position);
			velocity += new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * slideFactor;
			character.anim.SetTrigger("ChipOut");
			frozenBlockAnimator.SetTrigger("Jiggle");
			chipTimer = 0.2f;
			freezeTimer += freezeDuration / 12f;
		}
	}

	protected virtual void FallInHazard()
	{
		IsFrozen = false;
		character.anim.SetBool("IsFalling", value: false);
		character.FallInHazard();
		physicsCollider.enabled = false;
		DisableColliders();
		ToggleFire(onFire: false, extinguishPFX: true);
		if (Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.CloudPuff, character.transform.position + character.transform.TransformDirection(new Vector3(0f, 0f, 8f)));
			return;
		}
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splash, base.transform.position);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.WaterBubbles, base.transform.position);
	}

	public virtual void GetSquished()
	{
		GetExploded();
		physicsCollider.enabled = false;
	}

	protected virtual void GetExploded()
	{
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splatter, base.transform.position, character.Color);
		character.IsSquished = true;
		RotateSnapTo(new Vector2(0f, -1f));
		PrintSplatDecalsOnFloor();
	}

	protected void PrintSplatDecalsOnFloor()
	{
		for (int i = 0; i < 3; i++)
		{
			Vector3 vector = base.transform.position + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f), 0f);
			if (!Physics2D.OverlapPoint(vector, obstacleLayer))
			{
				MovingFloor movingFloor = null;
				if (IsPointOnMovingFloor(vector, ref movingFloor))
				{
					Singleton<DecalManager>.Instance.SpawnSplatDecal(movingFloor.transform, vector, Quaternion.identity, character.Color);
				}
				else
				{
					Singleton<DecalManager>.Instance.SpawnSplatDecal(null, vector, Quaternion.identity, character.Color);
				}
			}
		}
	}

	protected virtual IEnumerator TemporaryInvulnerability(float duration)
	{
		IsInvulnerable = true;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		IsInvulnerable = false;
		justBecomeVulernable = true;
	}

	public virtual void Die(DeathType deathType)
	{
		StopAiming();
		IsStunned = false;
		isShowingOff = false;
		IsSlimed = false;
		slimeTimer = 0f;
		isSlimeTiming = false;
		if (character.anim.isActiveAndEnabled)
		{
			character.anim.SetBool("IsFrozen", value: false);
			character.anim.SetFloat("MoveSpeed", 0f);
		}
		InterruptAttack();
		IsDashing = false;
		velocity = Vector2.zero;
		if (actorState == ActorState.Dead || Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu || Singleton<GameManager>.Instance.gameState == GameManager.GameState.Loading)
		{
			return;
		}
		actorState = ActorState.Dead;
		DisableColliders();
		base.gameObject.layer = LayerMask.NameToLayer("PlayerInsides");
		switch (deathType)
		{
		case DeathType.fall:
			FallInHazard();
			break;
		case DeathType.squished:
		case DeathType.telefrag:
			GetSquished();
			break;
		case DeathType.explosion:
			GetExploded();
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/exploded", base.transform.position);
			break;
		case DeathType.fire:
			if (character.hipJoint != null)
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.FireBurst, character.hipJoint.position, character.hipJoint);
			}
			else
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.FireBurst, base.transform.position);
			}
			break;
		default:
			if (!(this is Decoy))
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splatter, base.transform.position, character.Color, base.transform);
				if (deathType == DeathType.outofbounds)
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.OOBBurst, base.transform.position, character.Color);
					physicsCollider.enabled = false;
					character.IsVisible = false;
					Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/outofbounds", base.transform.position);
					character.LoseCurrentHat(-velocityStored.normalized);
				}
			}
			break;
		}
		if (this is Player)
		{
			Singleton<Camera3D>.Instance.Shake(0.4f, 0.4f);
		}
		if (IsFrozen && !(this is Decoy))
		{
			character.IsVisible = false;
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/shatter", base.transform.position);
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splatter, base.transform.position, character.Color, base.transform);
			character.LoseCurrentHat(UnityEngine.Random.insideUnitCircle);
		}
		InterruptAttackAnimation();
		dashTrail.Stop();
		NotifyPlantsOfDeath();
		StopAllSoundLoops();
	}

	public virtual void DisableColliders()
	{
		damageCollider.enabled = false;
		InterruptAttack();
	}

	protected void NotifyPlantsOfDeath()
	{
		Collider2D[] array = Physics2D.OverlapCircleAll(base.transform.position, physicsColliderRadius * 3f, 1 << LayerMask.NameToLayer("Plant"));
		for (int i = 0; i < array.Length; i++)
		{
			Plant component = array[i].GetComponent<Plant>();
			if (component != null)
			{
				component.DetectDeath(base.transform.position);
			}
		}
	}

	public virtual void StartFall()
	{
		character.anim.SetBool("IsStunned", value: false);
		character.anim.SetBool("IsFrozen", value: false);
		character.anim.SetBool("IsFalling", value: true);
		if (base.transform.parent != null)
		{
			MovingFloor component = base.transform.parent.GetComponent<MovingFloor>();
			if (component != null)
			{
				velocity += component.velocity * 12f * Mathf.Clamp01(Vector3.Dot(component.velocity.normalized, velocity.normalized)) * slideFactor;
			}
			else
			{
				velocity = Vector2.ClampMagnitude(velocity, speedLimit * 0.33f);
			}
		}
		else if (!IsPositionOverHazard((Vector2)base.transform.position - velocityStored.normalized) && IsPositionOverHazard((Vector2)base.transform.position + velocityStored.normalized * 2f))
		{
			velocity = velocity.normalized * speedLimit * 0.6f;
		}
		else
		{
			velocity = Vector2.ClampMagnitude(velocity, speedLimit * 0.33f);
		}
		fallTimer = 0f;
		actorState = ActorState.Falling;
		slideFactor = 1f;
		isFalling = true;
		if (Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
		{
			fallDuration = 0.74f;
		}
		else
		{
			fallDuration = 0.66f;
		}
		RotateSnapTo(new Vector2(0f, -1f).normalized);
	}

	protected virtual void FallSaved()
	{
		if (!character.IsSliced && fallsSaved <= 0)
		{
			character.anim.SetBool("IsFalling", value: false);
			if (IsFrozen)
			{
				character.anim.SetBool("IsFrozen", value: true);
			}
			actorState = ActorState.Alive;
			isFalling = false;
			fallsSaved++;
		}
	}

	protected IEnumerator StartFallAfterDeath()
	{
		character.anim.SetBool("IsFalling", value: true);
		yield return new WaitForSeconds(fallDuration);
		FallInHazard();
	}

	public void Evaporate()
	{
		if (!character.IsSquished)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SpawnBurst, base.transform.position);
			character.EvaporateSlice();
		}
		physicsCollider.enabled = false;
		character.IsVisible = false;
		UnparentFromMovingFloor();
	}

	public virtual void DisableActor()
	{
		character.Reset();
		velocity = Vector2.zero;
		physicsCollider.enabled = false;
		DisableColliders();
		base.transform.SetParent(null, worldPositionStays: true);
		interpolatedTransform.ForgetPreviousTransforms();
		StopAllSoundLoops();
	}

	public void EmitFootstep(int quanitity = 1, float scale = 1f, bool withSound = true)
	{
		if (IsDashing)
		{
			return;
		}
		if (IsOnWetFloor)
		{
			EmitSlidingPFX();
			return;
		}
		UpdateDustTrailColor();
		dustTrailMain.startSizeMultiplier = scale;
		dustTrailMain.startSpeedMultiplier = scale;
		dustTrail.Emit(quanitity);
		if (withSound)
		{
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/combat/footstep", "pitch", (float)playerID * 2f, base.transform);
		}
	}

	protected void UpdateDustTrailColor()
	{
		if (!(Singleton<LevelManager>.Instance.level == null) && !(dustTrail == null))
		{
			dustTrailMain.startColor = Singleton<ColorManager>.Instance.GetDustColorAtPosition(base.transform.position);
		}
	}

	public void EmitSlidingPFX()
	{
		if (!IsDashing)
		{
			if (Singleton<LevelManager>.InstanceExists && Singleton<LevelManager>.Instance.level != null)
			{
				slidingPFXMain.startColor = Singleton<LevelManager>.Instance.level.slidingColor;
			}
			slidingPFXMain.startSpeed = Mathf.Max(Easing.EaseOut(velocity.magnitude / speedLimit) * 8f, 3f);
			timeOfLastSlidingPFX = Singleton<TimeKeeper>.Instance.CurrentTime;
			slidingPFX.Emit(UnityEngine.Random.Range(1, 2));
		}
	}

	protected virtual void StopAllSoundLoops()
	{
		if (fireAudioLoop.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(fireAudioLoop);
		}
		if (wetFloorAudioLoop.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(wetFloorAudioLoop);
		}
	}

	private void OnDisable()
	{
		StopAllSoundLoops();
	}

	public bool IsInLineOfSight(Vector3 targetPos)
	{
		if ((bool)Physics2D.Raycast(base.transform.position, (targetPos - base.transform.position).normalized, (targetPos - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle")))
		{
			return false;
		}
		return true;
	}

	public bool IsOnCamera()
	{
		Vector3 vector = Singleton<Camera3D>.Instance.gameCamera.WorldToViewportPoint(base.transform.position);
		if (vector.x > 0f && vector.x < 1f && vector.y > 0f)
		{
			return vector.y < 1f;
		}
		return false;
	}
}
