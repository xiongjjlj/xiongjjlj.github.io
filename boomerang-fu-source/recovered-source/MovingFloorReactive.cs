using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

public class MovingFloorReactive : MovingFloor
{
	public enum MovementState
	{
		Static,
		Moving,
		PreMoving,
		SpringPausing
	}

	[Header("Reactive Movement")]
	public MovementState movementState;

	private bool isSpringingBack;

	[SerializeField]
	private bool isSpring;

	public float springPauseDur = 0.25f;

	public float springWindBackDur = 2f;

	public float preMoveDur = 0.35f;

	public Player moveOwner;

	private float toggleTimestamp;

	private bool reverse;

	public Vector3[] positions;

	private Color initColor;

	private Color activatorColor;

	public Renderer[] renderers;

	private float flashDuration = 0.6f;

	private IEnumerator flashRoutine;

	public DynamicRope[] dynamicRopes;

	[Header("PFX")]
	public GameObject[] speedTrailPFX;

	[Header("Pathfinding")]
	private new Bounds newBounds;

	private Queue<Bounds> boundsQueue = new Queue<Bounds>(4);

	private new static float scanRate = 0.15f;

	private new float scanTimer;

	[Header("Audio")]
	[SerializeField]
	private AnimationCurve audioCurve;

	private EventInstance movingSound;

	public new string movingSoundEventName = "event:/environment/moving_obstacle";

	private bool hasReachedEndAudio;

	protected override void Start()
	{
		base.Start();
		tr = base.transform;
		posIndex = 0;
		reverse = false;
		isSpringingBack = false;
		if (renderers.Length != 0)
		{
			initColor = renderers[0].material.color;
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			scanRate = 0.75f;
		}
		else
		{
			scanRate = 0.15f;
		}
	}

	protected override void Update()
	{
		if (movementState == MovementState.Static || movementState == MovementState.SpringPausing)
		{
			if (movingSound.isValid())
			{
				Singleton<AudioManager>.Instance.StopLooping(movingSound);
			}
		}
		else
		{
			if (!movingSound.isValid())
			{
				movingSound = Singleton<AudioManager>.Instance.PlayLooping(movingSoundEventName, base.transform);
			}
			if (movePercent > 0.9f && !hasReachedEndAudio)
			{
				hasReachedEndAudio = true;
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/moving_obstacle_stop", base.transform.position);
			}
		}
		if (movingSound.isValid())
		{
			float value = audioCurve.Evaluate((Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) - preMoveDur) / moveDur);
			if (isSpringingBack)
			{
				value = audioCurve.Evaluate((Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) - springPauseDur) / springWindBackDur) * 0.6f;
			}
			if (Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) < preMoveDur)
			{
				value = Easing.QuartIn(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) / preMoveDur) * 0.8f;
			}
			Singleton<AudioManager>.Instance.SetEventInstanceParameter(movingSound, "velocity", value);
		}
		if (movementState != MovementState.Static)
		{
			DynamicRope[] array = dynamicRopes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].UpdateConnection();
			}
		}
		if (Singleton<SettingsManager>.Instance.numBots != 0 || Singleton<LevelManager>.Instance.numEnemies != 0 || Singleton<GameManager>.Instance.decoysInMatch)
		{
			UpdateGraphPosition();
			if (useRotations && boundsQueue.Count > 0)
			{
				Singleton<LevelManager>.Instance.RescanGraphArea(boundsQueue.Dequeue());
			}
		}
	}

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

	private void PerformObstacleFlash(Color color)
	{
		activatorColor = color;
		if (flashRoutine != null)
		{
			StopCoroutine(flashRoutine);
		}
		flashRoutine = FlashColor();
		StartCoroutine(flashRoutine);
	}

	private IEnumerator FlashColor()
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material.SetColor("_Color", activatorColor);
		}
		float t = 0f;
		while (t < flashDuration)
		{
			t += Time.deltaTime;
			for (int j = 0; j < renderers.Length; j++)
			{
				renderers[j].material.SetColor("_Color", Color.Lerp(activatorColor, initColor, Easing.EaseOut(t / flashDuration)));
			}
			yield return null;
		}
		for (int k = 0; k < renderers.Length; k++)
		{
			renderers[k].material.SetColor("_Color", initColor);
		}
	}

	public override void StartMoving()
	{
	}

	public void Toggle(Actor activator = null)
	{
		hasReachedEndAudio = false;
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/moving_obstacle_stop", base.transform.position);
		ToggleTargetPosition(activator);
	}

	public override Vector3 GetInverseTransformPoint(Vector3 point)
	{
		if (useRotations)
		{
			return point;
		}
		return base.GetInverseTransformPoint(point);
	}

	public override Vector3 GetTransformPoint(Vector3 point)
	{
		if (useRotations)
		{
			return point;
		}
		return base.GetTransformPoint(point);
	}

	private void UpdatePaths()
	{
		if (useRotations)
		{
			Bounds bounds = collider.bounds;
			while (boundsQueue.Count > 16)
			{
				boundsQueue.Dequeue();
			}
			Vector2 vector = bounds.size * 0.5f;
			Vector2 vector2 = bounds.size * 0.25f;
			boundsQueue.Enqueue(new Bounds((Vector2)bounds.center - vector2, vector));
			boundsQueue.Enqueue(new Bounds(new Vector2(bounds.center.x - vector2.x, bounds.center.y + vector2.y), vector));
			boundsQueue.Enqueue(new Bounds(new Vector2(bounds.center.x + vector2.x, bounds.center.y + vector2.y), vector));
			boundsQueue.Enqueue(new Bounds(new Vector2(bounds.center.x + vector2.x, bounds.center.y - vector2.y), vector));
		}
	}

	private void OnDisable()
	{
		if (movingSound.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(movingSound);
		}
	}
}
