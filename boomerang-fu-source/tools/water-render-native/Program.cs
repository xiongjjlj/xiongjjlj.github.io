using System;using System.IO;using System.Collections;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.Rendering;
public static class OracleEntry {
 static void DisableScripts(Scene scene,LoadSceneMode mode){foreach(var root in scene.GetRootGameObjects())foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))if(script!=null){script.enabled=false;script.StopAllCoroutines();}}
 static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z}; static float[] C(Color c)=>new[]{c.r,c.g,c.b,c.a};
 public static IEnumerator Run(string[] args){
  if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new Exception("Graphics device required");
  SceneManager.sceneLoaded+=DisableScripts;
  for(int i=0;i<SceneManager.sceneCount;i++)DisableScripts(SceneManager.GetSceneAt(i),LoadSceneMode.Single);
  yield return SceneManager.LoadSceneAsync(4,LoadSceneMode.Additive);
  var scene=SceneManager.GetSceneByBuildIndex(4);SceneManager.SetActiveScene(scene);DisableScripts(scene,LoadSceneMode.Additive);
  Material original=null;foreach(var root in scene.GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material!=null&&material.shader.name=="FX/Water_DepthVignette")original=material;
  if(original==null)throw new Exception("Original water shader missing");
  var sourceLights=new List<Light>();Light sourceSun=null;
  foreach(var sceneRoot in scene.GetRootGameObjects())foreach(var lamp in sceneRoot.GetComponentsInChildren<Light>(true))if(lamp.enabled&&lamp.gameObject.activeInHierarchy&&lamp.type==LightType.Directional&&(lamp.cullingMask&1)!=0){sourceLights.Add(lamp);if(lamp.renderMode!=LightRenderMode.ForceVertex)sourceSun=lamp;}
  if(sourceSun==null)throw new Exception("Original scene main light missing");
  foreach(var existing in UnityEngine.Object.FindObjectsOfType<Light>())existing.enabled=false;
  var owned=new List<UnityEngine.Object>();var previous=RenderTexture.active;
  try {
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientIntensity=0;RenderSettings.reflectionIntensity=0;RenderSettings.fog=false;QualitySettings.pixelLightCount=1;QualitySettings.antiAliasing=0;
   var cameraObject=new GameObject("Private water fixture camera");owned.Add(cameraObject);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.fieldOfView=60;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-5);camera.transform.rotation=Quaternion.identity;camera.depthTextureMode=DepthTextureMode.Depth;
   var sunObject=new GameObject("Private water fixture sun");owned.Add(sunObject);var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.cullingMask=1<<31;sun.color=Color.white;sun.intensity=1;sun.renderMode=LightRenderMode.ForcePixel;sun.shadows=LightShadows.None;sun.transform.rotation=Quaternion.identity;RenderSettings.sun=sun;
   var mesh=new Mesh();owned.Add(mesh);mesh.vertices=new[]{new Vector3(-3,-3,0),new Vector3(3,-3,0),new Vector3(3,3,0),new Vector3(-3,3,0)};mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();
   var water=new GameObject("Private water quad");owned.Add(water);water.layer=31;water.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=water.AddComponent<MeshRenderer>();renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
   var material=new Material(original);owned.Add(material);renderer.sharedMaterial=material;
   var back=new GameObject("Private depth quad");owned.Add(back);back.layer=31;back.AddComponent<MeshFilter>().sharedMesh=mesh;back.transform.localScale=Vector3.one*8;var backRenderer=back.AddComponent<MeshRenderer>();var backMaterial=new Material(Shader.Find("Standard"));owned.Add(backMaterial);backMaterial.color=Color.white;backRenderer.sharedMaterial=backMaterial;
   var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);owned.Add(target);target.Create();camera.targetTexture=target;var image=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);owned.Add(image);
   var secondaryLights=new List<Light>();foreach(var source in sourceLights)if(source.renderMode==LightRenderMode.ForceVertex){var go=new GameObject("Private secondary fixture light");owned.Add(go);var secondary=go.AddComponent<Light>();secondary.type=LightType.Directional;secondary.cullingMask=1<<31;secondary.color=source.color;secondary.intensity=source.intensity;secondary.renderMode=LightRenderMode.ForceVertex;secondary.shadows=LightShadows.None;secondary.transform.rotation=source.transform.rotation;secondaryLights.Add(secondary);}
   var cases=new List<object>();
   foreach(var lightingMode in new[]{"white","source_day"}){
    bool sourceDay=lightingMode=="source_day";RenderSettings.ambientLight=sourceDay?new Color(.77f,.77f,.77f):Color.black;RenderSettings.ambientIntensity=sourceDay?1:0;
    sun.color=sourceDay?sourceSun.color:Color.white;sun.intensity=sourceDay?sourceSun.intensity:1;sun.transform.rotation=sourceDay?sourceSun.transform.rotation:Quaternion.identity;
    foreach(var secondary in secondaryLights)secondary.enabled=sourceDay;
    yield return null;
   foreach(var config in Materials.Values){
    material.SetColor("_ShallowColor",config.shallow);material.SetColor("_DeepColor",config.deep);material.SetColor("_VignetteColor",config.vignette);material.SetFloat("_DepthOffset",config.offset);material.SetFloat("_ShallowDepth",config.depth);material.SetFloat("_VignetteSize",config.size);material.SetFloat("_VignetteIntensity",config.intensity);
    foreach(float depth in new[]{.25f,2f,8f,16f}){
     back.transform.position=new Vector3(0,0,depth);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,64,64),0,0);image.Apply();
     foreach(var pixel in new[]{new[]{32,32},new[]{16,16},new[]{48,16},new[]{16,48},new[]{48,48},new[]{4,32},new[]{60,32}}){var ray=camera.ScreenPointToRay(new Vector3(pixel[0]+.5f,pixel[1]+.5f,0));var position=ray.origin+ray.direction*(-ray.origin.z/ray.direction.z);cases.Add(new{lighting=lightingMode,material=config.name,depth,uv=new[]{position.x/6+.5f,position.y/6+.5f},position=V(position),view=V((camera.transform.position-position).normalized),rgba=C(image.GetPixel(pixel[0],pixel[1]))});}
    }
   }
   }
   File.WriteAllText(args[0],JsonSerializer.Serialize(new{device=SystemInfo.graphicsDeviceName,color_space=QualitySettings.activeColorSpace.ToString(),shader=original.shader.name,camera=new{near=.1f,far=30f,position=V(camera.transform.position),fov=60f},light=new{direction=new[]{0f,0f,-1f},color=new[]{1f,1f,1f}},cases,scope="Original serialized water shader rendered on a private quad, controlled perspective depth, white direct-light and source day-light modes; reflections/fog disabled. Not original full-scene pixel parity."}));
   camera.targetTexture=null;target.Release();
  } finally {RenderTexture.active=previous;SceneManager.sceneLoaded-=DisableScripts;for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);}
 }
}
