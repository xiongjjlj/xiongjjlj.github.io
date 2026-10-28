using UnityEngine;

public class Enemy : Actor
{
	[Header("Enemy particle effects")]
	public ParticleSystem exclamPFX;

	public ParticleSystem questionPFX;

	public ParticleSystem stunnedPFX;

	public override bool IsStunned
	{
		get
		{
			return base.IsStunned;
		}
		set
		{
			if (base.IsStunned != value)
			{
				base.IsStunned = value;
				if (value && pushStunDuration > 0.5f)
				{
					stunnedPFX.gameObject.SetActive(value: true);
				}
				if (!value)
				{
					stunnedPFX.gameObject.SetActive(value: false);
				}
			}
		}
	}
}
