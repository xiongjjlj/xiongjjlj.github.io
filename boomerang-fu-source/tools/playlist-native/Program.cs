using System;using System.IO;using System.Collections.Generic;using System.Linq;using System.Text.Json;
public static class OracleEntry {
 public static void Run(string[] args){
  var cases=new List<object>();
  foreach(int seed in new[]{0,1,-73,86752,int.MinValue,int.MaxValue})
   foreach(int count in new[]{2,3,4,5,6})foreach(bool dlc in new[]{false,true})
    foreach(bool rematch in new[]{false,true})foreach(bool tutorial in new[]{false,true})
     cases.Add(RunCase(seed,count,dlc,rematch,tutorial,"default"));
  foreach(string mode in new[]{"custom","custom_all_disabled","switch_bots","switch_custom","trailer","debug","solo","forced_first"})
   foreach(int count in new[]{2,6})foreach(bool dlc in new[]{false,true})foreach(bool rematch in new[]{false,true})
    cases.Add(RunCase(561,count,dlc,rematch,true,mode));
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{scope="Unchanged original LevelManager and Chance methods in original UnityPlayer Mono; explicit catalog/settings and DLC availability; seeded System.Random",cases}));
 }
 static object RunCase(int seed,int count,bool dlc,bool rematch,bool tutorial,string mode){
  Chance.rng=new Random(seed);
  var s=Singleton<SettingsManager>.Instance=new SettingsManager();var g=Singleton<GameManager>.Instance=new GameManager();
  g.dlc=dlc;g.players=Enumerable.Range(0,count).ToList();s.MatchSettings.includeTutorial=tutorial;
  var m=new LevelManagerReference{levelAssets=Catalog.Create()};m.switchIncompatibleLevels=Catalog.SwitchIncompatible.Select(i=>m.levelAssets[i]).ToList();
  if(mode=="custom"||mode=="switch_custom"){s.UseCustomMaps=true;s.MatchSettings.disabledDeathmatchLevels=new List<int>{0,1,4,6,12,18,37,39,48,55};}
  if(mode=="custom_all_disabled"){s.UseCustomMaps=true;s.MatchSettings.disabledDeathmatchLevels=Enumerable.Range(0,58).ToList();}
  if(mode=="switch_bots"||mode=="switch_custom"){s.currentPlatform=SettingsManager.Platform.Switch;s.numBots=1;}
  s.trailerMode=mode=="trailer";s.debugMode=mode=="debug";
  if(mode=="solo"){m.levelAssets[1].soloInPlaylist=true;m.levelAssets[37].soloInPlaylist=true;}
  if(mode=="forced_first"){m.levelAssets[15].forceFirstInPlaylist=true;m.levelAssets[40].forceFirstInPlaylist=true;}
  m.BuildMatchPlaylist(rematch);
  var order=m.matchPlaylist.Select(x=>x.id).ToArray();int index=m.matchIndex;
  var consumed=new List<int>();for(int i=0;i<order.Length*2+3;i++)consumed.Add(m.Consume());
  return new{seed,count,dlc,rematch,tutorial,mode,order,index,powerups=m.powerupsCollectedSoFar,delay=m.lastMatchIndexBeforePowerupsSpawn,powerup_builds=m.powerupBuilds,consumed,final_index=m.matchIndex,final_order=m.matchPlaylist.Select(x=>x.id).ToArray()};
 }
}
