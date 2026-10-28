using UnityEngine;

public class Charger : Enemy
{
	public ParticleSystem meleeTrail;

	public Charger()
	{
		characterOffset = new Vector3(0f, 0f, 0f);
		walkSpeed = 190f;
		attackDuration = 0.5f;
		attackCooldownDuration = 2f;
		lungeSpeed = 600f;
		lungeDuration = 0.5f;
		lungeCooldownDuration = 1f;
		dashDuration = 0.05f;
		aimDuration = 0.8f;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		Init();
	}

	public override void Init()
	{
		base.Init();
	}

	protected override void Update()
	{
		base.Update();
		if (!isCoolingAttack)
		{
			attacksLeft = 1;
		}
	}

	public override bool Attack()
	{
		meleeTrail.Play();
		if (base.Attack())
		{
			return true;
		}
		return false;
	}

	public override void Aim()
	{
		if (!IsFrozen)
		{
			aimTimer += Time.deltaTime * timeScaler;
		}
		base.Aim();
	}

	protected override Vector2 CalculateLunge()
	{
		if (lungeTimer > 0f)
		{
			float num = Mathf.Lerp(0f, lungeSpeed, Easing.CubicOut(lungeTimer / lungeDuration));
			if (IsSlimed)
			{
				num *= slimedFactor;
			}
			lungeTimer -= Time.fixedDeltaTime * timeScaler;
			return lungeDirection * num;
		}
		return Vector2.zero;
	}

	public override void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
		base.CollisionEnter2D(normal, collider);
		if (isLunging && velocityStored.magnitude > speedLimit * 0.4f && (int)obstacleLayer == ((int)obstacleLayer | (1 << collider.gameObject.layer)))
		{
			InterruptAttack();
			StartPushback(normal, 0.4f, 0.66f);
			StopLunge();
		}
	}

	public override void StartPushback(Vector2 direction, float duration, float stunDuration = 0f)
	{
		if (isLunging && stunDuration > 0f)
		{
			stunDuration *= 3f;
			InterruptAttack();
		}
		base.StartPushback(direction, duration, stunDuration);
	}

	protected override void StopLunge(bool reduceVelocity = true)
	{
		base.StopLunge(reduceVelocity);
	}

	protected override bool HasWeapon()
	{
		return true;
	}

	protected override void InterruptAttack()
	{
		base.InterruptAttack();
		meleeTrail.Stop();
	}

	protected override void InterruptAttackAnimation()
	{
		base.InterruptAttackAnimation();
		meleeTrail.Stop();
	}
}
