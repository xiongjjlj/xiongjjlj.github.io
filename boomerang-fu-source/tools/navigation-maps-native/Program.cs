using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Pathfinding;

public class MapCase { public string key,file; public Vector3 position,scale; public Quaternion rotation; public Vector2 boundary; }
public static class OracleEntry {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static List<float[]> Points(List<Vector3> list){var result=new List<float[]>();if(list!=null)foreach(var p in list)result.Add(V(p));return result;}
 static float[] Matrix(object transform,string name){var matrix=(Matrix4x4)transform.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(transform);var result=new float[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)result[r*4+c]=matrix[r,c];return result;}
 public static void Run(string[] args) {
  var directory=System.IO.Path.GetDirectoryName(args[0]);
  int batch=int.Parse(File.ReadAllText(System.IO.Path.Combine(directory,"navigation-batch.txt")));
  if(batch<0||batch>9)throw new ArgumentOutOfRangeException("batch");
  var go=new GameObject("bounded map graph probe");go.SetActive(false);
  var astar=go.AddComponent<AstarPath>();astar.threadCount=ThreadCount.None;astar.scanOnStartup=false;astar.logPathResults=PathLog.None;astar.data=new AstarData();go.SetActive(true);
  var manager=new LevelManager();manager.pathfinder=astar;
  var smoothGo=new GameObject("native path smoothing");var smooth=smoothGo.AddComponent<SimpleSmoothModifier>();smooth.maxSegmentLength=1;smooth.strength=.5f;smooth.iterations=1;var ends=new StartEndModifier();
  using(var output=new StreamWriter(args[0])) {
   output.Write("{\"runtime\":\"Original UnityPlayer and AstarPathfindingProject\",\"batch\":"+batch+",\"maps\":[");bool comma=false;
   foreach(var c in MapCases.Create().Skip(batch*6).Take(6)) {
    manager.level.transform.position=c.position;manager.level.transform.rotation=c.rotation;manager.level.transform.localScale=c.scale;manager.levelBoundary.size=c.boundary;
    astar.data.DeserializeGraphs(File.ReadAllBytes(System.IO.Path.Combine(directory,"OriginalGraphs",c.file)));astar.FlushWorkItems();
    manager.SetOutOfBoundsTags();astar.FlushGraphUpdates();astar.FlushWorkItems();
    var graphs=new List<object>();
    foreach(var raw in astar.data.graphs) {
     if(raw==null)continue;
     var graph=raw as GridGraph;if(graph==null)throw new Exception(c.key+": unexpected graph "+raw.GetType());
     var rows=new List<int[]>();var valid=new List<GridNode>();int extra=0;
     foreach(var n in graph.nodes) {
      int connections=0;for(int d=0;d<8;d++)if(n.HasConnectionInDirection(d))connections|=1<<d;
      rows.Add(new[]{n.NodeInGridIndex,n.position.x,n.position.y,n.position.z,n.Walkable?1:0,(int)n.Penalty,(int)n.Tag,(int)n.Area,connections});
      if(n.Walkable)valid.Add(n);if(n.connections!=null)extra+=n.connections.Length;
     }
     var paths=new List<object>();
     for(int i=0;i<Math.Min(valid.Count,8);i++) {
      Vector3 start=(Vector3)valid[(i*valid.Count)/8].position,end=(Vector3)valid[(valid.Count-1)-(i*valid.Count)/8].position;
      var p=ABPath.Construct(start,end,null);p.nnConstraint.graphMask=1<<(int)graph.graphIndex;p.enabledTags=-1;p.tagPenalties=new int[32];p.tagPenalties[1]=4000;
      AstarPath.StartPath(p);AstarPath.BlockUntilCalculated(p);
      if(p.error){paths.Add(new{start=V(start),end=V(end),error=p.errorLog});continue;}
      var ids=new List<int>();foreach(var n in p.path)ids.Add(((GridNode)n).NodeInGridIndex);
      ends.Apply(p);bool edge=p.vectorPath.Count>2&&!((GridNode)p.path[p.path.Count-1]).HasConnectionsToAllEightNeighbours;if(edge)p.vectorPath.RemoveAt(p.vectorPath.Count-1);smooth.Apply(p);
      paths.Add(new{start=V(start),end=V(end),error="",node_ids=ids,smoothed=Points(p.vectorPath),edge});
     }
     graphs.Add(new{index=graph.graphIndex,width=graph.width,depth=graph.depth,node_size=graph.nodeSize,matrix=Matrix(graph.transform,"matrix"),inverse_matrix=Matrix(graph.transform,"inverseMatrix"),offsets=graph.neighbourOffsets,costs=graph.neighbourCosts,nodes=rows,walkable=valid.Count,extra_connections=extra,paths});
    }
    if(comma)output.Write(",");comma=true;output.Write(JsonSerializer.Serialize(new{key=c.key,graphs}));output.Flush();
    GC.Collect();
   }
   output.Write("]}");
  }
  UnityEngine.Object.DestroyImmediate(smoothGo);UnityEngine.Object.DestroyImmediate(manager.level.gameObject);UnityEngine.Object.DestroyImmediate(manager.gameObject);UnityEngine.Object.DestroyImmediate(go);
 }
}
public class Level { public GameObject gameObject=new GameObject("level");public Transform transform=>gameObject.transform; }
public class FakeBox { public Vector2 size; }
public class BaseManager { public GameObject gameObject=new GameObject("manager");public Transform transform=>gameObject.transform;public BaseManager(){transform.localRotation=new Quaternion(-.7071068286895752f,0,0,.7071068286895752f);} }
public partial class LevelManager:BaseManager { public Level level=new Level();public FakeBox levelBoundary=new FakeBox();public AstarPath pathfinder; }
public static class VectorExt { public static Vector3 RotatePointAroundPivot(Vector3 point,Vector3 pivot,Vector3 euler){return Quaternion.Euler(euler)*(point-pivot)+pivot;}public static Vector3 FlipYAndZ(Vector3 point){return new Vector3(point.x,point.z,point.y);} }
