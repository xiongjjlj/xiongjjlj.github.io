using System.Collections;
using UnityEngine;

public class DecoyAI : AI
{
	private enum ButtonType
	{
		dash
	}

	private Decoy decoy;

	[SerializeField]
	private DecoyInput decoyInput;

	private bool dashPressed;

	private int unsafeDashAttempts;

	protected override void Awake()
	{
		base.Awake();
		decoy = actor as Decoy;
	}

	public override void Init()
	{
		base.Init();
		fsm.ChangeState(AIState.Wandering);
	}

	protected override void UpdateBehaviour()
	{
		dashDistance = actor.dashDistance;
		divertingDistance = 3f;
		moveInputSpeed = 20f;
		base.UpdateBehaviour();
		decoyInput.inputMove.x = moveDir.x;
		decoyInput.inputMove.y = moveDir.y;
	}

	private void Wandering_Enter()
	{
		targetObject = null;
		SetRandomTargetPos();
		CreatePath();
		SetStateChangeReluctance(1.25f, 1.5f);
	}

	private void Wandering_Update()
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
			MoveOnPath(DashAcrossHazards);
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

	private bool DashAcrossHazards()
	{
		Vector2 vector = targetPosition - base.transform.position;
		Vector2 vector2 = base.transform.up;
		float num = 10f;
		if (decoy.IsDashing)
		{
			return false;
		}
		if (!decoy.IsPositionTouchingHazard((Vector2)base.transform.position + vector2.normalized))
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

	public void ForceDash(Vector2 dashDirection, bool avoidObstacles = true, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool trackFailedAttempts = true)
	{
		FindSafePlaceToDash(VectorExt.AngleFromVector(dashDirection));
	}

	protected override bool Dash(Vector2 dashDirection, bool avoidObstacles = true, bool allowOutOfBounds = true, bool allowHazardInFrontOfEndPoint = true, bool trackFailedAttempts = true)
	{
		if (base.Dash(dashDirection, avoidObstacles, allowOutOfBounds, allowHazardInFrontOfEndPoint))
		{
			decoyInput.inputMove = dashDirection;
			decoy.inputMove = dashDirection;
			PushButtonDown(ButtonType.dash);
			return true;
		}
		unsafeDashAttempts++;
		if (trackFailedAttempts && unsafeDashAttempts > 7)
		{
			FindSafePlaceToDash(VectorExt.AngleFromVector(base.transform.up));
		}
		return false;
	}

	private void FindSafePlaceToDash(float startingAngle)
	{
		if (actor.IsDashing)
		{
			return;
		}
		unsafeDashAttempts = 0;
		Vector2 zero = Vector2.zero;
		Random.Range(0f, 360f);
		for (int i = 0; i < 16; i++)
		{
			zero = VectorExt.VectorFromAngle(22.5f * (float)i + startingAngle) * actor.dashDistanceInvulnerable;
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

	private void PushButtonDown(ButtonType buttonType)
	{
		if (buttonType == ButtonType.dash)
		{
			inputTimer = 0f;
			decoyInput.inputDash = true;
			dashPressed = true;
		}
		StartCoroutine(ReleaseButtonUp(buttonType));
	}

	private IEnumerator ReleaseButtonUp(ButtonType buttonType)
	{
		if (buttonType == ButtonType.dash)
		{
			yield return null;
			decoyInput.inputDash = false;
			dashPressed = false;
		}
	}
}
