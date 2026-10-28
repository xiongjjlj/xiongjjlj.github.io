using DG.Tweening;
using FMOD.Studio;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class UIFillButton : MonoBehaviour
{
	private float prevAmt;

	public Image fillButton;

	public Image fillKey;

	public Image borderButton;

	public Image borderKey;

	public ButtonPrompt buttonPrompt;

	public Color emptyColor;

	public Color fillColor;

	public Color flashColor;

	public ParticleSystem fillPFX;

	private Vector3 initScale;

	public string onFlashSFX = string.Empty;

	private EventInstance fillSFXEvent;

	public void Awake()
	{
		initScale = base.transform.localScale;
	}

	private void OnEnable()
	{
		base.transform.localScale = initScale;
	}

	public void InitButtonPrompt(Rewired.Player rewiredPlayer, string action = null)
	{
		if (buttonPrompt != null)
		{
			buttonPrompt.Init(rewiredPlayer, action);
			if (Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(rewiredPlayer))
			{
				fillButton.gameObject.SetActive(value: true);
				borderButton.gameObject.SetActive(value: true);
				fillKey.gameObject.SetActive(value: false);
				borderKey.gameObject.SetActive(value: false);
				fillKey.color = emptyColor;
				borderKey.color = emptyColor;
			}
			else
			{
				fillButton.gameObject.SetActive(value: false);
				borderButton.gameObject.SetActive(value: false);
				fillKey.gameObject.SetActive(value: true);
				borderKey.gameObject.SetActive(value: true);
				fillKey.color = emptyColor;
				borderKey.color = emptyColor;
			}
		}
	}

	public void SetFill(float amt)
	{
		if (prevAmt == 0f && amt > 0f)
		{
			fillSFXEvent = Singleton<AudioManager>.Instance.CreateLoop("event:/ui/charge_up");
			if (fillSFXEvent.isValid())
			{
				fillSFXEvent.start();
			}
		}
		Singleton<AudioManager>.Instance.SetEventInstanceParameter(fillSFXEvent, "retrieveCharge", amt);
		if ((amt == 0f || amt == 1f) && fillSFXEvent.isValid())
		{
			fillSFXEvent.stop(STOP_MODE.ALLOWFADEOUT);
			fillSFXEvent.release();
		}
		if (fillButton != null)
		{
			if (fillButton.fillAmount == 0f && amt > 0f)
			{
				base.transform.DOComplete();
				base.transform.localScale = initScale;
				base.transform.DOPunchScale(Vector3.one * -0.05f, 0.2f, 2, 0f);
			}
			fillButton.fillAmount = amt;
			fillButton.color = Color.Lerp(emptyColor, fillColor, amt);
			if (amt > 0f)
			{
				borderButton.color = fillColor;
			}
			else
			{
				borderButton.color = emptyColor;
				if (fillSFXEvent.isValid())
				{
					fillSFXEvent.stop(STOP_MODE.ALLOWFADEOUT);
				}
			}
		}
		if (fillKey != null)
		{
			if (fillKey.fillAmount == 0f && amt > 0f)
			{
				base.transform.DOComplete();
				base.transform.localScale = initScale;
				base.transform.DOPunchScale(Vector3.one * -0.05f, 0.2f, 2, 0f);
			}
			fillKey.fillAmount = amt;
			fillKey.color = Color.Lerp(emptyColor, fillColor, amt);
			if (amt > 0f)
			{
				borderKey.color = fillColor;
			}
			else
			{
				borderKey.color = emptyColor;
			}
		}
		prevAmt = amt;
	}

	public void Flash()
	{
		if (fillKey != null && borderKey != null)
		{
			fillKey.transform.DOComplete();
			fillKey.DOColor(flashColor, 0.25f).SetEase(Ease.InQuad).From(isRelative: false);
			borderKey.DOColor(flashColor, 0.25f).SetEase(Ease.InQuad).From(isRelative: false);
			fillKey.transform.localScale = Vector3.one;
			fillKey.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 2, 0f);
		}
		if (fillButton != null && borderButton != null)
		{
			fillButton.transform.DOComplete();
			fillButton.DOColor(flashColor, 0.25f).SetEase(Ease.InQuad).From(isRelative: false);
			borderButton.DOColor(flashColor, 0.25f).SetEase(Ease.InQuad).From(isRelative: false);
			fillButton.transform.localScale = Vector3.one;
			fillButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 2, 0f);
		}
		if (fillPFX != null)
		{
			fillPFX.Play();
		}
		if (!string.IsNullOrEmpty(onFlashSFX))
		{
			Singleton<AudioManager>.Instance.PlayOneShot(onFlashSFX);
		}
	}

	private void OnDisable()
	{
		if (fillSFXEvent.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(fillSFXEvent);
		}
	}
}
