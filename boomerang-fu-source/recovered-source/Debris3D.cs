using System.Collections;
using UnityEngine;

public class Debris3D : CollisionBehaviour
{
	[Header("Components")]
	public Transform debris;

	public Rigidbody2D rb;

	protected float colliderRadius;

	public bool hasCircleCollider = true;

	protected Vector2 colliderSize;

	[Header("Movement")]
	public float speed = 10f;

	public float speedZ = 20f;

	private static float gravity = -9.82f;

	public float gravityMod = 1f;

	public float bounciness = 1f;

	public float floorThreshold;

	public float speedLimit = 50f;

	[SerializeField]
	protected float velocityZ;

	protected bool hasJustBounced;

	protected bool readyToBounce;

	[SerializeField]
	protected bool isLanded;

	[Header("Spin")]
	public float spinSpeedStart = 350f;

	public float spinSpeedBounce = 700f;

	[SerializeField]
	protected int numBounces;

	protected Vector3 spinAxis;

	protected float spinSpeed;

	[Header("Lifetime")]
	public float lifetimeDuration;

	protected float landedTime;

	protected bool isVanishing;

	public float vanishDuration = 0.75f;

	protected Vector3 initPos;

	public float debrisInitZ;

	protected Quaternion debrisInitRotation;

	public bool ignorePlayerCollisions;

	protected virtual void Awake()
	{
		if (hasCircleCollider)
		{
			colliderRadius = GetComponent<CircleCollider2D>().radius * base.transform.localScale.x * 0.66f;
		}
		else
		{
			colliderSize = GetComponent<BoxCollider2D>().size * base.transform.localScale.x * 0.66f;
		}
	}

	public virtual void InitDebris()
	{
		initPos = base.transform.localPosition;
		debrisInitZ = debris.transform.localPosition.z;
		debrisInitRotation = debris.localRotation;
	}

	public void ApplyForce(Vector3 direction)
	{
		base.gameObject.SetActive(value: true);
		rb.simulated = true;
		rb.velocity = direction * speed;
		velocityZ = 0f - speedZ;
		isLanded = false;
		spinAxis = (Quaternion.AngleAxis(-90f, Vector3.forward) * direction.normalized).normalized;
		spinSpeed = spinSpeedStart;
		hasJustBounced = false;
		readyToBounce = false;
		numBounces = 0;
	}

	public virtual void Reset()
	{
		rb.simulated = false;
		rb.velocity = Vector2.zero;
		base.transform.localPosition = initPos;
		debris.transform.localPosition = new Vector3(0f, 0f, debrisInitZ);
		debris.transform.localRotation = debrisInitRotation;
		debris.transform.localScale = Vector3.one;
		isVanishing = false;
		base.gameObject.SetActive(value: false);
	}

	protected void Update()
	{
		if (!isLanded)
		{
			if (!readyToBounce && debris.position.z > floorThreshold)
			{
				readyToBounce = true;
			}
			if (debris.position.z <= floorThreshold)
			{
				velocityZ -= gravity * gravityMod * Time.deltaTime;
				hasJustBounced = false;
			}
			else
			{
				CheckForHazards();
				if (readyToBounce && !hasJustBounced)
				{
					hasJustBounced = true;
					OnBounce();
				}
			}
			velocityZ *= Mathf.Clamp01(1f - rb.drag * Time.fixedDeltaTime);
			debris.transform.position += new Vector3(0f, 0f, velocityZ * Time.deltaTime);
			if ((Mathf.Abs(debris.position.z - floorThreshold) < 0.2f || rb.velocity.magnitude < 0.1f) && Mathf.Abs(velocityZ) < 1f)
			{
				velocityZ = 0f;
				isLanded = true;
				landedTime = Singleton<TimeKeeper>.Instance.CurrentTime + Random.Range(0f, 0.5f);
				OnLanded();
			}
		}
		if (numBounces > 0)
		{
			spinSpeed = spinSpeedBounce * new Vector3(rb.velocity.x, rb.velocity.y, velocityZ).magnitude;
		}
		if (Mathf.Abs(spinSpeed) > 15f)
		{
			debris.RotateAround(debris.transform.position, spinAxis, spinSpeed * Time.deltaTime);
		}
		if (rb.velocity.magnitude > speedLimit)
		{
			rb.velocity = Vector2.ClampMagnitude(rb.velocity, speedLimit);
		}
		if (isLanded && (rb.velocity.magnitude > 0.1f || (Singleton<LevelManager>.Instance.level != null && Singleton<LevelManager>.Instance.level.isFallingApart)))
		{
			CheckForHazards();
		}
		if (lifetimeDuration > 0f && !isVanishing && isLanded && Singleton<TimeKeeper>.Instance.TimeSince(landedTime) >= lifetimeDuration)
		{
			Vanish();
		}
	}

	protected virtual void OnBounce()
	{
		velocityZ = (0f - velocityZ) * bounciness;
		numBounces++;
	}

	protected virtual void OnLanded()
	{
	}

	public virtual void CheckForHazards()
	{
		if (hasCircleCollider)
		{
			if (!Physics2D.OverlapCircle(base.transform.position, colliderRadius * 0.75f, Singleton<LevelManager>.Instance.floorLayers))
			{
				FallInHazard();
			}
		}
		else if (!Physics2D.OverlapBox(base.transform.position, colliderSize * 0.75f, base.transform.eulerAngles.z, Singleton<LevelManager>.Instance.floorLayers))
		{
			FallInHazard();
		}
	}

	public override void OnTriggerEnter2D(Collider2D hit)
	{
		if (ignorePlayerCollisions)
		{
			return;
		}
		if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			Actor component = hit.GetComponent<Actor>();
			if (component.IsDashing)
			{
				return;
			}
			Vector2 vector = (hit.transform.position - base.transform.position).normalized;
			Vector2 vector2 = Vector2.Reflect(-component.velocityStored * 0.5f * Mathf.Clamp01(Vector2.Dot(vector, -component.velocityStored.normalized)), vector);
			rb.velocity += vector2;
		}
		if (isLanded)
		{
			if (hit.CompareTag("MeleeCollider") || hit.CompareTag("DisarmCollider"))
			{
				rb.velocity = (Vector2)(base.transform.position - hit.transform.parent.transform.position).normalized * speed * 1.75f;
			}
			spinAxis = (Quaternion.AngleAxis(-90f, Vector3.forward) * rb.velocity.normalized).normalized;
		}
	}

	public override void OnTriggerStay2D(Collider2D hit)
	{
		if (isLanded && hit.CompareTag("MovingFloor") && hit.OverlapPoint(base.transform.position) && (base.transform.parent == null || base.transform.parent != hit.transform))
		{
			_ = base.transform.parent;
			base.transform.SetParent(hit.transform, worldPositionStays: true);
		}
	}

	public override void OnTriggerExit2D(Collider2D hit)
	{
		if (base.gameObject.activeSelf && hit.CompareTag("MovingFloor"))
		{
			_ = base.transform.parent;
			base.transform.SetParent(null, worldPositionStays: true);
		}
	}

	protected virtual void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Floor"))
		{
			Debug.Log("non-trigger floor detected: " + collision.gameObject.name);
			return;
		}
		if (collision.collider.gameObject.CompareTag("MovingObstacle"))
		{
			MovingObstacle component = collision.collider.gameObject.GetComponent<MovingObstacle>();
			if ((component.movementState == MovingObstacle.MovementState.Moving && Vector2.Dot(component.velocity.normalized, (collision.contacts[0].point - (Vector2)base.transform.position).normalized) <= 0f) || collision.collider.OverlapPoint(base.transform.position))
			{
				GetSquished();
			}
		}
		spinAxis = (Quaternion.AngleAxis(-90f, Vector3.forward) * rb.velocity.normalized).normalized;
	}

	public virtual void GetSquished()
	{
		Reset();
	}

	protected void FallInHazard()
	{
		if (Singleton<LevelManager>.Instance.IsPositionOffCliff(base.transform.position))
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.CloudPuffMini, base.transform.position + new Vector3(0f, 0f, 12f));
		}
		else
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SplashMini, base.transform.position);
		}
		Reset();
	}

	protected void Vanish()
	{
		if (base.gameObject.activeSelf)
		{
			isVanishing = true;
			StartCoroutine(VanishRoutine());
		}
	}

	private IEnumerator VanishRoutine()
	{
		float t = 0f;
		while (t < vanishDuration)
		{
			t += Time.deltaTime;
			debris.transform.localScale = Vector3.one * Easing.CubicOut(Mathf.Clamp01(vanishDuration - t / vanishDuration));
			yield return null;
		}
		Reset();
	}
}
