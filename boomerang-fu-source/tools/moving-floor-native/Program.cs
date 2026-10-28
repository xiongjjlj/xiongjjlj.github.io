using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
[Serializable] public class Input { public Case[] cases; public int ticks; public float fixed_delta; }
[Serializable] public class Case { public string key; public int seed; public Pose[] chain; public Data data; }
[Serializable] public class Pose { public Vector3 position; public Quaternion rotation; public Vector3 scale; }
[Serializable] public class Data { public int posIndex, movementType, useRotations, counterClockwise; public Vector3[] positionRotations; public float movePercent, moveDurMin, moveDurMax; }
public static class OracleEntry {
 static readonly Type T=Assembly.Load("Assembly-CSharp").GetType("MovingFloor",true);
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Set(object f,string key,object value) {var field=T.GetField(key,Flags); if(field==null)throw new Exception("Missing field "+key);field.SetValue(f,value);}
 static object Get(object f,string key) {var field=T.GetField(key,Flags);if(field==null)throw new Exception("Missing field "+key);return field.GetValue(f);}
 static void Call(object f,string key)=>T.GetMethod(key,Flags).Invoke(f,null);
 public static void Run(string[] args) {
  var input=FixtureInput.Create();Time.fixedDeltaTime=input.fixed_delta;
  var output=new List<object>();
  foreach(var c in input.cases) {
   string stage="chain"; try {
   var owned=new List<GameObject>(); Transform tr=null;
   foreach(var p in c.chain) {
    var go=new GameObject(c.key);owned.Add(go);go.transform.SetParent(tr,false);tr=go.transform;
    tr.localPosition=p.position;tr.localRotation=p.rotation;tr.localScale=p.scale;
   }
   // Invoke the installed game's exact methods; initialize only Start's fields
   // used by motion, avoiding its unrelated singleton navigation/cell setup.
   stage="add component";var floor=tr.gameObject.AddComponent(T);((Behaviour)floor).enabled=false;
   stage="fields";Set(floor,"tr",tr);Set(floor,"zPos",tr.localPosition.z);Set(floor,"lastPos",(Vector2)tr.position);
   Set(floor,"moveDur",c.data.moveDurMin);Set(floor,"posIndex",c.data.posIndex);Set(floor,"movePercent",c.data.movePercent);
   Set(floor,"positionRotations",c.data.positionRotations);Set(floor,"useRotations",c.data.useRotations!=0);Set(floor,"counterClockwise",c.data.counterClockwise!=0);
   Set(floor,"movementType",Enum.ToObject(T.GetNestedType("MovementType"),c.data.movementType));Set(floor,"moveDurMin",c.data.moveDurMin);Set(floor,"moveDurMax",c.data.moveDurMax);
   stage="toggle";UnityEngine.Random.InitState(c.seed);UnityEngine.Random.Range(-.05f*.75f,0f);Call(floor,"ToggleTargetPosition");
   var samples=new List<object>();
   for(int tick=0;tick<=input.ticks;tick++) {
    stage="sample "+tick;if(tick>0)Call(floor,"FixedUpdate");
    Vector2 v=(Vector2)Get(floor,"velocity");Vector3 p=tr.localPosition;Quaternion q=tr.localRotation;
    samples.Add(new { tick, p=new[]{p.x,p.y,p.z},q=new[]{q.x,q.y,q.z,q.w},v=new[]{v.x,v.y},index=(int)Get(floor,"posIndex"),percent=(float)Get(floor,"movePercent"),duration=(float)Get(floor,"moveDur"),target_duration=(float)Get(floor,"newMoveDur") });
   }
   output.Add(new {key=c.key,seed=c.seed,samples});stage="destroy";UnityEngine.Object.DestroyImmediate(owned[0]);
   }catch(Exception ex){throw new Exception(c.key+" stage "+stage,ex);}
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(new {runtime="Installed Assembly-CSharp MovingFloor, original UnityPlayer/Mono",unity=Application.unityVersion,fixed_delta=Time.fixedDeltaTime,cases=output,scope="Motion fields from Start initialized explicitly; actual ToggleTargetPosition and FixedUpdate. Navigation, audio and trigger scheduling excluded."}));
 }
}
