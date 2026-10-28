using UnityEngine;
public partial class ReactiveReference : MovingFloor {
	protected override void FixedUpdate()
	{
		if (movementState == MovementState.PreMoving)
		{
			if (!(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) >= preMoveDur))
			{
				velocity = Vector2.zero;
				return;
			}
			movementState = MovementState.Moving;
			Singleton<LevelManager>.Instance.RecreatePathsNextOpportunity();
		}
		else if (movementState == MovementState.SpringPausing)
		{
			if (!(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) >= springPauseDur))
			{
				velocity = Vector2.zero;
				return;
			}
			movementState = MovementState.Moving;
			Singleton<LevelManager>.Instance.RecreatePathsNextOpportunity();
		}
		if (movementState != MovementState.Moving)
		{
			return;
		}
		if (movePercent < 1f)
		{
			if (isSpringingBack)
			{
				movePercent += Time.fixedDeltaTime / springWindBackDur;
			}
			else
			{
				movePercent += Time.fixedDeltaTime / moveDur;
			}
			if (reverse)
			{
				tr.localPosition = Vector2.Lerp(startPos, targetPos, Easing.EaseIn(Mathf.Clamp01(movePercent)));
			}
			else
			{
				tr.localPosition = Vector2.Lerp(startPos, targetPos, Easing.EaseOut(Mathf.Clamp01(movePercent)));
			}
			if (useRotations)
			{
				tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startRot, targetRot, Easing.EaseInOut(Mathf.Clamp01(movePercent))));
			}
		}
		else
		{
			tr.localPosition = targetPos;
			if (useRotations)
			{
				tr.localRotation = Quaternion.Euler(0f, 0f, targetRot);
			}
			movementState = MovementState.Static;
			UpdatePaths();
			Singleton<LevelManager>.Instance.RecreatePathsNextOpportunity();
			Singleton<Camera3D>.Instance.Shake(0.2f, 0.25f);
			if (isSpring && !isSpringingBack && posIndex == 1)
			{
				ToggleTargetPosition(moveOwner, springBack: true);
			}
			reverse = false;
		}
		UpdateGraphPosition();
		Singleton<LevelManager>.Instance.level.UpdateObstacles();
		tr.localPosition += new Vector3(0f, 0f, zPos);
		velocity = ((Vector2)tr.position - lastPos) / Time.fixedDeltaTime;
		lastPos = tr.position;
		if (scanTimer < scanRate)
		{
			scanTimer += Time.deltaTime;
			return;
		}
		scanTimer = 0f;
		UpdatePaths();
	}
	private void ToggleTargetPosition(Actor activator = null, bool springBack = false)
	{
		if (movementState == MovementState.SpringPausing)
		{
			movementState = MovementState.Moving;
		}
		else
		{
			startPos = positions[posIndex];
			if (useRotations)
			{
				startRot = positions[posIndex].z;
			}
			if ((movementState == MovementState.Moving || movementState == MovementState.PreMoving) && !reverse && positions.Length > 1)
			{
				posIndex = (posIndex - 1) % positions.Length;
				if (posIndex < 0)
				{
					posIndex = positions.Length - 1;
				}
				reverse = true;
			}
			else
			{
				posIndex = (posIndex + 1) % positions.Length;
				reverse = false;
				if (speedTrailPFX.Length != 0)
				{
					for (int i = 0; i < speedTrailPFX.Length; i++)
					{
						if (i == posIndex % speedTrailPFX.Length)
						{
							speedTrailPFX[i].SetActive(value: true);
						}
						else
						{
							speedTrailPFX[i].SetActive(value: false);
						}
					}
				}
			}
			targetPos = positions[posIndex];
			if (useRotations)
			{
				targetRot = positions[posIndex].z;
			}
			if ((movementState == MovementState.Moving || movementState == MovementState.PreMoving) && positions.Length > 1)
			{
				movePercent = 1f - movePercent;
			}
			else
			{
				movePercent = 0f;
			}
			prevBounds = collider.bounds;
			if (springBack)
			{
				movementState = MovementState.SpringPausing;
			}
			else
			{
				movementState = MovementState.PreMoving;
			}
		}
		toggleTimestamp = Singleton<TimeKeeper>.Instance.CurrentTime;
		if (activator != null)
		{
			moveOwner = activator as Player;
			if (!springBack)
			{
				PerformObstacleFlash(activator.character.Color);
			}
		}
		isSpringingBack = springBack;
	}
}
