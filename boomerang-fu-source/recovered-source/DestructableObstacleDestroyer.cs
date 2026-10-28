using UnityEngine;

public class DestructableObstacleDestroyer : MonoBehaviour
{
	public LayerMask collisionLayer;

	private Collider2D collider2D;

	private Collider2D[] overlappingColliders = new Collider2D[16];

	private ContactFilter2D contactFilter2D;

	private void Start()
	{
		collider2D = GetComponent<Collider2D>();
		contactFilter2D.SetLayerMask(collisionLayer);
		contactFilter2D.useTriggers = false;
	}

	private void Update()
	{
		int num = Physics2D.OverlapCollider(collider2D, contactFilter2D, overlappingColliders);
		for (int i = 0; i < num; i++)
		{
			if (!(overlappingColliders[i] != null))
			{
				continue;
			}
			if (overlappingColliders[i].CompareTag("InteractiveObstacle"))
			{
				overlappingColliders[i].GetComponent<InteractiveObstacle>().Interact(Vector2.zero);
			}
			else if (overlappingColliders[i].CompareTag("Powerup"))
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SparkleBurst, overlappingColliders[i].transform.position, Singleton<ColorManager>.Instance.powerupColor);
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/destroy_power", overlappingColliders[i].transform.position);
				Object.Destroy(overlappingColliders[i].gameObject);
			}
			else if (overlappingColliders[i].CompareTag("Player"))
			{
				if (Singleton<GameManager>.Instance.activeDecoys.Count > 0)
				{
					Decoy component = overlappingColliders[i].transform.GetComponent<Decoy>();
					if (component != null)
					{
						component.Die(Actor.DeathType.outofbounds);
					}
				}
				Player component2 = overlappingColliders[i].transform.GetComponent<Player>();
				if (component2 != null && component2.actorState == Actor.ActorState.Dead && component2.character != null && !component2.character.IsSquished)
				{
					component2.GetSquished();
				}
			}
			else if (overlappingColliders[i].gameObject.layer == LayerMask.NameToLayer("Particle3D"))
			{
				Debris3D component3 = overlappingColliders[i].gameObject.GetComponent<Debris3D>();
				if (component3 != null)
				{
					component3.GetSquished();
				}
			}
		}
	}
}
