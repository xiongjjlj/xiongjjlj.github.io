using System;

[Serializable]
public class MatchScore
{
	public int Points { get; set; }

	public int Suicides { get; set; }

	public int TeamKills { get; set; }

	public int Total { get; set; }

	public int Rank { get; set; }
}
