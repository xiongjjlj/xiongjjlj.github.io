using MonsterLove.StateMachine;
using Pathfinding;
using UnityEngine;

public class AI : MonoBehaviour
{
	public enum AIState
	{
		Wandering,
		Collecting,
		Attacking,
		Fleeing,
		Diverting,
		Retrieving,
		FlickingSwitch,
		Idle,
		Dead,
		Seeking
	}

	public enum InterestType
	{
		None,
		Desirable,
		Victim,
		Threat,
		Diversion
	}

	public enum ThreatType
	{
		Player,
		Explosive,
		Fire
	}

	public enum DesirableType
	{
		Powerup,
		DroppedDisc,
		DeadTeammate
	}

	protected delegate bool PathEndDelegate();

	[SerializeField]
	protected string reasonForLastDash;

	protected Actor actor;

	protected StateMachine<AIState> fsm;

	public float stateChangeReluctanceCooldown;

	public float stateChangeReluctance;

	public float stateChangeReluctanceDefault = 0.7f;

	protected MovingFloor currentGraphMovingFloor;

	protected Seeker seeker;

	protected SimpleSmoothModifier smoothModifier;

	protected PathEdgeModifier pathEdgeModifier;

	public GameObject targetObject;

	public Vector3 targetPosition = Vector3.zero;

	public Path path;

	public int currentWaypoint;

	public float pathLength;

	public bool reachedEndOfPath;

	public bool recalculatingPath;

	protected bool createPathAfterDash;

	protected Collider2D[] nearbyPlants = new Collider2D[8];

	protected RaycastHit2D raycastHit;

	protected int outOfBoundsReluctance = 4000;

	protected float nextWaypointDistance = 1.5f;

	protected float currentWaypointTooFarDistance = 3f;

	protected float divertingDistance = 3f;

	protected int fleeDistance = 15000;

	protected int fleeSpread = 1000;

	protected float fleeAimStrength = 0.75f;

	[SerializeField]
	protected Vector2 moveDir = Vector2.zero;

	protected float moveInputSpeed = 20f;

	protected float[] reactionTimes = new float[3] { 0.1f, 0.2f, 0.3f };

	[SerializeField]
	protected float scanTimer;

	protected float scanDelay = 0.3f;

	[SerializeField]
	protected float actionTimer;

	[SerializeField]
	protected float actionDelay = 0.2f;

	protected float inputTimer;

	protected float inputDelay = 0.15f;

	protected float createPathTimer;

	protected static float createPathDelay = 0.01666f;

	public GameObject desireTarget;

	public Player victimTarget;

	public GameObject threatTarget;

	protected float interestLevel;

	public float ambientInterestLevel;

	public float bestDesireSoFar;

	public float bestVictimSoFar;

	public float bestThreatSoFar;

	public float totalDesireLevel;

	public float totalVictimLevel;

	public float totalThreatLevel;

	public float ambientInterestMod = 1f;

	public float aggressionMod = 1f;

	public float nervousMod = 1f;

	public float greedyMod = 1f;

	protected float attackRadius = 5f;

	protected float throwRadius = 34f;

	protected float throwRadiusAimed = 41f;

	protected float blockRadiusDiscs = 10f;

	protected float blockRadiusPlayers = 2f;

	protected float throwMinDotProduct = 0.85f;

	protected float dashDistance;

	public string printState;

	private Vector2[] plantCollisionPoints = new Vector2[5];

	protected virtual void Awake()
	{
		actor = GetComponent<Actor>();
		fsm = StateMachine<AIState>.Initialize(this, AIState.Dead);
		dashDistance = actor.dashDistance;
	}

	public virtual void InitPathSettings()
	{
		if (seeker == null)
		{
			seeker = base.gameObject.AddComponent<Seeker>();
			smoothModifier = base.gameObject.AddComponent<SimpleSmoothModifier>();
			pathEdgeModifier = base.gameObject.AddComponent<PathEdgeModifier>();
			seeker.tagPenalties[1] = outOfBoundsReluctance;
		}
		smoothModifier.maxSegmentLength = 1f;
		smoothModifier.strength = 0.5f;
		smoothModifier.iterations = 1;
	}

	public virtual void Init()
	{
		StopAllCoroutines();
		DestroyPath();
		moveDir = Vector2.zero;
		threatTarget = null;
		victimTarget = null;
		desireTarget = null;
		targetObject = null;
		CreatePath();
		SetDifficulty();
	}

	protected virtual void SetDifficulty()
	{
	}

	protected virtual void Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && actor.actorState != Actor.ActorState.Dead)
		{
			UpdateBehaviour();
		}
		else if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			path = null;
			fsm.ChangeState(AIState.Dead);
		}
	}

	protected virtual void UpdateBehaviour()
	{
		if (scanTimer > scanDelay && fsm.State != AIState.Attacking)
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
			scanTimer = 0f;
			scanDelay = GetRandomReactionTime();
			if (Singleton<SettingsManager>.Instance.botDifficulty >= SettingsManager.BotDifficulty.hard)
			{
				scanDelay *= 3f;
			}
		}
		scanTimer += Time.deltaTime;
		actionTimer += Time.deltaTime;
		createPathTimer += Time.deltaTime;
		if (inputTimer <= inputDelay)
		{
			inputTimer += Time.deltaTime;
		}
		if (stateChangeReluctanceCooldown > 0f)
		{
			stateChangeReluctanceCooldown -= Time.deltaTime;
		}
		else
		{
			stateChangeReluctance = stateChangeReluctanceDefault;
		}
		if (actor.IsDashing)
		{
			createPathAfterDash = true;
		}
		else if (createPathAfterDash)
		{
			CreatePath();
			createPathAfterDash = false;
		}
		if (actionTimer > actionDelay)
		{
			actionTimer = 0f;
			actionDelay = GetRandomReactionTime();
			TakeAction();
		}
	}

	protected virtual float GetRandomReactionTime()
	{
		return reactionTimes[Random.Range(0, reactionTimes.Length)];
	}

	public void OnEnterMovingFloor(MovingFloor movingFloor)
	{
	}

	public void OnExitMovingFloor()
	{
	}

	protected bool IsObjectStillThere(GameObject target)
	{
		if (target == null)
		{
			return false;
		}
		if (!target.activeSelf)
		{
			return false;
		}
		return true;
	}

	protected virtual void LocateTarget()
	{
		if (IsObjectStillThere(targetObject))
		{
			targetPosition = targetObject.transform.position;
			return;
		}
		stateChangeReluctance = stateChangeReluctanceDefault;
		ScanPointsOfInterest();
		ConsiderChangingState();
	}

	public virtual void CreatePath()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || createPathTimer < createPathDelay)
		{
			return;
		}
		createPathTimer = 0f;
		if (Vector3.Distance(base.transform.position, targetPosition) < nextWaypointDistance)
		{
			if (fsm.State == AIState.FlickingSwitch && IsObjectStillThere(targetObject) && targetObject.GetComponent<Switch>().isActive)
			{
				actor.RotateSnapTo((targetObject.transform.position - base.transform.position).normalized);
				Attack();
			}
			else if (fsm.State == AIState.Collecting)
			{
				fsm.ChangeState(AIState.Diverting);
			}
			reachedEndOfPath = true;
			return;
		}
		Path path = null;
		Vector3 vector = base.transform.position;
		Vector3 inverseTransformPoint = targetPosition;
		seeker.graphMask = Singleton<LevelManager>.Instance.GetBestGraphForTarget(base.transform.position, targetPosition);
		if ((int)seeker.graphMask != 1)
		{
			for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; i++)
			{
				if ((int)Singleton<LevelManager>.Instance.level.allMovingFloors[i].graphMask == (int)seeker.graphMask)
				{
					vector = Singleton<LevelManager>.Instance.level.allMovingFloors[i].GetInverseTransformPoint(vector);
					inverseTransformPoint = Singleton<LevelManager>.Instance.level.allMovingFloors[i].GetInverseTransformPoint(inverseTransformPoint);
					break;
				}
			}
		}
		switch (fsm.State)
		{
		case AIState.Idle:
			return;
		case AIState.Wandering:
		case AIState.Collecting:
		case AIState.Attacking:
		case AIState.Diverting:
		case AIState.FlickingSwitch:
		case AIState.Seeking:
			path = ABPath.Construct(vector, inverseTransformPoint, OnPathComplete);
			break;
		case AIState.Fleeing:
		{
			FleePath fleePath = FleePath.Construct(vector, inverseTransformPoint, fleeDistance, OnPathComplete);
			fleePath.aimStrength = fleeAimStrength;
			fleePath.spread = fleeSpread;
			path = fleePath;
			break;
		}
		}
		if (path != null)
		{
			seeker.StartPath(path);
			recalculatingPath = true;
			reachedEndOfPath = false;
		}
	}

	protected virtual void OnPathComplete(Path p)
	{
		recalculatingPath = false;
		if (!p.error)
		{
			path = p;
			currentWaypoint = 0;
			pathLength = path.GetTotalLength();
		}
		currentGraphMovingFloor = null;
		if (path == null || (int)path.nnConstraint.graphMask == 1)
		{
			return;
		}
		for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; i++)
		{
			if ((int)Singleton<LevelManager>.Instance.level.allMovingFloors[i].graphMask == (int)path.nnConstraint.graphMask)
			{
				currentGraphMovingFloor = Singleton<LevelManager>.Instance.level.allMovingFloors[i];
			}
		}
	}

	private bool IsGraphNodeOnEdge(GridNode node)
	{
		if (node.HasConnectionsToAllEightNeighbours)
		{
			return false;
		}
		return true;
	}

	protected bool IsTargetPathable()
	{
		if (path == null)
		{
			return false;
		}
		if ((GetFinalWaypointPosition() - targetPosition).magnitude > 2f)
		{
			return false;
		}
		return true;
	}

	protected void DestroyPath()
	{
		reachedEndOfPath = true;
		path = null;
	}

	public bool IsPathOnWalkableNodes()
	{
		if (path == null)
		{
			return true;
		}
		if (path.path.Count < 2)
		{
			return true;
		}
		for (int i = 1; i < path.path.Count; i++)
		{
			if (!path.path[i].Walkable)
			{
				return false;
			}
		}
		return true;
	}

	protected virtual void MoveOnPath(PathEndDelegate OnEndReached = null)
	{
		if (actor.IsAiming)
		{
			return;
		}
		if (fsm.State != AIState.Fleeing)
		{
			moveDir = Vector2.Lerp(actor.inputMove, Vector2.zero, moveInputSpeed * 2f * Time.deltaTime);
		}
		else
		{
			moveDir = Vector2.Lerp(actor.inputMove, Vector2.zero, moveInputSpeed * 1f * Time.deltaTime);
		}
		if (path == null)
		{
			if (!recalculatingPath)
			{
				reachedEndOfPath = true;
			}
			return;
		}
		while (currentWaypoint < path.vectorPath.Count && IsPositionCloseToCurrentWaypoint())
		{
			currentWaypoint++;
			if (currentWaypoint == path.vectorPath.Count - 1 && Singleton<LevelManager>.Instance.IsPointOnHazard(GetCurrentWaypointPosition()))
			{
				if (Chance.PercentageTrue(25f) && actor.transform.parent != null)
				{
					fsm.ChangeState(AIState.Idle);
				}
				else
				{
					currentWaypoint = path.vectorPath.Count;
				}
			}
		}
		if (currentWaypoint >= path.vectorPath.Count)
		{
			path = null;
			reachedEndOfPath = true;
			moveDir = Vector2.zero;
			if (OnEndReached != null)
			{
				OnEndReached();
			}
			else if (!actor.IsDashing && actor.IsPositionTouchingHazard(base.transform.position + base.transform.up) && (targetPosition - base.transform.position).magnitude > 5f && Dash(base.transform.up, avoidObstacles: true, allowOutOfBounds: true, allowHazardInFrontOfEndPoint: false))
			{
				reasonForLastDash = "AI.cs:395 - dashed because target is on other side of obstacle";
			}
		}
		else
		{
			PushControllerTowardsNextWaypoint();
			AvoidOtherActors();
			AvoidFire();
		}
	}

	protected bool IsPositionCloseToCurrentWaypoint()
	{
		float num = nextWaypointDistance;
		if (actor.IsOnWetFloor)
		{
			num *= 2f;
		}
		return Vector3.Distance(base.transform.position, GetCurrentWaypointPosition()) < num;
	}

	protected bool IsPositionTooFarFromCurrentWaypoint()
	{
		return Vector3.Distance(base.transform.position, GetCurrentWaypointPosition()) > currentWaypointTooFarDistance;
	}

	protected Vector3 GetCurrentWaypointPosition()
	{
		if (currentGraphMovingFloor != null)
		{
			return currentGraphMovingFloor.GetTransformPoint(path.vectorPath[currentWaypoint]);
		}
		return path.vectorPath[currentWaypoint];
	}

	protected Vector3 GetFinalWaypointPosition()
	{
		Vector3 vector = path.vectorPath[path.vectorPath.Count - 1];
		if (currentGraphMovingFloor != null)
		{
			vector = currentGraphMovingFloor.GetTransformPoint(vector);
		}
		return vector;
	}

	protected virtual void PushControllerTowardsNextWaypoint()
	{
		moveDir = Vector2.Lerp(actor.inputMove, (GetCurrentWaypointPosition() - base.transform.position).normalized, moveInputSpeed * Time.deltaTime);
	}

	protected void AvoidOtherActors()
	{
		if (!actor.IsDashing)
		{
			DivertAroundObjectsOnLayer(actor.playerLayer);
		}
	}

	protected void AvoidFire()
	{
		if (!actor.IsDashing && !actor.IsInvulnerable)
		{
			Vector2 vector = base.transform.TransformPoint(new Vector2(0f, 3f));
			if (Singleton<CellManager>.Instance.IsPositionTouchingCellType(vector, CellManager.CellType.Fire))
			{
				DivertPath(vector);
			}
		}
	}

	protected void DivertAroundObjectsOnLayer(LayerMask layer)
	{
		raycastHit = Physics2D.Raycast(base.transform.position, moveDir.normalized, divertingDistance, layer);
		if ((bool)raycastHit && actor.IsInLineOfSight(raycastHit.point))
		{
			DivertPath(raycastHit.point);
		}
	}

	protected void DivertPath(Vector3 avoidPosition)
	{
		if (fsm.LastState != AIState.Diverting)
		{
			Vector3 vector = Vector3.Cross(base.transform.up, avoidPosition - base.transform.position);
			float num = Random.Range(40f, 70f);
			targetPosition = VectorExt.RotatePointAroundPivot(avoidPosition, base.transform.position, new Vector3(0f, 0f, (vector.z < 0f) ? (0f - num) : num));
			targetPosition += (targetPosition - base.transform.position).normalized * Random.Range(3f, 6f);
			fsm.ChangeState(AIState.Diverting);
		}
		else
		{
			ConsiderChangingState();
		}
	}

	protected virtual void SetRandomTargetPos()
	{
		float num = -1f;
		for (int i = 0; i < 3; i++)
		{
			Vector2 vector = new Vector2(Random.Range((0f - Singleton<LevelManager>.Instance.levelBoundary.size.x) * 0.5f + 2f, Singleton<LevelManager>.Instance.levelBoundary.size.x * 0.5f - 2f), Random.Range((0f - Singleton<LevelManager>.Instance.levelBoundary.size.y) * 0.5f + 2f, Singleton<LevelManager>.Instance.levelBoundary.size.y * 0.5f - 2f));
			vector = VectorExt.RotatePointAroundPivot(vector, Vector3.zero, Singleton<LevelManager>.Instance.level.transform.eulerAngles);
			float num2 = Vector2.Dot((vector - (Vector2)base.transform.position).normalized, base.transform.up);
			if (num2 > num)
			{
				targetPosition = vector;
				num = num2;
			}
		}
	}

	protected virtual void SetRandomTargetPosOnMovingFloor(MovingFloor mf)
	{
		if (!(mf == null))
		{
			targetPosition = new Vector2(Random.Range(mf.collider.bounds.min.x, mf.collider.bounds.max.x), Random.Range(mf.collider.bounds.min.y, mf.collider.bounds.max.y));
		}
	}

	public virtual void ReactToTeleportal()
	{
		bool flag = false;
		if (path != null && currentWaypoint < path.vectorPath.Count && IsPositionTooFarFromCurrentWaypoint())
		{
			for (int i = currentWaypoint; i < path.vectorPath.Count; i++)
			{
				Vector3 vector = path.vectorPath[i];
				if (currentGraphMovingFloor != null)
				{
					vector = currentGraphMovingFloor.GetTransformPoint(vector);
				}
				float num = Vector3.Distance(base.transform.position + base.transform.up, vector);
				if (num < currentWaypointTooFarDistance && num >= nextWaypointDistance)
				{
					currentWaypoint = i;
					flag = true;
				}
			}
		}
		if (!flag)
		{
			CreatePath();
		}
	}

	protected virtual void TakeAction()
	{
	}

	protected virtual void Block(GameObject go)
	{
	}

	protected virtual void Attack()
	{
	}

	protected virtual void Throw(GameObject go)
	{
	}

	protected virtual bool Dash(Vector2 dashDirection, bool avoidObstacles = true, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool trackFailedAttempts = true)
	{
		if (actor.canDash && actor.IsDashSafe(dashDirection, avoidObstacles, allowOutOfBounds, allowHazardInFrontOfEndPoint))
		{
			actor.RotateSnapTo(dashDirection);
			moveDir = dashDirection;
			actor.inputMove = dashDirection;
			path = null;
			return true;
		}
		return false;
	}

	protected virtual void ScanPointsOfInterest()
	{
		bestDesireSoFar = 0f;
		bestVictimSoFar = 0f;
		bestThreatSoFar = 0f;
		interestLevel = 0f;
		totalDesireLevel = 0f;
		totalThreatLevel = 0f;
		totalVictimLevel = 0f;
		for (int i = 0; i < Singleton<GameManager>.Instance.activeExplosives.Count; i++)
		{
			if (Singleton<GameManager>.Instance.activeExplosives[i] != null && (Singleton<GameManager>.Instance.activeExplosives[i].CompareTag("Grenade") || Singleton<GameManager>.Instance.activeExplosives[i].CompareTag("Explosion")))
			{
				ScanAsThreat(Singleton<GameManager>.Instance.activeExplosives[i], ThreatType.Explosive);
			}
		}
		int num = 0;
		int num2 = Singleton<GameManager>.Instance.activeFires.Count / 5;
		if (num2 <= 0)
		{
			return;
		}
		foreach (GameObject activeFire in Singleton<GameManager>.Instance.activeFires)
		{
			if (num % num2 == 0)
			{
				ScanAsThreat(activeFire, ThreatType.Fire);
			}
			num++;
		}
	}

	protected virtual void ScanAsThreat(GameObject go, ThreatType threatType)
	{
		interestLevel = GetInterestOfThreat(go, threatType);
		if (interestLevel > bestThreatSoFar)
		{
			threatTarget = go;
			bestThreatSoFar = interestLevel;
		}
		totalThreatLevel += interestLevel;
	}

	protected virtual void ScanAsVictim(Player otherPlayer)
	{
		interestLevel = GetInterestOfVictim(otherPlayer);
		if (interestLevel > bestVictimSoFar)
		{
			victimTarget = otherPlayer;
			bestVictimSoFar = interestLevel;
		}
		totalVictimLevel += interestLevel;
	}

	protected virtual void ScanAsDesirable(GameObject go, DesirableType desirableType)
	{
		interestLevel = GetInterestOfDesire(go, desirableType);
		if (interestLevel > bestDesireSoFar)
		{
			desireTarget = go;
			bestDesireSoFar = interestLevel;
		}
		totalDesireLevel += interestLevel;
	}

	protected virtual float GetInterestOfThreat(GameObject threat, ThreatType threatType)
	{
		return 0f;
	}

	protected virtual float GetInterestOfVictim(Player otherPlayer)
	{
		return 0f;
	}

	protected virtual float GetInterestOfDesire(GameObject desirable, DesirableType desirableType)
	{
		return 0f;
	}

	protected virtual void ConsiderChangingState()
	{
	}

	protected void SetStateChangeReluctance(float changeReluctance, float duration)
	{
		stateChangeReluctance = changeReluctance;
		stateChangeReluctanceCooldown = duration;
	}

	protected bool CanISeeTarget(Player targetPlayer)
	{
		if (targetPlayer == null)
		{
			return false;
		}
		if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.impossible)
		{
			return true;
		}
		if (targetPlayer.IsDisguised || targetPlayer.IsInTheShadows)
		{
			if (targetPlayer.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding)
			{
				return true;
			}
			if (targetPlayer.velocityStored.magnitude > 6f - (float)Singleton<SettingsManager>.Instance.botDifficulty * 2f || targetPlayer.timeInCurrentDisguise < Player.disguiseStartDuration * (5f + (float)Singleton<SettingsManager>.Instance.botDifficulty) * 5f)
			{
				return true;
			}
			return false;
		}
		return !IsTargetHiddenByPlants(targetPlayer);
	}

	protected bool IsTargetHiddenByPlants(Player targetPlayer)
	{
		if (targetPlayer.IsHidden)
		{
			return IsPositionHiddenByPlants(targetPlayer.transform.position, targetPlayer.physicsColliderRadius);
		}
		return false;
	}

	protected bool IsPositionHiddenByPlants(Vector3 pos, float colliderRadius)
	{
		plantCollisionPoints[0] = base.transform.position;
		plantCollisionPoints[1] = (Vector2)base.transform.position + new Vector2(0f, colliderRadius);
		plantCollisionPoints[2] = (Vector2)base.transform.position + new Vector2(colliderRadius, 0f);
		plantCollisionPoints[3] = (Vector2)base.transform.position + new Vector2(0f, 0f - colliderRadius);
		plantCollisionPoints[4] = (Vector2)base.transform.position + new Vector2(0f - colliderRadius, 0f);
		int num = 0;
		for (int i = 0; i < plantCollisionPoints.Length; i++)
		{
			if ((bool)Physics2D.OverlapPoint(plantCollisionPoints[i], 1 << LayerMask.NameToLayer("Plant")))
			{
				num++;
			}
		}
		if (num == 5)
		{
			return true;
		}
		return false;
	}

	protected GameObject NearbyPlant(Vector3 pos)
	{
		float radius = 10f;
		int num = Physics2D.OverlapCircleNonAlloc(pos, radius, nearbyPlants, 1 << LayerMask.NameToLayer("Plant"));
		if (num == 0)
		{
			return null;
		}
		GameObject gameObject = null;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			Plant component = nearbyPlants[i].GetComponent<Plant>();
			if (!(component.tiltFactor > 0.33f))
			{
				continue;
			}
			if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.hard)
			{
				if (component.tiltFactor >= num2)
				{
					gameObject = nearbyPlants[i].gameObject;
					num2 = component.tiltFactor;
				}
			}
			else if ((component.transform.position - pos).magnitude > num2)
			{
				gameObject = nearbyPlants[i].gameObject;
				num2 = component.tiltFactor;
			}
		}
		if (gameObject == null)
		{
			gameObject = nearbyPlants[Random.Range(0, num)].gameObject;
		}
		return gameObject;
	}
}
