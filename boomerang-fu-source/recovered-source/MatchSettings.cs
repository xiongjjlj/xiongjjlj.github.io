using System;
using System.Collections.Generic;

[Serializable]
public class MatchSettings
{
	public MatchLength matchLength = MatchLength.Medium;

	public VictoryCondition victoryConditionDeathMatch;

	public VictoryCondition victoryConditionTeamUp = VictoryCondition.Survival;

	public VictoryCondition victoryConditionGoldenBoomerang = VictoryCondition.Survival;

	public PowerupType availablePowerups;

	public PowerupSpawnFrequency powerupSpawnFrequency = PowerupSpawnFrequency.Normal;

	public bool includeTutorial = true;

	public bool canStealOpponentDiscs;

	public bool canCatchTeammatesDiscs;

	public bool canSwapDiscs = true;

	public bool teamRevives = true;

	public bool deadDiscsArePublic = true;

	public bool dashDisarmsOpponents;

	public bool discsHaveKnockback = true;

	public bool friendlyFire;

	public bool homingEnabled = true;

	public bool autoBalance = true;

	public bool canRetrieveDiscs = true;

	public bool canThrowDiscs = true;

	public float suddenDeathTime = 180f;

	public int edgeProtection = 1;

	public bool useCustomMapsForDeathmatch;

	public bool useCustomMapsForGoldenDisc;

	public List<int> disabledDeathmatchLevels;

	public List<int> disabledGoldenDiscLevels;

	public HideAndSeekAttempts hideAndSeekAttempts = HideAndSeekAttempts.Normal;

	public HideAndSeekTimeLimit hideAndSeekTimeLimit = HideAndSeekTimeLimit.Medium;

	public HideAndSeekHideTime hideAndSeekHideTime;

	public MatchSettings()
	{
		availablePowerups = Powerup.AllPowerups();
		disabledDeathmatchLevels = new List<int>();
		disabledGoldenDiscLevels = new List<int>();
	}
}
