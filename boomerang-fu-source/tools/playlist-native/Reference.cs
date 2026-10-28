using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
public class LevelManagerReference {
 public List<LevelAsset> levelAssets=new List<LevelAsset>(),switchIncompatibleLevels=new List<LevelAsset>(),matchPlaylist=new List<LevelAsset>();
 public int matchIndex,powerupsCollectedSoFar,lastMatchIndexBeforePowerupsSpawn,powerupBuilds;
 public void BuildPossiblePowerups(){powerupBuilds++;}
public void BuildMatchPlaylist(bool rematch = false)
	{
		matchPlaylist.Clear();
		if (Singleton<SettingsManager>.Instance.debugMode)
		{
			matchPlaylist = levelAssets;
			matchPlaylist.RemoveAll((LevelAsset x) => x.complexity == LevelAsset.Complexity.Tutorial);
			matchIndex = 0;
			powerupsCollectedSoFar = 0;
			if (rematch)
			{
				lastMatchIndexBeforePowerupsSpawn = 1;
			}
			else
			{
				lastMatchIndexBeforePowerupsSpawn = 4;
			}
			BuildPossiblePowerups();
			return;
		}
		List<LevelAsset> list = new List<LevelAsset>();
		bool flag = false;
		for (int num = 0; num < levelAssets.Count; num++)
		{
			if (levelAssets[num].soloInPlaylist)
			{
				flag = true;
				break;
			}
		}
		for (int num2 = 0; num2 < levelAssets.Count; num2++)
		{
			LevelAsset levelAsset = levelAssets[num2];
			if ((Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Switch || Singleton<SettingsManager>.Instance.UseCustomMaps || !switchIncompatibleLevels.Contains(levelAsset) || Singleton<SettingsManager>.Instance.numBots <= 0) && (!flag || levelAsset.soloInPlaylist) && (Singleton<SettingsManager>.Instance.UseCustomMaps || (levelAsset.minPlayers <= Singleton<GameManager>.Instance.players.Count && levelAsset.maxPlayers >= Singleton<GameManager>.Instance.players.Count)) && levelAsset.supportedMatchTypes.Contains(Singleton<SettingsManager>.Instance.matchType) && Singleton<GameManager>.Instance.IsDLCUnlocked(levelAsset.requiredDLC) && !IsLevelDisabledInMatchSettings(levelAsset))
			{
				list.Add(levelAsset);
			}
		}
		list.Shuffle();
		int baseInARow = 0;
		int dlcInARow = 0;
		int maximumLevelsInARow = 2;
		int num3 = ((!rematch && !Singleton<SettingsManager>.Instance.trailerMode && !Singleton<SettingsManager>.Instance.debugMode) ? 3 : 0);
		int num4 = 0;
		for (int num5 = list.Count - 1; num5 >= 0; num5--)
		{
			LevelAsset levelAsset2 = list[num5];
			if (levelAsset2.complexity == LevelAsset.Complexity.Tutorial)
			{
				if (!rematch && num4 == 0 && Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
				{
					matchPlaylist.Add(levelAsset2);
					baseInARow++;
					num4++;
				}
				list.RemoveAt(num5);
			}
		}
		while (list.Count > 0)
		{
			List<LevelAsset> list2 = list.Where((LevelAsset level) => level.complexity == LevelAsset.Complexity.Simple).ToList();
			if (num3 > 0 && list2.Count > 0)
			{
				FilterLevelsAndAddLevelToPlaylist(list, list2, ref baseInARow, ref dlcInARow, maximumLevelsInARow);
				num3--;
			}
			else
			{
				FilterLevelsAndAddLevelToPlaylist(list, list, ref baseInARow, ref dlcInARow, maximumLevelsInARow);
			}
		}
		for (int num6 = matchPlaylist.Count - 1; num6 >= 0; num6--)
		{
			if (matchPlaylist[num6].forceFirstInPlaylist)
			{
				MoveItemInPlaylist(ref matchPlaylist, num6, 0);
			}
		}
		matchIndex = 0;
		powerupsCollectedSoFar = 0;
		if (rematch)
		{
			lastMatchIndexBeforePowerupsSpawn = 1;
		}
		else
		{
			lastMatchIndexBeforePowerupsSpawn = 4;
		}
		BuildPossiblePowerups();
		Debug.Log($"BuildMatchPlaylist for {Singleton<SettingsManager>.Instance.matchType} mode");
	}
public void FilterLevelsAndAddLevelToPlaylist(List<LevelAsset> masterListOfLevels, List<LevelAsset> candidateLevels, ref int baseInARow, ref int dlcInARow, int maximumLevelsInARow)
	{
		if (baseInARow >= maximumLevelsInARow)
		{
			List<LevelAsset> list = candidateLevels.Where((LevelAsset level) => level.requiredDLC != DownloadableContentID.None).ToList();
			if (list.Count > 0)
			{
				AddLevelToPlaylist(masterListOfLevels, list);
				baseInARow = 0;
				dlcInARow = 1;
			}
			else
			{
				AddLevelToPlaylist(masterListOfLevels, candidateLevels);
			}
		}
		else if (dlcInARow >= maximumLevelsInARow)
		{
			List<LevelAsset> list2 = candidateLevels.Where((LevelAsset level) => level.requiredDLC == DownloadableContentID.None).ToList();
			if (list2.Count > 0)
			{
				AddLevelToPlaylist(masterListOfLevels, list2);
				baseInARow = 1;
				dlcInARow = 0;
			}
			else
			{
				AddLevelToPlaylist(masterListOfLevels, candidateLevels);
			}
		}
		else
		{
			if (candidateLevels[0].requiredDLC == DownloadableContentID.None)
			{
				baseInARow++;
			}
			else
			{
				dlcInARow++;
			}
			AddLevelToPlaylist(masterListOfLevels, candidateLevels);
		}
	}
public void AddLevelToPlaylist(List<LevelAsset> masterListOfLevels, List<LevelAsset> candidateLevels)
	{
		LevelAsset item = candidateLevels[0];
		matchPlaylist.Add(item);
		masterListOfLevels.Remove(item);
	}
public bool IsLevelDisabledInMatchSettings(LevelAsset levelAsset)
	{
		if (levelAsset.complexity == LevelAsset.Complexity.Tutorial)
		{
			return false;
		}
		int index = levelAssets.IndexOf(levelAsset);
		return IsLevelDisabledInMatchSettings(index);
	}
public bool IsLevelDisabledInMatchSettings(int index)
	{
		if (!Singleton<SettingsManager>.Instance.UseCustomMaps)
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch)
		{
			if (Singleton<SettingsManager>.Instance.MatchSettings.disabledDeathmatchLevels.Contains(index))
			{
				return true;
			}
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.MatchSettings.disabledGoldenDiscLevels.Contains(index))
		{
			return true;
		}
		return false;
	}
public void MoveItemInPlaylist(ref List<LevelAsset> list, int oldIndex, int newIndex)
	{
		LevelAsset item = list[oldIndex];
		list.RemoveAt(oldIndex);
		if (newIndex > oldIndex)
		{
			newIndex--;
		}
		list.Insert(newIndex, item);
	}
 public string currentScene; public int Consume(){ if(matchPlaylist.Count==0)return -1;int chosen=matchPlaylist[matchIndex%matchPlaylist.Count].id;
		currentScene = matchPlaylist[matchIndex % matchPlaylist.Count].path;
		if (matchPlaylist[matchIndex % matchPlaylist.Count].complexity == LevelAsset.Complexity.Tutorial)
		{
			matchPlaylist.RemoveAt(matchIndex);
		}
		else
		{
			matchIndex++;
		}
return chosen;}

}
