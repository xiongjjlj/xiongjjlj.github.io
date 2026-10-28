using UnityEngine;

public class LevelProcedural : Level
{
	[Header("Procedural stuff")]
	public GameObject[] chunkPrefabs;

	public GameObject arena;

	private GameObject nextChunk;

	private GameObject[] hazards;

	private GameObject[] ices;

	public override void Init(int spawnOffset)
	{
		base.Init(spawnOffset);
		GenerateNewLevel();
	}

	public void GenerateNewLevel()
	{
		int num = Random.Range(0, 1);
		arena.transform.Rotate(new Vector3(0f, 0f, 180f * (float)num));
		Vector2 vector = new Vector2(-21f, 14f);
		float num2 = 0f;
		for (int i = 0; i < chunkPrefabs.Length; i++)
		{
			GameObject gameObject = chunkPrefabs[i];
			int num3 = Random.Range(i, chunkPrefabs.Length);
			chunkPrefabs[i] = chunkPrefabs[num3];
			chunkPrefabs[num3] = gameObject;
		}
		for (int j = 0; j < 6; j++)
		{
			num2 = -90f * (float)Random.Range(0, 3);
			nextChunk = Object.Instantiate(chunkPrefabs[j], vector, Quaternion.identity);
			nextChunk.transform.RotateAround(vector + new Vector2(7f, -7f), Vector3.forward, num2);
			nextChunk.transform.SetParent(arena.transform);
			if (j != 2)
			{
				vector.x += 14f;
			}
			else if (j == 2)
			{
				vector = new Vector2(-21f, 0f);
			}
			if (j == 1 || j == 4)
			{
				Object.DestroyImmediate(nextChunk.transform.Find("SpawnPoint").gameObject);
			}
		}
		hazards = GameObject.FindGameObjectsWithTag("Hazard");
		ices = GameObject.FindGameObjectsWithTag("Ice");
		for (int k = 0; k < hazards.Length; k++)
		{
			if (Chance.PercentageTrue(50f))
			{
				DisableHazard(hazards[k]);
			}
			else
			{
				EnableHazard(hazards[k]);
			}
		}
		for (int l = 0; l < ices.Length; l++)
		{
			if (Chance.PercentageTrue(50f))
			{
				DisableHazard(ices[l]);
			}
			else
			{
				EnableHazard(ices[l]);
			}
		}
		playerSpawnPoints.Clear();
		GameObject[] array = GameObject.FindGameObjectsWithTag("SpawnPoint");
		if (Singleton<GameManager>.Instance.players.Count > 2)
		{
			for (int m = 0; m < array.Length; m++)
			{
				GameObject gameObject2 = array[m];
				int num4 = Random.Range(m, array.Length);
				array[m] = array[num4];
				array[num4] = gameObject2;
			}
		}
		for (int n = 0; n < array.Length; n++)
		{
			playerSpawnPoints.Add(array[n].transform);
		}
		base.transform.Rotate(0f, 0f, 45f);
	}

	private void DisableHazard(GameObject hazard)
	{
		GameObject gameObject = hazard.transform.Find("Floor").gameObject;
		if (gameObject != null)
		{
			gameObject.transform.SetParent(hazard.transform.parent.parent);
			hazard.SetActive(value: false);
		}
		else
		{
			Debug.Log("ALERT: Hazard had no child floor tile!");
		}
	}

	private void EnableHazard(GameObject hazard)
	{
		GameObject gameObject = hazard.transform.Find("Floor").gameObject;
		if (gameObject != null)
		{
			gameObject.SetActive(value: false);
		}
		else
		{
			Debug.Log("ALERT: Hazard had no child floor tile!");
		}
	}
}
