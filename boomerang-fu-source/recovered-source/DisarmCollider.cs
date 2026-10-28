using UnityEngine;

public class DisarmCollider : CollisionBehaviour
{
	private Player parent;

	private RaycastHit2D hit;

	private void Start()
	{
		parent = base.transform.parent.GetComponent<Player>();
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit)
		{
			parent.OnDisarmColliderTriggered(collider);
		}
	}
}
