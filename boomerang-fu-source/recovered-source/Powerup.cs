using System.Collections;
using I2.Loc;
using UnityEngine;

public class Powerup : MonoBehaviour
{
	public enum PowerupState
	{
		Locked,
		Unlocked,
		Collected
	}

	public PowerupType power;

	public PowerupState powerupState;

	private bool isMoving;

	private Vector2 velocity = Vector2.zero;

	private Vector2 initVelocity;

	private Vector2 moveAmount;

	private float mass = 2f;

	private float drag = 6f;

	private float colliderRadius;

	public LayerMask obstaclesLayer;

	public InterpolatedTransform interpolatedTransform;

	private Animator animator;

	public bool playersInRange;

	public Transform book;

	public GameObject sparkles;

	public GameObject glow;

	public Sprite[] icons;

	private Vector2 direction;

	private void Start()
	{
		animator = GetComponent<Animator>();
		colliderRadius = GetComponent<CircleCollider2D>().radius;
	}

	private void OnEnable()
	{
		Singleton<Camera3D>.Instance.AddCameraTarget(base.transform, 0.85f, 2f);
		powerupState = PowerupState.Locked;
		sparkles.SetActive(value: true);
		glow.SetActive(value: true);
	}

	private void Update()
	{
		book.Rotate(0f, 0f, Time.deltaTime * 60f);
	}

	private void FixedUpdate()
	{
		if (isMoving)
		{
			Move();
		}
	}

	private void Move()
	{
		if (velocity.magnitude > 0.5f)
		{
			velocity *= Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);
			moveAmount = CheckForCollisions(velocity);
			base.transform.position += (Vector3)moveAmount * Time.fixedDeltaTime;
			if (!Physics2D.OverlapCircle(base.transform.position, colliderRadius, Singleton<LevelManager>.Instance.floorLayers))
			{
				FallInHazard();
			}
		}
		if (velocity.magnitude < 0.5f)
		{
			isMoving = false;
		}
	}

	public void FallInHazard()
	{
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splash, base.transform.position);
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SparkleBurst, base.transform.position, Singleton<ColorManager>.Instance.powerupColor);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/destroy_power", base.transform.position);
		Destroy();
	}

	private Vector2 CheckForCollisions(Vector2 vel)
	{
		moveAmount = CollideWithObstacles(vel);
		return moveAmount;
	}

	private Vector2 CollideWithObstacles(Vector2 vel)
	{
		direction = vel.normalized;
		RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, colliderRadius, direction, vel.magnitude * Time.fixedDeltaTime, obstaclesLayer);
		if ((bool)raycastHit2D)
		{
			velocity = Vector2.Reflect(velocity, raycastHit2D.normal);
			moveAmount = raycastHit2D.centroid - (Vector2)base.transform.position - direction * 0.01f;
		}
		else
		{
			moveAmount = vel;
		}
		return moveAmount;
	}

	public void OnTriggerEnter2D(Collider2D collider)
	{
		if (powerupState == PowerupState.Unlocked && collider.CompareTag("PickupCollider"))
		{
			return;
		}
		if (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFire"))
		{
			Destroy();
		}
		if (collider.CompareTag("MovingObstacle"))
		{
			MovingObstacle component = collider.GetComponent<MovingObstacle>();
			if (component != null && component.movementState == MovingObstacle.MovementState.Moving && (component.isRotatingObstacle || Vector2.Dot(component.velocity.normalized, (base.transform.position - component.transform.position).normalized) > 0f))
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SparkleBurst, base.transform.position, Singleton<ColorManager>.Instance.powerupColor);
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/destroy_power", base.transform.position);
				Destroy();
			}
		}
	}

	public void OnTriggerStay2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && collider.OverlapPoint(base.transform.position) && (base.transform.parent == null || base.transform.parent != collider.transform))
		{
			Transform parent = base.transform.parent;
			base.transform.SetParent(collider.transform, worldPositionStays: true);
			interpolatedTransform.ReparentPreviousTransforms(parent, collider.transform);
		}
	}

	public void OnTriggerExit2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor"))
		{
			Transform parent = base.transform.parent;
			base.transform.SetParent(null, worldPositionStays: true);
			interpolatedTransform.ReparentPreviousTransforms(parent, null);
		}
	}

	private void SetPower(PowerupType powerupType)
	{
		power = powerupType;
	}

	private void SetIcon(int index)
	{
	}

	private void Unlock()
	{
		powerupState = PowerupState.Unlocked;
		SetIcon(GetPowerIndex());
	}

	private int GetPowerIndex()
	{
		return power switch
		{
			PowerupType.MoveFaster => 2, 
			PowerupType.DashThroughWalls => 3, 
			PowerupType.ReverseInputs => 4, 
			PowerupType.Shield => 5, 
			PowerupType.ExplosiveDisc => 6, 
			PowerupType.MultiDisc => 7, 
			PowerupType.TeleportDisc => 8, 
			PowerupType.FireDisc => 9, 
			PowerupType.ExtraDisc => 10, 
			PowerupType.SlimeDisc => 11, 
			PowerupType.Disguise => 12, 
			PowerupType.TimeImmune => 13, 
			PowerupType.ControlledDisc => 14, 
			PowerupType.ShrinkingBounds => 15, 
			PowerupType.Decoy => 16, 
			_ => 0, 
		};
	}

	public static string GetPowerupNameString(PowerupType powerupType)
	{
		return powerupType switch
		{
			PowerupType.None => LocalizationManager.GetTranslation("powerup_random"), 
			PowerupType.DashThroughWalls => LocalizationManager.GetTranslation("powerup_dashthroughwalls"), 
			PowerupType.MoveFaster => LocalizationManager.GetTranslation("powerup_movefaster"), 
			PowerupType.ReverseInputs => LocalizationManager.GetTranslation("powerup_reverseinputs"), 
			PowerupType.ExplosiveDisc => LocalizationManager.GetTranslation("powerup_explosivedisc"), 
			PowerupType.SlimeDisc => LocalizationManager.GetTranslation("powerup_icedisc"), 
			PowerupType.MultiDisc => LocalizationManager.GetTranslation("powerup_multidisc"), 
			PowerupType.Shield => LocalizationManager.GetTranslation("powerup_shield"), 
			PowerupType.TeleportDisc => LocalizationManager.GetTranslation("powerup_teleportdisc"), 
			PowerupType.ExtraDisc => LocalizationManager.GetTranslation("powerup_extradisc"), 
			PowerupType.FireDisc => LocalizationManager.GetTranslation("powerup_firedisc"), 
			PowerupType.Disguise => LocalizationManager.GetTranslation("powerup_disguise"), 
			PowerupType.TimeImmune => string.Empty, 
			PowerupType.ControlledDisc => LocalizationManager.GetTranslation("powerup_controlleddisc"), 
			PowerupType.ShrinkingBounds => LocalizationManager.GetTranslation("powerup_shrinkingbounds"), 
			PowerupType.Decoy => LocalizationManager.GetTranslation("powerup_decoy"), 
			_ => string.Empty, 
		};
	}

	private void OnTriggerExit2D()
	{
		animator.SetBool("IsBeingCollected", value: false);
	}

	public PowerupType GetCollected(Player player)
	{
		if (power == PowerupType.None)
		{
			SetPower(Singleton<LevelManager>.Instance.SelectRandomPower(player));
		}
		Singleton<LevelManager>.Instance.CyclePossiblePowerups(power);
		GetComponent<Collider2D>().enabled = false;
		player.alertBar.ShowPowerup(power);
		animator.SetTrigger("GetCollected");
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SparkleBurst, base.transform.position, player.alertBar.GetPowerupColor(power));
		sparkles.SetActive(value: false);
		glow.SetActive(value: false);
		StartCoroutine(DestroyAfterDelay());
		powerupState = PowerupState.Collected;
		return power;
	}

	private IEnumerator DestroyAfterDelay()
	{
		yield return new WaitForSeconds(0.2f);
		Destroy();
	}

	private void Destroy()
	{
		Singleton<GameManager>.Instance.spawnedPowerups.Remove(base.gameObject);
		Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform, 0.5f);
		Object.Destroy(base.gameObject);
	}

	public static PowerupType AllPowerups()
	{
		return PowerupType.MoveFaster | PowerupType.DashThroughWalls | PowerupType.ReverseInputs | PowerupType.Shield | PowerupType.ExplosiveDisc | PowerupType.MultiDisc | PowerupType.TeleportDisc | PowerupType.FireDisc | PowerupType.ExtraDisc | PowerupType.SlimeDisc | PowerupType.Disguise | PowerupType.ControlledDisc | PowerupType.ShrinkingBounds | PowerupType.Decoy;
	}
}
