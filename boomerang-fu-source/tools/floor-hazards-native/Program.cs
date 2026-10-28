using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
public static class OracleEntry {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static float[] ColorValues(Color c)=>new[]{c.r,c.g,c.b,c.a};
 public static void Run(string[] args){
  var cases=new List<object>();
  foreach(float dt in new[]{.016666f,1f/60,1f/30,.125f}){
   for(int size=0;size<3;size++)cases.Add(Sequence(true,dt,size,false,false));
   cases.Add(Sequence(false,dt,0,false,false));cases.Add(Sequence(false,dt,0,true,false));
  }
  cases.Add(Sequence(true,1f/60,0,false,true));
  var collapse=new List<object>();
  foreach(int count in new[]{6,7,31})foreach(float duration in new[]{.12f,.5f})foreach(float dt in new[]{.016666f,1f/30})collapse.Add(Collapse(count,duration,dt,"steady"));
  collapse.Add(Collapse(31,.12f,.016666f,"pause"));collapse.Add(Collapse(31,.12f,.016666f,"one_remaining"));
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{runtime="Original UnityPlayer Transform/Physics2D/Random/Mathf; unchanged recovered floor method bodies with explicit frame time and presentation/service adapters",cases,collapse}));
 }
 static object Collapse(int count,float duration,float dt,string mode){
  UnityEngine.Random.InitState(5621);AudioManager.Active.Clear();Events.Items.Clear();Time.fixedDeltaTime=dt;
  var root=new GameObject("collapse sequence");var box=root.AddComponent<BoxCollider2D>();box.size=new Vector2(60,54);
  var state=new LevelCollapseReference{box=box,fallingApartDuration=duration,allFallingFloors=new FallingFloor[count]};var positions=new List<float[]>();
  for(int i=0;i<count;i++){var node=new GameObject("floor "+i);node.transform.SetParent(root.transform);node.transform.position=new Vector3((i%6-3)*3.5f,(i/6-2)*4.5f,0);node.AddComponent<BoxCollider2D>();var floor=node.AddComponent<FallingFloor>();floor.enabled=false;state.allFallingFloors[i]=floor;positions.Add(V(node.transform.position));}
  state.Init();var order=new List<int>();foreach(var f in state.fallingFloorPlaylist)order.Add(Array.IndexOf(state.allFallingFloors,f));
  var samples=new List<object>();
  for(int frame=0;frame<1000;frame++){
   bool playing=!(mode=="pause"&&frame>=25&&frame<50);int opponents=mode=="one_remaining"&&frame>=100?1:2;
   Singleton<GameManager>.Instance.gameState=playing?GameManager.GameState.Playing:GameManager.GameState.Paused;Singleton<GameManager>.Instance.opponents=opponents;
   int before=state.fallingFloorPlaylist.Count;int next=before>0?Array.IndexOf(state.allFallingFloors,state.fallingFloorPlaylist[before-1]):-1;
   Events.Items.Clear();state.Update();samples.Add(new{frame,playing,opponents,collapsing=state.isFallingApart,timer=state.fallingApartTimer,count=state.fallingFloorPlaylist.Count,activated=before>state.fallingFloorPlaylist.Count?next:-1});
  }
  UnityEngine.Object.DestroyImmediate(root);
  return new{count,duration,dt,mode,seed=5621,positions,order,samples};
 }
 static object Sequence(bool trap,float dt,int size,bool pause,bool guarded){
  UnityEngine.Random.InitState(5621);Time.deltaTime=dt;AudioManager.Active.Clear();Events.Items.Clear();
  var all=new List<GameObject>();
  GameObject Make(string name,Transform parent=null){var go=new GameObject(name);all.Add(go);if(parent!=null)go.transform.SetParent(parent,false);return go;}
  var root=Make("hazard root");root.transform.position=new Vector3(5,-3,.4f);root.transform.rotation=Quaternion.Euler(0,0,23);root.layer=16;var box=root.AddComponent<BoxCollider2D>();box.size=new Vector2(4,4);box.isTrigger=true;
  var visual=Make("floor visual",root.transform);var renderer=visual.AddComponent<MeshRenderer>();
  var projectile=Make("resting projectile",visual.transform);projectile.AddComponent<Projectile>();
  var destructible=Make("crate",visual.transform);destructible.AddComponent<DestructableObstacle>();
  var powerup=Make("powerup");powerup.transform.position=root.transform.position;powerup.layer=14;powerup.tag="Powerup";powerup.AddComponent<CircleCollider2D>().radius=.1f;powerup.AddComponent<Powerup>();
  var debris=Make("debris");debris.transform.position=root.transform.position;debris.layer=20;debris.AddComponent<CircleCollider2D>().radius=.1f;debris.AddComponent<Debris3D>();
  TrapFloorReference t=null;FallingFloorReference f=null;
  if(trap){t=root.AddComponent<TrapFloorReference>();t.enabled=false;t.floor=visual;t.floorRenderer=renderer;t.tileSize=(TrapFloorReference.TileSize)size;t.Start();}
  else {f=root.AddComponent<FallingFloorReference>();f.enabled=false;f.floor=visual;f.floorRenderer=renderer;f.Start();}
  var samples=new List<object>();bool repeated=false;
  for(int tick=0;tick<(int)Math.Ceiling(20/dt);tick++){
   float elapsed=tick*dt;bool playing=!guarded||tick>5;bool collapsing=!(pause&&elapsed>=.6f&&elapsed<1.1f);bool activate=tick==0||(guarded&&tick==6)||(trap&&!repeated&&elapsed>=10);
   if(trap&&elapsed>=10)repeated=true;
   Singleton<GameManager>.Instance.gameState=playing?GameManager.GameState.Playing:GameManager.GameState.Paused;Singleton<LevelManager>.Instance.level.isFallingApart=collapsing;
   Events.Items.Clear();Physics2D.SyncTransforms();
   if(activate){if(trap)t.StartFalling();else f.StartFalling();}
   if(trap)t.Update();else f.Update();
   samples.Add(new{tick,activate,playing,collapsing,p=V(visual.transform.position),flash=ColorValues(renderer.material.GetColor("_FlashColor")),state=trap?(int)t.fallState:(int)f.fallState,layer=root.layer,timers=trap?new[]{t.warningTimer,t.shakeTimer,t.fallTimer,t.respawnTimer,t.risingTimer}:new[]{f.warningTimer,f.shakeTimer,f.fallTimer},rising_solid=trap&&t.risingSolidThresholdReached,visible=renderer.enabled&&visual.activeSelf,projectile_parented=projectile.transform.parent!=null,projectile_active=projectile.activeSelf,crate_active=destructible.activeSelf,events=Events.Items.ToArray()});
  }
  foreach(var go in all)if(go!=null)UnityEngine.Object.DestroyImmediate(go);
  return new{trap,dt,size,pause,guarded,seed=5621,samples};
 }
}
