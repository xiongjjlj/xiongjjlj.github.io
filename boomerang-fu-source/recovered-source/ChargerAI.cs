using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class ChargerAI : EnemyAI
{
	[SerializeField]
	private Vector2 targetDirection;

	[SerializeField]
	private bool isAiming;

	public ChargerAI()
	{
		reactionTimes[0] = 0.6f;
		reactionTimes[1] = 0.7f;
		reactionTimes[2] = 0.85f;
		visionDistance = 30f;
		EnemyAI.memoryDuration = 2.25f;
		attackRadius = visionDistance;
	}

	public void OnEnable()
	{
		Init();
	}

	public override void Init()
	{
		base.Init();
		fsm.ChangeState(AIState.Wandering);
	}

	protected override void UpdateBehaviour()
	{
		if (isAiming)
		{
			if (actor.IsStunned || actor.IsFrozen)
			{
				StopAiming();
			}
			else if (actor.aimTimer < actor.aimDuration)
			{
				if (victimTarget != null && CanISeeTarget(victimTarget) && actor.IsInLineOfSight(victimTarget.transform.position))
				{
					moveDir = Vector2.Lerp(actor.inputMove, (victimTarget.transform.position - base.transform.position).normalized, moveInputSpeed * 0.25f * (1f + (float)Singleton<SettingsManager>.Instance.botDifficulty) * Time.deltaTime);
				}
				actor.Aim();
			}
			else
			{
				StopAiming();
				if (!IsHazardInFront(moveDir.normalized))
				{
					Attack();
				}
			}
		}
		base.UpdateBehaviour();
		ambientInterestLevel = ambientInterestMod;
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!isAiming && !actor.IsStunned && playersInMemory.ContainsKey(Singleton<GameManager>.Instance.players[i]) && (!CanISeeTarget(Singleton<GameManager>.Instance.players[i]) || !IsInVisionCone(Singleton<GameManager>.Instance.players[i].transform.position) || !actor.IsInLineOfSight(Singleton<GameManager>.Instance.players[i].transform.position)))
			{
				ForgetPlayerOverTime(Singleton<GameManager>.Instance.players[i]);
			}
			if ((Singleton<GameManager>.Instance.players[i].transform.position - base.transform.position).magnitude < actor.physicsColliderRadius * 2.2f)
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

	public void Wandering_Enter()
	{
		targetObject = null;
		SetRandomTargetPos();
		CreatePath();
		SetStateChangeReluctance(1.25f, 1.5f);
	}

	public void Wandering_Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (reachedEndOfPath)
			{
				ScanPointsOfInterest();
				ConsiderChangingState();
				SetRandomTargetPos();
				CreatePath();
			}
			MoveOnPath();
			moveDir *= 0.5f;
		}
	}

	public void Idle_Enter()
	{
		reachedEndOfPath = true;
		targetObject = null;
		FaceRandomDirection();
		SetStateChangeReluctance(1.25f, 3f);
	}

	public void Idle_Update()
	{
		if (Vector2.Dot(base.transform.up, targetDirection) < 0.9f)
		{
			moveDir = targetDirection * 0.1f;
		}
		else
		{
			moveDir = Vector2.Lerp(actor.inputMove, Vector2.zero, moveInputSpeed * 3f * Time.deltaTime);
		}
	}

	private void Attacking_Enter()
	{
		LocateTarget();
		CreatePath();
		SetStateChangeReluctance(1.25f, 2f);
		moveDir = (victimTarget.transform.position - base.transform.position).normalized;
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
				if (IsTargetHiddenByPlants(victimTarget) && Chance.PercentageTrue(66f))
				{
					ChangeToRestState();
				}
				else
				{
					LocateTarget();
					CreatePath();
				}
			}
			else
			{
				reachedEndOfPath = true;
				victimTarget = null;
				targetObject = null;
				ScanPointsOfInterest();
				ConsiderChangingState();
			}
		}
		MoveOnPath();
	}

	private void Fleeing_Enter()
	{
		if (isAiming)
		{
			StopAiming();
		}
		path = null;
		LocateTarget();
		CreatePath();
		SetStateChangeReluctance(3f, 1f);
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
			MoveOnPath();
		}
	}

	private void Diverting_Enter()
	{
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

	protected override void TakeAction()
	{
		base.TakeAction();
		if (actor.IsOnCamera() && !actor.IsStunned)
		{
			for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
			{
				if (!playersInMemory.ContainsKey(Singleton<GameManager>.Instance.players[i]) && actor.actorState == Actor.ActorState.Alive && actor.IsInLineOfSight(Singleton<GameManager>.Instance.players[i].transform.position) && (IsInVisionCone(Singleton<GameManager>.Instance.players[i].transform.position) || IsInPersonalSpace(Singleton<GameManager>.Instance.players[i].transform.position)))
				{
					BecomeAwareOfPlayer(Singleton<GameManager>.Instance.players[i]);
				}
			}
			for (int j = 0; j < Singleton<GameManager>.Instance.activeDiscs.Count; j++)
			{
				if (Singleton<GameManager>.Instance.activeDiscs[j].movement != Projectile.Movement.Dropped && Singleton<GameManager>.Instance.activeDiscs[j].movement != Projectile.Movement.Squished && Singleton<GameManager>.Instance.activeDiscs[j].DiscOwner != null && !playersInMemory.ContainsKey(Singleton<GameManager>.Instance.activeDiscs[j].DiscOwner) && Singleton<GameManager>.Instance.activeDiscs[j].DiscOwner.actorState == Actor.ActorState.Alive && actor.IsInLineOfSight(Singleton<GameManager>.Instance.activeDiscs[j].transform.position) && (IsInVisionCone(Singleton<GameManager>.Instance.activeDiscs[j].transform.position) || IsInPersonalSpace(Singleton<GameManager>.Instance.activeDiscs[j].transform.position)))
				{
					BecomeAwareOfPlayer(Singleton<GameManager>.Instance.activeDiscs[j].DiscOwner);
				}
			}
		}
		if (Singleton<SettingsManager>.Instance.botDifficulty >= SettingsManager.BotDifficulty.medium || !Chance.PercentageTrue(25f))
		{
			if (actor.IsFrozen && Singleton<SettingsManager>.Instance.botDifficulty > SettingsManager.BotDifficulty.easy && Chance.PercentageTrue(66f))
			{
				actor.Dash();
			}
			GameObject gameObject = BestObjectToAttack();
			if (gameObject != null && Chance.PercentageTrue(90f) && IsAimingReasonable(gameObject))
			{
				moveDir = (gameObject.transform.position - base.transform.position).normalized;
				StartAiming();
			}
		}
	}

	protected override void Attack()
	{
		base.Attack();
		actor.Attack();
	}

	protected void StartAiming()
	{
		if (actor.attacksLeft > 0 && actor.IsOnCamera())
		{
			isAiming = true;
		}
	}

	protected void StopAiming()
	{
		isAiming = false;
		actor.StopAiming();
	}

	protected override void OnPathComplete(Path p)
	{
		base.OnPathComplete(p);
		if (!p.error && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (fsm.State == AIState.Idle)
			{
				reachedEndOfPath = true;
			}
			if (fsm.State != AIState.Fleeing && !IsTargetPathable())
			{
				fsm.ChangeState(AIState.Wandering);
			}
		}
	}

	private void FaceRandomDirection()
	{
		targetDirection = Random.insideUnitCircle.normalized;
	}

	protected override void ScanPointsOfInterest()
	{
		base.ScanPointsOfInterest();
		foreach (KeyValuePair<Player, float> item in playersInMemory)
		{
			if (item.Key != actor && item.Key.actorState == Actor.ActorState.Alive && Singleton<GameManager>.Instance.AreOpponents(item.Key, actor))
			{
				ScanAsVictim(item.Key);
				ScanAsThreat(item.Key.gameObject, ThreatType.Player);
			}
		}
	}

	protected override float GetInterestOfThreat(GameObject threat, ThreatType threatType)
	{
		switch (threatType)
		{
		case ThreatType.Player:
		{
			if (isAiming)
			{
				return 0f;
			}
			if (!actor.IsInLineOfSight(threat.transform.position))
			{
				return 0f;
			}
			Player component = threat.GetComponent<Player>();
			if (!CanISeeTarget(component))
			{
				return 0f;
			}
			if (component.IsDisarmed || component.IsFrozen)
			{
				return 0f;
			}
			float num2 = (0f - Vector2.Dot((threat.transform.position - base.transform.position).normalized, base.transform.up)) * 0.05f + 0.45f;
			float num3 = ((component.IsAiming && Vector2.Dot(threat.transform.up, (base.transform.position - threat.transform.position).normalized) > 0.25f) ? 2f : 1f);
			float num4 = Mathf.Max(1f - Vector2.Distance(base.transform.position, threat.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude * 2f, 0f);
			if ((component.transform.position - base.transform.position).magnitude <= actor.physicsColliderRadius * 4f)
			{
				return 4f * nervousMod;
			}
			return num2 * num4 * nervousMod * num3;
		}
		case ThreatType.Explosive:
			if (actor.IsInLineOfSight(threat.transform.position) && Vector2.Distance(base.transform.position, threat.transform.position) < 17f)
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

	protected override float GetInterestOfVictim(Player otherPlayer)
	{
		float num = Mathf.Min(Vector2.Dot((otherPlayer.transform.position - base.transform.position).normalized, base.transform.up) * 0.2f + 1f, 1f);
		float num2 = (actor.IsInLineOfSight(otherPlayer.transform.position) ? 1.5f : 1f);
		float num3 = (CanISeeTarget(otherPlayer) ? 1f : 0.25f);
		float num4 = Mathf.Min(Mathf.Max(1.5f - Vector2.Distance(base.transform.position, otherPlayer.transform.position) / Singleton<LevelManager>.Instance.level.box.size.magnitude, 0f), 2f);
		return num * num2 * num4 * aggressionMod * num3;
	}

	protected override void ConsiderChangingState()
	{
		if (bestThreatSoFar > bestDesireSoFar && bestThreatSoFar > bestVictimSoFar && bestThreatSoFar > ambientInterestLevel && bestThreatSoFar > stateChangeReluctance)
		{
			if (fsm.State != AIState.Fleeing || Chance.PercentageTrue(33f))
			{
				targetObject = threatTarget;
			}
			fsm.ChangeState(AIState.Fleeing);
			return;
		}
		if (playersInMemory.Count == 0 && reachedEndOfPath)
		{
			ChangeToRestState();
		}
		if (bestVictimSoFar > bestDesireSoFar && bestVictimSoFar > bestThreatSoFar && bestVictimSoFar > ambientInterestLevel && bestVictimSoFar > stateChangeReluctance)
		{
			targetObject = victimTarget.gameObject;
			if (IsTargetHiddenByPlants(victimTarget))
			{
				targetObject = NearbyPlant(victimTarget.transform.position);
			}
			fsm.ChangeState(AIState.Attacking);
		}
		else if (ambientInterestLevel > stateChangeReluctance)
		{
			ChangeToRestState();
		}
	}

	protected override void BecomeAwareOfPlayer(Player player)
	{
		aggressionMod *= 5f;
		ConsiderChangingState();
		aggressionMod /= 5f;
		base.BecomeAwareOfPlayer(player);
	}

	protected override void LocateTarget()
	{
		if (reachedEndOfPath && targetObject != null && !actor.IsInLineOfSight(targetObject.transform.position) && Chance.PercentageTrue(50f))
		{
			ScanPointsOfInterest();
			ConsiderChangingState();
		}
		else
		{
			base.LocateTarget();
		}
	}

	private bool ChangeToRestState()
	{
		if (fsm.State == AIState.Attacking && !actor.IsStunned && !actor.IsAiming && !actor.IsAttacking)
		{
			Enemy enemy = actor as Enemy;
			if (enemy != null)
			{
				enemy.questionPFX.Emit(1);
			}
		}
		if (Chance.PercentageTrue(33f))
		{
			fsm.ChangeState(AIState.Wandering);
		}
		else
		{
			if (fsm.State == AIState.Idle && Chance.PercentageTrue(50f))
			{
				FaceRandomDirection();
			}
			fsm.ChangeState(AIState.Idle);
		}
		return true;
	}

	private bool IsAimingReasonable(GameObject targetObject)
	{
		if ((targetObject.transform.position - base.transform.position).magnitude < attackRadius * 1.25f && actor.IsInLineOfSight(targetObject.transform.position) && !IsHazardInFront((targetObject.transform.position - base.transform.position).normalized))
		{
			return true;
		}
		return false;
	}

	private bool IsHazardInFront(Vector2 direction)
	{
		if (actor.IsPositionTouchingHazard((Vector2)base.transform.position + direction.normalized * actor.attackDistance * 0.25f))
		{
			return true;
		}
		return false;
	}

	protected override void SetDifficulty()
	{
		switch (Singleton<SettingsManager>.Instance.botDifficulty)
		{
		case SettingsManager.BotDifficulty.easy:
			reactionTimes[0] = 0.6f;
			reactionTimes[1] = 0.7f;
			reactionTimes[2] = 0.85f;
			break;
		case SettingsManager.BotDifficulty.medium:
			reactionTimes[0] = 0.41f;
			reactionTimes[1] = 0.52f;
			reactionTimes[2] = 0.63f;
			break;
		case SettingsManager.BotDifficulty.hard:
			reactionTimes[0] = 0.1f;
			reactionTimes[1] = 0.25f;
			reactionTimes[2] = 0.35f;
			break;
		case SettingsManager.BotDifficulty.impossible:
			reactionTimes[0] = 0.07f;
			reactionTimes[1] = 0.13f;
			reactionTimes[2] = 0.2f;
			break;
		}
		EnemyAI.memoryDuration += (float)Singleton<SettingsManager>.Instance.botDifficulty * 0.5f;
		visionDistance += (float)Singleton<SettingsManager>.Instance.botDifficulty * 5f;
	}
}
