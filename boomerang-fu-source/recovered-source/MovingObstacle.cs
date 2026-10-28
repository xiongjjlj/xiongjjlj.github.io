using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
	public enum MovementState
	{
		Static,
		Moving,
		PreMoving,
		SpringPausing
	}

	[Header("Moving Obstacle Behaviour")]
	[SerializeField]
	private bool isSpring;

	public bool isRotatingObstacle;

	public bool isRotatingClockwise = true;

	public bool randomizedStart;

	[Header("Movement")]
	[SerializeField]
	protected int posIndex;

	public Vector2[] positions;

	private Vector2 startPos;

	protected Vector2 targetPos;

	private float zPos;

	private Vector2 lastPos;

	public Vector2 velocity;

	private float angularVelocityAcceleration;

	private Transform obstacleTransform;

	private Collider2D collider;

	private Vector2 colliderSize;

	private Collider2D[] touchingFires = new Collider2D[32];

	private bool reverse;

	[SerializeField]
	private float movePercent;

	[Header("Speed")]
	public float moveDur = 0.5f;

	public float springPauseDur = 0.25f;

	public float rotatingDur = 4f;

	public float[] rotatingDurations;

	public float springWindBackDur = 2f;

	[Header("State")]
	public MovementState movementState;

	private bool isSpringingBack;

	private float toggleTimestamp;

	public float preMoveDur = 0.35f;

	public Player moveOwner;

	public bool rescanInQuadrants;

	private static float updateError = 0.25f;

	private static float scanRate = 0.15f;

	private float scanTimer;

	private Bounds newBounds;

	private Bounds prevBounds;

	private Queue<Bounds> boundsQueue;

	private Color initColor;

	private Color activatorColor;

	private Renderer[] renderers;

	private float flashDuration = 0.6f;

	private IEnumerator flashRoutine;

	public bool rescanPathsAfterMoving = true;

	[Header("Audio")]
	[SerializeField]
	private AnimationCurve audioCurve;

	private EventInstance movingSound;

	public string movingSoundEventName = "event:/environment/moving_obstacle";

	private bool hasReachedEndAudio;

	[Header("PFX")]
	public GameObject[] speedTrailPFX;

	[Header("Collisions")]
	public LayerMask obstacleLayer;

	private Collider2D[] overlappingColliders = new Collider2D[16];

	private ContactFilter2D contactFilter2D;

	public float angularVelocity => 360f / rotatingDur * (MathF.PI / 180f);

	private void Start()
	{
		obstacleTransform = base.transform;
		posIndex = 0;
		collider = GetComponent<Collider2D>();
		colliderSize = collider.bounds.size;
		zPos = obstacleTransform.localPosition.z;
		reverse = false;
		isSpringingBack = false;
		contactFilter2D.SetLayerMask(obstacleLayer);
		contactFilter2D.useTriggers = false;
		if (randomizedStart && UnityEngine.Random.value > 0.5f)
		{
			posIndex = 1;
			prevBounds = collider.bounds;
			obstacleTransform.localPosition = positions[posIndex];
			GetComponent<InterpolatedTransform>().ForgetPreviousTransforms();
		}
		lastPos = obstacleTransform.position;
		renderers = GetComponentsInChildren<Renderer>();
		initColor = renderers[0].material.color;
		if (isSpring && positions.Length != 2)
		{
			Debug.LogWarning("Moving obstacle" + base.gameObject.name + "' is spring' but has " + positions.Length + " positions when it should have 2");
			isSpring = false;
		}
		if (isRotatingObstacle && rotatingDurations != null && rotatingDurations.Length > 1)
		{
			rotatingDur = rotatingDurations[posIndex];
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			scanRate = 0.5f;
			if (isRotatingObstacle)
			{
				scanRate = 0.75f;
			}
		}
		else
		{
			scanRate = 0.15f;
		}
		if (rescanInQuadrants)
		{
			boundsQueue = new Queue<Bounds>(4);
		}
	}

	protected virtual void Update()
	{
		if (movementState == MovementState.Static || movementState == MovementState.SpringPausing)
		{
			if (movingSound.isValid())
			{
				Singleton<AudioManager>.Instance.StopLooping(movingSound);
			}
			return;
		}
		if (!movingSound.isValid())
		{
			movingSound = Singleton<AudioManager>.Instance.PlayLooping(movingSoundEventName, base.transform);
		}
		if (movePercent > 0.9f && !hasReachedEndAudio)
		{
			OnReachedTarget();
			hasReachedEndAudio = true;
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/moving_obstacle_stop", base.transform.position);
		}
		if (isRotatingObstacle)
		{
			float num = angularVelocity / 25f;
			Singleton<AudioManager>.Instance.SetEventInstanceParameter(movingSound, "velocity", Vector2.Dot(obstacleTransform.up, Vector2.up) * num + num);
		}
		else
		{
			float value = audioCurve.Evaluate((Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) - preMoveDur) / moveDur);
			if (Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) < preMoveDur)
			{
				value = Easing.QuartIn(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) / preMoveDur) * 0.8f;
			}
			Singleton<AudioManager>.Instance.SetEventInstanceParameter(movingSound, "velocity", value);
		}
		if (rescanInQuadrants && boundsQueue.Count > 0)
		{
			Singleton<LevelManager>.Instance.RescanGraphArea(boundsQueue.Dequeue());
		}
	}

	private void FixedUpdate()
	{
		if (isRotatingObstacle)
		{
			movementState = MovementState.Moving;
			if (rotatingDurations != null && rotatingDurations.Length > 1)
			{
				if (rotatingDur < rotatingDurations[posIndex])
				{
					rotatingDur = Mathf.SmoothDamp(rotatingDur, rotatingDurations[posIndex], ref angularVelocityAcceleration, 0.5f);
				}
				else
				{
					rotatingDur = rotatingDurations[posIndex];
				}
			}
			obstacleTransform.Rotate(new Vector3(0f, 0f, (isRotatingClockwise ? (-1f) : 1f) * 360f / rotatingDur * Time.fixedDeltaTime));
			if (scanTimer < scanRate)
			{
				scanTimer += Time.deltaTime;
				return;
			}
			scanTimer = 0f;
			UpdatePaths();
			return;
		}
		if (movementState == MovementState.PreMoving)
		{
			if (!(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) >= preMoveDur))
			{
				velocity = Vector2.zero;
				return;
			}
			movementState = MovementState.Moving;
		}
		else if (movementState == MovementState.SpringPausing)
		{
			if (!(Singleton<TimeKeeper>.Instance.TimeSince(toggleTimestamp) >= springPauseDur))
			{
				velocity = Vector2.zero;
				return;
			}
			movementState = MovementState.Moving;
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
				obstacleTransform.localPosition = Vector2.Lerp(startPos, targetPos, Easing.EaseIn(Mathf.Clamp01(movePercent)));
			}
			else
			{
				obstacleTransform.localPosition = Vector2.Lerp(startPos, targetPos, Easing.EaseOut(Mathf.Clamp01(movePercent)));
			}
		}
		else
		{
			obstacleTransform.localPosition = targetPos;
			movementState = MovementState.Static;
			Singleton<Camera3D>.Instance.Shake(0.2f, 0.25f);
			if (isSpring && !isSpringingBack && posIndex == 1)
			{
				ToggleTargetPosition(moveOwner, springBack: true);
			}
			reverse = false;
			UpdatePaths();
		}
		Singleton<LevelManager>.Instance.level.UpdateObstacles();
		obstacleTransform.localPosition += new Vector3(0f, 0f, zPos);
		velocity = ((Vector2)obstacleTransform.position - lastPos) / Time.fixedDeltaTime;
		lastPos = obstacleTransform.position;
		if ((int)obstacleLayer != 0)
		{
			int num = Physics2D.OverlapCollider(collider, contactFilter2D, overlappingColliders);
			for (int i = 0; i < num; i++)
			{
				if (overlappingColliders[i] != null && overlappingColliders[i].CompareTag("InteractiveObstacle"))
				{
					overlappingColliders[i].GetComponent<InteractiveObstacle>().Interact(velocity);
				}
			}
		}
		Singleton<CellManager>.Instance.ExtinguishCellsTouchingCollider2D(collider);
		if (scanTimer < scanRate)
		{
			scanTimer += Time.deltaTime;
			return;
		}
		UpdatePaths();
		scanTimer = 0f;
	}

	protected virtual void OnReachedTarget()
	{
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

	public void Toggle(Actor activator = null)
	{
		hasReachedEndAudio = false;
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/moving_obstacle_stop", base.transform.position);
		if (isRotatingObstacle)
		{
			if (rotatingDurations == null || rotatingDurations.Length < 2)
			{
				ToggleRotationDirection(activator);
			}
			else
			{
				SetNextRotationSpeed(rotatingDurations, activator);
			}
		}
		else
		{
			ToggleTargetPosition(activator);
		}
	}

	private void SetNextRotationSpeed(float[] rotDurations, Actor activator = null)
	{
		posIndex = (posIndex + 1) % rotDurations.Length;
		if (activator != null && activator.character != null)
		{
			PerformObstacleFlash(activator.character.Color);
		}
	}

	private void ToggleRotationDirection(Actor activator = null)
	{
		isRotatingClockwise = !isRotatingClockwise;
		toggleTimestamp = Singleton<TimeKeeper>.Instance.CurrentTime;
		if (activator != null)
		{
			moveOwner = activator as Player;
			PerformObstacleFlash(activator.character.Color);
		}
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

	public void UpdatePaths()
	{
		if (!rescanPathsAfterMoving)
		{
			return;
		}
		newBounds = collider.bounds;
		Bounds bounds = newBounds;
		bounds.Encapsulate(prevBounds);
		if (rescanInQuadrants)
		{
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
		else
		{
			if (BoundsVolume(bounds) < BoundsVolume(newBounds) + BoundsVolume(prevBounds))
			{
				Singleton<LevelManager>.Instance.RescanGraphArea(bounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			}
			else
			{
				Singleton<LevelManager>.Instance.RescanGraphArea(prevBounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
				Singleton<LevelManager>.Instance.RescanGraphArea(newBounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			}
			prevBounds = newBounds;
		}
	}

	private static float BoundsVolume(Bounds b)
	{
		return Math.Abs(b.size.x * b.size.y);
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

	private void OnDisable()
	{
		if (movingSound.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(movingSound);
		}
	}
}
