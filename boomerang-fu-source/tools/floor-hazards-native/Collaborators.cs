using System.Collections.Generic;
using UnityEngine;
public static class Time {public static float deltaTime;public static float fixedDeltaTime=.016666f;}
public static class Events {public static List<string> Items=new List<string>();}
public class Singleton<T> where T:new() {public static readonly T Instance=new T();public static bool InstanceExists=>true;}
public class GameManager {public enum GameState {Playing,Paused}public GameState gameState;public int opponents=2;public int OpponentsLeftStandingNow()=>opponents;}
public class SettingsManager {public enum MatchType {FreeForAll,GoldenDisc,KingOfTheHill}public MatchType matchType=MatchType.FreeForAll;}
public class LevelManager {
 public class Level {public bool isFallingApart=true;}public Level level=new Level();
 public void RescanGraphArea(Bounds b,bool forceAIsToRecreatePaths=true,bool reconnectTeleportals=true){Events.Items.Add("rescan");}
 public void TagAreaAsOutOfBounds(Vector3 p,Vector2 size,Quaternion q){Events.Items.Add("tag");}
}
public class ProbeMaterial {Color flash=new Color(1,.1f,.2f,0);public Color GetColor(string key)=>flash;public void SetColor(string key,Color value){flash=value;}}
public class MeshRenderer:MonoBehaviour {public ProbeMaterial material=new ProbeMaterial();}
namespace FMOD.Studio {public struct EventInstance {public int id;public bool isValid()=>AudioManager.Active.Contains(id);}}
public class AudioManager {
 public static HashSet<int> Active=new HashSet<int>();static int next;
 public FMOD.Studio.EventInstance PlayLooping(string path,Transform t){Events.Items.Add(path);var id=++next;Active.Add(id);return new FMOD.Studio.EventInstance{id=id};}
 public void StopLooping(FMOD.Studio.EventInstance e){Events.Items.Add("stop_loop");Active.Remove(e.id);}
 public void PlayOneShotAt(string path,Vector3 p){Events.Items.Add(path);}
}
public class CellManager {public void ExtinguishCellsTouchingCollider2D(Collider2D c){Events.Items.Add("extinguish");}}
public class DecalManager {public void RemoveDecalsTouchingCollider(Collider2D c){Events.Items.Add("remove_decals");}}
public enum PFXType {TileSplash,TileSplashMedium,TileSplashLarge}
public class ParticleManager {public void PlayPFX(PFXType type,Vector3 p){Events.Items.Add(type.ToString());}}
public class Powerup:MonoBehaviour {public void FallInHazard(){Events.Items.Add("powerup_fall");}}
public class Debris3D:MonoBehaviour {public void CheckForHazards(){Events.Items.Add("debris_hazard");}}
public class ProbeInterpolation {public void ReparentPreviousTransforms(Transform a,Transform b){Events.Items.Add("reparent_projectile");}}
public class Projectile:MonoBehaviour {public ProbeInterpolation interpolatedTransform=new ProbeInterpolation();}
public class DestructableObstacle:MonoBehaviour {public void TakeSinkDamage(){Events.Items.Add("sink_damage");gameObject.SetActive(false);}}
