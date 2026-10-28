using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Pathfinding;
public class Pose {public Vector3 position,scale;public Quaternion rotation;}
public class FloorCase {public int id,mask;public bool reactive,rotating;public Pose[] chain;}
public class MapCase {public string key,file;public FloorCase[] floors;}
public class Level {public MovingFloor[] allMovingFloors;}
public partial class LevelManager {public Level level=new Level();}
public static class OracleEntry {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static float[] M(Matrix4x4 m){var a=new float[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)a[r*4+c]=m[r,c];return a;}
 public static void Run(string[] args) {
  var root=new GameObject("navigation selection probe");root.SetActive(false);var astar=root.AddComponent<AstarPath>();astar.threadCount=ThreadCount.None;astar.scanOnStartup=false;astar.logPathResults=PathLog.None;astar.data=new AstarData();root.SetActive(true);
  var manager=new LevelManager();var results=new List<object>();
  foreach(var c in MapCases.Create()) {
   astar.data.DeserializeGraphs(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]),"OriginalGraphs",c.file)));astar.FlushWorkItems();
   var owned=new List<GameObject>();var floors=new List<MovingFloor>();var initial=new List<Matrix4x4>();var inverseInitial=new List<Matrix4x4>();var rotations=new List<Quaternion>();
   foreach(var f in c.floors) {
    Transform tr=null;
    foreach(var pose in f.chain){var go=new GameObject("platform "+f.id);if(tr==null)owned.Add(go);go.transform.SetParent(tr,false);tr=go.transform;tr.localPosition=pose.position;tr.localRotation=pose.rotation;tr.localScale=pose.scale;}
    MovingFloor floor=f.reactive?(MovingFloor)tr.gameObject.AddComponent<MovingFloorReactive>():tr.gameObject.AddComponent<MovingFloor>();floor.enabled=false;floor.useRotations=f.rotating;floor.graphMask=new GraphMask(f.mask);
    typeof(MovingFloor).GetField("originalMatrix",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(floor,tr.worldToLocalMatrix);
    floors.Add(floor);initial.Add(tr.localToWorldMatrix);inverseInitial.Add(tr.worldToLocalMatrix);rotations.Add(tr.rotation);
   }
   manager.level.allMovingFloors=floors.ToArray();var phases=new List<object>();
   for(int phase=0;phase<3;phase++) {
    var definitions=new List<object>();
    for(int i=0;i<floors.Count;i++) {
     var floor=floors[i];floor.transform.position=initial[i].MultiplyPoint3x4(Vector3.zero)+new Vector3(phase*(2.5f+i*.3f),phase*(-1.25f+i*.1f),0);
     floor.transform.rotation=Quaternion.AngleAxis(phase*23f,Vector3.forward)*rotations[i];
     var points=new List<object>();foreach(var p in new[]{Vector3.zero,new Vector3(3.14159f,-2.71828f,.75f),new Vector3(-11.2f,7.3f,-.2f)})points.Add(new{point=V(p),forward=V(floor.GetTransformPoint(p)),inverse=V(floor.GetInverseTransformPoint(p))});
     definitions.Add(new{id=c.floors[i].id,mask=c.floors[i].mask,reactive=c.floors[i].reactive,rotating=c.floors[i].rotating,initial=M(initial[i]),initial_inverse=M(inverseInitial[i]),current=M(floor.transform.localToWorldMatrix),points});
    }
    var candidates=new List<Vector3>();
    foreach(var raw in astar.data.graphs) {
     var graph=raw as GridGraph;if(graph==null)continue;MovingFloor paired=null;
     foreach(var floor in floors)if((int)floor.graphMask==(1<<(int)graph.graphIndex))paired=floor;
     if(graph.graphIndex>0&&paired==null)continue;
     var valid=new List<GridNode>();foreach(var node in graph.nodes)if(node.Walkable)valid.Add(node);
     if(valid.Count==0)continue;
     foreach(var j in new[]{valid.Count/4,valid.Count/2,valid.Count*3/4}){Vector3 p=(Vector3)valid[j].position;if(paired!=null)p=paired.GetTransformPoint(p);candidates.Add(p);}
    }
    var queries=new List<object>();
    for(int i=0;i<candidates.Count;i++)foreach(int j in new[]{0,candidates.Count/2,candidates.Count-1})queries.Add(new{point=V(candidates[i]),target=V(candidates[j]),mask=(int)manager.GetBestGraphForTarget(candidates[i],candidates[j])});
    phases.Add(new{phase,floors=definitions,queries});
   }
   results.Add(new{key=c.key,phases});foreach(var go in owned)UnityEngine.Object.DestroyImmediate(go);
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{runtime="Original UnityPlayer, installed MovingFloor transform methods and unchanged LevelManager graph-selection method",maps=results}));UnityEngine.Object.DestroyImmediate(root);
 }
}
