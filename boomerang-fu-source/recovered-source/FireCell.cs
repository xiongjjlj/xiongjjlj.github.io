using System.Collections;
using UnityEngine;

public class FireCell : MonoBehaviour
{
	public float spread;

	public float lifetime;

	private float initLifetime;

	public bool isBurning;

	public Actor fireOwner;

	public CircleCollider2D fireCollider;

	private float colliderRadius;

	private Bounds colliderBounds;

	public LayerMask collisionLayer;

	public LayerMask extinguishLayers;

	private Collider2D touchingExtinguisher;

	private Collider2D touchingFloor;

	private Collider2D[] touchingPlants = new Collider2D[6];

	public ParticleSystem particles;

	public void Burn(float lifetime = 3f, float moveDuration = 0f, Actor fireOwner = null)
	{
		colliderRadius = fireCollider.radius;
		this.lifetime = lifetime;
		this.fireOwner = fireOwner;
		base.transform.position = base.transform.position + Vector3.zero;
		isBurning = true;
		Singleton<GameManager>.Instance.activeFires.Add(base.gameObject);
		initLifetime = lifetime;
		CheckTouching();
	}

	private void Update()
	{
		if (lifetime > 0f)
		{
			lifetime -= Time.deltaTime;
		}
		else if (lifetime <= 0f && isBurning)
		{
			isBurning = false;
			StartCoroutine(Extinguish());
		}
	}

	private void CheckTouching()
	{
		if (isBurning)
		{
			if ((bool)touchingFloor && touchingFloor.CompareTag("MovingFloor"))
			{
				base.transform.SetParent(touchingFloor.transform, worldPositionStays: true);
			}
			int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, colliderRadius, touchingPlants, 1 << LayerMask.NameToLayer("Plant"));
			for (int i = 0; i < num; i++)
			{
			}
		}
	}

	public void ExtinguishAfterDelay()
	{
		base.gameObject.layer = LayerMask.NameToLayer("Default");
		StartCoroutine(Extinguish(1.75f));
	}

	public void ExtinguishImmediately(bool playSteamEffect = false)
	{
		isBurning = false;
		lifetime = 0f;
		StartCoroutine(Extinguish());
		if (playSteamEffect)
		{
			Singleton<CellManager>.Instance.PlaySteamPFXAt(base.transform.position);
		}
	}

	private IEnumerator Extinguish(float extinguishTimer = 0f)
	{
		particles.Stop();
		while (extinguishTimer < 0.3f)
		{
			extinguishTimer += Time.deltaTime;
			yield return null;
		}
		base.gameObject.layer = LayerMask.NameToLayer("Default");
		while (extinguishTimer < 1.7f)
		{
			extinguishTimer += Time.deltaTime;
			yield return null;
		}
		RemoveCell();
	}

	private void RemoveCell()
	{
		base.gameObject.layer = LayerMask.NameToLayer("Fire");
		Singleton<GameManager>.Instance.activeFires.Remove(base.gameObject);
		base.gameObject.Recycle();
	}

	private void UpdatePaths()
	{
		colliderBounds = new Bounds(base.transform.position, new Vector3(colliderRadius * 2f, colliderRadius * 2f, 1f));
		Singleton<LevelManager>.Instance.RescanGraphArea(colliderBounds);
	}
}
