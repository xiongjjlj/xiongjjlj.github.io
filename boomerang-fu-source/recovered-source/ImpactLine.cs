using UnityEngine;

public class ImpactLine : MonoBehaviour
{
	public ParticleSystem[] particles;

	public float startWidth;

	public float finishWidth;

	public float duration;

	private float timer;

	private Vector3 scale;

	private double lastTime;

	public void Play(Color startColor)
	{
		lastTime = Time.realtimeSinceStartup;
		timer = 0f;
		for (int i = 0; i < particles.Length; i++)
		{
			ParticleSystem.MainModule main = particles[i].main;
			main.startColor = startColor;
		}
	}

	private void Update()
	{
		if (timer < duration)
		{
			timer += Time.unscaledDeltaTime;
			float t = Time.realtimeSinceStartup - (float)lastTime;
			particles[0].Simulate(t, withChildren: true, restart: false);
			lastTime = Time.realtimeSinceStartup;
		}
		else
		{
			base.gameObject.Recycle();
		}
	}
}
