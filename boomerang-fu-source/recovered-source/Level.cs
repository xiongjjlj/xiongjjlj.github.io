using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class Level : MonoBehaviour
{
	[Header("Level Boundaries")]
	public bool destroyDiscsOOB;

	public BoxCollider2D box;

	public bool allowPlayersOutOfBounds;

	public TextAsset pathfindingGraph;

	[Header("Spawn Points")]
	public int playerStartingDiscs;

	public List<Transform> playerSpawnPoints = new List<Transform>();

	public List<Transform> discSpawnPoints = new List<Transform>();

	[Header("Lighting")]
	public GameObject lightingRigDay;

	public GameObject lightingRigNight;

	public Color fogColor = Color.white;

	public Color globalTintColorLight = Color.white;

	public Color globalTintColorDark = new Color(0.592f, 0.909f, 1f);

	public Color globalTintColorFlash = new Color(0.8f, 1f, 1f);

	[Header("Children")]
	[HideInInspector]
	public BoxCollider2D[] allMeshes;

	[HideInInspector]
	public List<Vector3> allCorners = new List<Vector3>();

	[HideInInspector]
	public Switch[] allSwitches;

	[HideInInspector]
	public MovingObstacle[] allMovingObstacles;

	[HideInInspector]
	public MovingFloor[] allMovingFloors;

	[HideInInspector]
	public Teleportal[] allTeleportals;

	[HideInInspector]
	public FallingFloor[] allFallingFloors;

	[HideInInspector]
	public FoliagePatch[] allFoliagePatches;

	[HideInInspector]
	public List<FallingFloor> fallingFloorPlaylist = new List<FallingFloor>();

	[HideInInspector]
	public List<InteractiveObstacle> allInteractiveObstacles;

	[HideInInspector]
	public CameraMagnet[] allCameraMagnets;

	public bool isTutorialLevel;

	public bool isFallingApart;

	private float fallingApartTimer;

	public float fallingApartDuration = 0.5f;

	public bool isCloudLevel;

	public bool isDarkLevel;

	[SerializeField]
	private float stormTimer;

	private float[] stormDurations = new float[2] { 2f, 4f };

	private float[] lightningDuration = new float[2] { 0.032f, 0.166f };

	[Header("Colours")]
	public Color dustColor = new Color(0.412f, 0.4f, 0.365f);

	public Color waterColor = new Color(0.289f, 0.908f, 0.984f);

	public Color movingPlatformColor = new Color(0.688f, 0.598f, 0.461f);

	public Color slidingColor = new Color(0.7176f, 0.9215f, 0.8862f);

	[Header("Disguises")]
	public Player.DisguiseType[] availableDisguiseTypes = new Player.DisguiseType[0];

	public List<GameObject> hideAndSeekPropRoots = new List<GameObject>();

	[Header("Audio")]
	[EventRef]
	public string ambientSound = "event:/atmos/mountains";

	public EventInstance ambientSoundInstance;

	public bool includeTeleportalsInPathfinding = true;

	public virtual void Init(int spawnPointOffset)
	{
		if (Singleton<GameManager>.Instance.players.Count > 2 && !Singleton<SettingsManager>.Instance.teamMatch && !Singleton<SettingsManager>.Instance.tieBreaker)
		{
			ShuffleSpawnPoints(playerSpawnPoints);
		}
		GetAllMeshes();
		GetAllCorners();
		GetAllSwitchesAndMovingObstacles();
		GetAllFoliagePatches();
		if (isFallingApart && allFallingFloors.Length != 0 && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.KingOfTheHill)
		{
			Vector2 finalPoint = new Vector2(Random.Range(0f - box.size.x, box.size.x) * 0.4f, Random.Range(0f - box.size.y, box.size.y) * 0.4f);
			fallingFloorPlaylist.AddRange(allFallingFloors);
			fallingFloorPlaylist.Sort((FallingFloor a, FallingFloor b) => Vector2.Distance(finalPoint, a.transform.position).CompareTo(Vector2.Distance(finalPoint, b.transform.position)));
			fallingApartTimer = fallingApartDuration;
		}
		if (isDarkLevel)
		{
			Shader.EnableKeyword("GlobalTint_ON");
			if (Chance.PercentageTrue(90f) || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek || Singleton<LevelManager>.Instance.matchIndex < 6 || Singleton<GameManager>.Instance.players.Count > 4)
			{
				SetAmbientLight();
				isDarkLevel = false;
				if (lightingRigDay != null)
				{
					lightingRigDay.SetActive(value: true);
				}
				if (lightingRigNight != null)
				{
					lightingRigNight.SetActive(value: false);
				}
				if (!string.IsNullOrEmpty(ambientSound))
				{
					ambientSoundInstance = Singleton<AudioManager>.Instance.PlayLooping(ambientSound, base.transform);
				}
			}
			else
			{
				SetAmbientDark();
				if (lightingRigDay != null)
				{
					lightingRigDay.SetActive(value: false);
				}
				if (lightingRigNight != null)
				{
					lightingRigNight.SetActive(value: true);
				}
				stormTimer = Random.Range(stormDurations[0], stormDurations[1]);
				if (!string.IsNullOrEmpty(ambientSound))
				{
					ambientSoundInstance = Singleton<AudioManager>.Instance.PlayLooping("event:/atmos/night", base.transform);
				}
			}
		}
		else
		{
			Shader.DisableKeyword("GlobalTint_ON");
			SetAmbientLight();
			if (!string.IsNullOrEmpty(ambientSound))
			{
				ambientSoundInstance = Singleton<AudioManager>.Instance.PlayLooping(ambientSound, base.transform);
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			for (int num = 0; num < hideAndSeekPropRoots.Count && (Singleton<SettingsManager>.Instance.numBots <= 0 || num <= 0); num++)
			{
				hideAndSeekPropRoots[num].SetActive(value: true);
			}
			allInteractiveObstacles = base.gameObject.GetComponentsInChildren<InteractiveObstacle>().ToList();
			List<InteractiveObstacle> list = allInteractiveObstacles.Where((InteractiveObstacle x) => x.disguiseType != Player.DisguiseType.None).ToList();
			list.Shuffle();
			int num2 = 0;
			for (int num3 = list.Count - 1; num3 >= 0; num3--)
			{
				if (!IsInBounds(list[num3].transform.position) || Singleton<LevelManager>.Instance.IsPositionOverHazard(list[num3].transform.position, 0.5f))
				{
					list.RemoveAt(num3);
					num2++;
				}
			}
			for (int num4 = 18 + Singleton<GameManager>.Instance.players.Count + Random.Range(0, 2); num4 < list.Count; num4++)
			{
				list[num4].gameObject.SetActive(value: false);
			}
			if (Singleton<SettingsManager>.Instance.numBots > 0)
			{
				Singleton<LevelManager>.Instance.RescanPathfindingGraph();
			}
			return;
		}
		foreach (GameObject hideAndSeekPropRoot in hideAndSeekPropRoots)
		{
			hideAndSeekPropRoot.SetActive(value: false);
		}
	}

	private bool IsInBounds(Vector2 point)
	{
		if (box == null)
		{
			return true;
		}
		point = (Vector2)box.transform.InverseTransformPoint(point) - box.offset;
		float num = box.size.x * 0.5f;
		float num2 = box.size.y * 0.5f;
		if (point.x < num && point.x > 0f - num && point.y < num2 && point.y > 0f - num2)
		{
			return true;
		}
		return false;
	}

	private void Update()
	{
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
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && isDarkLevel)
		{
			stormTimer -= Time.deltaTime;
			if (stormTimer <= 0f)
			{
				stormTimer = Random.Range(stormDurations[0], stormDurations[1]);
				StartCoroutine(LightningStrike());
			}
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu && ambientSoundInstance.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(ambientSoundInstance);
		}
	}

	public void UpdateObstacles()
	{
		GetAllCorners();
	}

	private void GetAllMeshes()
	{
		allMeshes = base.gameObject.GetComponentsInChildren<BoxCollider2D>();
	}

	private void GetAllCorners()
	{
		allCorners.Clear();
		for (int i = 0; i < allMeshes.Length; i++)
		{
			if (allMeshes[i].gameObject.layer == LayerMask.NameToLayer("Obstacle") && allMeshes[i].isActiveAndEnabled)
			{
				Transform transform = allMeshes[i].transform;
				Vector3 vector = transform.TransformPoint(0f, 0f, 0f);
				vector = new Vector3(vector.x, vector.y, 0f);
				Vector2 vector2 = new Vector2(allMeshes[i].size.x * transform.localScale.x * 0.5f, allMeshes[i].size.y * transform.localScale.y * 0.5f);
				Vector3 angles = new Vector3(0f, 0f, transform.eulerAngles.z);
				Vector3 point = new Vector2(0f - vector2.x, 0f - vector2.y);
				Vector3 point2 = new Vector2(0f - vector2.x, vector2.y);
				Vector3 point3 = new Vector2(vector2.x, 0f - vector2.y);
				Vector3 point4 = new Vector2(vector2.x, vector2.y);
				point = VectorExt.RotatePointAroundPivot(point, Vector3.zero, angles);
				point2 = VectorExt.RotatePointAroundPivot(point2, Vector3.zero, angles);
				point3 = VectorExt.RotatePointAroundPivot(point3, Vector3.zero, angles);
				point4 = VectorExt.RotatePointAroundPivot(point4, Vector3.zero, angles);
				allCorners.Add(vector + point);
				allCorners.Add(vector + point2);
				allCorners.Add(vector + point3);
				allCorners.Add(vector + point4);
			}
		}
	}

	private void GetAllSwitchesAndMovingObstacles()
	{
		allSwitches = base.gameObject.GetComponentsInChildren<Switch>();
		allMovingObstacles = base.gameObject.GetComponentsInChildren<MovingObstacle>();
		allMovingFloors = base.gameObject.GetComponentsInChildren<MovingFloor>();
		allTeleportals = base.gameObject.GetComponentsInChildren<Teleportal>();
		allFallingFloors = base.gameObject.GetComponentsInChildren<FallingFloor>();
		allInteractiveObstacles = base.gameObject.GetComponentsInChildren<InteractiveObstacle>().ToList();
		allCameraMagnets = base.gameObject.GetComponentsInChildren<CameraMagnet>();
	}

	private void GetAllFoliagePatches()
	{
		allFoliagePatches = base.gameObject.GetComponentsInChildren<FoliagePatch>();
		Transform[] array = new Transform[Singleton<GameManager>.Instance.players.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Singleton<GameManager>.Instance.players[i].transform;
		}
		for (int j = 0; j < allFoliagePatches.Length; j++)
		{
			allFoliagePatches[j].SetInfluencers(array);
		}
	}

	private void ShuffleSpawnPoints(List<Transform> spawnPoints)
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			Transform value = spawnPoints[i];
			int index = Random.Range(i, Singleton<GameManager>.Instance.players.Count);
			spawnPoints[i] = spawnPoints[index];
			spawnPoints[index] = value;
		}
	}

	private void SetAmbientDark()
	{
		RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
		Shader.SetGlobalColor("_GlobalTint", globalTintColorDark);
		RenderSettings.fogColor = new Color(0.85f, 0.875f, 1f);
		Singleton<Camera3D>.Instance.TurnOnRain();
	}

	private void LightningOn(float duration)
	{
		Shader.SetGlobalColor("_GlobalTint", globalTintColorFlash);
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			Singleton<GameManager>.Instance.players[i].Illuminate(duration);
		}
	}

	private void LightningOff()
	{
		Shader.SetGlobalColor("_GlobalTint", globalTintColorDark);
	}

	private void SetAmbientLight()
	{
		RenderSettings.ambientLight = new Color(0.77f, 0.77f, 0.77f);
		Shader.SetGlobalColor("_GlobalTint", globalTintColorLight);
		RenderSettings.fogColor = fogColor;
		Singleton<Camera3D>.Instance.TurnOffRain();
	}

	private IEnumerator LightningStrike()
	{
		Singleton<AudioManager>.Instance.PlayOneShot("event:/environment/lightning");
		float num = Random.Range(lightningDuration[0], lightningDuration[1]);
		LightningOn(num);
		yield return new WaitForSeconds(num);
		num = Random.Range(lightningDuration[0], lightningDuration[1]);
		LightningOff();
		yield return new WaitForSeconds(num);
		num = Random.Range(lightningDuration[0], lightningDuration[1]);
		LightningOn(num);
		yield return new WaitForSeconds(num);
		num = Random.Range(lightningDuration[0], lightningDuration[1]);
		LightningOff();
		yield return new WaitForSeconds(num);
		num = Random.Range(lightningDuration[0], lightningDuration[1]);
		LightningOn(num);
		yield return new WaitForSeconds(num);
		LightningOff();
	}

	public Player.DisguiseType GetNearestDisguse(Vector3 pos, float maxRadius)
	{
		float num = maxRadius;
		Player.DisguiseType result = Player.DisguiseType.None;
		for (int i = 0; i < allInteractiveObstacles.Count; i++)
		{
			if (allInteractiveObstacles[i].disguiseType != Player.DisguiseType.None && allInteractiveObstacles[i].gameObject.activeSelf)
			{
				float magnitude = (allInteractiveObstacles[i].transform.position - pos).magnitude;
				if (magnitude <= num)
				{
					num = magnitude;
					result = allInteractiveObstacles[i].disguiseType;
				}
			}
		}
		return result;
	}

	public void MakeStatuesReactToKill()
	{
		FacialExpression facialExpression = FacialExpression.shocked;
		facialExpression = ((!Chance.PercentageTrue(50f)) ? FacialExpression.happy : FacialExpression.panic);
		for (int i = 0; i < allInteractiveObstacles.Count; i++)
		{
			if (allInteractiveObstacles[i] is ExpressiveObstacle)
			{
				(allInteractiveObstacles[i] as ExpressiveObstacle).ReactToKill(facialExpression);
			}
		}
	}

	public void RescanRandomizedMovingObstacles()
	{
		MovingObstacle[] array = allMovingObstacles;
		foreach (MovingObstacle movingObstacle in array)
		{
			if (movingObstacle.randomizedStart)
			{
				movingObstacle.UpdatePaths();
			}
		}
	}

	private void DrawSpawnPoints(List<Transform> spawnPoints, Color color, float radius)
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (spawnPoints[i] != null)
			{
				Gizmos.color = color;
				Gizmos.DrawWireSphere(spawnPoints[i].position, radius);
			}
		}
	}

	public void OnDisable()
	{
		if (ambientSoundInstance.isValid() && Singleton<AudioManager>.Instance != null)
		{
			Singleton<AudioManager>.Instance.StopLooping(ambientSoundInstance);
		}
	}
}
