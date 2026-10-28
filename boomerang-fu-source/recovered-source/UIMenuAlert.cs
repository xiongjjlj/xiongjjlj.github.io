using DG.Tweening;
using I2.Loc;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuAlert : UIMenuBase
{
	private CanvasGroup canvasGroup;

	public Image background;

	public TextMeshProUGUI header;

	public GameObject connectedGlyph;

	public GameObject disconnectedGylph;

	public LocalizedString connectedString;

	public LocalizedString disconnectedString;

	private float alertTimestamp;

	private bool isFadingOut;

	private static float alertDuration = 2f;

	private static float fadeDuration = 0.4f;

	public Color disconnectedColor;

	public Color connectedColor;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	public override void Init()
	{
		canvasGroup.DOComplete();
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 0.15f);
		Singleton<UIManager>.Instance.ZoomFrom(1.111f, 0.15f);
		Singleton<Camera3D>.Instance.LerpFOVTo(17.5f, 0.15f);
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		alertTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		isFadingOut = false;
		UpdateMasterButtonPrompts();
	}

	public void ControllerConnected()
	{
		alertTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		header.text = connectedString;
		background.color = connectedColor;
		connectedGlyph.SetActive(value: true);
		disconnectedGylph.SetActive(value: false);
		ShowMasterButtonPrompts();
	}

	public void ControllerDisconnected()
	{
		alertTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		header.text = disconnectedString;
		background.color = disconnectedColor;
		connectedGlyph.SetActive(value: false);
		disconnectedGylph.SetActive(value: true);
		if (ReInput.controllers.joystickCount == 0 && Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC)
		{
			HideMasterButtonPrompts();
		}
		else
		{
			ShowMasterButtonPrompts();
		}
	}

	private void Update()
	{
		if (ReInput.controllers.joystickCount != 0 || Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Xbox)
		{
			if (!isFadingOut && Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(alertTimestamp - fadeDuration) >= alertDuration)
			{
				isFadingOut = true;
				canvasGroup.DOFade(0f, fadeDuration);
			}
			if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(alertTimestamp) >= alertDuration)
			{
				Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
				UIResponse_Close();
			}
		}
	}

	private void ShowMasterButtonPrompts()
	{
		for (int i = 0; i < masterButtonPrompts.Length; i++)
		{
			masterButtonPrompts[i].gameObject.SetActive(value: true);
		}
	}

	private void HideMasterButtonPrompts()
	{
		for (int i = 0; i < masterButtonPrompts.Length; i++)
		{
			masterButtonPrompts[i].gameObject.SetActive(value: false);
		}
	}

	public override void Cleanup()
	{
	}
}
