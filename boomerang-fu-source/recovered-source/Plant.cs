using UnityEngine;
using UnityEngine.Rendering;

public class Plant : CollisionBehaviour
{
	public enum DisturberType
	{
		none,
		projectile,
		player,
		dash,
		melee,
		death
	}

	[Header("General")]
	public Transform trunkTransform;

	[Header("Tilt")]
	public Vector3 initRotation = Vector3.zero;

	[SerializeField]
	private Vector3 targetRotation;

	public float tiltDuration = 1f;

	[Header("Disturbance")]
	[SerializeField]
	private Vector3 disturbDirection;

	public float disturbAmt;

	[SerializeField]
	public float tiltFactor;

	public DisturberType lastDisturberType;

	[Header("Burning")]
	public float burnHealth = 0.4f;

	public float burnTime;

	[Header("Art")]
	public MeshRenderer meshRenderer;

	public MeshFilter meshFilter;

	public Mesh[] plantMeshes;

	private Transform plantTransform;

	private float maxTilt = 25f;

	private float tiltTimer;

	private bool isAtRest = true;

	private DisturberType disturberType;

	private Actor disturberActor;

	private Projectile disturberProjectile;

	private Vector3 lastDisturbDirection;

	private float maxDisturbAmt;

	private bool isCut;

	private float growthTimer;

	private float growthDuration = 0.25f;

	public Collider2D plantCollider;

	public bool isAutumnPlant;

	private bool isBurnt;

	private Actor fireOwner;

	private int fireSpread = 1;

	[SerializeField]
	private static int plantUpdateFrame;

	[SerializeField]
	private static int cellUpdateFrame;

	[SerializeField]
	private int currentFrame;

	private void Start()
	{
		plantTransform = base.transform;
		if (plantCollider == null)
		{
			plantCollider = GetComponent<Collider2D>();
		}
		isBurnt = false;
		isCut = false;
		meshFilter.mesh = plantMeshes[Random.Range(0, plantMeshes.Length)];
		plantUpdateFrame = 3;
		cellUpdateFrame = 30;
		currentFrame = Random.Range(1, Mathf.Max(plantUpdateFrame, cellUpdateFrame));
		if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Switch)
		{
			meshRenderer.shadowCastingMode = ShadowCastingMode.On;
		}
	}

	private void Update()
	{
		if (currentFrame >= int.MaxValue)
		{
			currentFrame = 1;
		}
		else
		{
			currentFrame++;
		}
		if (currentFrame % cellUpdateFrame == 0)
		{
			if (Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Fire, checkMovingFloors: false))
			{
				Burn(Time.deltaTime * (float)cellUpdateFrame);
			}
			else if (Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Ice, checkMovingFloors: false))
			{
				Cut(spawnLeafLitter: true);
			}
			if (burnTime > 0f)
			{
				burnHealth -= Time.deltaTime * (float)cellUpdateFrame;
				burnTime -= Time.deltaTime * (float)cellUpdateFrame;
			}
		}
		if (burnHealth <= 0f && !isCut)
		{
			CatchFire();
		}
		if ((currentFrame % plantUpdateFrame != 0 && tiltFactor == 0f) || isAtRest || isCut || isBurnt)
		{
			return;
		}
		float num = 20f;
		if (disturbAmt > tiltFactor)
		{
			maxDisturbAmt = disturbAmt;
			tiltFactor = disturbAmt;
			switch (disturberType)
			{
			case DisturberType.death:
				num = 50f;
				tiltDuration = disturbAmt;
				break;
			case DisturberType.player:
				tiltDuration = disturbAmt * 0.25f;
				break;
			case DisturberType.melee:
				num = 35f;
				tiltDuration = disturbAmt * 0.2f;
				break;
			case DisturberType.projectile:
			case DisturberType.dash:
				num = 50f;
				tiltDuration = disturbAmt * 0.4f;
				break;
			}
			tiltTimer = 0f;
			lastDisturbDirection = disturbDirection;
		}
		else
		{
			disturbAmt = 0f;
			if (tiltTimer <= tiltDuration)
			{
				tiltTimer += Time.deltaTime;
				tiltFactor = Mathf.Lerp(maxDisturbAmt, 0f, Easing.EaseOut(tiltTimer / tiltDuration));
			}
			else
			{
				tiltFactor = 0f;
				isAtRest = true;
			}
			if (tiltTimer >= tiltDuration)
			{
				maxDisturbAmt = 0f;
			}
		}
		targetRotation = new Vector2(0f - lastDisturbDirection.y, lastDisturbDirection.x) * tiltFactor * maxTilt;
		trunkTransform.localRotation = Quaternion.Lerp(trunkTransform.localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * num);
		disturberType = DisturberType.none;
	}

	public void DetectDeath(Vector3 position)
	{
		if (trunkTransform != null && plantTransform != null)
		{
			isAtRest = false;
			disturberType = DisturberType.death;
			disturbDirection = trunkTransform.InverseTransformDirection((position - plantTransform.position).normalized);
			disturbAmt = 1.75f;
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		if (isCut || isBurnt)
		{
			return;
		}
		if (collider.CompareTag("ExplosionFire"))
		{
			Explosion component = collider.GetComponent<Explosion>();
			if (component != null && component.owner != null)
			{
				Burn(2.66f, component.owner);
			}
			return;
		}
		if (collider.CompareTag("Explosion") || collider.CompareTag("ExplosionFreeze"))
		{
			Cut(spawnLeafLitter: true);
			return;
		}
		DisturberType disturberType = DisturberType.none;
		if (collider.CompareTag("MeleeCollider"))
		{
			disturberType = DisturberType.melee;
		}
		else if (collider.CompareTag("Player") || collider.CompareTag("Enemy"))
		{
			disturberActor = collider.GetComponent<Actor>();
			disturberType = ((!(disturberActor != null) || (!disturberActor.IsDashing && !disturberActor.isLunging)) ? DisturberType.player : DisturberType.dash);
		}
		else if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			disturberType = DisturberType.projectile;
		}
		if (disturberType < this.disturberType)
		{
			return;
		}
		this.disturberType = disturberType;
		if (trunkTransform == null || plantTransform == null)
		{
			isAtRest = true;
			return;
		}
		switch (this.disturberType)
		{
		case DisturberType.melee:
			disturbDirection = trunkTransform.InverseTransformDirection((collider.transform.position - plantTransform.position).normalized);
			disturbAmt = 1.333f;
			break;
		case DisturberType.dash:
			disturbAmt = 1f;
			disturbDirection = trunkTransform.InverseTransformDirection(collider.transform.position - plantTransform.position);
			break;
		case DisturberType.player:
			if (disturberActor.velocity.magnitude > 5f)
			{
				disturbAmt = Mathf.Clamp01(disturberActor.velocity.magnitude / disturberActor.walkVelocity);
			}
			else
			{
				disturbAmt = 0f;
			}
			disturbDirection = trunkTransform.InverseTransformDirection(collider.transform.position - plantTransform.position).normalized;
			break;
		case DisturberType.projectile:
			disturberProjectile = collider.GetComponent<Projectile>();
			if (disturberProjectile != null && disturberProjectile.movement != Projectile.Movement.Dropped && disturberProjectile.movement != Projectile.Movement.Squished)
			{
				disturbAmt = 1f;
				disturbDirection = trunkTransform.InverseTransformDirection(collider.transform.position - plantTransform.position).normalized;
			}
			else
			{
				disturbAmt = 0f;
			}
			break;
		}
		if (disturbAmt >= tiltFactor)
		{
			lastDisturberType = this.disturberType;
		}
		isAtRest = false;
	}

	public void Cut(bool spawnLeafLitter = false)
	{
		if (!isCut)
		{
			isCut = true;
			if (spawnLeafLitter)
			{
				Singleton<CellManager>.Instance.PlayLeafBurstPFXAt(trunkTransform.position + new Vector3(0f, 0f, -3f), isAutumnPlant);
			}
			trunkTransform.gameObject.SetActive(value: false);
			plantCollider.enabled = false;
		}
	}

	public void Burn(float lifetime)
	{
		if (!isCut && !isBurnt)
		{
			burnTime += lifetime;
		}
	}

	public void Burn(float lifetime, Actor fireOwner)
	{
		if (!isCut && !isBurnt)
		{
			burnTime += lifetime;
			this.fireOwner = fireOwner;
		}
	}

	private void CatchFire()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		Singleton<CellManager>.Instance.IsPositionTouchingCellType(base.transform.position, CellManager.CellType.Fire, out fireOwner, checkMovingFloors: false);
		Vector3 vector = base.transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f);
		Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, vector, Random.Range(1.25f, 1.5f), fireOwner, checkMovingFloors: false);
		Singleton<CellManager>.Instance.PlayFirePFXAt(base.transform.position - new Vector3(0f, 0f, 3f));
		Singleton<CellManager>.Instance.SetPlantOnFire();
		if (Chance.PercentageTrue(60f))
		{
			int num = Random.Range(0, fireSpread - 1);
			for (int i = 0; i < num; i++)
			{
				vector = base.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
				if (!Physics2D.OverlapPoint(vector, 1 << LayerMask.NameToLayer("Fire")) && Singleton<LevelManager>.Instance.CanFireSpawnAt(vector))
				{
					Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, vector, Random.Range(1.25f, 1.5f), fireOwner);
				}
			}
		}
		Cut();
	}
}
