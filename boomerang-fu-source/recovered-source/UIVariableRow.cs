using UnityEngine;
using UnityEngine.UI;

public class UIVariableRow : MonoBehaviour
{
	[Header("Enable row")]
	public bool enableForDeathMatch = true;

	public bool enableForTeamMatch = true;

	public bool enableForGoldenDisc = true;

	public bool enableForHideAndSeek = true;

	public void TurnOnOff()
	{
		bool num = !Singleton<SettingsManager>.Instance.teamMatch && Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch;
		bool teamMatch = Singleton<SettingsManager>.Instance.teamMatch;
		bool flag = Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc;
		bool flag2 = Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek;
		bool flag3 = true;
		if (num)
		{
			flag3 = enableForDeathMatch;
		}
		else if (teamMatch)
		{
			flag3 = enableForTeamMatch;
		}
		else if (flag)
		{
			flag3 = enableForGoldenDisc;
		}
		else if (flag2)
		{
			flag3 = enableForHideAndSeek;
		}
		base.gameObject.SetActive(flag3);
		Selectable componentInChildren = GetComponentInChildren<Selectable>();
		if (componentInChildren != null)
		{
			componentInChildren.interactable = flag3;
		}
	}
}
