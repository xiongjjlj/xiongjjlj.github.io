using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;

public class UIImageLocalization : MonoBehaviour
{
	[Serializable]
	public class LocalizedSprite
	{
		public string language;

		public Sprite sprite;
	}

	public LocalizedSprite[] spriteOverrides;

	private Image image;

	private Sprite defaultSprite;

	private void OnEnable()
	{
		if (spriteOverrides == null)
		{
			return;
		}
		image = GetComponent<Image>();
		defaultSprite = image.sprite;
		for (int i = 0; i < spriteOverrides.Length; i++)
		{
			if (LocalizationManager.CurrentLanguage == spriteOverrides[i].language)
			{
				image.sprite = spriteOverrides[i].sprite;
				return;
			}
		}
		image.sprite = defaultSprite;
	}
}
