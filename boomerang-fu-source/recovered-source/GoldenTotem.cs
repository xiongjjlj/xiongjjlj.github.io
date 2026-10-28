using System;
using UnityEngine;

public class GoldenTotem : MonoBehaviour
{
	public Transform fillTransform;

	public GameObject winPFX;

	public ParticleSystem fillPFX;

	public ParticleSystem.MainModule fillPFXMain;

	public ParticleSystem.EmissionModule fillPFXEmission;

	private GoldenTotemFill totemFill;

	private int lastCharacterID = -1;

	private Color flashTargetColor;

	private Color emptyDefaultColor;

	private float flashTimer;

	private static float flashDuration = 0.3f;

	private void Start()
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		winPFX.SetActive(value: false);
		fillPFXMain = fillPFX.main;
		fillPFXEmission = fillPFX.emission;
		fillPFX.gameObject.SetActive(value: false);
		InitColors();
	}

	private void Update()
	{
		if (flashTimer > 0f)
		{
			flashTimer -= Time.deltaTime;
			totemFill.renderer.material.SetColor("_EmissionColor", Color.Lerp(flashTargetColor, totemFill.renderer.material.GetColor("_EmissionColor"), Easing.EaseOut(flashTimer / flashDuration)));
		}
	}

	public void InitColors()
	{
		totemFill = new GoldenTotemFill
		{
			transform = fillTransform,
			renderer = fillTransform.GetComponent<Renderer>(),
			fillAmount = 0f
		};
		Color color = Singleton<GameManager>.Instance.goldenDiscColor + new Color(0.25f, 0.25f, 0.25f);
		totemFill.renderer.material.color = color;
		totemFill.renderer.material.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(0.05f));
		totemFill.fillAmount = 0f;
		UpdateFillPositions();
		fillPFX.gameObject.SetActive(value: false);
	}

	public void SetPlayerTime(int characterID, float playerTime, float playerGoal)
	{
		if (totemFill.fillAmount == 0f && playerTime > 0f)
		{
			totemFill.transform.gameObject.SetActive(value: true);
		}
		else if (playerTime == 0f)
		{
			totemFill.transform.gameObject.SetActive(value: false);
		}
		float fillAmount = totemFill.fillAmount;
		totemFill.fillAmount = Mathf.Lerp(totemFill.fillAmount, Mathf.Clamp01(playerTime / playerGoal), Time.deltaTime * 10f);
		Color color = Singleton<GameManager>.Instance.characterColors[characterID][0];
		if (characterID != lastCharacterID)
		{
			totemFill.renderer.material.color = color;
			totemFill.renderer.material.SetColor("_EmissionColor", Color.white);
			flashTargetColor = color * Mathf.LinearToGammaSpace(0.05f);
			flashTimer = flashDuration;
		}
		else if (totemFill.fillAmount > fillAmount)
		{
			float num = Mathf.Clamp01(playerTime / playerGoal);
			totemFill.renderer.material.color = color * (1f - num * 0.3f + num * 0.3f * Mathf.Cos(MathF.PI * Time.time * 2f));
		}
		lastCharacterID = characterID;
		UpdateFillPositions();
	}

	public void WinRound(int characterID)
	{
		winPFX.SetActive(value: true);
		ParticleSystem.MainModule main = winPFX.GetComponent<ParticleSystem>().main;
		main.startColor = Singleton<GameManager>.Instance.characterColors[characterID][0];
		main = winPFX.transform.Find("WinDebris").GetComponent<ParticleSystem>().main;
		main.startColor = Singleton<GameManager>.Instance.characterColors[characterID][0];
		totemFill.fillAmount = 1.012f;
		UpdateFillPositions();
		totemFill.renderer.material.SetColor("_EmissionColor", Color.white);
		flashTimer = flashDuration;
	}

	private void UpdateFillPositions()
	{
		float num = 0f;
		if (totemFill.fillAmount == 0f)
		{
			totemFill.transform.gameObject.SetActive(value: false);
		}
		else
		{
			totemFill.transform.gameObject.SetActive(value: true);
		}
		totemFill.transform.localScale = new Vector3(1.01f, 1.01f, totemFill.fillAmount * 0.999f);
		totemFill.transform.localPosition = new Vector3(0f, 0f, 0f - (totemFill.fillAmount * 0.5f + num));
		num += totemFill.fillAmount;
	}
}
