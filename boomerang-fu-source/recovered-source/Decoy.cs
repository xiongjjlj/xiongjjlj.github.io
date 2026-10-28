using System.Collections;
using UnityEngine;

public class Decoy : Actor
{
	public PlayerType playerType;

	[Header("Player input")]
	public DecoyInput decoyInput;

	[Header("Player appearance")]
	private Animator outOfBoundsEffect;

	public OutOfBoundsArrow outOfBoundsArrow;

	public Animator shieldAnimator;

	public GameObject shield;

	[SerializeField]
	private bool _isShielded;

	[Header("Player states")]
	[SerializeField]
	private bool _isHidden;

	[SerializeField]
	private int _hasDiscs;

	private static float combatDelayAtStart = 1.2f;

	public static float outOfBoundsDuration = 3f;

	[Header("Player combat")]
	public float outOfBoundsTimer;

	public bool isOutOfBounds;

	[Header("Powerups")]
	[BitMask(typeof(PowerupType))]
	public PowerupType activePowerup;

	private IEnumerator respawnDiscRoutine;

	public ParticleSystem moveFasterPFX;

	public Player owner;

	public float spawnedTimestamp;

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
			_isShielded = value;
		}
	}

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
			}
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

	public void SetupCharacter(Character setCharacter)
	{
		if (character != null)
		{
			Object.Destroy(character.gameObject);
		}
		character = Object.Instantiate(Singleton<GameManager>.Instance.GetCharacterPrefab(setCharacter.characterType), appearance);
		character.actor = this;
		if (setCharacter.CurrentHat != null && setCharacter.CurrentHat.hatType == HatType.Crown)
		{
			character.equippedHat = HatType.Crown;
		}
		else
		{
			character.equippedHat = setCharacter.equippedHat;
		}
		character.InitCharacter(Character.CharacterMode.Gameplay);
	}

	protected override void Start()
	{
		decoyInput = GetComponent<DecoyInput>();
		base.Start();
	}

	public void Init(int startDiscs = 0, bool resetPowerups = false, bool respawn = false, bool spawnNewDisc = true)
	{
		base.Init();
		collisionLayer = obstacleLayer;
		damageCollider.enabled = true;
		base.gameObject.layer = LayerMask.NameToLayer("Player");
		canWalk = true;
		canRotate = true;
		attacksLeft = HasDiscs;
		timeScaler = 1f;
		StopAiming();
		if (respawnDiscRoutine != null)
		{
			StopCoroutine(respawnDiscRoutine);
		}
		StopAllCoroutines();
		HasDiscs = startDiscs;
		character.SetMeleeDiscs(HasDiscs);
		SetAppearanceColors(character.Color);
		character.anim.SetBool("IsRetrieving", value: false);
		character.IsVisible = true;
		IsShielded = activePowerup.HasPowerup(PowerupType.Shield);
		Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/decoy_spawn", base.gameObject);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DecoyPuff, base.transform.position, character.Color);
		spawnedTimestamp = Singleton<TimeKeeper>.Instance.CurrentTime;
	}

	protected override void FixedUpdate()
	{
		inputMove = decoyInput.inputMove;
		base.FixedUpdate();
	}

	protected override void ToggleFire(bool onFire, bool extinguishPFX = false)
	{
		if (onFire)
		{
			Die(DeathType.outofbounds);
		}
		else
		{
			base.ToggleFire(onFire, extinguishPFX);
		}
	}

	protected override void Update()
	{
		base.Update();
		timeScaler = 1f;
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && actorState != ActorState.Dead)
		{
			CheckIfPlayerIsInBounds();
		}
		else if ((Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu || actorState == ActorState.Dead) && isOutOfBounds)
		{
			isOutOfBounds = false;
			StopOutOfBoundsEffect();
		}
		if (actorState == ActorState.Alive && activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			moveFasterPFX.gameObject.SetActive(value: true);
		}
		else
		{
			moveFasterPFX.gameObject.SetActive(value: false);
		}
		if (isWading && velocity.magnitude < 12f && !IsAttacking)
		{
			IsHidden = true;
		}
		else
		{
			IsHidden = false;
		}
		character.anim.speed = timeScaler;
	}

	protected override float GetMoveSpeed()
	{
		float num = walkSpeed;
		num = walkSpeed;
		if (activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			num *= 1.4f;
		}
		if (isWading)
		{
			num *= wadeFactor;
		}
		return num * Mathf.Clamp(timeScaler, 1f, 1.75f);
	}

	public override bool Dash()
	{
		if (actorState == ActorState.Falling && fallTimer > fallAffordance)
		{
			return false;
		}
		if (base.Dash())
		{
			character.SetFacialExpression(FacialExpression.aggressive);
			return true;
		}
		return false;
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		if (actorState == ActorState.Dead)
		{
			return;
		}
		if (collider.CompareTag("Player"))
		{
			Actor component = collider.GetComponent<Actor>();
			Player player = component as Player;
			if (component.actorState == ActorState.Dead || (owner != null && player == owner))
			{
				return;
			}
			if (!IsDashing)
			{
				Die(DeathType.outofbounds);
				return;
			}
		}
		base.CollisionEnter2D(normal, collider);
	}

	protected override void PenetrateActors()
	{
		if (actorState != ActorState.Alive || IsDashing || Singleton<TimeKeeper>.Instance.TimeSince(spawnedTimestamp) <= 0.4f)
		{
			return;
		}
		int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, physicsColliderRadius, touchingObstacles, playerLayer);
		for (int i = 0; i < num; i++)
		{
			if (!(touchingObstacles[i].transform != base.transform))
			{
				continue;
			}
			Player component = touchingObstacles[i].transform.GetComponent<Player>();
			if (component != null && component == owner)
			{
				continue;
			}
			if (Singleton<GameManager>.Instance.activeDecoys.Count > 1)
			{
				Decoy component2 = touchingObstacles[i].transform.GetComponent<Decoy>();
				if (component2 != null)
				{
					component2.Die(DeathType.outofbounds);
				}
			}
			if (component != null)
			{
				SetKiller(component);
			}
			Die(DeathType.outofbounds);
		}
	}

	protected override void GetExploded()
	{
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
			}
			else if (outOfBoundsTimer <= 0f)
			{
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

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		if (canUseTeleportal && collider.CompareTag("Teleportal"))
		{
			moveFasterPFX.gameObject.SetActive(value: false);
		}
		base.OnTriggerEnter2D(collider);
	}

	protected override bool HasWeapon()
	{
		return HasDiscs > 0;
	}

	public override void GetDisarmed(Vector3 disarmDirection)
	{
		base.GetDisarmed(disarmDirection);
	}

	public override void CheckForSquishing(ref Vector3 newPosition, int recursionDepth = 0)
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
			touchingObstacles[i].Distance(physicsCollider);
			if (!touchingObstacles[i].CompareTag("MovingObstacle"))
			{
				continue;
			}
			movingObstacle = touchingObstacles[i].GetComponent<MovingObstacle>();
			if (movingObstacle.movementState == MovingObstacle.MovementState.Moving)
			{
				if (movingObstacle.moveOwner != null)
				{
					SetKiller(movingObstacle.moveOwner, KillerType.MovingObstacle, 1.5f);
				}
				Die(DeathType.outofbounds);
			}
		}
	}

	public override void OnDamageColliderTriggered(Collider2D collider)
	{
		if (actorState == ActorState.Dead || IsDashing)
		{
			return;
		}
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			Projectile component = collider.GetComponent<Projectile>();
			if (!(component == null))
			{
				SetKiller(component.owner, KillerType.Disc);
				Die(DeathType.disc);
			}
		}
		else
		{
			if ((!collider.CompareTag("MeleeCollider") && !collider.CompareTag("DisarmCollider")) || (bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer))
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
				return;
			}
			SetKiller(component2, KillerType.Melee);
			Die(DeathType.melee);
		}
	}

	public override void OnDamageColliderStay(Collider2D collider)
	{
		if (!IsDashing && !base.IsInvulnerable && (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFire") || collider.CompareTag("ExplosionFreeze")))
		{
			Explosion component = collider.GetComponent<Explosion>();
			SetKiller(component.owner, KillerType.Explosion);
			Die(DeathType.explosion);
		}
	}

	public void PickUpDisc()
	{
		if (actorState != ActorState.Dead && (actorState != ActorState.Falling || !(fallTimer > fallAffordance)))
		{
			HSBColor hsbColor = HSBColor.FromColor(character.Color);
			hsbColor.b -= 0.2f;
			Color color = HSBColor.ToColor(hsbColor);
			Transform transform = ((HasDiscs == 1) ? character.leftDisc.transform : character.rightDisc.transform);
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, transform.position, color, transform);
			if (HasDiscs < 2)
			{
				HasDiscs++;
			}
		}
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
		if (HasDiscs >= 2)
		{
			yield break;
		}
		yield return new WaitForSeconds(waitTime);
		if (actorState == ActorState.Dead || (actorState == ActorState.Falling && fallTimer > fallAffordance))
		{
			yield break;
		}
		for (int i = 0; i < numDiscs; i++)
		{
			if (HasDiscs < 2)
			{
				PickUpDisc();
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
			yield return null;
		}
		character.IsVisible = true;
		character.SetMeleeDiscs(HasDiscs);
		base.IsInvulnerable = false;
		justBecomeVulernable = true;
	}

	public override void Die(DeathType deathType)
	{
		if (actorState != ActorState.Dead && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			IsShielded = false;
			base.Die(DeathType.outofbounds);
			if (respawnDiscRoutine != null)
			{
				StopCoroutine(respawnDiscRoutine);
			}
			ToggleFire(onFire: false, extinguishPFX: true);
			physicsCollider.enabled = false;
			character.IsVisible = false;
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/decoy_die", base.transform.position);
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DecoyPuff, base.transform.position, character.Color);
			Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
			Singleton<GameManager>.Instance.activeDecoys.Remove(this);
			base.gameObject.Recycle();
		}
	}

	public override void StartFall()
	{
		base.StartFall();
		StopAiming();
		character.SetFacialExpression(FacialExpression.panic);
		character.IsVisible = true;
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
		hsbColor = HSBColor.FromColor(targetColor);
		hsbColor.b = 1f;
		main = moveFasterPFX.main;
		main.startColor = HSBColor.ToColor(hsbColor);
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
}
