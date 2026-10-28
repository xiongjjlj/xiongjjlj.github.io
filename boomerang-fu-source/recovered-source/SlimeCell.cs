using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class SlimeCell : MonoBehaviour
{
	public float lifetime;

	public float moveDuration;

	public LayerMask collisionLayer;

	public bool isActive = true;

	public Player slimeOwner;

	public Transform bigSlime;

	public Transform[] miniSlimes;

	public Vector3[] miniSlimeScales;

	public Color colorDefault;

	public Color colorFloor;

	public SpriteRenderer[] renderers;

	public LayerMask hazardLayers;

	public Vector3 initScale;

	public AnimationCurve growthCurve;

	private float appearTimer;

	public CircleCollider2D collider;

	private float colliderRadius;

	private Bounds colliderBounds;

	private bool isFloor;

	private Vector2[] hazardCollisionPoints = new Vector2[5];

	private Collider2D[] touchingPlants = new Collider2D[4];

	private Collider2D[] floorColliders = new Collider2D[3];

	private void Awake()
	{
		colliderRadius = collider.radius;
	}

	public void Init(float lifetime = 4f, Player slimeOwner = null)
	{
		this.lifetime = lifetime;
		this.slimeOwner = slimeOwner;
		base.transform.localScale = initScale;
		bigSlime.localScale = Vector3.one;
		isFloor = false;
		collider.radius = colliderRadius;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].color = colorDefault;
			renderers[i].receiveShadows = true;
			renderers[i].shadowCastingMode = ShadowCastingMode.On;
		}
		miniSlimeScales = new Vector3[miniSlimes.Length];
		float num = Random.Range(1.4f, 2f);
		base.transform.localScale = new Vector3(num, num, 1f);
		for (int j = 0; j < miniSlimes.Length; j++)
		{
			miniSlimes[j].localPosition = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
			num = Random.Range(0.4f, 1f);
			miniSlimeScales[j] = new Vector3(num, num, 1f);
			miniSlimes[j].localScale = Vector3.one * 0.3f;
		}
		base.transform.position = base.transform.position + Vector3.zero;
		isActive = true;
		collider.enabled = true;
		Singleton<GameManager>.Instance.activeSlimes.Add(base.gameObject);
		CheckForHazardsAndFloor(base.transform.position);
		int num2 = Physics2D.OverlapCircleNonAlloc(base.transform.position, colliderRadius, touchingPlants, 1 << LayerMask.NameToLayer("Plant"));
		for (int k = 0; k < num2; k++)
		{
			Plant component = touchingPlants[k].GetComponent<Plant>();
			if (component != null)
			{
				component.Cut(spawnLeafLitter: true);
			}
		}
		base.transform.Rotate(new Vector3(0f, 0f, Random.Range(0f, 359f)));
		appearTimer = 0f;
	}

	private void Update()
	{
		if (appearTimer < 1f)
		{
			appearTimer += Time.deltaTime;
			for (int i = 0; i < miniSlimes.Length; i++)
			{
				miniSlimes[i].localScale = Vector3.Lerp(Vector3.one * 0.3f, miniSlimeScales[i], growthCurve.Evaluate(appearTimer / 1f));
			}
		}
		if (lifetime > 0f)
		{
			lifetime -= Time.deltaTime;
		}
		else if (lifetime <= 0f && isActive)
		{
			isActive = false;
			StartCoroutine(Extinguish());
		}
	}

	private void OnTriggerEnter2D(Collider2D collider)
	{
		if (collider.CompareTag("MovingFloor") && isFloor)
		{
			lifetime = 0f;
		}
	}

	public void ExtinguishImmediately()
	{
		collider.enabled = false;
		StartCoroutine(Extinguish(0.2f));
	}

	private IEnumerator Extinguish(float extinguishTimer = 0f)
	{
		extinguishTimer = 0f;
		while (extinguishTimer < 0.1f)
		{
			extinguishTimer += Time.deltaTime;
			bigSlime.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, Easing.EaseIn(extinguishTimer / 0.2f));
			yield return null;
		}
		collider.enabled = false;
		while (extinguishTimer < 0.2f)
		{
			extinguishTimer += Time.deltaTime;
			bigSlime.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, Easing.EaseIn(extinguishTimer / 0.2f));
			yield return null;
		}
		RemoveCell();
	}

	private void RemoveCell()
	{
		Singleton<GameManager>.Instance.activeSlimes.Remove(base.gameObject);
		base.transform.SetParent(null);
		base.gameObject.Recycle();
	}

	private void CheckForHazardsAndFloor(Vector2 position)
	{
		hazardCollisionPoints[0] = position;
		hazardCollisionPoints[1] = position + colliderRadius * new Vector2(0f, 1f).normalized;
		hazardCollisionPoints[2] = position + colliderRadius * new Vector2(1f, 0f).normalized;
		hazardCollisionPoints[3] = position + colliderRadius * new Vector2(0f, -1f).normalized;
		hazardCollisionPoints[4] = position + colliderRadius * new Vector2(-1f, 0f).normalized;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 1; i++)
		{
			int num3 = Physics2D.OverlapPointNonAlloc(hazardCollisionPoints[i], floorColliders, Singleton<LevelManager>.Instance.floorLayers);
			int num4 = 0;
			if (num3 > 0)
			{
				for (int j = 0; j < num3; j++)
				{
					num4++;
					if (!isFloor && floorColliders[j].CompareTag("MovingFloor"))
					{
						base.transform.SetParent(floorColliders[j].transform, worldPositionStays: true);
						break;
					}
				}
				if (num4 > 0)
				{
					num2++;
				}
			}
			if (num3 == 0 || num4 == 0)
			{
				num++;
			}
		}
		if (num > 0)
		{
			base.transform.position += new Vector3(0f, 0f, 0.5f);
			isFloor = true;
			base.gameObject.layer = LayerMask.NameToLayer("Floor");
			for (int k = 0; k < renderers.Length; k++)
			{
				renderers[k].color = colorFloor;
			}
			base.transform.localScale *= 1.25f;
			collider.radius *= 1.25f;
		}
	}
}
