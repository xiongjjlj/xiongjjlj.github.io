using System.Collections.Generic;
using I2.Loc;
using UnityEngine;

[CreateAssetMenu(fileName = "Levels/Level", menuName = "Level/LevelAsset")]
public class LevelAsset : ScriptableObject
{
	public enum Complexity
	{
		Tutorial,
		Simple,
		Advanced
	}

	[Header("ID")]
	public string path = string.Empty;

	public LocalizedString levelName;

	public Sprite mapSprite;

	[Header("Size")]
	public int minPlayers;

	public int maxPlayers = 8;

	[Header("Complexity")]
	public Complexity complexity = Complexity.Simple;

	public bool isTrickyToNavigate;

	[Header("Debug")]
	public bool forceFirstInPlaylist;

	public bool soloInPlaylist;

	public List<SettingsManager.MatchType> supportedMatchTypes = new List<SettingsManager.MatchType>();

	public DownloadableContentID requiredDLC;
}
