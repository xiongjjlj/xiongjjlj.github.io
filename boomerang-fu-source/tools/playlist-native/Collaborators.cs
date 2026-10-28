using System.Collections.Generic;
public enum DownloadableContentID { None,FreshFlavorsPack,JustDessertsPack }
public class LevelAsset {
 public enum Complexity { Tutorial,Simple,Advanced }
 public int id,minPlayers,maxPlayers;public string path;public Complexity complexity;
 public bool forceFirstInPlaylist,soloInPlaylist;public DownloadableContentID requiredDLC;
 public List<SettingsManager.MatchType> supportedMatchTypes;
}
public class Singleton<T> where T:new() { public static T Instance=new T(); }
public class MatchSettings {
 public bool includeTutorial;public List<int> disabledDeathmatchLevels=new List<int>(),disabledGoldenDiscLevels=new List<int>();
}
public class SettingsManager {
 public enum Platform { PC,Switch,Xbox }
 public enum MatchType { DeathMatch,GoldenDisc,KingOfTheHill,HideAndSeek }
 public Platform currentPlatform;public MatchType matchType;public bool debugMode,trailerMode,UseCustomMaps;
 public int numBots;public MatchSettings MatchSettings=new MatchSettings();
}
public class GameManager {
 public List<int> players=new List<int>();public bool dlc;
 public bool IsDLCUnlocked(DownloadableContentID id)=>id==DownloadableContentID.None||dlc;
}
