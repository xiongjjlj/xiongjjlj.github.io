using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : CollisionBehaviour
{
	public enum Movement
	{
		Throwing,
		Returning,
		Free,
		Dropped,
		Squished,
		Controlled
	}

	public Movement movement;

	public Actor owner;

	public float minDistanceFromOwner = 4.5f;

	public bool canCollideWithOwner;

	public bool wasJustDeflected;

	protected float wasJustDeflectedTimer;

	protected float wasJustDeflectedDuration = 0.15f;

	public bool projectileStartedDropped;

	public Collider2D projectileCollider;

	public static float projectileColliderRadius = 0.5f;

	public LayerMask collisionLayers;

	public LayerMask collisionLayersDropped;

	public LayerMask playerLayer;

	public LayerMask obstacleLayer;

	public Vector2 velocity = Vector2.zero;

	protected Vector2 _velocityStored = Vector2.zero;

	protected Vector2 moveAmount;

	protected float mass = 0.6f;

	protected float drag = 2.8f;

	public float bounciness = 0.8f;

	protected List<Collider2D> collisionsThisFrame = new List<Collider2D>(32);

	public InterpolatedTransform interpolatedTransform;

	protected Collider2D[] touchingObstacles = new Collider2D[16];

	protected RaycastHit2D[] touchingTriggers = new RaycastHit2D[16];

	protected CircleCollider2D obstacleCircleCollider;

	protected BoxCollider2D obstacleBoxCollider;

	protected bool canUseTeleportal = true;

	public float throwDuration = 0.45f;

	public float throwSpeed = 190f;

	public float speedLimit;

	protected float throwStrength;

	protected float throwTimer;

	public Vector2 throwDirection;

	public bool canBePickedUp = true;

	protected Vector2 freeDirection;

	protected float freeStrength;

	public float timeScaler = 1f;

	public Color projectileColor;

	public Vector2 velocityStored
	{
		get
		{
			return _velocityStored;
		}
		set
		{
			_velocityStored = value;
		}
	}

	protected virtual void OnEnable()
	{
		base.gameObject.layer = LayerMask.NameToLayer("Projectile");
		speedLimit = 45f;
		minDistanceFromOwner = 4.5f;
		canBePickedUp = true;
		canCollideWithOwner = false;
		projectileStartedDropped = false;
	}

	public virtual void Init(bool isDropped = false)
	{
		interpolatedTransform.ForgetPreviousTransforms();
		velocity = Vector2.zero;
		velocityStored = Vector2.zero;
		canUseTeleportal = true;
		wasJustDeflected = false;
		projectileCollider.enabled = true;
		canCollideWithOwner = false;
		if (!isDropped)
		{
			StartThrow();
		}
		else
		{
			StartDropped();
		}
		CheckForSquishing();
		collisionsThisFrame.Clear();
	}

	protected virtual void Update()
	{
		if (movement == Movement.Dropped)
		{
			canBePickedUp = true;
		}
		if (wasJustDeflected && wasJustDeflectedTimer > 0f)
		{
			wasJustDeflectedTimer -= Time.deltaTime;
		}
		else if (wasJustDeflected)
		{
			wasJustDeflected = false;
			wasJustDeflectedTimer = 0f;
		}
	}

	protected virtual void FixedUpdate()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionOut || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionIn || Singleton<GameManager>.Instance.gameState == GameManager.GameState.Loading)
		{
			return;
		}
		switch (movement)
		{
		case Movement.Throwing:
			velocity += VectorExt.AddForce(CalculateThrow(), ForceMode2D.Force, mass) * timeScaler;
			break;
		case Movement.Free:
			velocity += VectorExt.AddForce(CalculateFreeform(), ForceMode2D.Force, mass);
			if (velocity.magnitude < 10f)
			{
				movement = Movement.Dropped;
				canCollideWithOwner = true;
				base.gameObject.layer = LayerMask.NameToLayer("Collectables");
			}
			break;
		case Movement.Dropped:
			if (velocity.magnitude < 1f && projectileStartedDropped)
			{
				projectileStartedDropped = false;
			}
			break;
		}
		collisionsThisFrame.Clear();
		MoveProjectile();
		if (!canCollideWithOwner && owner != null && (base.transform.position - owner.transform.position).magnitude > minDistanceFromOwner && !IsAnExplosive())
		{
			canCollideWithOwner = true;
		}
	}

	protected virtual void MoveProjectile()
	{
		if (velocity.magnitude > 0f)
		{
			velocity *= Mathf.Clamp01(1f - drag * Time.fixedDeltaTime * Mathf.Clamp(timeScaler, 1f, 2f));
		}
		if (velocity.magnitude > speedLimit)
		{
			velocity = Vector2.ClampMagnitude(velocity, speedLimit);
		}
		CheckForSquishing();
		moveAmount = CheckForCollisions(velocity);
		base.transform.position += (Vector3)moveAmount * Time.fixedDeltaTime;
		velocityStored = velocity;
	}

	protected Vector2 CheckForCollisions(Vector2 vel)
	{
		RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, projectileColliderRadius, vel.normalized, vel.magnitude * Time.fixedDeltaTime, 1 << LayerMask.NameToLayer("Teleportal"));
		if ((bool)raycastHit2D)
		{
			OnTriggerEnter2D(raycastHit2D.collider);
		}
		raycastHit2D = Physics2D.CircleCast(base.transform.position, projectileColliderRadius, vel.normalized, vel.magnitude * Time.fixedDeltaTime, obstacleLayer);
		if ((bool)raycastHit2D)
		{
			CollisionEnter2D(raycastHit2D.normal, raycastHit2D.collider);
			CollisionBehaviour component = raycastHit2D.collider.GetComponent<CollisionBehaviour>();
			if (component != null)
			{
				component.CollisionEnter2D((raycastHit2D.collider.transform.position - base.transform.position).normalized, projectileCollider);
			}
			_ = (raycastHit2D.centroid - (Vector2)base.transform.position).magnitude;
			moveAmount = raycastHit2D.centroid - (Vector2)base.transform.position - vel.normalized * 0.001f;
		}
		else
		{
			moveAmount = vel;
		}
		int num = Physics2D.CircleCastNonAlloc(layerMask: (movement != Movement.Dropped && !projectileStartedDropped) ? collisionLayers : collisionLayersDropped, origin: base.transform.position, radius: projectileColliderRadius, direction: moveAmount.normalized, results: touchingTriggers, distance: moveAmount.magnitude * Time.fixedDeltaTime);
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				if (!touchingTriggers[i].collider.isTrigger)
				{
					continue;
				}
				CollisionBehaviour component2 = touchingTriggers[i].collider.GetComponent<CollisionBehaviour>();
				if (!(component2 != null) || (!canCollideWithOwner && touchingTriggers[i].collider.CompareTag("MeleeCollider") && owner != null && component2.transform.parent.GetComponent<Actor>() == owner))
				{
					continue;
				}
				Disc disc = component2 as Disc;
				if (!(disc != null) || !disc.isMiniDisc)
				{
					component2.OnTriggerEnter2D(projectileCollider);
					OnTriggerEnter2D(touchingTriggers[i].collider);
					if ((touchingTriggers[i].collider.CompareTag("MeleeCollider") && !Physics2D.Raycast(base.transform.position, (touchingTriggers[i].collider.transform.position - base.transform.position).normalized, (touchingTriggers[i].collider.transform.position - base.transform.position).magnitude, obstacleLayer)) || (touchingTriggers[i].collider.CompareTag("DamageCollider") && owner != null && component2.transform.parent.GetComponent<Actor>() != owner && !Singleton<GameManager>.Instance.AreOpponents(component2.transform.parent.GetComponent<Actor>(), this)))
					{
						moveAmount = touchingTriggers[i].centroid - (Vector2)base.transform.position + Vector2.Reflect(vel, (base.transform.position - component2.transform.position).normalized);
						break;
					}
				}
			}
		}
		return moveAmount;
	}

	public virtual void CheckForSquishing(int recursionDepth = 0)
	{
		if (!projectileCollider.enabled || recursionDepth > 5)
		{
			return;
		}
		float num = -0.01f;
		int num2 = Physics2D.OverlapCircleNonAlloc(base.transform.position, projectileColliderRadius + num, touchingObstacles, obstacleLayer);
		MovingObstacle movingObstacle = null;
		for (int i = 0; i < num2; i++)
		{
			ColliderDistance2D colliderDistance2D = touchingObstacles[i].Distance(projectileCollider);
			if (touchingObstacles[i].CompareTag("MovingObstacle"))
			{
				movingObstacle = touchingObstacles[i].GetComponent<MovingObstacle>();
				if (movingObstacle.movementState == MovingObstacle.MovementState.Moving)
				{
					if (recursionDepth == 0)
					{
						if (movingObstacle.isRotatingObstacle)
						{
							Vector2 vector = Vector2.Lerp(colliderDistance2D.pointA, colliderDistance2D.pointB, 0.5f) - (Vector2)movingObstacle.transform.position;
							float num3 = vector.magnitude * movingObstacle.angularVelocity;
							Vector2 vector2 = new Vector2(vector.y, 0f - vector.x);
							if (!movingObstacle.isRotatingClockwise)
							{
								vector2 = new Vector2(0f - vector.y, vector.x);
							}
							velocity += vector2.normalized * num3;
						}
						else
						{
							velocity += movingObstacle.velocity * Mathf.Clamp01(Vector3.Dot(movingObstacle.velocity.normalized, colliderDistance2D.normal));
						}
					}
					if (recursionDepth > 3)
					{
						GetSquished();
						break;
					}
				}
			}
			if (colliderDistance2D.isOverlapped)
			{
				Vector2 vector3 = -colliderDistance2D.normal * colliderDistance2D.distance;
				RaycastHit2D raycastHit2D = Physics2D.CircleCast(base.transform.position, projectileColliderRadius + num, vector3.normalized, vector3.magnitude, obstacleLayer);
				Vector2 vector4 = vector3 - raycastHit2D.normal * Vector3.Dot(vector3, raycastHit2D.normal);
				base.transform.position += (Vector3)vector4;
				velocity += vector4 * Time.fixedDeltaTime;
				Physics2D.SyncTransforms();
				CheckForSquishing(recursionDepth + 1);
				break;
			}
		}
	}

	protected virtual void GetSquished()
	{
		GetPickedUp();
	}

	public virtual void StartThrow(bool reset = false, bool inheritOwnerVelocity = false)
	{
		movement = Movement.Throwing;
		throwTimer = 0f;
		throwStrength = throwSpeed;
		if (!reset)
		{
			speedLimit += speedLimit;
		}
		velocity = VectorExt.AddForce(throwDirection * throwStrength, ForceMode2D.Force, mass) * 9f * timeScaler;
		velocityStored = velocity;
		if (inheritOwnerVelocity)
		{
			velocity += owner.velocity;
		}
	}

	protected Vector2 CalculateThrow()
	{
		if (throwTimer < throwDuration)
		{
			throwTimer += Time.fixedDeltaTime;
			float num = Mathf.Lerp(throwStrength, 0f, Easing.EaseIn(throwTimer / throwDuration));
			return throwDirection * num;
		}
		EndThrow();
		return Vector2.zero;
	}

	protected virtual void EndThrow()
	{
		freeDirection = velocity.normalized;
		freeStrength = velocity.magnitude * 0.1f;
		StartFreeform();
		throwTimer = throwDuration;
		throwStrength = 0f;
	}

	protected virtual void StartDropped()
	{
		projectileStartedDropped = true;
		velocity = throwDirection * throwSpeed * 0.5f;
		base.gameObject.layer = LayerMask.NameToLayer("Collectables");
		movement = Movement.Dropped;
		StartFreeform();
	}

	protected virtual void StartFreeform(float speedBoost = 0f, bool stayDropped = false)
	{
		freeDirection = velocity.normalized;
		freeStrength = velocity.magnitude * bounciness;
		if (!stayDropped)
		{
			movement = Movement.Free;
		}
		if (speedBoost > 0f)
		{
			velocity += VectorExt.AddForce(freeDirection * speedBoost, ForceMode2D.Impulse, mass) * timeScaler;
		}
	}

	protected Vector2 CalculateFreeform()
	{
		freeStrength = Mathf.Lerp(freeStrength, 0f, Time.fixedDeltaTime * 20f);
		return velocity.normalized * freeStrength;
	}

	public virtual void Bounce(Vector2 collisionVelocity, Vector2 collisionNormal)
	{
		if (collisionVelocity.magnitude < speedLimit * 0.05f)
		{
			collisionVelocity = collisionVelocity.normalized * speedLimit * 0.05f;
		}
		velocity = Vector2.Reflect(collisionVelocity, collisionNormal) * bounciness;
		if (movement == Movement.Throwing)
		{
			if (throwTimer > throwDuration * 0.1f)
			{
				throwTimer -= 0.01f;
			}
			throwDirection = velocity.normalized;
		}
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		if (collisionsThisFrame.Contains(collider))
		{
			return;
		}
		collisionsThisFrame.Add(collider);
		if (!canCollideWithOwner && !IsAnExplosive() && !Physics2D.OverlapCircle(base.transform.position, projectileColliderRadius, 1 << LayerMask.NameToLayer("Teleportal")) && !projectileStartedDropped)
		{
			canCollideWithOwner = true;
		}
		Vector2 vector = normal;
		Vector2 collisionVelocity = velocity;
		Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.LookRotation(Vector3.forward, vector));
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			Projectile component = collider.gameObject.GetComponent<Projectile>();
			if (component != null)
			{
				collisionVelocity -= component.velocityStored;
				wasJustDeflectedTimer = wasJustDeflectedDuration;
				wasJustDeflected = true;
				Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, 90f);
				Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/environment/disc_rico_disc", "velocity", collisionVelocity.magnitude / speedLimit, base.transform);
				if (component.owner != owner)
				{
					if (!Singleton<TimeKeeper>.Instance.slowMo)
					{
						Singleton<TimeKeeper>.Instance.SlowMoEffect(0.1f, 0.075f, 0.5f);
					}
					if (owner != null)
					{
						float magnitude = (base.transform.position - owner.transform.position).magnitude;
						if (magnitude < minDistanceFromOwner * 2f && magnitude < (component.transform.position - owner.transform.position).magnitude)
						{
							owner.DeflectColliderImmediately(collider);
						}
					}
				}
				float angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
				Vector2 vector2 = (Vector2)base.transform.position + vector * 0.25f;
				Singleton<GameManager>.Instance.impactLineMini.Spawn(vector2, Quaternion.AngleAxis(angle, Vector3.forward)).GetComponent<ImpactLine>().Play(projectileColor);
			}
		}
		else
		{
			Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, Mathf.Clamp(velocity.magnitude, 0f, 80f));
			if (!collider.CompareTag("InteractiveObstacle"))
			{
				Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/environment/disc_rico_stone", "velocity", velocity.magnitude / speedLimit, base.transform);
			}
		}
		if (collider.CompareTag("InteractiveObstacle"))
		{
			InteractiveObstacle component2 = collider.GetComponent<InteractiveObstacle>();
			if (component2 != null)
			{
				if (movement != Movement.Dropped && velocity.magnitude > 15f)
				{
					component2.Interact((collider.transform.position - base.transform.position).normalized * velocity.magnitude);
					collisionVelocity *= 0.8f;
				}
				string eventName = "event:/environment/disc_rico_stone";
				switch (component2.obstacleMaterial)
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
				Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(eventName, "velocity", velocity.magnitude / speedLimit, base.transform);
			}
		}
		Bounce(collisionVelocity, vector);
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			CollisionEnter2D((collider.transform.position - base.transform.position).normalized, collider);
		}
		if (collisionsThisFrame.Contains(collider))
		{
			return;
		}
		collisionsThisFrame.Add(collider);
		if (canUseTeleportal && collider.CompareTag("Teleportal"))
		{
			RemoveTrail();
			Teleportal component = collider.GetComponent<Teleportal>();
			Vector2 vector = component.exit.transform.position + component.exit.transform.up * (projectileColliderRadius + component.exit.thickness);
			base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
			interpolatedTransform.ForgetPreviousTransforms();
			component.Flash(projectileColor);
			component.exit.Flash(projectileColor);
			canUseTeleportal = false;
			StartCoroutine(EnableTeleportalCollisionAfterFrames());
			AddTrail();
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/teleportal_disc", base.transform.position);
			return;
		}
		if (owner != null && (collider.CompareTag("MeleeCollider") || (collider.CompareTag("ShieldCollider") && collider.transform.parent.GetComponent<Actor>() != owner) || (collider.CompareTag("DamageCollider") && collider.transform.parent.GetComponent<Actor>() != owner && !Singleton<GameManager>.Instance.AreOpponents(collider.transform.parent.GetComponent<Actor>(), this))))
		{
			Actor component2 = collider.transform.parent.GetComponent<Actor>();
			if (component2.actorState == Actor.ActorState.Dead || component2.deflectedColliders.Contains(GetComponent<Collider2D>()) || (!canCollideWithOwner && collider.CompareTag("MeleeCollider") && owner != null && component2 == owner) || (collider.CompareTag("MeleeCollider") && (bool)Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, obstacleLayer)))
			{
				return;
			}
			if (!projectileStartedDropped && !IsAnExplosive())
			{
				canCollideWithOwner = true;
			}
			Vector2 vector2 = (base.transform.position - collider.transform.parent.transform.position).normalized;
			Vector2 vector3 = velocity;
			Vector2 vector4 = velocity.magnitude * -vector2;
			if (Vector2.Dot(vector3.normalized, vector2) > 0f)
			{
				vector3 = vector4;
			}
			else if (Vector2.Dot(vector3.normalized, vector2) > -0.5f)
			{
				vector3 = vector3 * 0.25f + vector4 * 0.75f;
			}
			if (velocity.magnitude < 0.1f)
			{
				vector3 = -vector2;
			}
			if (collider.CompareTag("MeleeCollider"))
			{
				if (Vector2.Dot(Vector2.Reflect(vector3, vector2).normalized, (owner.transform.position - base.transform.position).normalized) > 0.9f)
				{
					vector3 = Quaternion.AngleAxis(-45f, Vector3.forward) * vector3;
				}
				if (Vector2.Dot(Vector2.Reflect(vector3, vector2).normalized, (collider.transform.parent.transform.position - base.transform.position).normalized) > 0.9f)
				{
					vector2 *= -1f;
				}
				wasJustDeflectedTimer = wasJustDeflectedDuration;
				wasJustDeflected = true;
				Bounce(vector3, vector2);
				if (movement != Movement.Dropped)
				{
					if (collider.transform.parent.GetComponent<Actor>() == owner)
					{
						throwDirection = velocity.normalized;
						StartThrow(reset: true);
					}
					else
					{
						StartFreeform(2000f);
						if (owner != null && (base.transform.position - owner.transform.position).magnitude < (collider.transform.position - owner.transform.position).magnitude)
						{
							owner.DeflectColliderImmediately(collider);
						}
					}
				}
				else if (!IsAnExplosive())
				{
					StartFreeform(24f, stayDropped: true);
				}
				else
				{
					StartFreeform(12f, stayDropped: true);
				}
				if (base.gameObject.activeSelf)
				{
					StartCoroutine(DisableColliderForOneFrame());
				}
			}
			else if ((collider.CompareTag("ShieldCollider") || collider.CompareTag("DamageCollider")) && !component2.IsDashing)
			{
				Bounce(vector3, vector2);
			}
			return;
		}
		if (collider.CompareTag("Powerup"))
		{
			Bounce(velocity, (base.transform.position - collider.transform.position).normalized);
		}
		if (collider.CompareTag("DamageCollider"))
		{
			Actor component3 = collider.transform.parent.GetComponent<Actor>();
			if (owner != null && owner != component3 && !Singleton<GameManager>.Instance.AreOpponents(component3, owner))
			{
				Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.LookRotation(Vector3.forward, (collider.transform.position - base.transform.position).normalized));
				Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, 90f);
			}
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && collider.OverlapPoint(base.transform.position) && movement == Movement.Dropped && (base.transform.parent == null || base.transform.parent != collider.transform))
		{
			Transform parent = base.transform.parent;
			base.transform.SetParent(collider.transform, worldPositionStays: true);
			interpolatedTransform.ReparentPreviousTransforms(parent, collider.transform);
		}
	}

	public override void OnTriggerExit2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && base.gameObject.activeSelf)
		{
			Transform parent = base.transform.parent;
			base.transform.SetParent(null, worldPositionStays: true);
			interpolatedTransform.ReparentPreviousTransforms(parent, null);
		}
	}

	protected IEnumerator EnableTeleportalCollisionAfterFrames(int frames = 2)
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		canUseTeleportal = true;
	}

	public virtual void GetPickedUp(bool vanish = false)
	{
		RemoveTrail();
		if (vanish)
		{
			StartCoroutine(Vanish());
			return;
		}
		StopAllCoroutines();
		base.gameObject.Recycle();
	}

	public IEnumerator Vanish()
	{
		projectileCollider.enabled = false;
		yield return new WaitForSeconds(0.5f);
		projectileCollider.enabled = true;
		if (base.gameObject.activeSelf)
		{
			base.gameObject.Recycle();
		}
	}

	protected IEnumerator DisableColliderForOneFrame()
	{
		projectileCollider.enabled = false;
		yield return new WaitForSecondsRealtime(0.02f);
		projectileCollider.enabled = true;
	}

	protected virtual void RemoveTrail()
	{
	}

	protected virtual void AddTrail()
	{
	}

	public virtual bool IsAnExplosive()
	{
		return false;
	}
}
