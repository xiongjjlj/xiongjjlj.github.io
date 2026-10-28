using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : Singleton<LevelManager>
{
	[Header("Scene loading")]
	public string currentScene = string.Empty;

	public Level level;

	public List<LevelAsset> levelAssets;

	public List<LevelAsset> switchIncompatibleLevels;

	public bool nextLevelLoaded = true;

	[SerializeField]
	private List<LevelAsset> matchPlaylist = new List<LevelAsset>();

	public int matchIndex;

	public BoxCollider2D levelBoundary;

	public LevelBoundaryLine levelBoundaryLine;

	public ParticleSystem shrinkingBoundsMarker;

	public bool isLevelBoundaryShrinking;

	public Player levelBoundaryOwner;

	public AnimationCurve shrinkCurve;

	private Vector2 levelBoundaryStartSize;

	private Vector2 levelBoundaryShrunkSize;

	private Vector2[] levelBoundaryStartCorners = new Vector2[4];

	private Vector2[] levelBoundaryShrunkCorners = new Vector2[4];

	private float levelBoundaryShrinkTimer;

	private const float levelBoundaryShrinkSpeed = 7f;

	private float levelBoundaryShrinkDuration = 6f;

	private const float levelBoundaryShrunkWidth = 10f;

	[Header("Pathfinding")]
	public AstarPath pathfinder;

	private bool recreatePathsNextOpportunity;

	private List<GridNode> teleportalNodeLinks;

	[Header("Hazards")]
	public LayerMask floorLayers;

	public LayerMask extinguishingLayers;

	private Vector2[] hazardCollisionPoints = new Vector2[9];

	private List<PowerupType> powerupProbabilities = new List<PowerupType>
	{
		PowerupType.ExplosiveDisc,
		PowerupType.MultiDisc,
		PowerupType.TeleportDisc,
		PowerupType.FireDisc,
		PowerupType.SlimeDisc,
		PowerupType.MoveFaster,
		PowerupType.DashThroughWalls,
		PowerupType.Disguise,
		PowerupType.ExtraDisc,
		PowerupType.Shield,
		PowerupType.ControlledDisc,
		PowerupType.ShrinkingBounds,
		PowerupType.Decoy
	};

	[Header("Powerups")]
	public List<PowerupType> possiblePowerups;

	public List<PowerupType> usefulPowerups;

	private Queue<PowerupType> removedPowerups;

	public GameObject powerupPrefab;

	public LayerMask powerupSpawnAvoidLayersClose;

	public LayerMask powerupSpawnAvoidLayersFar;

	[SerializeField]
	private float powerupSpawnDelay;

	private int maxSpawnedPowerups = 1;

	private int lastMatchIndexBeforePowerupsSpawn = 3;

	private int powerupsCollectedSoFar;

	private int powerupsSpawnedThisRound;

	private int powerupSpawnAttemptsThisRound;

	[SerializeField]
	private float powerupsCollectedThreshold;

	private float timeOfLastPowerupSpawnAttempt;

	public List<Transform> powerupSpawnPoints = new List<Transform>();

	private Vector2 powerupSpawnPoint;

	private IEnumerator powerupSpawner;

	[Header("Deprecated stuff")]
	public GameObject[] levelChunks;

	public int numEnemies;

	private void Start()
	{
		currentScene = string.Empty;
		Shader.EnableKeyword("GLOBAL_TINT_ON");
	}

	private void Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && recreatePathsNextOpportunity && !pathfinder.IsAnyGraphUpdateQueued)
		{
			RecreatePaths();
			recreatePathsNextOpportunity = false;
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups != PowerupType.None && Singleton<TimeKeeper>.Instance.TimeSince(timeOfLastPowerupSpawnAttempt) > powerupSpawnDelay)
		{
			AttemptToSpawnPowerup();
			timeOfLastPowerupSpawnAttempt = GetRandomisedTimeOfLastPowerupSpawnAttempt();
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && isLevelBoundaryShrinking)
		{
			if (levelBoundaryShrinkTimer < levelBoundaryShrinkDuration)
			{
				levelBoundaryShrinkTimer += Time.deltaTime;
				float t = shrinkCurve.Evaluate(levelBoundaryShrinkTimer / levelBoundaryShrinkDuration);
				levelBoundary.size = Vector2.Lerp(levelBoundaryStartSize, levelBoundaryShrunkSize, t);
				levelBoundary.transform.position = GetBoxCenterFromCorners(Vector2.Lerp(levelBoundaryStartCorners[0], levelBoundaryShrunkCorners[0], t), Vector2.Lerp(levelBoundaryStartCorners[2], levelBoundaryShrunkCorners[2], t));
				Debug.DrawLine(levelBoundaryShrunkCorners[0], levelBoundaryShrunkCorners[1], Color.red);
				Debug.DrawLine(levelBoundaryShrunkCorners[1], levelBoundaryShrunkCorners[2], Color.red);
				Debug.DrawLine(levelBoundaryShrunkCorners[2], levelBoundaryShrunkCorners[3], Color.red);
				Debug.DrawLine(levelBoundaryShrunkCorners[3], levelBoundaryShrunkCorners[0], Color.red);
			}
			else
			{
				levelBoundary.size = levelBoundaryShrunkSize;
				StopShrinkingLevelBoundary();
			}
			if (levelBoundaryLine != null)
			{
				levelBoundaryLine.SetLineCorners(GetBoxCorners(levelBoundary.transform.position, levelBoundary.transform.eulerAngles, levelBoundary.size));
				levelBoundaryLine.SetGlowSize(levelBoundary.transform.position, levelBoundary.size);
			}
		}
	}

	private float GetRandomisedTimeOfLastPowerupSpawnAttempt()
	{
		return Singleton<TimeKeeper>.Instance.CurrentTime + powerupSpawnDelay * Random.Range(0f, 0.5f);
	}

	public LevelAsset GetLevelAssetByIndex(int index)
	{
		if (index >= 0 && index < levelAssets.Count)
		{
			return levelAssets[index];
		}
		return null;
	}

	public int GetCurrentLevelIndex()
	{
		for (int i = 0; i < levelAssets.Count; i++)
		{
			if (currentScene == levelAssets[i].path)
			{
				return i;
			}
		}
		return -1;
	}

	public void BuildMatchPlaylist(bool rematch = false)
	{
		matchPlaylist.Clear();
		if (Singleton<SettingsManager>.Instance.debugMode)
		{
			matchPlaylist = levelAssets;
			matchPlaylist.RemoveAll((LevelAsset x) => x.complexity == LevelAsset.Complexity.Tutorial);
			matchIndex = 0;
			powerupsCollectedSoFar = 0;
			if (rematch)
			{
				lastMatchIndexBeforePowerupsSpawn = 1;
			}
			else
			{
				lastMatchIndexBeforePowerupsSpawn = 4;
			}
			BuildPossiblePowerups();
			return;
		}
		List<LevelAsset> list = new List<LevelAsset>();
		bool flag = false;
		for (int num = 0; num < levelAssets.Count; num++)
		{
			if (levelAssets[num].soloInPlaylist)
			{
				flag = true;
				break;
			}
		}
		for (int num2 = 0; num2 < levelAssets.Count; num2++)
		{
			LevelAsset levelAsset = levelAssets[num2];
			if ((Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Switch || Singleton<SettingsManager>.Instance.UseCustomMaps || !switchIncompatibleLevels.Contains(levelAsset) || Singleton<SettingsManager>.Instance.numBots <= 0) && (!flag || levelAsset.soloInPlaylist) && (Singleton<SettingsManager>.Instance.UseCustomMaps || (levelAsset.minPlayers <= Singleton<GameManager>.Instance.players.Count && levelAsset.maxPlayers >= Singleton<GameManager>.Instance.players.Count)) && levelAsset.supportedMatchTypes.Contains(Singleton<SettingsManager>.Instance.matchType) && Singleton<GameManager>.Instance.IsDLCUnlocked(levelAsset.requiredDLC) && !IsLevelDisabledInMatchSettings(levelAsset))
			{
				list.Add(levelAsset);
			}
		}
		list.Shuffle();
		int baseInARow = 0;
		int dlcInARow = 0;
		int maximumLevelsInARow = 2;
		int num3 = ((!rematch && !Singleton<SettingsManager>.Instance.trailerMode && !Singleton<SettingsManager>.Instance.debugMode) ? 3 : 0);
		int num4 = 0;
		for (int num5 = list.Count - 1; num5 >= 0; num5--)
		{
			LevelAsset levelAsset2 = list[num5];
			if (levelAsset2.complexity == LevelAsset.Complexity.Tutorial)
			{
				if (!rematch && num4 == 0 && Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
				{
					matchPlaylist.Add(levelAsset2);
					baseInARow++;
					num4++;
				}
				list.RemoveAt(num5);
			}
		}
		while (list.Count > 0)
		{
			List<LevelAsset> list2 = list.Where((LevelAsset level) => level.complexity == LevelAsset.Complexity.Simple).ToList();
			if (num3 > 0 && list2.Count > 0)
			{
				FilterLevelsAndAddLevelToPlaylist(list, list2, ref baseInARow, ref dlcInARow, maximumLevelsInARow);
				num3--;
			}
			else
			{
				FilterLevelsAndAddLevelToPlaylist(list, list, ref baseInARow, ref dlcInARow, maximumLevelsInARow);
			}
		}
		for (int num6 = matchPlaylist.Count - 1; num6 >= 0; num6--)
		{
			if (matchPlaylist[num6].forceFirstInPlaylist)
			{
				MoveItemInPlaylist(ref matchPlaylist, num6, 0);
			}
		}
		matchIndex = 0;
		powerupsCollectedSoFar = 0;
		if (rematch)
		{
			lastMatchIndexBeforePowerupsSpawn = 1;
		}
		else
		{
			lastMatchIndexBeforePowerupsSpawn = 4;
		}
		BuildPossiblePowerups();
		Debug.Log($"BuildMatchPlaylist for {Singleton<SettingsManager>.Instance.matchType} mode");
	}

	private void FilterLevelsAndAddLevelToPlaylist(List<LevelAsset> masterListOfLevels, List<LevelAsset> candidateLevels, ref int baseInARow, ref int dlcInARow, int maximumLevelsInARow)
	{
		if (baseInARow >= maximumLevelsInARow)
		{
			List<LevelAsset> list = candidateLevels.Where((LevelAsset level) => level.requiredDLC != DownloadableContentID.None).ToList();
			if (list.Count > 0)
			{
				AddLevelToPlaylist(masterListOfLevels, list);
				baseInARow = 0;
				dlcInARow = 1;
			}
			else
			{
				AddLevelToPlaylist(masterListOfLevels, candidateLevels);
			}
		}
		else if (dlcInARow >= maximumLevelsInARow)
		{
			List<LevelAsset> list2 = candidateLevels.Where((LevelAsset level) => level.requiredDLC == DownloadableContentID.None).ToList();
			if (list2.Count > 0)
			{
				AddLevelToPlaylist(masterListOfLevels, list2);
				baseInARow = 1;
				dlcInARow = 0;
			}
			else
			{
				AddLevelToPlaylist(masterListOfLevels, candidateLevels);
			}
		}
		else
		{
			if (candidateLevels[0].requiredDLC == DownloadableContentID.None)
			{
				baseInARow++;
			}
			else
			{
				dlcInARow++;
			}
			AddLevelToPlaylist(masterListOfLevels, candidateLevels);
		}
	}

	private void AddLevelToPlaylist(List<LevelAsset> masterListOfLevels, List<LevelAsset> candidateLevels)
	{
		LevelAsset item = candidateLevels[0];
		matchPlaylist.Add(item);
		masterListOfLevels.Remove(item);
	}

	public bool IsLevelDisabledInMatchSettings(LevelAsset levelAsset)
	{
		if (levelAsset.complexity == LevelAsset.Complexity.Tutorial)
		{
			return false;
		}
		int index = levelAssets.IndexOf(levelAsset);
		return IsLevelDisabledInMatchSettings(index);
	}

	private bool IsLevelDisabledInMatchSettings(int index)
	{
		if (!Singleton<SettingsManager>.Instance.UseCustomMaps)
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch)
		{
			if (Singleton<SettingsManager>.Instance.MatchSettings.disabledDeathmatchLevels.Contains(index))
			{
				return true;
			}
		}
		else if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.MatchSettings.disabledGoldenDiscLevels.Contains(index))
		{
			return true;
		}
		return false;
	}

	private void ShufflePlaylist(ref List<LevelAsset> playlist)
	{
		for (int i = 0; i < playlist.Count; i++)
		{
			LevelAsset value = playlist[i];
			int index = Random.Range(i, playlist.Count);
			playlist[i] = playlist[index];
			playlist[index] = value;
		}
	}

	private void MoveItemInPlaylist(ref List<LevelAsset> list, int oldIndex, int newIndex)
	{
		LevelAsset item = list[oldIndex];
		list.RemoveAt(oldIndex);
		if (newIndex > oldIndex)
		{
			newIndex--;
		}
		list.Insert(newIndex, item);
	}

	public void InsertLevelNextInPlaylist(LevelAsset item)
	{
		matchPlaylist.Insert(matchIndex, item);
	}

	public void UnloadCurrentLevel()
	{
		StartCoroutine(UnloadCurrentLevelAsync());
	}

	private IEnumerator UnloadCurrentLevelAsync()
	{
		if (powerupSpawner != null)
		{
			StopCoroutine(powerupSpawner);
		}
		if (!string.IsNullOrEmpty(currentScene))
		{
			yield return SceneManager.UnloadSceneAsync(currentScene);
			yield return null;
			currentScene = string.Empty;
		}
	}

	public IEnumerator LoadNewLevel()
	{
		nextLevelLoaded = false;
		if (!string.IsNullOrEmpty(currentScene))
		{
			yield return StartCoroutine(UnloadCurrentLevelAsync());
		}
		if (Singleton<GameManager>.Instance.abortLoading || Singleton<GameManager>.Instance.matchWinners.Count == 1)
		{
			yield break;
		}
		if (matchPlaylist.Count == 0)
		{
			Debug.LogWarning("LevelManager.Instance.matchPlaylist has nothing in it!");
			Singleton<UIManager>.Instance.QuitMatch();
			yield break;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<GameManager>.Instance.IsNextSeekerABot())
		{
			for (int i = 0; i < matchPlaylist.Count; i++)
			{
				if (!matchPlaylist[matchIndex % matchPlaylist.Count].isTrickyToNavigate)
				{
					break;
				}
				Debug.Log("A bot is the next seeker: skipping levels that are tricky for the AI to navigate");
				matchIndex++;
			}
		}
		currentScene = matchPlaylist[matchIndex % matchPlaylist.Count].path;
		if (matchPlaylist[matchIndex % matchPlaylist.Count].complexity == LevelAsset.Complexity.Tutorial)
		{
			matchPlaylist.RemoveAt(matchIndex);
		}
		else
		{
			matchIndex++;
		}
		yield return SceneManager.LoadSceneAsync(currentScene, LoadSceneMode.Additive);
		yield return null;
		level = Object.FindObjectOfType<Level>();
		nextLevelLoaded = true;
		if (Singleton<GameManager>.Instance.abortLoading)
		{
			Debug.Log("Abort current level load");
			yield return StartCoroutine(UnloadCurrentLevelAsync());
		}
	}

	public IEnumerator InitNewLevel(int spawnPointOffset = 0)
	{
		level.Init(spawnPointOffset);
		powerupSpawnDelay = 8f / ((float)Mathf.Clamp(Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots, 0, 5) * 0.5f);
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			powerupSpawnDelay *= 4f;
		}
		powerupsSpawnedThisRound = 0;
		powerupSpawnAttemptsThisRound = 0;
		timeOfLastPowerupSpawnAttempt = float.MaxValue;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			powerupsCollectedThreshold = 4f * (float)Mathf.Clamp(Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots, 0, 5);
			if (Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency == PowerupSpawnFrequency.Fast)
			{
				powerupsCollectedThreshold += (float)(matchIndex - lastMatchIndexBeforePowerupsSpawn) * 0.35f * (float)Mathf.Clamp(Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots, 0, 4);
			}
		}
		else
		{
			powerupsCollectedThreshold = (float)(matchIndex - lastMatchIndexBeforePowerupsSpawn) * 0.35f * (float)Mathf.Clamp(Singleton<SettingsManager>.Instance.numPlayers + Singleton<SettingsManager>.Instance.numBots, 0, 4);
		}
		powerupsCollectedThreshold *= Mathf.Clamp((possiblePowerups.Count + removedPowerups.Count) / 10, 0.38f, 1f);
		if (Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency == PowerupSpawnFrequency.Slow)
		{
			powerupsCollectedThreshold *= 0.8f;
			powerupSpawnDelay *= 1.5f;
		}
		else if (Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency == PowerupSpawnFrequency.Fast)
		{
			powerupsCollectedThreshold *= 1.33f;
			powerupSpawnDelay *= 0.75f;
			lastMatchIndexBeforePowerupsSpawn = 1;
		}
		Debug.Log("Load new level: " + matchIndex);
		Debug.Log($"powerupsCollectedThreshold: {powerupsCollectedThreshold}, powerupSpawnDelay: {powerupSpawnDelay}");
		if (level.discSpawnPoints.Count > 0)
		{
			int num = ((level.discSpawnPoints.Count > Singleton<GameManager>.Instance.players.Count) ? Singleton<GameManager>.Instance.players.Count : level.discSpawnPoints.Count);
			for (int i = 0; i < num; i++)
			{
				Disc disc = Singleton<GameManager>.Instance.discPrefab.Spawn(level.discSpawnPoints[i].position, Quaternion.identity);
				disc.throwDirection = Vector2.zero;
				disc.aimPower = 0f;
				disc.Init(isDropped: true);
				Singleton<GameManager>.Instance.activeDiscs.Add(disc);
			}
		}
		recreatePathsNextOpportunity = false;
		levelBoundary.size = level.box.size;
		levelBoundaryStartSize = levelBoundary.size;
		levelBoundaryShrunkSize = new Vector2(10f, 10f);
		levelBoundary.transform.position = level.box.transform.position;
		levelBoundary.transform.rotation = level.box.transform.rotation;
		levelBoundary.offset = level.box.offset;
		levelBoundaryOwner = null;
		if (levelBoundaryLine != null)
		{
			levelBoundaryLine.HideGlow();
			if (level.allowPlayersOutOfBounds)
			{
				levelBoundaryLine.gameObject.SetActive(value: false);
			}
			else
			{
				levelBoundaryLine.gameObject.SetActive(value: true);
				levelBoundaryLine.SetLineCorners(GetBoxCorners(levelBoundary.transform.position, levelBoundary.transform.eulerAngles, levelBoundary.size));
				levelBoundaryLine.SetGlowSize(levelBoundary.transform.position, levelBoundary.size);
			}
		}
		StopShrinkingLevelBoundary();
		LoadPathfindingGraph();
		level.RescanRandomizedMovingObstacles();
		yield break;
	}

	public void LoadPathfindingGraph()
	{
		if (level.pathfindingGraph == null)
		{
			SetOutOfBoundsTags();
		}
		else
		{
			AstarPath.active.data.DeserializeGraphs(level.pathfindingGraph.bytes);
			SetOutOfBoundsTags();
			if (level != null && level.allTeleportals.Length != 0)
			{
				ConnectTeleportalPaths();
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<SettingsManager>.Instance.numBots > 0)
		{
			RescanPathfindingGraph();
		}
	}

	public void RescanPathfindingGraph()
	{
		if (pathfinder.graphs.Length != 0)
		{
			pathfinder.Scan(pathfinder.graphs[0]);
			if (level != null && level.allTeleportals.Length != 0)
			{
				ConnectTeleportalPaths();
			}
		}
	}

	public void StartMovingObjects()
	{
		for (int i = 0; i < level.allMovingFloors.Length; i++)
		{
			level.allMovingFloors[i].StartMoving();
		}
	}

	public void StartSpawningPowerups()
	{
		timeOfLastPowerupSpawnAttempt = GetRandomisedTimeOfLastPowerupSpawnAttempt() * 0.66f;
	}

	public bool IsInBounds(Vector2 point)
	{
		if (level == null || levelBoundary == null)
		{
			return true;
		}
		if (level.allowPlayersOutOfBounds)
		{
			return true;
		}
		point = (Vector2)levelBoundary.transform.InverseTransformPoint(point) - levelBoundary.offset;
		float num = levelBoundary.size.x * 0.5f;
		float num2 = levelBoundary.size.y * 0.5f;
		if (point.x < num && point.x > 0f - num && point.y < num2 && point.y > 0f - num2)
		{
			return true;
		}
		return false;
	}

	public bool IsCloseToBoundaryEdge(Vector2 point)
	{
		if (levelBoundary == null)
		{
			return false;
		}
		point = (Vector2)levelBoundary.transform.InverseTransformPoint(point) - levelBoundary.offset;
		float num = 4f;
		float num2 = levelBoundary.size.x * 0.5f - num;
		float num3 = levelBoundary.size.y * 0.5f - num;
		if (point.x > num2 || point.x < 0f - num2 || point.y > num3 || point.y < 0f - num3)
		{
			return true;
		}
		return false;
	}

	public Vector3 ClampPositionInBounds(Vector3 pos, Vector2 padding)
	{
		pos = VectorExt.RotatePointAroundPivot(pos, Vector3.zero, -levelBoundary.transform.eulerAngles);
		pos.x = Mathf.Min(pos.x, level.box.size.x * 0.5f - padding.x);
		pos.x = Mathf.Max(pos.x, (0f - level.box.size.x) * 0.5f + padding.x);
		pos.y = Mathf.Min(pos.y, level.box.size.y * 0.5f - padding.y);
		pos.y = Mathf.Max(pos.y, (0f - level.box.size.y) * 0.5f + padding.y);
		pos = VectorExt.RotatePointAroundPivot(pos, Vector3.zero, levelBoundary.transform.eulerAngles);
		return pos;
	}

	public bool IsPositionOffCliff(Vector2 pos)
	{
		if (level != null && level.isCloudLevel && !Physics2D.OverlapPoint(pos, 1 << LayerMask.NameToLayer("Water")))
		{
			return true;
		}
		return false;
	}

	public void StartShrinkingLevelBoundary(Vector3 targetPos, bool showMarker = false, Player boundaryOwner = null)
	{
		levelBoundaryShrinkTimer = 0f;
		isLevelBoundaryShrinking = true;
		levelBoundaryStartSize = levelBoundary.size;
		levelBoundaryShrinkDuration = levelBoundary.size.x / 7f;
		levelBoundaryStartCorners = GetBoxCorners(levelBoundary.transform.position, levelBoundary.transform.eulerAngles, levelBoundary.size);
		if (levelBoundaryLine != null && level.allowPlayersOutOfBounds)
		{
			levelBoundaryLine.gameObject.SetActive(value: true);
			level.allowPlayersOutOfBounds = false;
			levelBoundaryLine.SetLineCorners(GetBoxCorners(levelBoundary.transform.position, levelBoundary.transform.eulerAngles, levelBoundary.size));
			levelBoundaryLine.SetGlowSize(levelBoundary.transform.position, levelBoundary.size);
		}
		levelBoundaryLine.Flash();
		targetPos = ClampPositionInBounds(targetPos, levelBoundaryShrunkSize / 2f);
		levelBoundaryShrunkCorners = GetBoxCorners(targetPos, levelBoundary.transform.eulerAngles, levelBoundaryShrunkSize);
		if (showMarker)
		{
			shrinkingBoundsMarker.transform.position = targetPos + levelBoundaryLine.zOffset;
			shrinkingBoundsMarker.transform.rotation = levelBoundary.transform.rotation;
			shrinkingBoundsMarker.Play();
		}
		levelBoundaryOwner = boundaryOwner;
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/battle_royale", targetPos);
	}

	public void StopShrinkingLevelBoundary()
	{
		isLevelBoundaryShrinking = false;
		shrinkingBoundsMarker.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}

	private Vector2[] GetBoxCorners(Vector2 worldPos, Vector3 eulerAngles, Vector2 size)
	{
		Vector2[] array = new Vector2[4];
		size *= 0.5f;
		array[0] = worldPos + new Vector2(0f - size.x, 0f - size.y);
		array[1] = worldPos + new Vector2(0f - size.x, size.y);
		array[2] = worldPos + new Vector2(size.x, size.y);
		array[3] = worldPos + new Vector2(size.x, 0f - size.y);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = VectorExt.RotatePointAroundPivot(array[i], worldPos, eulerAngles);
		}
		return array;
	}

	private Vector2 GetBoxCenterFromCorners(Vector2 minCorner, Vector2 maxCorner)
	{
		return Vector2.Lerp(minCorner, maxCorner, 0.5f);
	}

	public void RescanGraphArea(Bounds bounds, bool forceAIsToRecreatePaths = true, bool reconnectTeleportals = true)
	{
		if (Singleton<SettingsManager>.Instance.numBots > 0 || numEnemies > 0 || Singleton<GameManager>.Instance.decoysInMatch)
		{
			GraphUpdateObject graphUpdateObject = new GraphUpdateObject(bounds);
			graphUpdateObject.updatePhysics = true;
			graphUpdateObject.nnConstraint.graphMask = new GraphMask(1);
			pathfinder.UpdateGraphs(graphUpdateObject);
			if (forceAIsToRecreatePaths)
			{
				recreatePathsNextOpportunity = true;
			}
			if (reconnectTeleportals && level != null && level.allTeleportals.Length != 0)
			{
				ConnectTeleportalPaths();
			}
		}
	}

	public void RescanLocalGraphArea(Bounds bounds, GraphMask graphMask)
	{
		if (Singleton<SettingsManager>.Instance.numBots > 0 || numEnemies > 0 || Singleton<GameManager>.Instance.decoysInMatch)
		{
			GraphUpdateObject graphUpdateObject = new GraphUpdateObject(bounds);
			graphUpdateObject.setWalkability = true;
			graphUpdateObject.modifyWalkability = true;
			graphUpdateObject.nnConstraint.graphMask = graphMask;
			pathfinder.UpdateGraphs(graphUpdateObject);
			recreatePathsNextOpportunity = true;
			if (level != null && level.allTeleportals.Length != 0)
			{
				ConnectTeleportalPaths();
			}
		}
	}

	public void RecreatePathsNextOpportunity()
	{
		if (Singleton<SettingsManager>.Instance.numBots > 0 || numEnemies > 0 || Singleton<GameManager>.Instance.decoysInMatch)
		{
			recreatePathsNextOpportunity = true;
		}
	}

	private void RecreatePaths()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			AI component = Singleton<GameManager>.Instance.players[i].GetComponent<AI>();
			if (component != null && !component.recalculatingPath && !component.IsPathOnWalkableNodes())
			{
				component.CreatePath();
			}
		}
		for (int j = 0; j < Singleton<GameManager>.Instance.activeDecoys.Count; j++)
		{
			AI component2 = Singleton<GameManager>.Instance.activeDecoys[j].GetComponent<AI>();
			if (component2 != null && !component2.recalculatingPath && !component2.IsPathOnWalkableNodes())
			{
				component2.CreatePath();
			}
		}
	}

	public void SetOutOfBoundsTags()
	{
		Vector3[][] array = new Vector3[4][];
		Vector3 vector = level.transform.TransformPoint(0f, 0f, 0f);
		vector.z = 0f;
		Vector2 vector2 = new Vector2(levelBoundary.size.x * level.transform.localScale.x * 0.5f, levelBoundary.size.y * level.transform.localScale.y * 0.5f);
		Vector3 vector3 = new Vector2(0f - vector2.x, 0f - vector2.y);
		Vector3 vector4 = new Vector2(0f - vector2.x, vector2.y);
		Vector3 vector5 = new Vector2(vector2.x, vector2.y);
		Vector3 vector6 = new Vector2(vector2.x, 0f - vector2.y);
		array[0] = new Vector3[4]
		{
			vector3 + new Vector3(0f, 0f - vector2.x),
			vector3 + new Vector3(0f - vector2.x, 0f - vector2.x),
			vector4 + new Vector3(0f - vector2.x, vector2.x),
			vector4 + new Vector3(0f, vector2.x)
		};
		array[2] = new Vector3[4]
		{
			vector5 + new Vector3(0f, vector2.x),
			vector5 + new Vector3(vector2.x, vector2.x),
			vector6 + new Vector3(vector2.x, 0f - vector2.x),
			vector6 + new Vector3(0f, 0f - vector2.x)
		};
		array[1] = new Vector3[4]
		{
			vector4,
			array[0][3],
			array[2][0],
			vector5
		};
		array[3] = new Vector3[4]
		{
			vector6,
			array[2][3],
			array[0][0],
			vector3
		};
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < array[i].Length; j++)
			{
				array[i][j] = VectorExt.RotatePointAroundPivot(array[i][j], Vector3.zero, level.transform.eulerAngles) + vector;
				array[i][j] = VectorExt.FlipYAndZ(array[i][j]);
			}
			GraphUpdateShape graphUpdateShape = new GraphUpdateShape(array[i], convex: true, base.transform.localToWorldMatrix, 1f);
			GraphUpdateObject graphUpdateObject = new GraphUpdateObject(graphUpdateShape.GetBounds());
			graphUpdateObject.shape = graphUpdateShape;
			graphUpdateObject.modifyTag = true;
			graphUpdateObject.setTag = 1;
			graphUpdateObject.updatePhysics = false;
			graphUpdateObject.nnConstraint.graphMask = new GraphMask(1);
			pathfinder.UpdateGraphs(graphUpdateObject);
		}
	}

	public void TagAreaAsOutOfBounds(Vector2 worldPosition, Vector2 extents, Quaternion rotation)
	{
		Vector3[] array = new Vector3[4]
		{
			new Vector2(0f - extents.x, 0f - extents.y),
			new Vector2(0f - extents.x, extents.y),
			new Vector2(extents.x, extents.y),
			new Vector2(extents.x, 0f - extents.y)
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (Vector2)VectorExt.RotatePointAroundPivot(array[i], Vector3.zero, rotation.eulerAngles) + worldPosition;
			array[i] = VectorExt.FlipYAndZ(array[i]);
		}
		GraphUpdateShape graphUpdateShape = new GraphUpdateShape(array, convex: true, base.transform.localToWorldMatrix, 1f);
		GraphUpdateObject graphUpdateObject = new GraphUpdateObject(graphUpdateShape.GetBounds());
		graphUpdateObject.shape = graphUpdateShape;
		graphUpdateObject.modifyTag = true;
		graphUpdateObject.setTag = 1;
		graphUpdateObject.updatePhysics = false;
		pathfinder.UpdateGraphs(graphUpdateObject);
	}

	public void ConnectTeleportalPaths()
	{
		if (level == null || level.allTeleportals == null || level.allTeleportals.Length == 0 || !level.includeTeleportalsInPathfinding)
		{
			return;
		}
		AstarPath.active.AddWorkItem(delegate(IWorkItemContext ctx)
		{
			if (teleportalNodeLinks == null)
			{
				teleportalNodeLinks = new List<GridNode>(level.allTeleportals.Length);
			}
			else if (teleportalNodeLinks.Count > 0)
			{
				for (int i = 0; i < teleportalNodeLinks.Count; i++)
				{
					teleportalNodeLinks[i].ClearCustomConnections(alsoReverse: true);
				}
				teleportalNodeLinks.Clear();
			}
			if (level != null && level.allTeleportals != null)
			{
				for (int j = 0; j < level.allTeleportals.Length; j++)
				{
					if (level != null && level.allTeleportals[j] != null && level.allTeleportals[j].exit != null && level.allTeleportals[j].isSafeForAIs)
					{
						GridNode gridNode = AstarPath.active.GetNearest(level.allTeleportals[j].transform.position).node as GridNode;
						GridNode node = AstarPath.active.GetNearest(level.allTeleportals[j].exit.transform.position).node as GridNode;
						gridNode.AddConnection(node, 0u);
						NNConstraint none = NNConstraint.None;
						none.constrainWalkability = true;
						none.walkable = true;
						none.constrainTags = false;
						none.graphMask = 1;
						GridNode gridNode2 = AstarPath.active.GetNearest((Vector3)gridNode.position, none).node as GridNode;
						if (!gridNode.ContainsConnection(gridNode2))
						{
							gridNode.AddConnection(gridNode2, 0u);
							gridNode2.AddConnection(gridNode, 0u);
						}
						gridNode.Walkable = true;
						gridNode.Tag = 3u;
						AstarPath.active.data.gridGraph.CalculateConnectionsForCellAndNeighbours(gridNode.XCoordinateInGrid, gridNode.ZCoordinateInGrid);
						AstarPath.active.data.gridGraph.CalculateConnectionsForCellAndNeighbours(gridNode2.XCoordinateInGrid, gridNode2.ZCoordinateInGrid);
						Vector3 position = AstarPath.active.GetNearest(level.allTeleportals[j].transform.position).position;
						Vector3 position2 = AstarPath.active.GetNearest((Vector3)gridNode.position, none).position;
						Debug.DrawLine(position, position2, Color.magenta, 1f);
						teleportalNodeLinks.Add(gridNode);
					}
				}
			}
			ctx.QueueFloodFill();
		});
	}

	public GraphMask GetBestGraphForTarget(Vector3 point, Vector3 targetPosition)
	{
		NNConstraint none = NNConstraint.None;
		none.constrainWalkability = true;
		none.walkable = true;
		none.constrainTags = false;
		float num = 7f;
		float num2 = float.MaxValue;
		GraphMask graphMask = 1;
		GraphMask result = 0;
		for (int i = 0; i < level.allMovingFloors.Length; i++)
		{
			graphMask |= level.allMovingFloors[i].graphMask;
		}
		for (int j = 0; j < AstarPath.active.data.graphs.Length; j++)
		{
			if ((int)(graphMask & GraphMask.FromGraph(AstarPath.active.data.graphs[j])) != (int)GraphMask.FromGraph(AstarPath.active.data.graphs[j]))
			{
				continue;
			}
			Vector3 vector = point;
			Vector3 vector2 = targetPosition;
			if (j > 0)
			{
				for (int k = 0; k < level.allMovingFloors.Length; k++)
				{
					if ((int)level.allMovingFloors[k].graphMask == (int)GraphMask.FromGraph(AstarPath.active.data.graphs[j]))
					{
						vector = level.allMovingFloors[k].GetInverseTransformPoint(vector);
						vector2 = level.allMovingFloors[k].GetInverseTransformPoint(targetPosition);
						break;
					}
				}
			}
			if (AstarPath.active.data.graphs[j].GetNearestForce(vector, none).node == null)
			{
				Debug.Log("This graph does not have any nearest nodes that meet the constraints");
				continue;
			}
			Vector3 vector3 = (Vector3)AstarPath.active.data.graphs[j].GetNearestForce(vector, none).node.position;
			if ((vector3 - vector).sqrMagnitude <= num)
			{
				float sqrMagnitude = (vector3 - vector2).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					result = GraphMask.FromGraph(AstarPath.active.data.graphs[j]);
				}
			}
		}
		return result;
	}

	private void AttemptToSpawnPowerup()
	{
		if ((float)powerupsCollectedSoFar >= powerupsCollectedThreshold || Singleton<GameManager>.Instance.spawnedPowerups.Count >= maxSpawnedPowerups || Singleton<GameManager>.Instance.OpponentsLeftStandingNow() <= 1 || powerupsSpawnedThisRound > 1 || powerupSpawnAttemptsThisRound > 6)
		{
			return;
		}
		if (possiblePowerups.Count < 4 && Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups != PowerupType.ReverseInputs)
		{
			bool flag = true;
			for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
			{
				if (Singleton<GameManager>.Instance.players[i].powerupHistory.Count < possiblePowerups.Count)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				Debug.Log("All possible powerups collected by all players!");
				return;
			}
		}
		if (FindSuitablePowerupSpawnPoint(out powerupSpawnPoint) && Chance.PercentageTrue(100f))
		{
			GameObject gameObject = Object.Instantiate(powerupPrefab, powerupSpawnPoint, Quaternion.identity);
			gameObject.GetComponent<Powerup>().interpolatedTransform.ForgetPreviousTransforms();
			Singleton<GameManager>.Instance.spawnedPowerups.Add(gameObject);
			powerupsSpawnedThisRound++;
			Debug.Log("Spawned powerup!");
		}
		powerupSpawnAttemptsThisRound++;
	}

	public bool FindSuitablePowerupSpawnPoint(out Vector2 point)
	{
		float num = 20f;
		point = Vector2.zero;
		Vector2 vector = VectorExt.RotatePointAroundPivot(-Singleton<GameManager>.Instance.GetPlayerWithMostPowerups(livingPlayersOnly: true).transform.position.normalized, Vector2.zero, -level.transform.eulerAngles);
		for (int i = 0; (float)i < num; i++)
		{
			Vector2 vector2 = levelBoundary.transform.TransformPoint(new Vector2(Random.Range(3f, levelBoundary.size.x * 0.5f - 3f) * vector.x, Random.Range(3f, levelBoundary.size.y * 0.5f - 3f) * vector.y));
			if (IsSpawnPointClear(vector2))
			{
				point = vector2;
				return true;
			}
		}
		return false;
	}

	public bool FindSuitableDiscSpawnPoint(out Vector3 point)
	{
		float num = 40f;
		point = Vector2.zero;
		Vector2 vector = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
		Vector2 vector2 = Vector3.zero;
		int num2 = 0;
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (Singleton<GameManager>.Instance.players[i].actorState != Actor.ActorState.Dead)
			{
				num2++;
				vector2 += (Vector2)Singleton<GameManager>.Instance.players[i].transform.position;
			}
		}
		vector2 /= (float)num2;
		if (vector2.magnitude > 0.01f)
		{
			vector = VectorExt.RotatePointAroundPivot(-vector2.normalized, Vector2.zero, -level.transform.eulerAngles);
		}
		for (int j = 0; (float)j < num; j++)
		{
			Vector2 vector3 = new Vector2(Random.Range(1f, levelBoundary.size.x * 0.5f - 3f) * vector.x, Random.Range(1f, levelBoundary.size.y * 0.5f - 3f) * vector.y);
			vector3 = VectorExt.RotatePointAroundPivot(vector3, Vector3.zero, level.transform.eulerAngles);
			if (IsSpawnPointClear(vector3))
			{
				point = (Vector3)vector3 + new Vector3(0f, 0f, -0.25f);
				return true;
			}
		}
		return false;
	}

	private bool IsSpawnPointClear(Vector2 possibleSpawnPoint)
	{
		if (HazardContactPoints(possibleSpawnPoint, 3f) > 0)
		{
			return false;
		}
		if ((bool)Physics2D.OverlapCircle(possibleSpawnPoint, 3f, powerupSpawnAvoidLayersClose))
		{
			return false;
		}
		if ((bool)Physics2D.OverlapCircle(possibleSpawnPoint, 8f, powerupSpawnAvoidLayersFar))
		{
			return false;
		}
		return true;
	}

	public void BuildPossiblePowerups()
	{
		possiblePowerups = new List<PowerupType>();
		usefulPowerups = new List<PowerupType>();
		removedPowerups = new Queue<PowerupType>();
		foreach (PowerupType powerupProbability in powerupProbabilities)
		{
			if (Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups.HasPowerup(powerupProbability))
			{
				possiblePowerups.Add(powerupProbability);
			}
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			while (possiblePowerups.Contains(PowerupType.ShrinkingBounds))
			{
				possiblePowerups.Remove(PowerupType.ShrinkingBounds);
			}
			while (possiblePowerups.Contains(PowerupType.TeleportDisc))
			{
				possiblePowerups.Remove(PowerupType.TeleportDisc);
			}
			while (possiblePowerups.Contains(PowerupType.ControlledDisc))
			{
				possiblePowerups.Remove(PowerupType.ControlledDisc);
			}
			while (possiblePowerups.Contains(PowerupType.ExtraDisc))
			{
				possiblePowerups.Remove(PowerupType.ExtraDisc);
			}
			while (possiblePowerups.Contains(PowerupType.DashThroughWalls))
			{
				possiblePowerups.Remove(PowerupType.DashThroughWalls);
			}
			while (possiblePowerups.Contains(PowerupType.MoveFaster))
			{
				possiblePowerups.Remove(PowerupType.MoveFaster);
			}
		}
	}

	public PowerupType SelectRandomPower(Player player, Powerup collectedPowerup = null)
	{
		if (Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups == PowerupType.ReverseInputs)
		{
			return PowerupType.ReverseInputs;
		}
		PowerupType powerupType = PowerupType.None;
		usefulPowerups.Clear();
		usefulPowerups = possiblePowerups.ConvertAll((PowerupType x) => x);
		if (usefulPowerups.Count > 0)
		{
			do
			{
				powerupType = usefulPowerups[Random.Range(0, usefulPowerups.Count)];
				if (player.activePowerup.HasPowerup(powerupType) || (powerupType == PowerupType.ExtraDisc && player.HasDiscs + player.pendingDiscs > 1) || (powerupType == PowerupType.SlimeDisc && player.activePowerup.HasPowerup(PowerupType.FireDisc)))
				{
					usefulPowerups.Remove(powerupType);
					continue;
				}
				if (powerupType != PowerupType.ShrinkingBounds || IsShrinkingBoundsPowerupAllowed(collectedPowerup))
				{
					break;
				}
				usefulPowerups.Remove(powerupType);
			}
			while (usefulPowerups.Count > 0);
		}
		if (usefulPowerups.Count == 0)
		{
			int num = Random.Range(0, possiblePowerups.Count);
			powerupType = possiblePowerups[num];
			if (powerupType == PowerupType.ShrinkingBounds && IsShrinkingBoundsPowerupAllowed(collectedPowerup))
			{
				num = (num + Random.Range(0, possiblePowerups.Count - 2)) % possiblePowerups.Count;
				powerupType = possiblePowerups[num];
			}
		}
		int num2 = EnumExt.CountSetFlags((ulong)player.activePowerup);
		if (powerupsCollectedSoFar > 0 && !Singleton<SettingsManager>.Instance.GameOptions.conventionMode && Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups.HasPowerup(PowerupType.ReverseInputs) && Chance.PercentageTrue(5f + (float)num2 * 0.25f))
		{
			powerupType = PowerupType.ReverseInputs;
		}
		else
		{
			powerupsCollectedSoFar++;
		}
		return powerupType;
	}

	private bool IsShrinkingBoundsPowerupAllowed(Powerup collectedPowerup)
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.DeathMatch)
		{
			return false;
		}
		if (collectedPowerup != null && collectedPowerup.transform.parent != null)
		{
			return false;
		}
		if (isLevelBoundaryShrinking || Singleton<GameManager>.Instance.suddenDeathStarted)
		{
			return false;
		}
		return true;
	}

	public void CyclePossiblePowerups(PowerupType power)
	{
		if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc && possiblePowerups.Count > 4)
		{
			possiblePowerups.Remove(power);
			removedPowerups.Enqueue(power);
			if (removedPowerups.Count > 4)
			{
				possiblePowerups.Add(removedPowerups.Dequeue());
			}
		}
	}

	public Vector3 FindRandomPlayerRespawnPoint()
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (Singleton<GameManager>.Instance.players[i].actorState != Actor.ActorState.Dead)
			{
				list.Add(Singleton<GameManager>.Instance.players[i].transform.position);
			}
		}
		float num = -1f;
		Vector3 position = level.playerSpawnPoints[Random.Range(0, level.playerSpawnPoints.Count - 1)].position;
		Transform transform = null;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			for (int j = 0; j < Singleton<GameManager>.Instance.activeDiscs.Count; j++)
			{
				if (Singleton<GameManager>.Instance.activeDiscs[j].IsGoldenDisc)
				{
					transform = Singleton<GameManager>.Instance.activeDiscs[j].transform;
					break;
				}
			}
		}
		for (int k = 0; k < level.playerSpawnPoints.Count; k++)
		{
			float num2 = float.MaxValue;
			for (int l = 0; l < list.Count; l++)
			{
				float sqrMagnitude = (level.playerSpawnPoints[k].position - list[l]).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
				}
			}
			if ((Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || !(transform != null) || !((level.playerSpawnPoints[k].position - transform.transform.position).magnitude < 10f)) && num2 > num)
			{
				position = level.playerSpawnPoints[k].position;
				num = num2;
			}
		}
		Debug.DrawLine(position, position + (Vector3)Vector2.one, Color.magenta, 1f);
		return new Vector3(position.x, position.y, Actor.zPos);
	}

	public Vector3 FindNearestRespawnPoint(Vector3 pos)
	{
		float num = float.MaxValue;
		Vector3 vector = pos;
		for (int i = 0; i < level.playerSpawnPoints.Count; i++)
		{
			if ((level.playerSpawnPoints[i].position - pos).magnitude < num)
			{
				vector = level.playerSpawnPoints[i].position;
				num = (level.playerSpawnPoints[i].position - pos).magnitude;
			}
		}
		Debug.DrawLine(vector, vector + (Vector3)Vector2.one, Color.magenta, 1f);
		return new Vector3(vector.x, vector.y, Actor.zPos);
	}

	public Vector2 GetRandomPointInLevelBounds()
	{
		Vector2 vector = -Singleton<LevelManager>.Instance.levelBoundary.size * 0.5f;
		return VectorExt.RotatePointAroundPivot((Vector2)Singleton<LevelManager>.Instance.levelBoundary.transform.position + new Vector2(Random.Range(0f - vector.x, vector.x), Random.Range(0f - vector.y, vector.y)), Singleton<LevelManager>.Instance.levelBoundary.transform.position, Singleton<LevelManager>.Instance.levelBoundary.transform.eulerAngles);
	}

	public int HazardContactPoints(Vector2 position, float radius, bool includeObstacles = true)
	{
		hazardCollisionPoints[0] = position + Vector2.zero;
		hazardCollisionPoints[1] = position + radius * 1.25f * new Vector2(0f, 1f).normalized;
		hazardCollisionPoints[2] = position + radius * new Vector2(1f, 1f).normalized;
		hazardCollisionPoints[3] = position + radius * new Vector2(1f, 0f).normalized;
		hazardCollisionPoints[4] = position + radius * new Vector2(1f, -1f).normalized;
		hazardCollisionPoints[5] = position + radius * new Vector2(0f, -1f).normalized;
		hazardCollisionPoints[6] = position + radius * new Vector2(-1f, -1f).normalized;
		hazardCollisionPoints[7] = position + radius * new Vector2(-1f, 0f).normalized;
		hazardCollisionPoints[8] = position + radius * new Vector2(-1f, 1f).normalized;
		int num = 0;
		bool flag = false;
		for (int i = 0; i < hazardCollisionPoints.Length; i++)
		{
			if (!Physics2D.OverlapPoint(hazardCollisionPoints[i], floorLayers))
			{
				num++;
				if (i == 0)
				{
					flag = true;
				}
			}
			if ((includeObstacles & flag) && i > 0 && (bool)Physics2D.OverlapPoint(hazardCollisionPoints[i], 1 << LayerMask.NameToLayer("Obstacle")))
			{
				num++;
			}
		}
		return num;
	}

	public bool IsPointOnSolidGround(Vector2 position, float radius)
	{
		hazardCollisionPoints[0] = position + Vector2.zero;
		hazardCollisionPoints[1] = position + radius * 1.25f * new Vector2(0f, 1f).normalized;
		hazardCollisionPoints[2] = position + radius * new Vector2(1f, 1f).normalized;
		hazardCollisionPoints[3] = position + radius * new Vector2(1f, 0f).normalized;
		hazardCollisionPoints[4] = position + radius * new Vector2(1f, -1f).normalized;
		hazardCollisionPoints[5] = position + radius * new Vector2(0f, -1f).normalized;
		hazardCollisionPoints[6] = position + radius * new Vector2(-1f, -1f).normalized;
		hazardCollisionPoints[7] = position + radius * new Vector2(-1f, 0f).normalized;
		hazardCollisionPoints[8] = position + radius * new Vector2(-1f, 1f).normalized;
		for (int i = 0; i < hazardCollisionPoints.Length; i++)
		{
			Collider2D collider2D = Physics2D.OverlapPoint(hazardCollisionPoints[i], floorLayers);
			if (collider2D != null && !collider2D.CompareTag("TrapFloor"))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPointOnHazard(Vector2 point)
	{
		if (!Physics2D.OverlapPoint(point, floorLayers))
		{
			return true;
		}
		return false;
	}

	public bool IsPositionOverHazard(Vector2 position, float radius)
	{
		if (HazardContactPoints(position, radius) > 8)
		{
			return true;
		}
		return false;
	}

	public bool IsPositionTouchingHazard(Vector2 position, float radius, int minContactPoints = 0)
	{
		if (HazardContactPoints(position, radius) > minContactPoints)
		{
			return true;
		}
		return false;
	}

	public bool CanFireSpawnAt(Vector2 point)
	{
		if (IsPointOnHazard(point))
		{
			return false;
		}
		if ((bool)Physics2D.OverlapPoint(point, extinguishingLayers))
		{
			return false;
		}
		return true;
	}

	public bool CanIceSpawnAt(Vector2 point)
	{
		if (IsPointOnHazard(point))
		{
			return false;
		}
		return true;
	}

	public void OnTutorialSwitchFlipped()
	{
		if (!HaveAllHumansCompletedTutorial())
		{
			return;
		}
		for (int i = 0; i < level.allSwitches.Length; i++)
		{
			if (level.allSwitches[i].isActive)
			{
				level.allSwitches[i].ActivateAfterDelay(Random.Range(1f, 3f));
			}
		}
		for (int j = 0; j < Singleton<GameManager>.Instance.players.Count; j++)
		{
			Singleton<GameManager>.Instance.players[j].hasCompletedTutorial = true;
		}
	}

	private bool HaveAllHumansCompletedTutorial()
	{
		int num = 0;
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!(Singleton<GameManager>.Instance.players[i].GetComponent<PlayerAI>() != null) && Singleton<GameManager>.Instance.players[i].hasCompletedTutorial)
			{
				num++;
			}
		}
		if (num >= Singleton<SettingsManager>.Instance.numPlayers)
		{
			return true;
		}
		return false;
	}

	public List<LevelAsset> GetLevelAssetsByDLCID(DownloadableContentID dlcID)
	{
		List<LevelAsset> list = new List<LevelAsset>();
		foreach (LevelAsset levelAsset in levelAssets)
		{
			if (levelAsset.requiredDLC == dlcID)
			{
				list.Add(levelAsset);
			}
		}
		return list;
	}
}
