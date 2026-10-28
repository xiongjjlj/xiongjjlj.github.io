using System.Collections.Generic;
using MonsterLove.StateMachine;
using UnityEngine;

public class EnemyAI : AI
{
	public Dictionary<Player, float> playersInMemory = new Dictionary<Player, float>();

	public static float memoryDuration;

	protected float visionDistance = 25f;

	protected float visionCone;

	protected float personalSpaceRadius = 10f;

	protected float wanderRadius = 20f;

	protected override void Awake()
	{
		base.Awake();
		fsm = StateMachine<AIState>.Initialize(this, AIState.Idle);
	}

	protected virtual void Start()
	{
		InitPathSettings();
	}

	public override void Init()
	{
		base.Init();
		playersInMemory.Clear();
	}

	protected override void UpdateBehaviour()
	{
		base.UpdateBehaviour();
		actor.inputMove = moveDir;
	}

	public bool IsAwareOfPlayer(Player player)
	{
		if (playersInMemory.ContainsKey(player))
		{
			if (player.actorState == Actor.ActorState.Dead)
			{
				playersInMemory.Remove(player);
				return false;
			}
			return true;
		}
		return false;
	}

	protected virtual void BecomeAwareOfPlayer(Player player)
	{
		if (!playersInMemory.ContainsKey(player) && player.actorState == Actor.ActorState.Alive)
		{
			if (playersInMemory.Count == 0)
			{
				Enemy enemy = actor as Enemy;
				if (!actor.IsStunned && enemy != null)
				{
					enemy.exclamPFX.Emit(1);
				}
				if (fsm.State != AIState.Attacking)
				{
					actor.inputMove = (player.transform.position - base.transform.position).normalized;
					moveDir = actor.inputMove;
				}
			}
			playersInMemory.Add(player, 0f);
		}
		else
		{
			playersInMemory[player] = 0f;
		}
	}

	protected void ForgetPlayerOverTime(Player player)
	{
		if (IsAwareOfPlayer(player))
		{
			playersInMemory[player] += Time.deltaTime;
			if (playersInMemory[player] >= memoryDuration)
			{
				ForgetPlayerNow(player);
			}
		}
	}

	protected void ForgetPlayerNow(Player player)
	{
		if (!IsAwareOfPlayer(player))
		{
			return;
		}
		playersInMemory.Remove(player);
		if (fsm.State != AIState.Attacking && playersInMemory.Count == 0)
		{
			Enemy enemy = actor as Enemy;
			if (!actor.IsStunned && enemy != null)
			{
				enemy.questionPFX.Emit(1);
			}
		}
	}

	protected void ApproachTargetFromNewAngle()
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

	protected bool IsInVisionCone(Vector3 targetPos)
	{
		if ((targetPos - base.transform.position).magnitude > visionDistance)
		{
			return false;
		}
		if (Vector2.Dot((targetPos - base.transform.position).normalized, base.transform.up) >= visionCone)
		{
			return true;
		}
		return false;
	}

	protected bool IsInPersonalSpace(Vector3 targetPos)
	{
		if ((targetPos - base.transform.position).magnitude <= personalSpaceRadius)
		{
			return true;
		}
		return false;
	}

	protected virtual GameObject BestObjectToAttack()
	{
		GameObject result = null;
		float magnitude = attackRadius;
		foreach (KeyValuePair<Player, float> item in playersInMemory)
		{
			if ((item.Key.transform.position - base.transform.position).magnitude <= magnitude && Vector2.Dot((item.Key.transform.position - base.transform.position).normalized, base.transform.up) > 0.25f && item.Key.actorState == Actor.ActorState.Alive && CanISeeTarget(item.Key) && actor.IsInLineOfSight(item.Key.transform.position))
			{
				magnitude = (item.Key.transform.position - base.transform.position).magnitude;
				result = item.Key.gameObject;
			}
		}
		return result;
	}
}
