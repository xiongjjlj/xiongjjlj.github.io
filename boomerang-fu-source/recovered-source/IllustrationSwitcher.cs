using UnityEngine;
using UnityEngine.UI;

public class IllustrationSwitcher : MonoBehaviour
{
	public UIButtonStyle buttonStyle;

	public Image image;

	public Sprite spriteA;

	public Sprite spriteB;

	private const float aMinDur = 2f;

	private const float aMaxDur = 4f;

	private const float bDur = 0.2f;

	private float dur = 1f;

	private float t;

	private void Update()
	{
		if (!buttonStyle.isSelected)
		{
			image.sprite = spriteA;
			return;
		}
		t += Time.unscaledDeltaTime;
		if (!(t < dur))
		{
			if (t < dur + 0.2f)
			{
				image.sprite = spriteB;
				return;
			}
			dur = Random.Range(2f, 4f);
			image.sprite = spriteA;
			t = 0f;
		}
	}
}
