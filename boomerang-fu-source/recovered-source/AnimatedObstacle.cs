using FMODUnity;
using UnityEngine;

public class AnimatedObstacle : InteractiveObstacle
{
	public Animator anim;

	public float animationCooldown;

	private float lastAnimationTime;

	[EventRef]
	public string soundEvent;

	public override void Interact(Vector2 force)
	{
		base.Interact(force);
		if (animationCooldown > 0f && Singleton<TimeKeeper>.Instance.TimeSince(lastAnimationTime) < animationCooldown)
		{
			return;
		}
		if (anim != null)
		{
			if (Vector2.Angle(force, -base.transform.up) > 90f)
			{
				anim.SetTrigger("Hit");
			}
			else
			{
				anim.SetTrigger("HitBack");
			}
		}
		if (!string.IsNullOrEmpty(soundEvent))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt(soundEvent, base.transform.position);
		}
	}
}
