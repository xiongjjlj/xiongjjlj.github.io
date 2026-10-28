using UnityEngine;

public class Blink : MonoBehaviour
{
	private bool blinkOn;

	private float blinkTimer;

	private float blinkRate = 0.075f;

	private Renderer blinkRenderer;

	public bool isBlinking;

	public int fadeAmt;

	private void Start()
	{
		blinkRenderer = GetComponent<Renderer>();
	}

	private void Update()
	{
		if (isBlinking)
		{
			if (blinkTimer < blinkRate)
			{
				blinkTimer += Time.deltaTime;
				return;
			}
			ToggleBlink();
			blinkTimer = 0f;
		}
		else
		{
			blinkRenderer.material.color = new Color(blinkRenderer.material.color.r, blinkRenderer.material.color.g, blinkRenderer.material.color.b, 0.25f);
		}
	}

	private void ToggleBlink()
	{
		if (blinkOn)
		{
			blinkOn = false;
			blinkRenderer.material.color = new Color(blinkRenderer.material.color.r, blinkRenderer.material.color.g, blinkRenderer.material.color.b, 0f);
		}
		else
		{
			blinkOn = true;
			blinkRenderer.material.color = new Color(blinkRenderer.material.color.r, blinkRenderer.material.color.g, blinkRenderer.material.color.b, 0.5f);
		}
	}

	public void SetScale(int complete, int total)
	{
		float x = (float)complete / (float)total * 0.66f;
		base.transform.localScale = new Vector3(x, 0.66f, 0.66f);
	}

	public void ResetScale()
	{
		base.transform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
	}
}
