using System.Collections.Generic;
using UnityEngine;

public class LevelTutorial : Level
{
	public List<GameObject> playerSpawnPods;

	public override void Init(int spawnPointOffset)
	{
		for (int i = 0; i < playerSpawnPods.Count; i++)
		{
			if (Singleton<GameManager>.Instance.players.Count <= 2)
			{
				if ((spawnPointOffset + i) % 2 == 0 && i - spawnPointOffset <= Singleton<GameManager>.Instance.players.Count)
				{
					playerSpawnPoints.Add(playerSpawnPods[i].transform.Find("PlayerSpawn"));
				}
				else
				{
					playerSpawnPods[i].SetActive(value: false);
				}
			}
			else if (i < Singleton<GameManager>.Instance.players.Count)
			{
				playerSpawnPoints.Add(playerSpawnPods[i].transform.Find("PlayerSpawn"));
			}
			else
			{
				playerSpawnPods[i].SetActive(value: false);
			}
		}
		base.Init(spawnPointOffset);
	}
}
