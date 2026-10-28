using UnityEngine;

public class MeleeCollider : CollisionBehaviour
{
	private Actor parentActor;

	private RaycastHit2D hit;

	private void Start()
	{
		parentActor = base.transform.parent.GetComponent<Actor>();
	}

	private void OnEnable()
	{
		base.transform.position = base.transform.position + Vector3.zero;
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit || (bool)hit == (bool)collider)
		{
			parentActor.OnMeleeColliderTriggered(collider);
		}
	}
}
