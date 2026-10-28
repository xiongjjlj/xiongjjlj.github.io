using UnityEngine;

public class HatDebris : Debris3D
{
	[Header("Sound")]
	public string ricoSoundName = "event:/environment/crown_rico";

	public override void Reset()
	{
		rb.simulated = false;
		rb.velocity = Vector2.zero;
		debris.transform.localPosition = new Vector3(0f, 0f, debrisInitZ);
		debris.transform.localRotation = debrisInitRotation;
		debris.transform.localScale = Vector3.one;
		isVanishing = false;
		base.gameObject.SetActive(value: false);
	}

	protected override void OnBounce()
	{
		base.OnBounce();
		Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(ricoSoundName, "velocity", velocityZ / (speedLimit * 0.1f), base.transform);
	}

	protected override void OnLanded()
	{
		base.OnLanded();
		Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(ricoSoundName, "velocity", 0f, base.transform);
	}

	protected override void OnCollisionEnter2D(Collision2D collision)
	{
		base.OnCollisionEnter2D(collision);
		if (!isLanded)
		{
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(ricoSoundName, "velocity", velocityZ / (speedLimit * 0.1f), base.transform);
		}
	}
}
