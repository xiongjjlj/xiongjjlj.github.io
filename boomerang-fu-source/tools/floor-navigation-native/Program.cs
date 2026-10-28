using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using Pathfinding;
public class MapCase {public string key,file;public Vector3 position,scale;public Quaternion rotation;public Vector2 boundary;}
public static class OracleEntry {
 public static void Run(string[] args){
  var root=new GameObject("floor graph oracle");root.SetActive(false);var astar=root.AddComponent<AstarPath>();astar.threadCount=ThreadCount.None;astar.scanOnStartup=false;astar.logPathResults=PathLog.None;astar.data=new AstarData();root.SetActive(true);
  var manager=new LevelManager();manager.pathfinder=astar;var cases=new List<object>();
  foreach(var key in new[]{"level14","level18","level40"})foreach(float angle in new[]{0f,17f,45f,90f,135f}){
   var c=Array.Find(MapCases.Create(),x=>x.key==key);manager.level.transform.position=c.position;manager.level.transform.rotation=c.rotation;manager.level.transform.localScale=c.scale;manager.levelBoundary.size=c.boundary;
   astar.data.DeserializeGraphs(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]),"OriginalGraphs",c.file)));astar.FlushWorkItems();manager.SetOutOfBoundsTags();astar.FlushGraphUpdates();astar.FlushWorkItems();
   var point=angle==0?Vector2.zero:new Vector2(3.2f,-5.4f);var extent=angle==0?new Vector2(2.5f,3.5f):new Vector2(6,3);var rotation=Quaternion.Euler(0,0,angle);
   manager.TagAreaAsOutOfBounds(point,extent,rotation);astar.FlushGraphUpdates();astar.FlushWorkItems();
   var graphs=new List<object>();foreach(var raw in astar.data.graphs){if(raw==null)continue;var grid=(GridGraph)raw;var tags=new List<int>();foreach(var n in grid.nodes)if(n.Tag==1)tags.Add(n.NodeInGridIndex);graphs.Add(new{index=grid.graphIndex,tags});}
   var vertices=new[]{new Vector3(-extent.x,-extent.y),new Vector3(-extent.x,extent.y),new Vector3(extent.x,extent.y),new Vector3(extent.x,-extent.y)};
   for(int i=0;i<vertices.Length;i++)vertices[i]=VectorExt.FlipYAndZ((Vector2)VectorExt.RotatePointAroundPivot(vertices[i],Vector3.zero,rotation.eulerAngles)+point);
   var shape=new GraphUpdateShape(vertices,true,manager.transform.localToWorldMatrix,1);var bounds=shape.GetBounds();var vectors=new List<float[]>();foreach(var v in vertices)vectors.Add(new[]{v.x,v.y,v.z});
   float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
   cases.Add(new{map=key,angle,point=new[]{point.x,point.y},extent=new[]{extent.x,extent.y},euler=V(rotation.eulerAngles),vertices=vectors,bounds=new{center=V(bounds.center),size=V(bounds.size)},right=V(manager.transform.TransformVector(Vector3.right)),up=V(manager.transform.TransformVector(Vector3.up)),forward=V(manager.transform.TransformVector(Vector3.forward)),graphs});
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{runtime="Original AstarPathfindingProject and unchanged recovered LevelManager.TagAreaAsOutOfBounds",cases}));
  foreach(var go in new[]{manager.level.gameObject,manager.gameObject,root})UnityEngine.Object.DestroyImmediate(go);
 }
}
public class Level {public GameObject gameObject=new GameObject("level");public Transform transform=>gameObject.transform;}
public class FakeBox {public Vector2 size;}
public class BaseManager {public GameObject gameObject=new GameObject("manager");public Transform transform=>gameObject.transform;public BaseManager(){transform.localRotation=new Quaternion(-.7071068286895752f,0,0,.7071068286895752f);}}
public partial class LevelManager:BaseManager {public Level level=new Level();public FakeBox levelBoundary=new FakeBox();public AstarPath pathfinder;}
public static class VectorExt {public static Vector3 RotatePointAroundPivot(Vector3 point,Vector3 pivot,Vector3 euler)=>Quaternion.Euler(euler)*(point-pivot)+pivot;public static Vector3 FlipYAndZ(Vector3 p)=>new Vector3(p.x,p.z,p.y);}
