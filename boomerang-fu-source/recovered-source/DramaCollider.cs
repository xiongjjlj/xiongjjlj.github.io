using UnityEngine;

public class DramaCollider : CollisionBehaviour
{
	private Player parent;

	private void Start()
	{
		parent = base.transform.parent.GetComponent<Player>();
	}

	public override void OnTriggerEnter2D(Collider2D hit)
	{
		if (!hit.gameObject.CompareTag("Disc") || !(hit.GetComponent<Disc>().DiscOwner != parent))
		{
			hit.gameObject.CompareTag("MeleeCollider");
		}
	}
}
