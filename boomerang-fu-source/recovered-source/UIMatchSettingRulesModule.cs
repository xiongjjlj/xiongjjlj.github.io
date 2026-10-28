using UnityEngine;

public class UIMatchSettingRulesModule : UIMatchSettingModule
{
	public GameObject modifiedMatchSettingsBadge;

	public GameObject modifiedMapsBadge;

	public GameObject chooseMapsChoice;

	public override void Enable()
	{
		base.Enable();
		if (modifiedMatchSettingsBadge != null)
		{
			modifiedMatchSettingsBadge.SetActive(!AreModifiersDefaultValues());
		}
		if (modifiedMapsBadge != null)
		{
			modifiedMapsBadge.SetActive(Singleton<SettingsManager>.Instance.UseCustomMaps);
		}
		Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch)
		{
			chooseMapsChoice.SetActive(value: true);
		}
		else
		{
			chooseMapsChoice.SetActive(value: false);
		}
	}

	private bool AreModifiersDefaultValues()
	{
		MatchSettings matchSettings = Singleton<SettingsManager>.Instance.MatchSettings;
		MatchSettings matchSettings2 = new MatchSettings();
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (matchSettings.matchLength != matchSettings2.matchLength || matchSettings.hideAndSeekAttempts != matchSettings2.hideAndSeekAttempts || matchSettings.hideAndSeekTimeLimit != matchSettings2.hideAndSeekTimeLimit || matchSettings.hideAndSeekHideTime != matchSettings2.hideAndSeekHideTime || matchSettings.canRetrieveDiscs != matchSettings2.canRetrieveDiscs || matchSettings.homingEnabled != matchSettings2.homingEnabled || matchSettings.edgeProtection != matchSettings2.edgeProtection)
			{
				return false;
			}
			return true;
		}
		if (matchSettings.autoBalance != matchSettings2.autoBalance || matchSettings.availablePowerups != matchSettings2.availablePowerups || matchSettings.powerupSpawnFrequency != matchSettings2.powerupSpawnFrequency || matchSettings.hideAndSeekAttempts != matchSettings2.hideAndSeekAttempts || matchSettings.canCatchTeammatesDiscs != matchSettings2.canCatchTeammatesDiscs || matchSettings.canRetrieveDiscs != matchSettings2.canRetrieveDiscs || matchSettings.canStealOpponentDiscs != matchSettings2.canStealOpponentDiscs || matchSettings.canSwapDiscs != matchSettings2.canSwapDiscs || matchSettings.canThrowDiscs != matchSettings2.canThrowDiscs || matchSettings.dashDisarmsOpponents != matchSettings2.dashDisarmsOpponents || matchSettings.deadDiscsArePublic != matchSettings2.deadDiscsArePublic || matchSettings.discsHaveKnockback != matchSettings2.discsHaveKnockback || matchSettings.homingEnabled != matchSettings2.homingEnabled || matchSettings.edgeProtection != matchSettings2.edgeProtection || matchSettings.matchLength != matchSettings2.matchLength)
		{
			return false;
		}
		if (!Singleton<SettingsManager>.Instance.teamMatch && Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch)
		{
			if (matchSettings.includeTutorial != matchSettings2.includeTutorial || matchSettings.suddenDeathTime != matchSettings2.suddenDeathTime || matchSettings.victoryConditionDeathMatch != matchSettings2.victoryConditionDeathMatch)
			{
				return false;
			}
		}
		else if (Singleton<SettingsManager>.Instance.teamMatch)
		{
			if (matchSettings.includeTutorial != matchSettings2.includeTutorial || matchSettings.friendlyFire != matchSettings2.friendlyFire || matchSettings.teamRevives != matchSettings2.teamRevives || matchSettings.suddenDeathTime != matchSettings2.suddenDeathTime || matchSettings.victoryConditionTeamUp != matchSettings2.victoryConditionTeamUp || matchSettings.edgeProtection != matchSettings2.edgeProtection)
			{
				return false;
			}
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && matchSettings.victoryConditionGoldenBoomerang != matchSettings2.victoryConditionGoldenBoomerang)
		{
			return false;
		}
		return true;
	}
}
