using Steamworks;
using UnityEngine;

public class SteamAchievementManager : AchievementManager
{
	public void LoadAchievements()
	{
		Achievement[] array = achievements;
		foreach (Achievement achievement in array)
		{
			if (!SteamUserStats.GetAchievement(achievement.apiName, out achievement.achieved))
			{
				Debug.LogWarning("SteamUserStats.GetAchievement failed for Achievement " + achievement.apiName + "\nIs it registered in the Steam Partner site?");
			}
		}
	}

	public override Achievement UnlockAchievement(AchievementType achievementType)
	{
		if (!SteamManager.Initialized)
		{
			return null;
		}
		if (!Singleton<SteamUpdater>.Instance.areLocalStatsValid)
		{
			return null;
		}
		if (Singleton<SettingsManager>.Instance.demoMode)
		{
			return null;
		}
		Achievement achievement = base.UnlockAchievement(achievementType);
		if (achievement == null)
		{
			return null;
		}
		Debug.Log("Achievement unlocked! " + achievementType);
		SteamUserStats.SetAchievement(achievement.apiName);
		Singleton<SteamUpdater>.Instance.StoreStatsNextFrame();
		return achievement;
	}
}
