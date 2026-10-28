public sealed class MatchAward
{
	public static readonly MatchAward AccurateThrows = new MatchAward("AccurateThrows", "matchaward_accuratethrows");

	public static readonly MatchAward CloseSecond = new MatchAward("CloseSecond", "matchaward_closesecond", priority: true);

	public static readonly MatchAward DoubleKill = new MatchAward("DoubleKills", "matchaward_doublekill");

	public static readonly MatchAward GoodCatch = new MatchAward("GoodCatch", "matchaward_goodcatch");

	public static readonly MatchAward InaccurateThrows = new MatchAward("InaccurateThrows", "matchaward_inaccuratethrows");

	public static readonly MatchAward LastPlayerStanding = new MatchAward("LastPlayerStanding", "matchaward_lastplayerstanding");

	public static readonly MatchAward LeastDisarmed = new MatchAward("LeastDisarmed", "matchaward_leastdisarmed");

	public static readonly MatchAward LeastUnitsMoved = new MatchAward("LeastUnitsMoved", "matchaward_leastunitsmoved");

	public static readonly MatchAward LongestKillDistance = new MatchAward("LongestKillDistance", "matchaward_longestkilldistance");

	public static readonly MatchAward MoreTeamKillsThanKills = new MatchAward("MoreTeamKillsThanKills", "matchaward_moreteamkillsthankills", priority: true);

	public static readonly MatchAward MostBamboozled = new MatchAward("MostBamboozled", "matchaward_mostbamboozled");

	public static readonly MatchAward MostBlocks = new MatchAward("MostBlocks", "matchaward_mostblocks");

	public static readonly MatchAward MostControlledKills = new MatchAward("MostControlledKills", "matchaward_mostcontrolledkills", priority: true);

	public static readonly MatchAward MostDashes = new MatchAward("MostDashes", "matchaward_mostdashes");

	public static readonly MatchAward MostDashesOverHazards = new MatchAward("MostDashesOverHazards", "matchaward_mostdashesoverhazards");

	public static readonly MatchAward MostDeaths = new MatchAward("MostDeaths", "matchaward_mostdeaths");

	public static readonly MatchAward MostDisarmedByPlayers = new MatchAward("MostDisarmedByPlayers", "matchaward_mostdisarmedbyplayers");

	public static readonly MatchAward MostDiscsStolen = new MatchAward("MostDiscsStolen", "matchaward_mostdiscsstolen");

	public static readonly MatchAward MostDodges = new MatchAward("MostDodges", "matchaward_mostdodges", priority: true);

	public static readonly MatchAward MostEfficient = new MatchAward("MostEfficient", "matchaward_mostefficient");

	public static readonly MatchAward MostExplosiveKills = new MatchAward("MostExplosiveKills", "matchaward_mostexplosivekills");

	public static readonly MatchAward MostExplosiveSuicides = new MatchAward("MostExplosiveSuicides", "matchaward_mostexplosivesuicides", priority: true);

	public static readonly MatchAward MostFallSuicides = new MatchAward("MostFallSuicides", "matchaward_mostfallsuicides");

	public static readonly MatchAward MostFireKills = new MatchAward("MostFireKills", "matchaward_mostfirekills", priority: true);

	public static readonly MatchAward MostFiresExtinguished = new MatchAward("MostFiresExtinguished", "matchaward_mostfiresextinguished");

	public static readonly MatchAward MostFlippedSwitches = new MatchAward("MostFlippedSwitches", "matchaward_mostflippedswitches");

	public static readonly MatchAward MostFrozenKills = new MatchAward("MostFrozenKills", "matchaward_mostfrozenkills", priority: true);

	public static readonly MatchAward MostKills = new MatchAward("MostKills", "matchaward_mostkills");

	public static readonly MatchAward MostKillsAfterDeath = new MatchAward("MostKillsAfterDeath", "matchaward_mostkillsafterdeath", priority: true);

	public static readonly MatchAward MostKillsWhileBamboozled = new MatchAward("MostKillsWhileBamboozled", "matchaward_mostkillswhilebamboozled");

	public static readonly MatchAward MostKillsWhileDisarmed = new MatchAward("MostKillsWhileDisarmed", "matchaward_mostkillswhiledisarmed", priority: true);

	public static readonly MatchAward MostKillsWhileTimeImmune = new MatchAward("MostKillsWhileTimeImmune", string.Empty);

	public static readonly MatchAward MostMeleeKills = new MatchAward("MostMeleeKills", string.Empty);

	public static readonly MatchAward MostOnFire = new MatchAward("MostOnFire", "matchaward_mostonfire");

	public static readonly MatchAward MostOrbitalThrows = new MatchAward("MostOrbitalThrows", "matchaward_mostorbitalthrows");

	public static readonly MatchAward MostOutOfBoundsSuicides = new MatchAward("MostOutOfBoundsSuicides", "matchaward_mostoutofboundssuicides");

	public static readonly MatchAward MostPlayersDisarmed = new MatchAward("MostPlayersDisarmed", "matchaward_mostplayersdisarmed");

	public static readonly MatchAward MostPowerupsCollected = new MatchAward("MostPowerupsCollected", "matchaward_mostpowerupscollected");

	public static readonly MatchAward MostPushes = new MatchAward("MostPushes", "matchaward_mostpushes");

	public static readonly MatchAward MostPushKills = new MatchAward("MostPushKills", "matchaward_mostpushkills");

	public static readonly MatchAward MostSelfDeflections = new MatchAward("MostSelfDeflections", string.Empty);

	public static readonly MatchAward MostSpin = new MatchAward("MostSpin", "matchaward_mostspin");

	public static readonly MatchAward MostSquishDeaths = new MatchAward("MostSquishDeaths", "matchaward_mostsquishdeaths");

	public static readonly MatchAward MostSquishKills = new MatchAward("MostSquishKills", "matchaward_mostsquishkills", priority: true);

	public static readonly MatchAward MostTeamDeaths = new MatchAward("MostTeamDeaths", "matchaward_mostteamdeaths", priority: true);

	public static readonly MatchAward MostTeamKills = new MatchAward("MostTeamKills", "matchaward_mostteamkills", priority: true);

	public static readonly MatchAward MostTeleportDeaths = new MatchAward("MostTeleportDeaths", "matchaward_mostteleportdeaths", priority: true);

	public static readonly MatchAward MostTeleportKills = new MatchAward("MostTeleportKills", "matchaward_mostteleportkills", priority: true);

	public static readonly MatchAward MostThrowKills = new MatchAward("MostThrowKills", "matchaward_mostthrowkills");

	public static readonly MatchAward MostTimeAiming = new MatchAward("MostTimeAiming", "matchaward_mosttimeaiming");

	public static readonly MatchAward MostTimeDisguised = new MatchAward("MostTimeDisguised", "matchaward_mosttimedisguised", priority: true);

	public static readonly MatchAward MostTimeImmuneKills = new MatchAward("MostTimeImmuneKills", string.Empty);

	public static readonly MatchAward MostTimeHoldingGoldenDisc = new MatchAward("MostTimeHoldingGoldenDisc", "matchaward_mosttimeholdinggoldendisc");

	public static readonly MatchAward MostTimeInPlants = new MatchAward("MostTimeInPlants", "matchaward_mosttimeinplants", priority: true);

	public static readonly MatchAward MostTimeOutOfBounds = new MatchAward("MostTimeOutOfBounds", "matchaward_mosttimeoutofbounds");

	public static readonly MatchAward MostTimeSlimed = new MatchAward("MostTimeSlimed", "matchaward_mosttimeslimed");

	public static readonly MatchAward MostUnarmed = new MatchAward("MostUnsarmed", "matchaward_mostunarmed");

	public static readonly MatchAward MostUnitsMoved = new MatchAward("MostUnitsMoved", "matchaward_mostunitsmoved");

	public static readonly MatchAward NeedlessViolence = new MatchAward("NeedlessViolence", "matchaward_needlessviolence");

	public static readonly MatchAward NoAward = new MatchAward("NoAward", "matchaward_noaward");

	public static readonly MatchAward NoDashes = new MatchAward("NoDashes", "matchaward_nodashes");

	public static readonly MatchAward NoKills = new MatchAward("NoKills", "matchaward_nokills");

	public static readonly MatchAward OnlyMeleeKills = new MatchAward("OnlyMeleeKills", "matchaward_onlymeleekills");

	public static readonly MatchAward OnlyThrowKills = new MatchAward("OnlyThrowKills", "matchaward_onlythrowkills");

	public static readonly MatchAward Pacifist = new MatchAward("Pacifist", "matchaward_pacifist");

	public static readonly MatchAward ShortestKillDistance = new MatchAward("ShortestKillDistance", "matchaward_shortestkilldistance");

	public static readonly MatchAward Swordsmanship = new MatchAward("Swordsmanship", "matchaward_swordsmanship");

	public static readonly MatchAward TripleKill = new MatchAward("TripleKills", "matchaward_triplekill");

	public readonly string Name;

	public readonly string Description;

	public readonly bool Priority;

	private MatchAward(string name, string description, bool priority = false)
	{
		Name = name;
		Description = description;
		Priority = priority;
	}
}
