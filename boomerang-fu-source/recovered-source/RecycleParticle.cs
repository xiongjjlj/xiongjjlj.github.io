using UnityEngine;

public class RecycleParticle : MonoBehaviour
{
	private ParticleSystem particle;

	private bool particleStarted;

	private void Start()
	{
		particle = GetComponent<ParticleSystem>();
	}

	private void OnEnable()
	{
		particleStarted = true;
	}

	private void Update()
	{
		if (!particle.isPlaying && particleStarted)
		{
			particleStarted = false;
			base.gameObject.Recycle();
		}
	}
}
