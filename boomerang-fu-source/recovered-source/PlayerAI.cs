using System.Collections;
using System.Collections.Generic;
using MonsterLove.StateMachine;
using Pathfinding;
using UnityEngine;

public class PlayerAI : AI
{
	private enum ButtonType
	{
		dash,
		attack,
		throwdisc
	}

	private PlayerInput playerInput;

	private Player player;

	public float bravery = 1f;

	public float gettingBearingsTimer;

	private float gettingBearingsDuration;

	private float gettingBearingsSpeed;

	private float standingInPlaceTimer;

	private float timeSinceLastTeleportThreshold = 1f;

	private int unsafeDashAttempts;

	private const float minRetrieveDistanceDelta = 3f;

	private const float retrieveDistanceCheckDuration = 0.4f;

	private float closestRetrieveDistanceLastCheck = float.MaxValue;

	private float timeOfLastRetrieveDistanceCheck;

	private Collider2D[] touchingDeadPlayers = new Collider2D[6];

	private List<GameObject> hideAndSeekPotentialTargets = new List<GameObject>();

	private bool dashPressed;

	private bool attackPressed;

	private IEnumerator holdThrowDownRoutine;

	private bool isTeammateAlmostRevived;

	protected override void Awake()
	{
		base.Awake();
		playerInput = GetComponent<PlayerInput>();
		player = actor as Player;
	}

	public override void Init()
	{
		base.Init();
		StartCoroutine(RandomiseInterestMods());
		aggressionMod = 0f;
		fsm.ChangeState(AIState.Wandering);
		unsafeDashAttempts = 0;
		hideAndSeekPotentialTargets.Clear();
	}

	protected override void SetDifficulty()
	{
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			reactionTimes[0] = 0.6f;
			reactionTimes[1] = 0.7f;
			reactionTimes[2] = 0.85f;
			inputDelay = 0.3f;
			gettingBearingsDuration = 8f;
			timeSinceLastTeleportThreshold = 1.5f;
			break;
		case SettingsManager.BotDifficulty.medium:
			reactionTimes[0] = 0.41f;
			reactionTimes[1] = 0.52f;
			reactionTimes[2] = 0.63f;
			inputDelay = 0.25f;
			gettingBearingsDuration = 4f;
			timeSinceLastTeleportThreshold = 0.75f;
			break;
		case SettingsManager.BotDifficulty.hard:
			reactionTimes[0] = 0.1f;
			reactionTimes[1] = 0.25f;
			reactionTimes[2] = 0.35f;
			inputDelay = 0.2f;
			gettingBearingsDuration = 2f;
			timeSinceLastTeleportThreshold = 0.25f;
			break;
		case SettingsManager.BotDifficulty.impossible:
			reactionTimes[0] = 0.07f;
			reactionTimes[1] = 0.13f;
			reactionTimes[2] = 0.2f;
			inputDelay = 0.1f;
			gettingBearingsDuration = 0.1f;
			timeSinceLastTeleportThreshold = 0f;
			break;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.KingOfTheHill)
		{
			gettingBearingsDuration *= 0.66f;
		}
		gettingBearingsSpeed = (Chance.PercentageTrue(50f) ? 1f : 0.05f);
		gettingBearingsTimer = gettingBearingsDuration * Random.Range(0.75f, 1f);
	}

	public void RemoveComponent()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Menu)
		{
			StopAllCoroutines();
		}
		playerInput.inputIsEnabled = false;
		playerInput.inputMove = Vector2.zero;
		playerInput.inputDash = false;
		playerInput.inputAttack = false;
		playerInput.inputThrow = false;
		playerInput.inputThrowHeld = false;
		playerInput.inputThrowIsBlocked = false;
		player.StopAiming();
		DestroyPath();
		seeker.CancelCurrentPathRequest();
		Object.Destroy(GetComponent<StateMachineRunner>());
		Object.Destroy(this);
	}

	protected override void UpdateBehaviour()
	{
		if (fsm.State == AIState.Dead)
		{
			return;
		}
		if (player.activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			dashDistance = actor.dashDistance * 1.037037f;
		}
		else
		{
			dashDistance = actor.dashDistance;
		}
		if (player.HasDiscs > 0)
		{
			divertingDistance = 3f;
		}
		else
		{
			divertingDistance = 5f;
		}
		if (playerInput.inputThrowHeld && player.retrieveCharge > 0.2f && AreRetrievingDiscsStuck())
		{
			StopHoldingThrow();
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
		if (player.activePowerup.HasPowerup(PowerupType.MoveFaster))
		{
			moveInputSpeed = 40f;
		}
		else
		{
			moveInputSpeed = 20f;
		}
		if (Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
		{
			ambientInterestLevel = 0.7f * ambientInterestMod;
		}
		else
		{
			ambientInterestLevel = 2.75f * ambientInterestMod;
			nervousMod = 0.5f;
		}
		if (player.IsAiming && victimTarget != null && CanISeeTarget(victimTarget))
		{
			moveDir = Vector2.Lerp(playerInput.inputMove, (victimTarget.transform.position - base.transform.position).normalized, moveInputSpeed * Time.deltaTime);
			if (Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.easy && threatTarget != null && (threatTarget.transform.position - base.transform.position).magnitude < attackRadius * 1.1f && player.IsInLineOfSight(threatTarget.transform.position) && CanISeeTarget(threatTarget.GetComponent<Player>()) && actionTimer > actionDelay * 0.5f)
			{
				StopHoldingThrow();
			}
		}
		if (gettingBearingsTimer > 0f)
		{
			gettingBearingsTimer -= Time.deltaTime;
			if (gettingBearingsTimer > gettingBearingsDuration * 0.85f)
			{
				moveDir *= gettingBearingsSpeed;
			}
		}
		base.UpdateBehaviour();
		playerInput.inputMove.x = moveDir.x;
		playerInput.inputMove.y = moveDir.y;
		float num = 90f;
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			num = 60f;
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				num = 0f;
			}
			break;
		case SettingsManager.BotDifficulty.medium:
			num = 70f;
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				num = 0f;
			}
			break;
		case SettingsManager.BotDifficulty.hard:
			num = 80f;
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				num = 20f;
			}
			break;
		}
		if (gettingBearingsTimer > 0f)
		{
			num *= 0.5f;
		}
		if ((player.HasDiscs <= 0 && Singleton<SettingsManager>.Instance.botDifficulty != SettingsManager.BotDifficulty.impossible) || !Chance.PercentageTrue(num) || !(actionTimer > actionDelay * 0.5f))
		{
			return;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!(Singleton<GameManager>.Instance.players[i] == player) && (Singleton<GameManager>.Instance.players[i].transform.position - base.transform.position).magnitude < player.physicsColliderRadius * 2.2f)
			{
				if (BestObjectToAttack() != null)
				{
					Attack();
					actionTimer = 0f;
				}
				break;
			}
		}
	}

	protected override float GetRandomReactionTime()
	{
		return reactionTimes[Random.Range(0, reactionTimes.Length)] * (1f + Mathf.Clamp01(gettingBearingsTimer / gettingBearingsDuration));
	}

	private void Wandering_Enter()
	{
		Debug.Log("Enter state wandering");
		targetObject = null;
		SetRandomTargetPos();
		CreatePath();
		SetStateChangeReluctance(1.25f, 1.5f);
	}

	private void Wandering_Update()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<GameManager>.Instance.IsInSeekingPhase)
		{
			ConsiderChangingState();
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (reachedEndOfPath)
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
				SetRandomTargetPos();
				CreatePath();
			}
			MoveOnPath(DashAcrossHazards);
		}
	}

	private void Diverting_Enter()
	{
		Debug.Log("Enter state diverting");
		CreatePath();
		SetStateChangeReluctance(2f, 1f);
	}

	private void Diverting_Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			MoveOnPath();
			if (reachedEndOfPath)
			{
				fsm.ChangeState(fsm.LastState);
			}
		}
	}

	private void Attacking_Enter()
	{
		Debug.Log("Enter state attacking");
		if (player.retrieveCharge > 0f)
		{
			StopHoldingThrow();
		}
		LocateTarget();
		CreatePath();
		seeker.tagPenalties[2] = 100;
		SetStateChangeReluctance(1.5f, 2.5f);
	}

	private void Attacking_Update()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		if (reachedEndOfPath || scanTimer > scanDelay)
		{
			scanTimer = 0f;
			if (victimTarget != null && victimTarget.actorState == Actor.ActorState.Alive)
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
				if (Singleton<SettingsManager>.Instance.botDifficulty != SettingsManager.BotDifficulty.impossible && IsTargetHiddenByPlants(victimTarget) && Chance.PercentageTrue(95f))
				{
					if ((IsObjectStillThere(targetObject) && !targetObject.CompareTag("Plant")) || reachedEndOfPath)
					{
						targetObject = NearbyPlant(victimTarget.transform.position);
						LocateTarget();
						CreatePath();
					}
					if (Chance.PercentageTrue(70f) && player.IsHidden && player.IsLungeSafe(moveDir) && player.HasDiscs > 0)
					{
						Attack();
					}
				}
				else
				{
					targetObject = victimTarget.gameObject;
					LocateTarget();
					CreatePath();
				}
			}
			else
			{
				reachedEndOfPath = true;
				victimTarget = null;
				targetObject = null;
				fsm.ChangeState(AIState.Wandering);
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
		}
		MoveOnPath(TryToAttack);
	}

	private void Attacking_Exit()
	{
		if (playerInput.inputThrowHeld && (player.IsAiming || player.retrieveCharge > 0f))
		{
			StopHoldingThrow();
		}
	}

	private void Collecting_Enter()
	{
		Debug.Log("Enter state collecting");
		if (IsObjectStillThere(targetObject))
		{
			LocateTarget();
			CreatePath();
		}
		else
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
	}

	private void Collecting_Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (IsRevivingTeammate() && (isTeammateAlmostRevived || IsPositionSafeToStandStill(base.transform.position, 5f)))
			{
				moveDir = Vector2.zero;
			}
			else if (reachedEndOfPath)
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
				LocateTarget();
				CreatePath();
			}
			else
			{
				MoveOnPath(DashTowardsDroppedDisc);
			}
		}
	}

	private bool IsRevivingTeammate()
	{
		isTeammateAlmostRevived = false;
		if (!Singleton<SettingsManager>.Instance.teamMatch || !Singleton<SettingsManager>.Instance.MatchSettings.teamRevives)
		{
			return false;
		}
		int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, player.physicsColliderRadius, touchingDeadPlayers, 1 << LayerMask.NameToLayer("PlayerDeath"));
		for (int i = 0; i < num; i++)
		{
			if (touchingDeadPlayers[i].transform.root == null || touchingDeadPlayers[i].transform.root == base.transform)
			{
				continue;
			}
			Player component = touchingDeadPlayers[i].transform.root.GetComponent<Player>();
			if (component != null && component.team == player.team && component.actorState == Actor.ActorState.Dead)
			{
				if (component.reviveTimer > Actor.reviveDuration - 1f)
				{
					isTeammateAlmostRevived = true;
				}
				return true;
			}
		}
		return false;
	}

	private bool IsPositionSafeToStandStill(Vector3 position, float detectEnemyRadius)
	{
		if (!Singleton<LevelManager>.Instance.IsInBounds(position))
		{
			return false;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!(Singleton<GameManager>.Instance.players[i] == player) && Singleton<GameManager>.Instance.players[i].team != player.team && Singleton<GameManager>.Instance.players[i].actorState == Actor.ActorState.Alive && !Singleton<GameManager>.Instance.players[i].IsDisarmed && !Singleton<GameManager>.Instance.players[i].IsFrozen && (Singleton<GameManager>.Instance.players[i].transform.position - position).magnitude < detectEnemyRadius && Singleton<GameManager>.Instance.players[i].IsInLineOfSight(position))
			{
				Debug.Log("An enemy is standing too close to my fallen comrade");
				return false;
			}
		}
		if (Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.easy)
		{
			foreach (Disc activeDisc in Singleton<GameManager>.Instance.activeDiscs)
			{
				if (!(activeDisc.owner == null) && !(activeDisc.owner == player) && activeDisc.owner.team != player.team && activeDisc.movement != Projectile.Movement.Dropped && (activeDisc.discPowerup.HasPowerup(PowerupType.ExplosiveDisc) || activeDisc.discPowerup.HasPowerup(PowerupType.MultiDisc)) && (activeDisc.transform.position - position).magnitude < 17f && !Physics2D.Raycast(position, (activeDisc.transform.position - position).normalized, (activeDisc.transform.position - position).magnitude, 1 << LayerMask.NameToLayer("Obstacle")))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void Fleeing_Enter()
	{
		Debug.Log("Enter state fleeing");
		StopHoldingThrow();
		path = null;
		LocateTarget();
		CreatePath();
		SetStateChangeReluctance(1.5f, 1f);
	}

	private void Fleeing_Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (reachedEndOfPath)
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
				LocateTarget();
				CreatePath();
			}
			MoveOnPath(DashAcrossHazards);
		}
	}

	private void Retrieving_Enter()
	{
		float probability = 100f;
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			probability = 60f;
			break;
		case SettingsManager.BotDifficulty.medium:
			probability = 80f;
			break;
		}
		if (!Chance.PercentageTrue(probability) || player.isOutOfBounds)
		{
			targetObject = null;
			LocateTarget();
			return;
		}
		if (player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.IsItSafeToTeleport())
		{
			player.Teleport();
			CreatePath();
			return;
		}
		if (player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc))
		{
			targetObject = null;
			LocateTarget();
			return;
		}
		SetRandomTargetPos();
		CreatePath();
		if (holdThrowDownRoutine != null)
		{
			StopCoroutine(holdThrowDownRoutine);
		}
		holdThrowDownRoutine = HoldButtonDown(ButtonType.throwdisc, 5f);
		StartCoroutine(holdThrowDownRoutine);
		closestRetrieveDistanceLastCheck = float.MaxValue;
		timeOfLastRetrieveDistanceCheck = Singleton<TimeKeeper>.Instance.CurrentTime;
	}

	private void Retrieving_Update()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		MoveOnPath();
		if (reachedEndOfPath)
		{
			SetRandomTargetPos();
			CreatePath();
			if (!playerInput.inputThrowHeld && player.thrownDiscs.Count > 0)
			{
				if (holdThrowDownRoutine != null)
				{
					StopCoroutine(holdThrowDownRoutine);
				}
				holdThrowDownRoutine = HoldButtonDown(ButtonType.throwdisc, Random.Range(3f, 4f));
				StartCoroutine(holdThrowDownRoutine);
				return;
			}
		}
		if (player.thrownDiscs.Count == 0)
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
	}

	private bool AreRetrievingDiscsStuck()
	{
		if (player.thrownDiscs.Count == 0)
		{
			return false;
		}
		if (Singleton<TimeKeeper>.Instance.TimeSince(timeOfLastRetrieveDistanceCheck) < 0.4f)
		{
			return false;
		}
		timeOfLastRetrieveDistanceCheck = Singleton<TimeKeeper>.Instance.CurrentTime;
		float num = float.MaxValue;
		for (int i = 0; i < player.thrownDiscs.Count; i++)
		{
			float magnitude = (player.thrownDiscs[i].transform.position - base.transform.position).magnitude;
			if (magnitude < num)
			{
				num = magnitude;
			}
		}
		if (closestRetrieveDistanceLastCheck - num < 3f)
		{
			closestRetrieveDistanceLastCheck = num;
			return true;
		}
		closestRetrieveDistanceLastCheck = num;
		return false;
	}

	private void FlickingSwitch_Enter()
	{
		Debug.Log("Enter state flicking switch");
		if (IsObjectStillThere(targetObject))
		{
			targetPosition = targetObject.transform.position - targetObject.transform.up * Random.Range(1.25f, 2.5f);
			CreatePath();
		}
		else
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
		TryToHitSwitch();
	}

	private void FlickingSwitch_Update()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		if (reachedEndOfPath)
		{
			if (!DashAcrossHazards())
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
			if (IsObjectStillThere(targetObject))
			{
				targetPosition = targetObject.transform.position - targetObject.transform.up * 0.5f;
				Debug.DrawLine(base.transform.position, targetPosition, Color.blue, 1f);
			}
			else
			{
				LocateTarget();
			}
			CreatePath();
		}
		else
		{
			MoveOnPath(TryToHitSwitch);
		}
	}

	private void Seeking_Enter()
	{
		if (IsObjectStillThere(targetObject))
		{
			targetPosition = targetObject.transform.position - (targetObject.transform.position - base.transform.position).normalized * Random.Range(1f, 2f);
			CreatePath();
		}
		else
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
		Debug.Log("Enter state seeking");
	}

	private void Seeking_Update()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		if (reachedEndOfPath)
		{
			if (!DashAcrossHazards())
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
			if (IsObjectStillThere(targetObject))
			{
				targetPosition = targetObject.transform.position - (targetObject.transform.position - base.transform.position).normalized * Random.Range(1f, 2f);
				Debug.DrawLine(base.transform.position, targetPosition, Color.blue, 1f);
			}
			else
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
			CreatePath();
		}
		else
		{
			MoveOnPath(TryToAttackHider);
		}
	}

	private void Idle_Enter()
	{
		Debug.Log("Enter state idle");
		moveDir = Vector2.zero;
		reachedEndOfPath = true;
		targetObject = null;
		standingInPlaceTimer = 1f;
		SetStateChangeReluctance(1.5f, 1f);
	}

	private void Idle_Update()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
		{
			return;
		}
		if (reachedEndOfPath)
		{
			if (standingInPlaceTimer <= 0f)
			{
				if (standingInPlaceTimer > -1f)
				{
					standingInPlaceTimer -= Time.deltaTime;
				}
				else if (!actor.IsDashing)
				{
					standingInPlaceTimer = 0f;
				}
				moveDir = Vector2.zero;
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
			else
			{
				if (Vector2.Dot(base.transform.up, (targetPosition - base.transform.position).normalized) < 0.9f)
				{
					moveDir = (targetPosition - base.transform.position).normalized * 0.1f;
				}
				else
				{
					moveDir = Vector2.Lerp(actor.inputMove, Vector2.zero, moveInputSpeed * 3f * Time.deltaTime);
				}
				standingInPlaceTimer -= Time.deltaTime;
			}
		}
		else
		{
			MoveOnPath(DashAcrossHazards);
		}
	}

	private void Dead_Enter()
	{
		Debug.Log("Enter state dead");
		moveDir = Vector2.zero;
		reachedEndOfPath = true;
		targetObject = null;
		if (playerInput != null)
		{
			playerInput.inputMove.x = moveDir.x;
			playerInput.inputMove.y = moveDir.y;
		}
	}

	protected override void ScanPointsOfInterest()
	{
		base.ScanPointsOfInterest();
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (!(player == actor))
			{
				bool flag = Singleton<GameManager>.Instance.AreOpponents(player, actor);
				if ((player.actorState == Actor.ActorState.Alive) & flag)
				{
					ScanAsVictim(player);
					ScanAsThreat(player.gameObject, ThreatType.Player);
				}
				else if (Singleton<SettingsManager>.Instance.teamMatch && Singleton<SettingsManager>.Instance.MatchSettings.teamRevives && player.actorState == Actor.ActorState.Dead && !flag && Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.easy)
				{
					ScanAsDesirable(player.gameObject, DesirableType.DeadTeammate);
				}
			}
		}
		foreach (Disc activeDisc in Singleton<GameManager>.Instance.activeDiscs)
		{
			if (activeDisc.movement == Projectile.Movement.Dropped)
			{
				ScanAsDesirable(activeDisc.gameObject, DesirableType.DroppedDisc);
			}
			else if (activeDisc.discPowerup.HasPowerup(PowerupType.ExplosiveDisc) || activeDisc.discPowerup.HasPowerup(PowerupType.MultiDisc))
			{
				ScanAsThreat(activeDisc.gameObject, ThreatType.Explosive);
			}
		}
		foreach (GameObject spawnedPowerup in Singleton<GameManager>.Instance.spawnedPowerups)
		{
			ScanAsDesirable(spawnedPowerup, DesirableType.Powerup);
		}
	}

	protected override float GetInterestOfDesire(GameObject desirable, DesirableType desirableType)
	{
		float num = Mathf.Min(Mathf.Max(1.25f - Vector2.Distance(base.transform.position, desirable.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude, 0f), 1f);
		switch (desirableType)
		{
		case DesirableType.DeadTeammate:
		{
			if (Singleton<LevelManager>.Instance.IsPositionOverHazard(desirable.transform.position, player.physicsColliderRadius))
			{
				return 0f;
			}
			if (!Singleton<LevelManager>.Instance.IsInBounds(desirable.transform.position))
			{
				return 0f;
			}
			float num4 = 1f;
			if (player.IsDisarmed)
			{
				num4 = 0.1f;
			}
			float num5 = 1f;
			if (!IsPositionSafeToStandStill(desirable.transform.position, 17f))
			{
				num5 = 0.2f;
			}
			return num * num5 * num4 * 4f;
		}
		case DesirableType.Powerup:
		{
			Powerup component2 = desirable.GetComponent<Powerup>();
			if (component2.powerupState == Powerup.PowerupState.Unlocked && component2.power == PowerupType.ReverseInputs)
			{
				return 0f;
			}
			return num * 1.5f * greedyMod;
		}
		case DesirableType.DroppedDisc:
		{
			Disc component = desirable.GetComponent<Disc>();
			if (component.IsGoldenDisc)
			{
				return 100f;
			}
			if (component.discPowerup.HasPowerup(PowerupType.ExplosiveDisc) || component.isBurning || (component.DiscOwner == null && player.IsPositionOverHazard(component.transform.position)))
			{
				return 0f;
			}
			int num2 = 0;
			foreach (Disc thrownDisc in player.thrownDiscs)
			{
				if (thrownDisc.movement == Projectile.Movement.Throwing || thrownDisc.movement == Projectile.Movement.Returning)
				{
					num2++;
				}
			}
			if (player.HasDiscs + num2 > 1)
			{
				return 0f;
			}
			float num3 = 1f;
			if (component.DiscOwner != null && component.DiscOwner == player)
			{
				num3 = 4f;
			}
			else
			{
				if (component.DiscOwner == null || Singleton<SettingsManager>.Instance.MatchSettings.canStealOpponentDiscs)
				{
					if (player.IsDisarmed)
					{
						num3 = 4f;
					}
					else if (Singleton<LevelManager>.Instance.IsInBounds(desirable.transform.position))
					{
						num3 = ((player.HasGoldenDisc != Player.HoldingGoldenDisc.IsHolding && player.HasGoldenDisc != Player.HoldingGoldenDisc.IsThrown) ? 3f : 0.5f);
					}
					else
					{
						num3 = 0.5f;
						if (Mathf.Abs(desirable.transform.position.x) > 40f || Mathf.Abs(desirable.transform.position.y) > 30f)
						{
							return 0f;
						}
					}
				}
				else if (component.DiscOwner != null && component.DiscOwner.actorState == Actor.ActorState.Dead)
				{
					return 0f;
				}
				if (component.DiscOwner != null && !Singleton<GameManager>.Instance.AreOpponents(player, component))
				{
					return 0f;
				}
			}
			return num * greedyMod * num3;
		}
		default:
			return 0f;
		}
	}

	protected override float GetInterestOfVictim(Player otherPlayer)
	{
		if ((bool)otherPlayer)
		{
			if (player.IsDisarmed)
			{
				return 0f;
			}
			float num = 1f;
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
			{
				if (otherPlayer.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding || otherPlayer.HasGoldenDisc == Player.HoldingGoldenDisc.IsThrown)
				{
					return 10f;
				}
				if (player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding && Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills)
				{
					num = 2f;
				}
				else if (player.HasGoldenDisc == Player.HoldingGoldenDisc.No)
				{
					num = 0.5f;
				}
			}
			float num2 = (otherPlayer.IsDisarmed ? 1.2f : ((otherPlayer.HasDiscs < player.HasDiscs) ? 1f : ((otherPlayer.HasDiscs != player.HasDiscs) ? 0.8f : 1f)));
			float num3 = Mathf.Min(Vector2.Dot((otherPlayer.transform.position - base.transform.position).normalized, base.transform.up) * 0.2f + 1f, 1f);
			float num4 = (actor.IsInLineOfSight(otherPlayer.transform.position) ? 1.1f : 1f);
			float num5 = (CanISeeTarget(otherPlayer) ? 1f : 0.25f);
			float num6 = Mathf.Min(Mathf.Max(1.5f - Vector2.Distance(base.transform.position, otherPlayer.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude, 0f), 2f);
			float num7 = (otherPlayer.IsFrozen ? 1.3f : 1f);
			float num8 = 1f;
			if (Singleton<GameManager>.Instance.IsLeadingPlayer(otherPlayer))
			{
				num8 = 2.5f - Mathf.Min((float)Singleton<GameManager>.Instance.PointsLeftToWin(otherPlayer) * 0.2f, 1.25f);
			}
			return num3 * num4 * num2 * num6 * aggressionMod * num8 * num5 * num * num7;
		}
		return 0f;
	}

	protected override float GetInterestOfThreat(GameObject threat, ThreatType threatType)
	{
		switch (threatType)
		{
		case ThreatType.Player:
		{
			Player component = threat.GetComponent<Player>();
			float num2 = 1f;
			if (component.IsDisarmed || component.IsFrozen)
			{
				num2 = 0f;
			}
			else if (player.IsDisarmed)
			{
				num2 = 5.25f;
			}
			else if (player.HasDiscs == 0)
			{
				num2 = 2f;
			}
			else if (component.HasDiscs < player.HasDiscs)
			{
				num2 = 0.8f;
			}
			else if (component.HasDiscs == player.HasDiscs)
			{
				num2 = 1f;
			}
			float num3 = 1f;
			if (component.isOnFire && !player.isOnFire)
			{
				num3 = 5f;
			}
			float num4 = (0f - Vector2.Dot((threat.transform.position - base.transform.position).normalized, base.transform.up)) * 0.05f + 0.45f;
			float num5 = (CanISeeTarget(component) ? 1f : 0.5f);
			float num6 = (player.IsInLineOfSight(threat.transform.position) ? 1f : 0.6f);
			float num7 = ((component.IsAiming && Vector2.Dot(threat.transform.up, (base.transform.position - threat.transform.position).normalized) > 0.25f) ? 2f : 1f);
			float num8 = Mathf.Max(1f - Vector2.Distance(base.transform.position, threat.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude * 2f, 0f);
			return num4 * num6 * num2 * num8 * nervousMod * num7 * num5 * num3;
		}
		case ThreatType.Explosive:
			if (player.IsInLineOfSight(threat.transform.position) && Vector2.Distance(base.transform.position, threat.transform.position) < 17f)
			{
				return 6f * nervousMod;
			}
			return 1f * nervousMod;
		case ThreatType.Fire:
		{
			float num = Mathf.Max(1f - Vector2.Distance(base.transform.position, threat.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude * 2f, 0f);
			if (num < 0.5f)
			{
				return 0f;
			}
			return ((0f - Vector2.Dot((threat.transform.position - base.transform.position).normalized, base.transform.up)) * 0.5f + 0.5f) * num * nervousMod;
		}
		default:
			return 0f;
		}
	}

	protected override void ConsiderChangingState()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			if (player.IsSeeker)
			{
				if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || Singleton<GameManager>.Instance.IsInHidingPhase)
				{
					return;
				}
				if (fsm.State == AIState.Collecting && player.HasDiscs > 0)
				{
					targetObject = null;
				}
				if (player.HasDiscs == 0 && player.thrownDiscs.Count > 0)
				{
					desireTarget = player.thrownDiscs[0].gameObject;
					targetObject = desireTarget;
					if (fsm.LastState != AIState.Retrieving && !player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc) && !player.isOutOfBounds && player.IsDisarmed && player.thrownDiscs.Count > 0 && targetObject.CompareTag("Disc") && player.IsInLineOfSight(targetObject.transform.position) && (((targetObject.transform.position - base.transform.position).magnitude > dashDistance * 2f && (Chance.PercentageTrue((Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy) ? 10f : 33f) || !Singleton<LevelManager>.Instance.IsInBounds(targetObject.transform.position))) || !player.IsDashSafe(targetObject.transform.position - base.transform.position, andNoObstaclesInTheWay: true)) && bestThreatSoFar < bestDesireSoFar * 0.5f)
					{
						fsm.ChangeState(AIState.Retrieving);
					}
					else
					{
						fsm.ChangeState(AIState.Collecting);
					}
					return;
				}
				foreach (Player player in Singleton<GameManager>.Instance.players)
				{
					if (!(player == this.player) && player.actorState == Actor.ActorState.Alive && player.velocity.magnitude > 0f)
					{
						victimTarget = player;
						targetObject = victimTarget.gameObject;
						fsm.ChangeState(AIState.Attacking);
						return;
					}
				}
				if (targetObject == null)
				{
					ChooseNewHideAndSeekTarget();
				}
				if (targetObject != null)
				{
					fsm.ChangeState(AIState.Seeking);
				}
			}
			else if (Singleton<GameManager>.Instance.IsInSeekingPhase)
			{
				fsm.ChangeState(AIState.Dead);
			}
			else if (Singleton<GameManager>.Instance.IsInHidingPhase)
			{
				fsm.ChangeState(AIState.Wandering);
			}
			return;
		}
		if (Singleton<LevelManager>.Instance.level.isTutorialLevel && !this.player.hasCompletedTutorial && Chance.PercentageTrue(66f))
		{
			targetObject = null;
			float num = float.MaxValue;
			for (int i = 0; i < Singleton<LevelManager>.Instance.level.allSwitches.Length; i++)
			{
				if ((base.transform.position - Singleton<LevelManager>.Instance.level.allSwitches[i].transform.position).magnitude < num && this.player.IsInLineOfSight(Singleton<LevelManager>.Instance.level.allSwitches[i].transform.position))
				{
					targetObject = Singleton<LevelManager>.Instance.level.allSwitches[i].gameObject;
					num = (base.transform.position - Singleton<LevelManager>.Instance.level.allSwitches[i].transform.position).magnitude;
				}
			}
			if (targetObject != null)
			{
				fsm.ChangeState(AIState.FlickingSwitch);
				return;
			}
		}
		if (gettingBearingsTimer > 0.1f && Singleton<SettingsManager>.Instance.botDifficulty != SettingsManager.BotDifficulty.impossible)
		{
			fsm.ChangeState(AIState.Wandering);
		}
		else if (bestDesireSoFar > bestVictimSoFar && bestDesireSoFar > bestThreatSoFar && bestDesireSoFar > ambientInterestLevel && bestDesireSoFar > stateChangeReluctance && Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 1 && desireTarget != null && Vector2.Distance(desireTarget.transform.position, base.transform.position) >= nextWaypointDistance)
		{
			targetObject = desireTarget;
			if (fsm.LastState != AIState.Retrieving && !this.player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc) && !this.player.isOutOfBounds && this.player.IsDisarmed && this.player.thrownDiscs.Count > 0 && targetObject.CompareTag("Disc") && this.player.IsInLineOfSight(targetObject.transform.position) && (((targetObject.transform.position - base.transform.position).magnitude > dashDistance * 2f && (Chance.PercentageTrue((Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy) ? 10f : 33f) || !Singleton<LevelManager>.Instance.IsInBounds(targetObject.transform.position))) || !this.player.IsDashSafe(targetObject.transform.position - base.transform.position, andNoObstaclesInTheWay: true)) && bestThreatSoFar < bestDesireSoFar * 0.5f)
			{
				fsm.ChangeState(AIState.Retrieving);
			}
			else
			{
				fsm.ChangeState(AIState.Collecting);
			}
		}
		else if (bestVictimSoFar > bestDesireSoFar && bestVictimSoFar > bestThreatSoFar && bestVictimSoFar > ambientInterestLevel && bestVictimSoFar > stateChangeReluctance)
		{
			targetObject = victimTarget.gameObject;
			if (IsTargetHiddenByPlants(victimTarget))
			{
				targetObject = NearbyPlant(victimTarget.transform.position);
			}
			fsm.ChangeState(AIState.Attacking);
		}
		else if (bestThreatSoFar > bestDesireSoFar && bestThreatSoFar > bestVictimSoFar && bestThreatSoFar > ambientInterestLevel && bestThreatSoFar > stateChangeReluctance)
		{
			if (fsm.State != AIState.Fleeing || Chance.PercentageTrue(33f))
			{
				targetObject = threatTarget;
			}
			fsm.ChangeState(AIState.Fleeing);
		}
		else if (ambientInterestLevel > stateChangeReluctance)
		{
			fsm.ChangeState(AIState.Wandering);
		}
	}

	private void ChooseNewHideAndSeekTarget()
	{
		if (hideAndSeekPotentialTargets.Count == 0)
		{
			foreach (InteractiveObstacle allInteractiveObstacle in Singleton<LevelManager>.Instance.level.allInteractiveObstacles)
			{
				if (allInteractiveObstacle != null && allInteractiveObstacle.gameObject.activeSelf && allInteractiveObstacle.disguiseType != Player.DisguiseType.None && Singleton<LevelManager>.Instance.IsInBounds(allInteractiveObstacle.gameObject.transform.position) && !Singleton<LevelManager>.Instance.IsPositionOverHazard(allInteractiveObstacle.transform.position, 0.5f))
				{
					hideAndSeekPotentialTargets.Add(allInteractiveObstacle.gameObject);
				}
			}
			foreach (Player player in Singleton<GameManager>.Instance.players)
			{
				if (!(player == this.player) && player.actorState == Actor.ActorState.Alive)
				{
					hideAndSeekPotentialTargets.Add(player.gameObject);
				}
			}
		}
		if (Chance.PercentageTrue((Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy) ? 50f : 33f))
		{
			ChooseHideAndSeekTargetAtRandom();
		}
		else
		{
			ChooseHideAndSeekTargetNearby(throwRadius);
		}
	}

	private void ChooseHideAndSeekTargetAtRandom()
	{
		if (hideAndSeekPotentialTargets.Count <= 0)
		{
			return;
		}
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (!(player == this.player) && player.actorState == Actor.ActorState.Alive && CanISeeTarget(player))
			{
				targetObject = player.gameObject;
				return;
			}
		}
		bool flag = AvoidPlayersWhenChoosingHideAndSeekTargets();
		int num = Random.Range(0, hideAndSeekPotentialTargets.Count);
		targetObject = hideAndSeekPotentialTargets[num];
		for (int i = 0; i < hideAndSeekPotentialTargets.Count; i++)
		{
			int index = (i + num) % hideAndSeekPotentialTargets.Count;
			if (!(hideAndSeekPotentialTargets[index] == null) && (!hideAndSeekPotentialTargets[index].CompareTag("Player") || (!flag && targetObject.GetComponent<Player>().actorState == Actor.ActorState.Alive)))
			{
				targetObject = hideAndSeekPotentialTargets[index];
				break;
			}
		}
	}

	private void ChooseHideAndSeekTargetNearby(float radius)
	{
		if (hideAndSeekPotentialTargets.Count <= 0)
		{
			return;
		}
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (!(player == this.player) && player.actorState == Actor.ActorState.Alive && CanISeeTarget(player))
			{
				targetObject = player.gameObject;
				return;
			}
		}
		bool flag = AvoidPlayersWhenChoosingHideAndSeekTargets();
		int num = Random.Range(0, hideAndSeekPotentialTargets.Count);
		targetObject = hideAndSeekPotentialTargets[num];
		for (int i = 0; i < hideAndSeekPotentialTargets.Count; i++)
		{
			int index = (i + num) % hideAndSeekPotentialTargets.Count;
			if (hideAndSeekPotentialTargets[index] == null)
			{
				continue;
			}
			if (hideAndSeekPotentialTargets[index].CompareTag("Player"))
			{
				if (flag)
				{
					continue;
				}
				Player component = targetObject.GetComponent<Player>();
				if (component != null && component.actorState != Actor.ActorState.Alive)
				{
					continue;
				}
			}
			if (!((targetObject.transform.position - base.transform.position).magnitude > radius))
			{
				targetObject = hideAndSeekPotentialTargets[index];
				break;
			}
		}
	}

	private bool AvoidPlayersWhenChoosingHideAndSeekTargets()
	{
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			return true;
		case SettingsManager.BotDifficulty.medium:
			if (player.seekAttemptsRemaining > Singleton<GameManager>.Instance.players.Count - 1)
			{
				return true;
			}
			break;
		case SettingsManager.BotDifficulty.hard:
			if ((float)player.seekAttemptsRemaining > (float)Singleton<GameManager>.Instance.GetNumSeekAttempts() * 0.8f)
			{
				return true;
			}
			break;
		}
		return false;
	}

	protected override void TakeAction()
	{
		if (Singleton<SettingsManager>.Instance.botDifficulty < SettingsManager.BotDifficulty.medium && Chance.PercentageTrue(25f))
		{
			return;
		}
		if (actor.IsFrozen && Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.easy && Chance.PercentageTrue(66f))
		{
			PushButtonDown(ButtonType.attack);
		}
		if (fsm.State == AIState.FlickingSwitch && TryToHitSwitch())
		{
			return;
		}
		if (fsm.State == AIState.Seeking && (targetObject.transform.position - base.transform.position).magnitude <= attackRadius && Vector2.Dot((targetObject.transform.position - base.transform.position).normalized, base.transform.up) > 0.2f && Chance.PercentageTrue(40f))
		{
			TryToAttackHider();
			return;
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && (fsm.State == AIState.Attacking || fsm.State == AIState.Seeking))
		{
			foreach (Player player in Singleton<GameManager>.Instance.players)
			{
				if (!(player == this.player) && player.actorState == Actor.ActorState.Alive && (player.transform.position - base.transform.position).magnitude <= attackRadius * 0.5f && Vector2.Dot((player.transform.position - base.transform.position).normalized, base.transform.up) > 0.2f)
				{
					player.RotateSnapTo((player.transform.position - base.transform.position).normalized);
					Attack();
					return;
				}
			}
		}
		if (fsm.State == AIState.Seeking)
		{
			return;
		}
		GameObject gameObject = BestObjectToBlock();
		if (gameObject != null && ((Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy && Chance.PercentageTrue(20f)) || (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.medium && Chance.PercentageTrue(50f)) || (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.hard && Chance.PercentageTrue(75f)) || (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.impossible && Chance.PercentageTrue(90f))))
		{
			Block(gameObject);
			return;
		}
		GameObject obj = BestObjectToAttack();
		float probability = 90f;
		if (this.player.HasDiscs == 0 && Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.medium)
		{
			probability = 45f;
		}
		else if (this.player.HasDiscs == 0 && Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy)
		{
			probability = 0f;
		}
		if (obj != null && gettingBearingsTimer <= 0f && (fsm.State == AIState.Attacking || this.player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding || Chance.PercentageTrue(probability)))
		{
			Attack();
			return;
		}
		GameObject gameObject2 = BestObjectToThrowAt();
		if (gameObject2 != null && gettingBearingsTimer <= 0f && (fsm.State == AIState.Attacking || Chance.PercentageTrue(75f)))
		{
			if (this.player.HasDiscs > 1)
			{
				actionDelay *= 2f;
			}
			Throw(gameObject2);
		}
		else
		{
			if ((this.player.HasDiscs > 0 && gettingBearingsTimer <= 0f && !playerInput.inputThrowHeld && Chance.PercentageTrue(4f * (float)(1 + Singleton<SettingsManager>.Instance.botDifficulty)) && fsm.State == AIState.Attacking && victimTarget != null && (victimTarget.transform.position - base.transform.position).magnitude > throwRadius * 0.5f && CanISeeTarget(victimTarget) && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || victimTarget.HasGoldenDisc != Player.HoldingGoldenDisc.No || this.player.HasGoldenDisc != Player.HoldingGoldenDisc.No || !Chance.PercentageTrue(75f)) && AimDisc()) || (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.impossible && Chance.PercentageTrue(30f)))
			{
				return;
			}
			raycastHit = Physics2D.Raycast(base.transform.position, base.transform.up, dashDistance * 0.5f, 1 << LayerMask.NameToLayer("Teleportal"));
			if (!this.player.IsDashing && (bool)raycastHit && Chance.PercentageTrue(50f) && raycastHit.collider.GetComponent<Teleportal>().isSafeForAIs && actor.IsInLineOfSight(raycastHit.point) && Dash(this.player.lastRotation, avoidObstacles: false))
			{
				reasonForLastDash = "PlayerAI.cs:903 - dashed through teleportal";
				return;
			}
			if (!this.player.IsDashing && Chance.PercentageTrue(60f) && this.player.IsInLineOfSight(targetPosition) && !IsTargetPathable() && actor.IsPositionTouchingHazard(base.transform.position + (targetPosition - base.transform.position).normalized * dashDistance * 0.5f) && Dash((targetPosition - base.transform.position).normalized, avoidObstacles: true, allowOutOfBounds: false, allowHazardInFrontOfEndPoint: false))
			{
				reasonForLastDash = "PlayerAI.cs:865 - dashed towards target because hazard in the way";
				return;
			}
			if (this.player.isOutOfBounds && path != null && pathLength * (1f - Mathf.Clamp01((float)currentWaypoint / (float)path.vectorPath.Count)) > this.player.velocityStored.magnitude * Time.fixedDeltaTime * this.player.outOfBoundsTimer && (fsm.State == AIState.Wandering || fsm.State == AIState.Attacking) && (Singleton<SettingsManager>.Instance.botDifficulty < SettingsManager.BotDifficulty.hard || Chance.PercentageTrue(25f)))
			{
				Vector2 vector = -Singleton<LevelManager>.Instance.levelBoundary.size * 0.5f;
				Vector2 vector2 = (Vector2)Singleton<LevelManager>.Instance.levelBoundary.transform.position + new Vector2(Random.Range(0f - vector.x, vector.x), Random.Range(0f - vector.y, vector.y));
				vector2 = VectorExt.RotatePointAroundPivot(vector2, Singleton<LevelManager>.Instance.levelBoundary.transform.position, Singleton<LevelManager>.Instance.levelBoundary.transform.eulerAngles);
				if (!this.player.IsDashing && Dash((vector2 - (Vector2)base.transform.position).normalized))
				{
					reasonForLastDash = "PlayerAI.cs:881 - dashed to get back in bounds";
					return;
				}
				for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; i++)
				{
					vector2 = Singleton<LevelManager>.Instance.level.allMovingFloors[i].transform.position;
					if (!this.player.IsDashing && Dash((vector2 - (Vector2)base.transform.position).normalized))
					{
						reasonForLastDash = "PlayerAI.cs:889 - dashed to get back in bounds";
						return;
					}
				}
			}
			if (!this.player.IsDashing && base.transform.parent == null && Chance.PercentageTrue(Singleton<LevelManager>.Instance.IsInBounds(base.transform.position) ? 33 : 50))
			{
				float num = Vector2.Dot((targetPosition - base.transform.position).normalized, base.transform.up.normalized);
				Vector2 vector3 = targetPosition - base.transform.position;
				float magnitude = vector3.magnitude;
				if (this.player.activePowerup.HasPowerup(PowerupType.DashThroughWalls) && !this.player.IsInLineOfSight(targetPosition))
				{
					if (magnitude > dashDistance * 0.75f && num > 0.9f && Dash(Vector2.Lerp(vector3.normalized, base.transform.up.normalized, Random.Range(0f, 1f)), avoidObstacles: false))
					{
						reasonForLastDash = "PlayerAI.cs:920 - dashed through walls inaccurately";
						return;
					}
					if (Chance.PercentageTrue(25f) && Dash(vector3.normalized, avoidObstacles: false))
					{
						reasonForLastDash = "PlayerAI.cs:924 - dashed through walls accurately";
						return;
					}
				}
				if (magnitude > dashDistance && (num > 0.99f || (fsm.State == AIState.Fleeing && num > 0.75f)) && (Singleton<SettingsManager>.Instance.botDifficulty < SettingsManager.BotDifficulty.hard || Chance.PercentageTrue(25f)) && Dash(base.transform.up.normalized))
				{
					reasonForLastDash = "PlayerAI.cs:931 - Dashed to get closer to target, or to flee";
					return;
				}
			}
			if (this.player.HasDiscs == 0 && this.player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && !this.player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc) && (Chance.PercentageTrue(30f) || this.player.IsDisarmed) && this.player.IsItSafeToTeleport())
			{
				this.player.Teleport();
				CreatePath();
			}
		}
	}

	private bool AimDisc(float holdTime = 1f)
	{
		if (player.isOutOfBounds || player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding)
		{
			return false;
		}
		if (!Physics2D.OverlapCircle(base.transform.position, 3f, player.obstacleLayer))
		{
			if (player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc))
			{
				holdTime *= 0.4f;
			}
			if (holdThrowDownRoutine != null)
			{
				StopCoroutine(holdThrowDownRoutine);
			}
			holdThrowDownRoutine = HoldButtonDown(ButtonType.throwdisc, holdTime);
			StartCoroutine(holdThrowDownRoutine);
			return true;
		}
		return false;
	}

	private void StopHoldingThrow()
	{
		if (holdThrowDownRoutine != null)
		{
			StopCoroutine(holdThrowDownRoutine);
		}
		StartCoroutine(ReleaseButtonUp(ButtonType.throwdisc));
	}

	private GameObject BestObjectToBlock()
	{
		GameObject result = null;
		float magnitude = blockRadiusPlayers;
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (player != this.player && (player.transform.position - base.transform.position).magnitude <= magnitude && player.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(this.player, player) && this.player.IsInLineOfSight(player.transform.position) && CanISeeTarget(player))
			{
				magnitude = (player.transform.position - base.transform.position).magnitude;
				result = player.gameObject;
			}
		}
		magnitude = blockRadiusDiscs;
		foreach (Disc activeDisc in Singleton<GameManager>.Instance.activeDiscs)
		{
			if ((activeDisc.transform.position - base.transform.position).magnitude <= magnitude && activeDisc.DiscOwner != null && activeDisc.DiscOwner != this.player && activeDisc.movement != Projectile.Movement.Dropped && activeDisc.movement != Projectile.Movement.Squished && Singleton<GameManager>.Instance.AreOpponents(this.player, activeDisc) && this.player.IsInLineOfSight(activeDisc.transform.position) && ((double)Vector2.Dot(activeDisc.velocity.normalized, base.transform.position - activeDisc.transform.position) > 0.75 || activeDisc.movement == Projectile.Movement.Controlled))
			{
				magnitude = (activeDisc.transform.position - base.transform.position).magnitude;
				result = activeDisc.gameObject;
			}
		}
		return result;
	}

	private GameObject BestObjectToAttack()
	{
		GameObject gameObject = null;
		float magnitude = attackRadius;
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (!(player == this.player) && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || player.HasGoldenDisc != Player.HoldingGoldenDisc.No || this.player.HasGoldenDisc != Player.HoldingGoldenDisc.No || !Chance.PercentageTrue(33f)) && (!this.player.IsSeeker || !(player.velocity.magnitude < 0.1f) || !(targetObject != null) || !(targetObject != player.gameObject)) && !(Singleton<TimeKeeper>.Instance.TimeSince(player.lastTeleportTimestamp) < timeSinceLastTeleportThreshold) && (player.transform.position - base.transform.position).magnitude <= magnitude && Vector2.Dot((player.transform.position - base.transform.position).normalized, base.transform.up) > 0.2f && player.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(player, this.player) && CanISeeTarget(player) && (this.player.IsInLineOfSight(player.transform.position) || Chance.PercentageTrue(40f)))
			{
				magnitude = (player.transform.position - base.transform.position).magnitude;
				gameObject = player.gameObject;
			}
		}
		if (Singleton<SettingsManager>.Instance.botDifficulty < SettingsManager.BotDifficulty.hard)
		{
			foreach (Decoy activeDecoy in Singleton<GameManager>.Instance.activeDecoys)
			{
				if (!(activeDecoy.owner == null) && !(activeDecoy.owner == this.player) && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || !Chance.PercentageTrue(33f)) && (activeDecoy.transform.position - base.transform.position).magnitude <= magnitude && Vector2.Dot((activeDecoy.transform.position - base.transform.position).normalized, base.transform.up) > 0.2f && activeDecoy.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(activeDecoy.owner, this.player) && (this.player.IsInLineOfSight(activeDecoy.transform.position) || Chance.PercentageTrue(40f)))
				{
					magnitude = (activeDecoy.transform.position - base.transform.position).magnitude;
					gameObject = activeDecoy.gameObject;
				}
			}
		}
		if (gameObject != null)
		{
			return gameObject;
		}
		if (this.player.hasCompletedTutorial && Chance.PercentageTrue(60f) && fsm.State != AIState.FlickingSwitch)
		{
			return null;
		}
		magnitude = attackRadius * 0.75f;
		Switch[] allSwitches = Singleton<LevelManager>.Instance.level.allSwitches;
		foreach (Switch obj in allSwitches)
		{
			if ((obj.transform.position - base.transform.position).magnitude <= magnitude && Vector2.Dot((obj.transform.position - base.transform.position).normalized, base.transform.up) > 0.5f && this.player.IsInLineOfSight(obj.transform.position) && obj.isActive)
			{
				magnitude = (obj.transform.position - base.transform.position).magnitude;
				gameObject = obj.gameObject;
			}
		}
		return gameObject;
	}

	private GameObject BestObjectToThrowAt()
	{
		if (player.HasDiscs == 0)
		{
			return null;
		}
		if (player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding && Chance.PercentageTrue(70f))
		{
			return null;
		}
		GameObject result = null;
		float num = throwMinDotProduct;
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			num -= 0.3f;
			break;
		case SettingsManager.BotDifficulty.medium:
			num -= 0.15f;
			break;
		}
		if (player.activePowerup.HasPowerup(PowerupType.ExplosiveDisc) || player.activePowerup.HasPowerup(PowerupType.MultiDisc) || player.activePowerup.HasPowerup(PowerupType.TeleportDisc) || player.HasDiscs > 1)
		{
			num *= 0.75f;
		}
		float num2 = throwRadius;
		if (Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.medium && player.aimPower == player.aimMaxPower)
		{
			num2 = throwRadiusAimed;
		}
		foreach (Player player in Singleton<GameManager>.Instance.players)
		{
			if (!(player == this.player) && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || player.HasGoldenDisc != Player.HoldingGoldenDisc.No || this.player.HasGoldenDisc != Player.HoldingGoldenDisc.No || !Chance.PercentageTrue(33f)) && (!this.player.IsSeeker || !(player.velocity.magnitude < 0.1f) || !(targetObject != null) || !(targetObject != player.gameObject)) && !(Singleton<TimeKeeper>.Instance.TimeSince(player.lastTeleportTimestamp) < timeSinceLastTeleportThreshold) && (player.transform.position - base.transform.position).magnitude <= num2 && Vector2.Dot((player.transform.position - base.transform.position).normalized, base.transform.up) > num && player.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(player, this.player) && CanISeeTarget(player) && this.player.IsInLineOfSight(TargetLocationAfterThrow(player.gameObject)))
			{
				num = Vector2.Dot((player.transform.position - base.transform.position).normalized, base.transform.up);
				result = player.gameObject;
			}
		}
		if (Singleton<SettingsManager>.Instance.botDifficulty < SettingsManager.BotDifficulty.hard)
		{
			foreach (Decoy activeDecoy in Singleton<GameManager>.Instance.activeDecoys)
			{
				if (!(activeDecoy.owner == null) && !(activeDecoy.owner == this.player) && (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc || !Chance.PercentageTrue(33f)) && (activeDecoy.transform.position - base.transform.position).magnitude <= num2 && Vector2.Dot((activeDecoy.transform.position - base.transform.position).normalized, base.transform.up) > num && activeDecoy.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(activeDecoy.owner, this.player) && this.player.IsInLineOfSight(TargetLocationAfterThrow(activeDecoy.gameObject)))
				{
					num = Vector2.Dot((activeDecoy.transform.position - base.transform.position).normalized, base.transform.up);
					result = activeDecoy.gameObject;
				}
			}
		}
		return result;
	}

	private Vector3 TargetLocationAfterThrow(GameObject go)
	{
		if ((bool)go.GetComponent<Rigidbody2D>())
		{
			float num = 27.32f;
			float num2 = 0.74f;
			switch (Singleton<SettingsManager>.Instance.botDifficulty)
			{
			case SettingsManager.BotDifficulty.easy:
				num2 *= 0f;
				break;
			case SettingsManager.BotDifficulty.medium:
				num2 *= 0.47f;
				break;
			case SettingsManager.BotDifficulty.hard:
				num2 *= 0.9f;
				break;
			}
			float num3 = Easing.EaseOut((go.transform.position - base.transform.position).magnitude / num);
			return (Vector2)go.transform.position + go.GetComponent<Rigidbody2D>().velocity * num2 * num3;
		}
		return go.transform.position;
	}

	protected override void Block(GameObject go)
	{
		base.Block(go);
		if (player.IsAiming || player.retrieveCharge > 0f)
		{
			if (Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.medium && Vector2.Dot((go.transform.position - base.transform.position).normalized, base.transform.up) > 0.4f)
			{
				moveDir = (go.transform.position - base.transform.position).normalized;
				player.RotateSnapTo(moveDir);
			}
			StopHoldingThrow();
		}
		if (Chance.PercentageTrue((float)Singleton<SettingsManager>.Instance.botDifficulty * 40f) && player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.thrownDiscs.Count > 0 && player.IsItSafeToTeleport() && (go.transform.position - player.thrownDiscs[0].transform.position).magnitude > 5f)
		{
			Debug.Log("blocked with a teleport");
			player.Teleport();
			CreatePath();
		}
		if (Vector2.Dot((go.transform.position - base.transform.position).normalized, base.transform.up) > -0.1f)
		{
			if (player.HasDiscs > 0 && (Chance.PercentageTrue(80f) || !player.IsDashSafe(player.lastRotation, andNoObstaclesInTheWay: true)))
			{
				if (player.IsLungeSafe(moveDir))
				{
					PushButtonDown(ButtonType.attack);
				}
			}
			else if (player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.thrownDiscs.Count < 1 && player.IsItSafeToTeleport())
			{
				player.Teleport();
				CreatePath();
			}
			else if (!player.IsDashing && Dash(player.transform.up))
			{
				reasonForLastDash = "blocked threat in front by dashing towards it";
			}
			else
			{
				FindSafePlaceToDash();
			}
		}
		else if (Chance.PercentageTrue(95f))
		{
			if (player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.thrownDiscs.Count < 1 && player.IsItSafeToTeleport())
			{
				player.Teleport();
				CreatePath();
			}
			else if (player.IsDashSafe(player.transform.up, andNoObstaclesInTheWay: true))
			{
				PushButtonDown(ButtonType.dash);
			}
		}
	}

	protected override void Attack()
	{
		base.Attack();
		float num = 100f;
		if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy)
		{
			num = 90f;
			if (player.HasDiscs == 0)
			{
				num *= 0.1f;
			}
		}
		if (player.HasDiscs == 2 && Chance.PercentageTrue(70f) && player.HasGoldenDisc != Player.HoldingGoldenDisc.IsHolding)
		{
			PushButtonDown(ButtonType.throwdisc);
		}
		else if (!player.IsLungeSafe(moveDir) && victimTarget != null)
		{
			Throw(victimTarget.gameObject);
		}
		else if (Chance.PercentageTrue(num))
		{
			PushButtonDown(ButtonType.attack);
		}
	}

	protected override void Throw(GameObject go)
	{
		base.Throw(go);
		float probability = 100f;
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			probability = 80f;
			break;
		case SettingsManager.BotDifficulty.medium:
			probability = 90f;
			break;
		case SettingsManager.BotDifficulty.hard:
			probability = 95f;
			break;
		}
		if (Chance.PercentageTrue(probability) || player.HasDiscs > 1)
		{
			Vector2 vector = (TargetLocationAfterThrow(go) - base.transform.position).normalized;
			if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy)
			{
				vector = ((!Chance.PercentageTrue(66f)) ? ((Vector2)player.transform.up) : ((Vector2)VectorExt.RotatePointAroundPivot(vector, Vector2.zero, new Vector3(0f, 0f, Random.Range(-30f, 30f)))));
			}
			if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.medium)
			{
				vector = new Vector2((base.transform.up.x + vector.x) * 0.5f, (base.transform.up.y + vector.y) * 0.5f);
				player.RotateSnapTo(vector);
			}
			else
			{
				player.RotateSnapTo(vector);
			}
			if (player.IsAiming)
			{
				StopHoldingThrow();
			}
			else
			{
				PushButtonDown(ButtonType.throwdisc);
			}
			if (player.activePowerup.HasPowerup(PowerupType.FireDisc))
			{
				StartCoroutine(DivertPathAfterThrow());
			}
		}
		else if (player.IsDashSafe(player.lastRotation, andNoObstaclesInTheWay: true, allowOutOfBounds: false, allowHazardInFrontOfEndPoint: false) && Chance.PercentageTrue((player.transform.parent == null) ? 25f : 75f))
		{
			PushButtonDown(ButtonType.dash);
		}
	}

	private IEnumerator DivertPathAfterThrow()
	{
		yield return new WaitForSeconds(0.1f);
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			DivertPath(base.transform.up);
		}
	}

	protected override bool Dash(Vector2 dashDirection, bool avoidObstacles = true, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool trackFailedAttempts = true)
	{
		if (player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.IsItSafeToTeleport())
		{
			player.Teleport();
			CreatePath();
			unsafeDashAttempts = 0;
			return true;
		}
		if (base.Dash(dashDirection, avoidObstacles, allowOutOfBounds, allowHazardInFrontOfEndPoint))
		{
			player.inputMove = dashDirection;
			PushButtonDown(ButtonType.dash);
			unsafeDashAttempts = 0;
			return true;
		}
		unsafeDashAttempts++;
		if (trackFailedAttempts && unsafeDashAttempts > 7)
		{
			FindSafePlaceToDash();
		}
		return false;
	}

	private bool DashTowardsDroppedDisc()
	{
		if (IsObjectStillThere(targetObject) && (bool)targetObject.GetComponent<Disc>() && player.HasDiscs < 1)
		{
			Vector2 dashDirection = (targetObject.transform.position - base.transform.position).normalized;
			if (!Dash(dashDirection, avoidObstacles: true, allowOutOfBounds: false, allowHazardInFrontOfEndPoint: false))
			{
				if (targetObject != null && Chance.PercentageTrue(75f) && player.IsInLineOfSight(targetObject.transform.position) && player.IsDisarmed && player.thrownDiscs.Count > 0 && (threatTarget == null || (threatTarget.transform.position - base.transform.position).magnitude > 5f))
				{
					fsm.ChangeState(AIState.Retrieving);
				}
				else
				{
					ApproachTargetFromNewAngle();
				}
			}
		}
		else if (!DashAcrossHazards())
		{
			fsm.ChangeState(AIState.Wandering);
		}
		return false;
	}

	private bool TryToAttack()
	{
		LocateTarget();
		if (victimTarget != null && victimTarget.actorState == Actor.ActorState.Alive && (victimTarget.transform.position - base.transform.position).magnitude <= attackRadius && Vector2.Dot((victimTarget.transform.position - base.transform.position).normalized, base.transform.up) > 0.25f)
		{
			Attack();
			ConsiderChangingState();
			return true;
		}
		DashAcrossHazards();
		ConsiderChangingState();
		return false;
	}

	private bool TryToHitSwitch()
	{
		if (IsObjectStillThere(targetObject) && (targetObject.GetComponent<Switch>() == null || targetObject.GetComponent<Switch>().isActive) && (targetObject.transform.position - base.transform.position).magnitude <= attackRadius && (reachedEndOfPath || (victimTarget != null && Vector2.Dot((victimTarget.transform.position - base.transform.position).normalized, base.transform.up) >= 0f)))
		{
			Attack();
			return true;
		}
		if (reachedEndOfPath)
		{
			if (!DashAcrossHazards())
			{
				fsm.ChangeState(AIState.Wandering);
			}
			return false;
		}
		return false;
	}

	private bool TryToAttackHider()
	{
		LocateTarget();
		if (reachedEndOfPath)
		{
			moveDir = (targetObject.transform.position - base.transform.position).normalized;
			playerInput.inputMove = moveDir;
			player.RotateSnapTo(moveDir);
		}
		if (targetObject != null && (targetObject.transform.position - base.transform.position).magnitude <= attackRadius * 1.25f && Vector2.Dot((targetObject.transform.position - base.transform.position).normalized, base.transform.up) >= 0f)
		{
			if (!targetObject.CompareTag("Player") || targetObject.GetComponent<Player>().actorState == Actor.ActorState.Alive)
			{
				Attack();
			}
			if (reachedEndOfPath)
			{
				if (hideAndSeekPotentialTargets.Contains(targetObject))
				{
					hideAndSeekPotentialTargets.Remove(targetObject);
				}
				targetObject = null;
			}
			ConsiderChangingState();
			return true;
		}
		if (!DashAcrossHazards())
		{
			targetObject = null;
		}
		ConsiderChangingState();
		return false;
	}

	private bool DashAcrossHazards()
	{
		Vector2 vector = targetPosition - base.transform.position;
		Vector2 vector2 = base.transform.up;
		float num = 10f;
		if (!player.hasCompletedTutorial && (fsm.State == AIState.FlickingSwitch || fsm.State == AIState.Seeking))
		{
			LocateTarget();
			vector = targetPosition + new Vector3(2f, -2f, 0f) - base.transform.position;
			vector2 = vector;
			switch (Singleton<SettingsManager>.Instance.botDifficulty)
			{
			case SettingsManager.BotDifficulty.easy:
				num = 1f;
				break;
			case SettingsManager.BotDifficulty.medium:
				num = 5f;
				break;
			case SettingsManager.BotDifficulty.impossible:
				num = 50f;
				break;
			}
		}
		if (player.IsDashing)
		{
			return false;
		}
		if (!player.IsPositionTouchingHazard((Vector2)base.transform.position + vector2.normalized))
		{
			return false;
		}
		if (Chance.PercentageTrue(100f - num))
		{
			return false;
		}
		if (vector.magnitude <= 5f)
		{
			return false;
		}
		if (Dash(vector2.normalized, avoidObstacles: false, allowOutOfBounds: false, allowHazardInFrontOfEndPoint: false))
		{
			reasonForLastDash = "PlayerAI.cs:1351 - DashAcrossHazards()";
			return true;
		}
		return false;
	}

	private void FindSafePlaceToDash()
	{
		if (actor.IsDashing)
		{
			return;
		}
		unsafeDashAttempts = 0;
		Vector2 zero = Vector2.zero;
		float num = Random.Range(0f, 360f);
		for (int i = 0; i < 16; i++)
		{
			zero = VectorExt.VectorFromAngle(22.5f * (float)i + num) * actor.dashDistanceInvulnerable;
			if (Singleton<LevelManager>.Instance.IsPointOnHazard(zero + (Vector2)base.transform.position))
			{
				continue;
			}
			if (base.transform.parent != null && actor.movingFloor != null)
			{
				Collider2D collider2D = Physics2D.OverlapPoint(zero + (Vector2)base.transform.position, 1 << LayerMask.NameToLayer("MovingFloor"));
				if (collider2D != null && collider2D == actor.movingFloor.collider)
				{
					continue;
				}
			}
			Debug.DrawLine(base.transform.position, zero + (Vector2)base.transform.position, Color.magenta, 0.5f);
			if (Dash(zero, avoidObstacles: false, allowOutOfBounds: true, allowHazardInFrontOfEndPoint: true, trackFailedAttempts: false))
			{
				reasonForLastDash = "Dashed to escape island I feel trapped on";
				ScanPointsOfInterest();
				ConsiderChangingState();
				break;
			}
		}
	}

	protected override void OnPathComplete(Path p)
	{
		base.OnPathComplete(p);
		if (p.error || Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || IsTargetPathable())
		{
			return;
		}
		if (targetObject != null && fsm.State == AIState.Collecting && player.IsDisarmed && player.thrownDiscs.Count > 0 && (threatTarget == null || (threatTarget.transform.position - base.transform.position).magnitude > 5f) && (player.IsInLineOfSight(targetObject.transform.position) || Chance.PercentageTrue(50f)))
		{
			fsm.ChangeState(AIState.Retrieving);
		}
		else
		{
			if (DashAcrossHazards())
			{
				return;
			}
			if (player.HasDiscs > 0 && fsm.State != AIState.Retrieving && player.HasGoldenDisc != Player.HoldingGoldenDisc.IsHolding)
			{
				GameObject gameObject = BestObjectToThrowAt();
				if (fsm.State == AIState.Attacking && gameObject != null && actionTimer > actionDelay * 0.25f)
				{
					Throw(gameObject);
					return;
				}
				if (victimTarget != null && (fsm.State == AIState.Attacking || fsm.State == AIState.Diverting) && !playerInput.inputThrowHeld && inputTimer >= inputDelay && victimTarget != null && CanISeeTarget(victimTarget) && victimTarget.actorState == Actor.ActorState.Alive && (victimTarget.transform.position - base.transform.position).magnitude <= throwRadius * 1.33f && (victimTarget.transform.position - base.transform.position).magnitude > throwRadius * 0.5f && !Physics2D.Raycast(base.transform.position, (victimTarget.transform.position - base.transform.position).normalized, throwRadius * 0.75f, player.obstacleLayer) && AimDisc(Random.Range(1f, 1.5f)))
				{
					return;
				}
			}
			if ((path != null && path.vectorPath != null && actor.IsDashSafeFromPosition(GetFinalWaypointPosition(), targetPosition - GetFinalWaypointPosition().normalized, andNoObstaclesInTheWay: false, allowOutOfBounds: true, allowHazardInFrontOfEndPoint: true, debugGizmos: false)) || !player.hasCompletedTutorial)
			{
				return;
			}
			if (base.transform.parent != null && actor.movingFloor != null)
			{
				if (fsm.State != AIState.Diverting && Chance.PercentageTrue(20f))
				{
					SetRandomTargetPosOnMovingFloor(actor.movingFloor);
					fsm.ChangeState(AIState.Diverting);
					return;
				}
				if (Chance.PercentageTrue(66f))
				{
					fsm.ChangeState(AIState.Idle);
					return;
				}
			}
			if (player.IsSeeker && Chance.PercentageTrue(50f))
			{
				ChooseHideAndSeekTargetNearby(throwRadius * 0.33f);
			}
			else
			{
				fsm.ChangeState(AIState.Wandering);
			}
		}
	}

	protected override void PushControllerTowardsNextWaypoint()
	{
		if (player.activePowerup.HasPowerup(PowerupType.ReverseInputs))
		{
			moveDir = -Vector2.Lerp(-playerInput.inputMove, (GetCurrentWaypointPosition() - base.transform.position).normalized, moveInputSpeed * Time.deltaTime);
		}
		else
		{
			moveDir = Vector2.Lerp(playerInput.inputMove, (GetCurrentWaypointPosition() - base.transform.position).normalized, moveInputSpeed * Time.deltaTime);
		}
	}

	private void ApproachTargetFromNewAngle()
	{
		if (fsm.LastState != AIState.Diverting)
		{
			targetPosition += new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0f);
			fsm.ChangeState(AIState.Diverting);
		}
		else
		{
			ConsiderChangingState();
		}
	}

	private IEnumerator RandomiseInterestMods()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f);
			switch (Singleton<SettingsManager>.Instance.botDifficulty)
			{
			case SettingsManager.BotDifficulty.easy:
				ambientInterestMod = Random.Range(1f, 2f);
				break;
			case SettingsManager.BotDifficulty.medium:
				ambientInterestMod = Random.Range(0.9f, 1.5f);
				break;
			case SettingsManager.BotDifficulty.hard:
				ambientInterestMod = Random.Range(0.8f, 1.2f);
				break;
			case SettingsManager.BotDifficulty.impossible:
				ambientInterestMod = Random.Range(0.6f, 1f);
				break;
			}
			aggressionMod = Random.Range(0.6f, 1f);
			if (Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.medium)
			{
				aggressionMod += 0.3f;
			}
			if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills && (player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding || player.HasGoldenDisc == Player.HoldingGoldenDisc.IsThrown))
			{
				aggressionMod *= Random.Range(1f, 2f);
			}
			if (Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Survival && (player.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding || player.HasGoldenDisc == Player.HoldingGoldenDisc.IsThrown))
			{
				nervousMod = Random.Range(1f, 2f);
			}
			else
			{
				nervousMod = Random.Range(0.8f, 1.2f);
			}
			if (Singleton<SettingsManager>.Instance.botDifficulty == SettingsManager.BotDifficulty.easy)
			{
				nervousMod += 0.3f;
			}
			greedyMod = Random.Range(0.8f, 1.2f);
			if (player.activePowerup.HasPowerup(PowerupType.ReverseInputs))
			{
				nervousMod *= 1.5f;
				ambientInterestMod *= 5f;
			}
			if (fsm.State == AIState.Retrieving)
			{
				nervousMod *= 1.25f;
			}
		}
	}

	private void PushButtonDown(ButtonType buttonType)
	{
		switch (buttonType)
		{
		case ButtonType.dash:
			inputTimer = 0f;
			playerInput.inputDash = true;
			dashPressed = true;
			break;
		case ButtonType.attack:
			if (attackPressed || inputTimer < inputDelay)
			{
				return;
			}
			inputTimer = 0f;
			playerInput.inputAttack = true;
			attackPressed = true;
			break;
		case ButtonType.throwdisc:
			if (playerInput.inputThrowHeld || playerInput.inputThrowIsBlocked || inputTimer < inputDelay)
			{
				return;
			}
			inputTimer = 0f;
			playerInput.inputThrowHeld = true;
			break;
		}
		StartCoroutine(ReleaseButtonUp(buttonType));
	}

	private IEnumerator HoldButtonDown(ButtonType buttonType, float duration)
	{
		float timer = 0f;
		if (buttonType != ButtonType.throwdisc || inputTimer < inputDelay)
		{
			yield break;
		}
		inputTimer = 0f;
		if (playerInput.inputThrowIsBlocked)
		{
			yield return StartCoroutine(ReleaseButtonUp(ButtonType.throwdisc));
		}
		playerInput.inputThrowHeld = true;
		while (timer < duration)
		{
			timer += Time.deltaTime;
			if (!playerInput.inputThrowHeld)
			{
				yield break;
			}
			yield return null;
		}
		if (playerInput.inputThrowHeld)
		{
			playerInput.inputThrowHeld = false;
			playerInput.inputThrow = true;
			yield return null;
			playerInput.inputThrow = false;
		}
	}

	private IEnumerator ReleaseButtonUp(ButtonType buttonType)
	{
		switch (buttonType)
		{
		case ButtonType.dash:
			yield return null;
			playerInput.inputDash = false;
			dashPressed = false;
			break;
		case ButtonType.attack:
			yield return null;
			playerInput.inputAttack = false;
			attackPressed = false;
			break;
		case ButtonType.throwdisc:
			if (playerInput.inputThrowHeld || playerInput.inputThrowIsBlocked)
			{
				playerInput.inputThrowHeld = false;
				playerInput.inputThrow = true;
				yield return null;
				playerInput.inputThrow = false;
			}
			break;
		}
	}
}
