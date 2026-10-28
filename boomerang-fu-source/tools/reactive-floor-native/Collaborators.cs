using UnityEngine;
public class Singleton<T> where T : new() { public static readonly T Instance=new T(); }
public class TimeKeeper { public float CurrentTime; public float TimeSince(float previous)=>CurrentTime-previous; }
public class LevelManager {
 public Level level=new Level();
 public void RecreatePathsNextOpportunity(){}
 public class Level {public void UpdateObstacles(){}}
}
public class Camera3D {public void Shake(float magnitude,float duration){}}
public partial class ReactiveReference {
 public enum MovementState {Static,Moving,PreMoving,SpringPausing}
 public MovementState movementState;
 public bool isSpringingBack,isSpring,reverse;
 public float springPauseDur,springWindBackDur,preMoveDur,toggleTimestamp;
 public Player moveOwner;
 public Vector3[] positions;
 public GameObject[] speedTrailPFX=new GameObject[0];
 private new float scanTimer;
 private new static float scanRate=.15f;
 protected new void UpdateGraphPosition(){}
 private void UpdatePaths(){}
 private void PerformObstacleFlash(Color color){}
 public void Step()=>FixedUpdate();
 public void Activate()=>ToggleTargetPosition();
 public void Configure(Transform transform,Vector3[] points,float duration,float delay,float pause,float windback,bool spring,bool rotations){
  tr=transform;collider=tr.gameObject.AddComponent<BoxCollider2D>();zPos=tr.localPosition.z;
  lastPos=tr.position;positions=points;moveDur=duration;preMoveDur=delay;springPauseDur=pause;springWindBackDur=windback;isSpring=spring;useRotations=rotations;
 }
}
