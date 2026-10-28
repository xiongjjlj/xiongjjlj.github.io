using I2.Loc;
using TMPro;
using UnityEngine;

public class UISliderButtonLanguage : UISliderButtonNoLocalization
{
	public Localize localizeTarget;

	private TextMeshProUGUI localizeTargetText;

	public override void SetValue(int input, bool sendCallback = true)
	{
		base.SetValue(input, sendCallback);
		TranslateLocalizeTarget();
	}

	public void TranslateLocalizeTarget()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		if (localizeTarget != null && localizeTargetText == null)
		{
			localizeTargetText = localizeTarget.GetComponent<TextMeshProUGUI>();
		}
		if (localizeTarget != null && localizeTargetText != null)
		{
			string languageString = Singleton<SettingsManager>.Instance.GetLanguageString(value);
			if (!string.IsNullOrEmpty(languageString))
			{
				localizeTargetText.text = LocalizationManager.GetTranslation(localizeTarget.Term, FixForRTL: true, 0, ignoreRTLnumbers: true, applyParameters: false, null, languageString);
			}
		}
	}
}
