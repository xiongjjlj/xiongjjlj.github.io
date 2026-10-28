using UnityEngine;

public class InsidesCollider : MonoBehaviour
{
	private Actor actor;

	private void Start()
	{
		actor = base.transform.parent.GetComponent<Actor>();
	}

	private void OnTriggerEnter2D(Collider2D hit)
	{
		if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Debris, base.transform.position, actor.character.Color, null, Quaternion.Inverse(base.transform.rotation));
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/dashthroughwalls_out", base.transform.position);
			if (hit.gameObject.CompareTag("OOB"))
			{
				actor.ReverseDash();
			}
		}
	}

	private void OnTriggerExit2D(Collider2D hit)
	{
		if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Debris, base.transform.position, actor.character.Color, null, base.transform.rotation);
		}
	}
}
