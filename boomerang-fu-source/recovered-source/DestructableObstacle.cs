using FMODUnity;
using UnityEngine;

public class DestructableObstacle : InteractiveObstacle
{
	public MovingFloor movingFloor;

	public int hitsLeft = 1;

	public PFXType particleEffect;

	public Transform derisParent;

	private Debris3D[] debris;

	[EventRef]
	public string soundEventOnDestroy;

	public bool rescanPathfindingWhenDestroyed = true;

	protected override void Start()
	{
		base.Start();
		if (derisParent != null)
		{
			debris = derisParent.GetComponentsInChildren<Debris3D>();
			for (int i = 0; i < debris.Length; i++)
			{
				debris[i].gameObject.SetActive(value: false);
			}
		}
	}

	public override void Interact(Vector2 force)
	{
		base.Interact(force);
		TakeDamage(force.normalized);
	}

	private void TakeDamage(Vector2 force)
	{
		hitsLeft--;
		if (hitsLeft != 0)
		{
			return;
		}
		if (particleEffect != PFXType.None)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(particleEffect, base.transform.position, base.transform.parent);
		}
		if (derisParent != null)
		{
			for (int i = 0; i < debris.Length; i++)
			{
				debris[i].gameObject.SetActive(value: true);
				Vector2 vector = ((Vector2)(debris[i].transform.position - base.transform.position)).normalized;
				if (vector.magnitude < 1f)
				{
					vector = Random.insideUnitCircle;
				}
				vector += force;
				debris[i].ApplyForce(vector.normalized);
				debris[i].transform.SetParent(null, worldPositionStays: true);
			}
		}
		if (!string.IsNullOrEmpty(soundEventOnDestroy))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt(soundEventOnDestroy, base.transform.position);
		}
		if (rescanPathfindingWhenDestroyed && !(movingFloor != null))
		{
			Singleton<LevelManager>.Instance.RescanGraphArea(GetComponent<Collider2D>().bounds);
			Singleton<LevelManager>.Instance.level.UpdateObstacles();
		}
		base.gameObject.SetActive(value: false);
	}

	public void TakeSinkDamage()
	{
		hitsLeft--;
		if (hitsLeft == 0)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Sink, base.transform.position, base.transform.parent);
			base.gameObject.SetActive(value: false);
		}
	}
}
