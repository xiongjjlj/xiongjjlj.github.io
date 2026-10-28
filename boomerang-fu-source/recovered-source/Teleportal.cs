using UnityEngine;

public class Teleportal : MonoBehaviour
{
	public Teleportal exit;

	[HideInInspector]
	public float thickness;

	public bool isSafeForAIs = true;

	private Color flashColor;

	private Color glowColor;

	public Color initColor;

	private Color initGlowColor;

	private static float flashDuration = 0.5f;

	private float flashTime;

	private Renderer rend;

	private Renderer glowRend;

	private ScrollingUV scrollingUV;

	private Vector2 initScrollSpeed;

	private Vector2 flashScrollSpeed = new Vector2(-0.5f, 0f);

	private void Start()
	{
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		thickness = component.size.y * 0.5f + component.offset.y;
		rend = base.transform.Find("Teleport_Wall_Exit").GetComponent<Renderer>();
		glowRend = rend.transform.Find("GroundGlow").GetComponent<Renderer>();
		initColor = rend.material.GetColor("_Color");
		initGlowColor = glowRend.material.GetColor("_Color");
		scrollingUV = base.transform.GetComponentInChildren<ScrollingUV>();
		initScrollSpeed = scrollingUV.scrollSpeed;
	}

	private void Update()
	{
		if (flashTime > 0f)
		{
			flashTime -= Time.deltaTime;
			rend.material.SetColor("_Color", Color.Lerp(initColor, flashColor, Easing.EaseOut(flashTime / flashDuration)));
			glowRend.material.SetColor("_Color", Color.Lerp(initGlowColor, glowColor, Easing.EaseOut(flashTime / flashDuration)));
			scrollingUV.scrollSpeed = Vector2.Lerp(initScrollSpeed, flashScrollSpeed, Easing.EaseOut(flashTime / flashDuration));
		}
	}

	public void Flash(Color inputColor)
	{
		flashTime = flashDuration;
		flashColor = inputColor;
		glowColor = new Color(inputColor.r, inputColor.g, inputColor.b, glowColor.a);
	}
}
