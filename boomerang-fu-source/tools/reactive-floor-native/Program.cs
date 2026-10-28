using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
public class Input { public Case[] cases; public int ticks; public float fixed_delta; }
public class Case { public string key; public int seed; public Pose[] chain; public Data data; }
public class Pose { public Vector3 position; public Quaternion rotation; public Vector3 scale; }
public class Data { public int isSpring,useRotations;public float moveDurMin,preMoveDur,springPauseDur,springWindBackDur;public Vector3[] positions; }
public static class OracleEntry {
 static object Base(ReactiveReference floor,string field)=>typeof(MovingFloor).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(floor);
 public static void Run(string[] args){
  var input=FixtureInput.Create();Time.fixedDeltaTime=input.fixed_delta;var output=new List<object>();
  foreach(var c in input.cases)foreach(var triggers in new[]{new[]{0},new[]{0,5},new[]{0,70}}){
   GameObject root=null;Transform tr=null;
   foreach(var p in c.chain){var go=new GameObject(c.key);if(root==null)root=go;go.transform.SetParent(tr,false);tr=go.transform;tr.localPosition=p.position;tr.localRotation=p.rotation;tr.localScale=p.scale;}
   var floor=tr.gameObject.AddComponent<ReactiveReference>();floor.enabled=false;
   floor.Configure(tr,c.data.positions,c.data.moveDurMin,c.data.preMoveDur,c.data.springPauseDur,c.data.springWindBackDur,c.data.isSpring!=0,c.data.useRotations!=0);
   var samples=new List<object>();
   for(int tick=0;tick<=input.ticks;tick++){
    float time=3.25f+(tick/2)*(input.fixed_delta*2f);Singleton<TimeKeeper>.Instance.CurrentTime=time;
    bool trigger=Array.IndexOf(triggers,tick)>=0;if(trigger)floor.Activate();if(tick>0)floor.Step();
    Vector3 p=tr.localPosition;Quaternion q=tr.localRotation;Vector2 v=floor.velocity;
    samples.Add(new {tick,time,trigger,p=new[]{p.x,p.y,p.z},q=new[]{q.x,q.y,q.z,q.w},v=new[]{v.x,v.y},index=floor.posIndex,percent=floor.movePercent,state=(int)floor.movementState,reverse=floor.reverse,spring_back=floor.isSpringingBack,start_rot=(float)Base(floor,"startRot"),target_rot=(float)Base(floor,"targetRot")});
   }
   output.Add(new {key=c.key,triggers,samples});UnityEngine.Object.DestroyImmediate(root);
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(new {runtime="Original UnityPlayer/Mono; unchanged recovered MovingFloorReactive motion bodies, explicit clock and side-effect stubs",cases=output,fixed_delta=Time.fixedDeltaTime}));
 }
}
