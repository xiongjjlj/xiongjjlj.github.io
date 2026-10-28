using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Pathfinding;
public class MapCase {public string key,file;public Vector3 position,scale;public Quaternion rotation;public Vector2 boundary;}
public static class OracleEntry {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static object B(Bounds b)=>new{center=V(b.center),size=V(b.size)};
 static List<int[]> Nodes(GridGraph g){var rows=new List<int[]>();foreach(var n in g.nodes){int edges=0;for(int d=0;d<8;d++)if(n.HasConnectionInDirection(d))edges|=1<<d;rows.Add(new[]{n.NodeInGridIndex,n.position.x,n.position.y,n.position.z,n.Walkable?1:0,(int)n.Penalty,(int)n.Tag,edges});}return rows;}
 static object Shape(Collider2D c)=>new{active=c.enabled,layer=c.gameObject.layer,kind=c is CircleCollider2D?"circle":"box",position=V(c.transform.position),rotation=c.transform.eulerAngles.z,size=c is BoxCollider2D box?V(box.size):null,radius=c is CircleCollider2D circle?circle.radius:0,bounds=B(c.bounds)};
 public static void Run(string[] args){
  var root=new GameObject("bounded navigation update oracle");root.SetActive(false);var astar=root.AddComponent<AstarPath>();astar.threadCount=ThreadCount.None;astar.scanOnStartup=false;astar.logPathResults=PathLog.None;astar.data=new AstarData();root.SetActive(true);
  var selector=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]),"navigation-update-map.txt");var key=File.Exists(selector)?File.ReadAllText(selector).Trim():"level2";var c=Array.Find(MapCases.Create(),entry=>entry.key==key);var manager=new LevelManager();manager.pathfinder=astar;manager.level.transform.position=c.position;manager.level.transform.rotation=c.rotation;manager.level.transform.localScale=c.scale;manager.levelBoundary.size=c.boundary;
  astar.data.DeserializeGraphs(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]),"OriginalGraphs",c.file)));astar.FlushWorkItems();manager.SetOutOfBoundsTags();astar.FlushGraphUpdates();astar.FlushWorkItems();var grid=(GridGraph)astar.data.graphs[0];var initial=Nodes(grid);
  var floor=new GameObject("probe floor");floor.layer=LayerMask.NameToLayer("Floor");var floorBox=floor.AddComponent<BoxCollider2D>();floorBox.size=new Vector2(32,28);floorBox.isTrigger=true;
  var block=new GameObject("probe obstacle");block.layer=8;var box=block.AddComponent<BoxCollider2D>();box.size=new Vector2(4,10);box.enabled=false;
  var round=new GameObject("probe circular obstacle");round.layer=8;var circle=round.AddComponent<CircleCollider2D>();circle.radius=2.4f;round.transform.position=new Vector3(-4,2,3);circle.enabled=false;
  Physics2D.queriesHitTriggers=true;
  var modifiers=new GameObject("probe modifiers");var smooth=modifiers.AddComponent<SimpleSmoothModifier>();smooth.maxSegmentLength=1;smooth.strength=.5f;smooth.iterations=1;var ends=new StartEndModifier();var phases=new List<object>();
  void Step(string name,Bounds bounds,bool physics=true,int tag=-1,int walkability=-1){
   Physics2D.SyncTransforms();var update=new GraphUpdateObject(bounds){updatePhysics=physics};update.nnConstraint.graphMask=1;if(tag>=0){update.modifyTag=true;update.setTag=tag;}if(walkability>=0){update.modifyWalkability=true;update.setWalkability=walkability!=0;}
   astar.UpdateGraphs(update);astar.FlushGraphUpdates();astar.FlushWorkItems();var paths=new List<object>();
   foreach(var pair in new[]{new[]{new Vector3(0,-10,0),new Vector3(0,10,0)},new[]{new Vector3(-12,-8,0),new Vector3(12,8,0)},new[]{new Vector3(-10,3,0),new Vector3(10,3,0)}}){
    var path=ABPath.Construct(pair[0],pair[1],null);path.nnConstraint.graphMask=1;path.enabledTags=-1;path.tagPenalties=new int[32];path.tagPenalties[1]=4000;AstarPath.StartPath(path);AstarPath.BlockUntilCalculated(path);
    if(path.error){paths.Add(new{start=V(pair[0]),end=V(pair[1]),error=path.errorLog});continue;}
    var ids=new List<int>();foreach(var node in path.path)ids.Add(((GridNode)node).NodeInGridIndex);ends.Apply(path);bool edge=path.vectorPath.Count>2&&!((GridNode)path.path[path.path.Count-1]).HasConnectionsToAllEightNeighbours;if(edge)path.vectorPath.RemoveAt(path.vectorPath.Count-1);smooth.Apply(path);var points=new List<float[]>();foreach(var p in path.vectorPath)points.Add(V(p));
    paths.Add(new{start=V(pair[0]),end=V(pair[1]),error="",node_ids=ids,smoothed=points,edge});
   }
   phases.Add(new{name,bounds=B(bounds),physics,tag,walkability,shapes=new[]{Shape(floorBox),Shape(box),Shape(circle)},nodes=Nodes(grid),paths});
  }
  Step("replace static floor region",new Bounds(Vector3.zero,new Vector3(40,36,2)));
  box.enabled=true;block.transform.rotation=Quaternion.Euler(0,0,17);Physics2D.SyncTransforms();Step("add rotated box",box.bounds);
  var old=box.bounds;block.transform.position=new Vector3(4.6f,-1.2f,0);Physics2D.SyncTransforms();old.Encapsulate(box.bounds);Step("move box",old);
  old=box.bounds;box.enabled=false;Step("remove box",old);
  circle.enabled=true;Physics2D.SyncTransforms();Step("add circle",circle.bounds);
  old=floorBox.bounds;floorBox.enabled=false;Step("remove floor",old);
  floorBox.enabled=true;Step("restore floor",old);
  Step("mark hazardous strip",new Bounds(Vector3.zero,new Vector3(4,22,2)),false,1);
  Step("refresh physics retains tags",new Bounds(Vector3.zero,new Vector3(4,22,2)));
  Step("disable walkability",new Bounds(new Vector3(0,5,0),new Vector3(4,4,2)),false,-1,0);
  Step("enable walkability",new Bounds(new Vector3(0,5,0),new Vector3(4,4,2)),true,-1,1);
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{runtime="Original UnityPlayer Physics2D, GridGraph.UpdateArea and ABPath; synthetic geometry with original Enclosed graph inputs",map=c.key,initial,phases}));
  foreach(var go in new[]{modifiers,round,block,floor,manager.level.gameObject,manager.gameObject,root})UnityEngine.Object.DestroyImmediate(go);
 }
}
public class Level {public GameObject gameObject=new GameObject("level");public Transform transform=>gameObject.transform;}
public class FakeBox {public Vector2 size;}
public class BaseManager {public GameObject gameObject=new GameObject("manager");public Transform transform=>gameObject.transform;public BaseManager(){transform.localRotation=new Quaternion(-.7071068286895752f,0,0,.7071068286895752f);}}
public partial class LevelManager:BaseManager {public Level level=new Level();public FakeBox levelBoundary=new FakeBox();public AstarPath pathfinder;}
public static class VectorExt {public static Vector3 RotatePointAroundPivot(Vector3 point,Vector3 pivot,Vector3 euler)=>Quaternion.Euler(euler)*(point-pivot)+pivot;public static Vector3 FlipYAndZ(Vector3 p)=>new Vector3(p.x,p.z,p.y);}
