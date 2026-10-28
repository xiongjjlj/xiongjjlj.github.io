using System;
using UnityEngine;

public class AchievementManager : Singleton<AchievementManager>
{
	public enum AchievementType
	{
		VengefulGhost = 1,
		MasterOfDisguise,
		TeleportAssassin,
		TookOneForTheTeam,
		TelekineticKiller,
		ArtfulDodger,
		ShortFuse,
		Firestarter,
		IceBreaker,
		TrashCompactor,
		WhoNeedsWeapons,
		Rambo,
		Punctuality
	}

	[Serializable]
	public class Achievement
	{
		public AchievementType achievementType;

		public string apiName;

		public bool achieved;

		public Achievement(AchievementType achievementType, string apiName)
		{
			this.achievementType = achievementType;
			this.apiName = apiName;
			achieved = false;
		}
	}

	[SerializeField]
	protected Achievement[] achievements = new Achievement[13]
	{
		new Achievement(AchievementType.VengefulGhost, "Vengeful_Ghost"),
		new Achievement(AchievementType.MasterOfDisguise, "Master_of_Disguise"),
		new Achievement(AchievementType.TeleportAssassin, "Teleport_Assassin"),
		new Achievement(AchievementType.TookOneForTheTeam, "Took_One_For_The_Team"),
		new Achievement(AchievementType.TelekineticKiller, "Telekinetic_Killer"),
		new Achievement(AchievementType.ArtfulDodger, "Artful_Dodger"),
		new Achievement(AchievementType.ShortFuse, "Short_Fuse"),
		new Achievement(AchievementType.Firestarter, "Firestarter"),
		new Achievement(AchievementType.IceBreaker, "Ice_Breaker"),
		new Achievement(AchievementType.TrashCompactor, "Trash_Compactor"),
		new Achievement(AchievementType.WhoNeedsWeapons, "Who_Needs_Weapons?"),
		new Achievement(AchievementType.Rambo, "Rambo"),
		new Achievement(AchievementType.Punctuality, "Punctuality")
	};

	public void UnlockAchievementFromMatchAward(MatchAward award)
	{
		if (!Singleton<SettingsManager>.Instance.demoMode)
		{
			switch (award.Name)
			{
			case "MostKillsAfterDeath":
				UnlockAchievement(AchievementType.VengefulGhost);
				break;
			case "MostTimeDisguised":
				UnlockAchievement(AchievementType.MasterOfDisguise);
				break;
			case "MostTeleportKills":
				UnlockAchievement(AchievementType.TeleportAssassin);
				break;
			case "MostTeamDeaths":
				UnlockAchievement(AchievementType.TookOneForTheTeam);
				break;
			case "MostControlledKills":
				UnlockAchievement(AchievementType.TelekineticKiller);
				break;
			case "MostDodges":
				UnlockAchievement(AchievementType.ArtfulDodger);
				break;
			case "MostExplosiveSuicides":
				UnlockAchievement(AchievementType.ShortFuse);
				break;
			case "MostFireKills":
				UnlockAchievement(AchievementType.Firestarter);
				break;
			case "MostFrozenKills":
				UnlockAchievement(AchievementType.IceBreaker);
				break;
			case "MostSquishKills":
				UnlockAchievement(AchievementType.TrashCompactor);
				break;
			case "MostKillsWhileDisarmed":
				UnlockAchievement(AchievementType.WhoNeedsWeapons);
				break;
			case "MostTimeInPlants":
				UnlockAchievement(AchievementType.Rambo);
				break;
			case "NoAward":
				UnlockAchievement(AchievementType.Punctuality);
				break;
			}
		}
	}

	protected Achievement GetAchievement(AchievementType achievementType)
	{
		Achievement[] array = achievements;
		foreach (Achievement achievement in array)
		{
			if (achievement.achievementType == achievementType)
			{
				return achievement;
			}
		}
		Debug.LogError("Achievement " + achievementType.ToString() + " not found in local achivements array");
		return null;
	}

	public virtual Achievement UnlockAchievement(AchievementType achievementType)
	{
		Achievement achievement = GetAchievement(achievementType);
		if (achievement == null)
		{
			return null;
		}
		if (achievement.achieved)
		{
			return null;
		}
		return achievement;
	}
}
