using System;using System.IO;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using UnityEngine.Rendering;
public static class OracleEntry {
 public static void Run(string[] args){
  var maps=new Dictionary<string,object>();
  foreach(var map in Catalog.Maps){
   var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.77f,.77f,.77f).linear);var lights=new List<object>();
   foreach(var lamp in map.lights){Quaternion q=Quaternion.identity;foreach(var local in lamp.rotations)q=q*local;Vector3 direction=-(q*Vector3.forward);Color color=lamp.color*lamp.intensity;if(lamp.vertex)sh.AddDirectionalLight(direction,color.linear,.5f);lights.Add(new{name=lamp.name,direction=new[]{direction.x,direction.y,direction.z},color_gamma=new[]{color.r,color.g,color.b},vertex=lamp.vertex,shadow=lamp.shadow});}
   var coefficients=new float[3][];for(int c=0;c<3;c++){coefficients[c]=new float[9];for(int i=0;i<9;i++)coefficients[c][i]=sh[c,i];}
   maps.Add(map.key,new{name=map.name,sh_coefficients_rgb=coefficients,lights});
  }
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{maps,scope="Unity native SphericalHarmonicsL2 and Color.linear, source active light transforms. Secondary light weight 0.5 follows measured LevelEnclosed shader fixture; full-map rendering not yet verified."}));
 }
}
