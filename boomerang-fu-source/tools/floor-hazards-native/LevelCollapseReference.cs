using UnityEngine;using System.Collections.Generic;
public class FallingFloor:FallingFloorReference {}
public class LevelCollapseReference {
 public bool isFallingApart=true;
 public FallingFloor[] allFallingFloors;
 public List<FallingFloor> fallingFloorPlaylist=new List<FallingFloor>();
 public float fallingApartDuration=.5f,fallingApartTimer;
 public BoxCollider2D box;
 public void Init(){
		if (isFallingApart && allFallingFloors.Length != 0 && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.KingOfTheHill)
		{
			Vector2 finalPoint = new Vector2(Random.Range(0f - box.size.x, box.size.x) * 0.4f, Random.Range(0f - box.size.y, box.size.y) * 0.4f);
			fallingFloorPlaylist.AddRange(allFallingFloors);
			fallingFloorPlaylist.Sort((FallingFloor a, FallingFloor b) => Vector2.Distance(finalPoint, a.transform.position).CompareTo(Vector2.Distance(finalPoint, b.transform.position)));
			fallingApartTimer = fallingApartDuration;
		}
 }
 public void Update(){
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.KingOfTheHill && isFallingApart && fallingFloorPlaylist.Count > 6 && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 1)
		{
			if (fallingApartTimer > 0f)
			{
				fallingApartTimer -= Time.fixedDeltaTime;
			}
			else
			{
				fallingApartTimer = fallingApartDuration * Random.Range(0.9f, 1.111f);
				int index = fallingFloorPlaylist.Count - 1;
				fallingFloorPlaylist[index].StartFalling();
				fallingFloorPlaylist.RemoveAt(index);
			}
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && isFallingApart && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() <= 1)
		{
			isFallingApart = false;
		}
 }
}
