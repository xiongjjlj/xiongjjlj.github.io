using UnityEngine;

public class VFXRico : MonoBehaviour
{
	public ParticleSystem[] particles;

	private int numParticles;

	public float lifetime;

	private float timer;

	public void Emit(Color color, float strength)
	{
		timer = 0f;
		Color color2 = color;
		ParticleSystem.MainModule main = particles[0].main;
		main.startColor = color2;
		if (strength >= 20f)
		{
			main = particles[1].main;
			main.startColor = color2;
			_ = particles[1].emission;
			particles[1].Emit(Mathf.RoundToInt(Mathf.Lerp(0f, 12f, Easing.EaseIn(Mathf.Clamp01(strength / 90f)))));
		}
		color2 -= new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(strength / 90f));
		if (strength >= 90f)
		{
			particles[2].gameObject.SetActive(value: true);
			main = particles[2].main;
			main.startColor = color2;
		}
		else
		{
			particles[2].gameObject.SetActive(value: false);
		}
		main = particles[0].main;
		main.startColor = color - new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(strength / 90f));
		particles[0].Play();
	}

	private void Update()
	{
		if (timer < lifetime)
		{
			timer += Time.deltaTime;
		}
		else
		{
			base.gameObject.Recycle();
		}
	}
}
