using UnityEngine;

public class PickupCollider : CollisionBehaviour
{
	private Player parentCandidate;

	private Enemy parentEnemy;

	public bool isEnemy;

	private bool colliderTriggeredThisFrame;

	private void Start()
	{
		if (base.transform.parent.CompareTag("Player"))
		{
			parentCandidate = base.transform.parent.GetComponent<Player>();
			isEnemy = false;
		}
		else
		{
			parentEnemy = base.transform.parent.GetComponent<Enemy>();
			isEnemy = true;
		}
	}

	private void FixedUpdate()
	{
		colliderTriggeredThisFrame = false;
	}

	public override void OnTriggerEnter2D(Collider2D hit)
	{
		if (isEnemy)
		{
			parentEnemy.OnPickupColliderTriggered(hit);
		}
		else
		{
			parentCandidate.OnPickupColliderTriggered(hit);
		}
		colliderTriggeredThisFrame = true;
	}

	public override void OnTriggerStay2D(Collider2D hit)
	{
		if (!colliderTriggeredThisFrame && !isEnemy)
		{
			parentCandidate.OnPickupColliderStay(hit);
		}
	}
}
