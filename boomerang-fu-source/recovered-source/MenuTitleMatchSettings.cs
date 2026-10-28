using TMPro;
using UnityEngine;

public class MenuTitleMatchSettings : MonoBehaviour
{
	public TextMeshProUGUI text;

	public void UpdateTitle()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
		{
			text.text = "KING OF THE HILL";
		}
		else if (Singleton<SettingsManager>.Instance.teamMatch)
		{
			text.text = "TEAM MATCH";
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch)
		{
			text.text = "DEATH MATCH";
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			text.text = "GOLDEN BOOMERANG";
		}
	}
}
