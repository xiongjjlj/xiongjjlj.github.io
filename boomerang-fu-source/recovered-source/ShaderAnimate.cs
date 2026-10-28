using UnityEngine;
using UnityEngine.UI;

public class ShaderAnimate : MonoBehaviour
{
	private Image theImage;

	private float timer;

	public float speed = 0.06f;

	public float maxValue = 0.172f;

	private void Start()
	{
		theImage = GetComponent<Image>();
		theImage.material = Object.Instantiate(theImage.material);
	}

	private void Update()
	{
		if (theImage != null)
		{
			timer = (timer + speed * Time.unscaledDeltaTime) % maxValue;
			theImage.material.SetFloat("_SetTimer", timer);
		}
	}
}
