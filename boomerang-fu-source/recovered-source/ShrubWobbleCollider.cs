using FMODUnity;
using UnityEngine;

public class ShrubWobbleCollider : MonoBehaviour
{
	public WobblyObstacle wobblyObstacle;

	private Collision2D lastCollision2D;

	private Actor lastActor;

	[EventRef]
	public string soundEvent;

	private void OnCollisionEnter2D(Collision2D other)
	{
		Actor component = other.gameObject.GetComponent<Actor>();
		if (component != null && !string.IsNullOrEmpty(soundEvent))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(soundEvent, "velocity", component.velocity.magnitude / component.speedLimit, base.transform);
		}
	}

	private void OnCollisionStay2D(Collision2D other)
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
		{
			if (lastCollision2D == null || lastCollision2D != other || lastActor == null)
			{
				lastActor = other.gameObject.GetComponent<Actor>();
				lastCollision2D = other;
			}
			if (lastActor != null)
			{
				wobblyObstacle.Interact((base.transform.position - other.transform.position).normalized * lastActor.velocity.magnitude * 0.3f);
			}
		}
	}
}
