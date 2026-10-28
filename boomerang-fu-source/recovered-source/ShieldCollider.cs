using UnityEngine;

public class ShieldCollider : CollisionBehaviour
{
	private Player parentPlayer;

	private RaycastHit2D hit;

	private void Start()
	{
		if (base.transform.parent.CompareTag("Player"))
		{
			parentPlayer = base.transform.parent.GetComponent<Player>();
		}
	}

	public override void OnTriggerEnter2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit)
		{
			parentPlayer.OnShieldColliderTriggered(collider);
		}
	}

	public override void OnTriggerStay2D(Collider2D collider)
	{
		hit = Physics2D.Raycast(base.transform.position, (collider.transform.position - base.transform.position).normalized, (collider.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle"));
		if (!hit)
		{
			parentPlayer.OnShieldColliderStay(collider);
		}
	}
}
