using UnityEngine;

public class DamageCollider : CollisionBehaviour
{
	private Actor parentActor;

	private Enemy parentEnemy;

	public bool isEnemy;

	private RaycastHit2D hit;

	private void Start()
	{
		if (base.transform.parent.CompareTag("Player"))
		{
			parentActor = base.transform.parent.GetComponent<Actor>();
			isEnemy = false;
		}
		else
		{
			parentEnemy = base.transform.parent.GetComponent<Enemy>();
			isEnemy = true;
		}
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit)
		{
			if (isEnemy)
			{
				parentEnemy.OnDamageColliderTriggered(collider);
			}
			else
			{
				parentActor.OnDamageColliderTriggered(collider);
			}
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit)
		{
			if (isEnemy)
			{
				parentEnemy.OnDamageColliderStay(collider);
			}
			else
			{
				parentActor.OnDamageColliderStay(collider);
			}
		}
	}
}
