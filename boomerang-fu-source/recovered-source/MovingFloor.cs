using System;
using FMOD.Studio;
using Pathfinding;
using Pathfinding.Util;
using UnityEngine;

public class MovingFloor : MonoBehaviour
{
	public enum MovementType
	{
		linear,
		circular,
		eased,
		supercircular,
		easedsoft
	}

	public int posIndex;

	public Vector3[] positionRotations;

	public bool useRotations;

	public bool counterClockwise;

	public MovementType movementType;

	protected Vector2 startPos;

	protected Vector2 targetPos;

	protected float zPos;

	protected float startRot;

	protected float targetRot;

	protected Vector2 lastPos;

	public Vector2 velocity;

	protected Transform tr;

	public Collider2D collider;

	public Collider2D[] childColliders;

	protected static float updateError = 0.25f;

	protected static float scanRate = 0.05f;

	protected float scanTimer;

	protected Bounds newBounds;

	protected Bounds prevBounds;

	protected Bounds merged;

	protected Quaternion newRotation;

	protected Quaternion prevRotation;

	protected Vector3 minDiff;

	protected Vector3 maxDiff;

	public float movePercent;

	protected float moveDur;

	protected float newMoveDur;

	public float moveDurMin = 0.5f;

	public float moveDurMax = 0.5f;

	public bool isMoving;

	[Header("Audio")]
	private EventInstance movingSound;

	public string movingSoundEventName = string.Empty;

	private Matrix4x4 originalMatrix;

	public GraphMask graphMask;

	private Vector3 originalPosition;

	private Quaternion originalRotation;

	public float cellGridWidth;

	public float cellGridHeight;

	public CellManager.Cell[][] cellGrid;

	public GraphTransform graphTransformation { get; private set; }

	protected virtual void Start()
	{
		tr = base.transform;
		collider = GetComponent<Collider2D>();
		zPos = tr.localPosition.z;
		isMoving = false;
		lastPos = tr.position;
		moveDur = moveDurMin;
		scanTimer = UnityEngine.Random.Range((0f - scanRate) * 0.75f, 0f);
		originalMatrix = base.transform.worldToLocalMatrix;
		originalPosition = base.transform.position;
		originalRotation = base.transform.rotation;
		base.transform.hasChanged = true;
		UpdateGraphPosition();
		int num = Mathf.RoundToInt(cellGridHeight / CellManager.cellSize);
		int num2 = Mathf.RoundToInt(cellGridWidth / CellManager.cellSize);
		cellGrid = new CellManager.Cell[num][];
		for (int i = 0; i < cellGrid.Length; i++)
		{
			cellGrid[i] = new CellManager.Cell[num2];
			for (int j = 0; j < cellGrid[i].Length; j++)
			{
				cellGrid[i][j] = new CellManager.Cell();
			}
		}
	}

	public virtual void StartMoving()
	{
		ToggleTargetPosition();
		if (movingSoundEventName != string.Empty && !movingSound.isValid())
		{
			movingSound = Singleton<AudioManager>.Instance.PlayLooping(movingSoundEventName, base.transform);
			Singleton<AudioManager>.Instance.SetEventInstanceParameter(movingSound, "velocity", 0.3f);
		}
	}

	protected virtual void Update()
	{
		if (isMoving && (Singleton<SettingsManager>.Instance.numBots != 0 || Singleton<LevelManager>.Instance.numEnemies != 0 || Singleton<GameManager>.Instance.decoysInMatch))
		{
			UpdateGraphPosition();
		}
	}

	protected void UpdateGraphPosition()
	{
		if (base.transform.hasChanged)
		{
			graphTransformation = new GraphTransform(base.transform.localToWorldMatrix * originalMatrix);
			base.transform.hasChanged = false;
		}
	}

	public virtual Vector3 GetInverseTransformPoint(Vector3 point)
	{
		UpdateGraphPosition();
		return graphTransformation.InverseTransform(point);
	}

	public virtual Vector3 GetTransformPoint(Vector3 point)
	{
		UpdateGraphPosition();
		return graphTransformation.Transform(point);
	}

	protected virtual void FixedUpdate()
	{
		if (!isMoving)
		{
			return;
		}
		if (movePercent < 1f)
		{
			moveDur = Mathf.Lerp(moveDur, newMoveDur, Time.fixedDeltaTime);
			movePercent += Time.fixedDeltaTime / moveDur;
			switch (movementType)
			{
			case MovementType.linear:
				tr.localPosition = Vector2.Lerp(startPos, targetPos, movePercent);
				break;
			case MovementType.circular:
				tr.localPosition = new Vector2(Mathf.Lerp(startPos.x, targetPos.x, (posIndex % 2 == 0) ? Easing.EaseIn(movePercent) : Easing.EaseOut(movePercent)), Mathf.Lerp(startPos.y, targetPos.y, (posIndex % 2 == 0) ? Easing.EaseOut(movePercent) : Easing.EaseIn(movePercent)));
				break;
			case MovementType.supercircular:
			{
				float t = Easing.SubtleOutIn(movePercent);
				tr.localPosition = Vector2.Lerp(new Vector2(Mathf.Lerp(startPos.x, startPos.x + targetPos.x, Easing.EaseInOut(movePercent)), Mathf.Lerp(startPos.y, startPos.y + targetPos.y, Easing.EaseInOut(movePercent))), new Vector2(Mathf.Lerp(startPos.x + targetPos.x, targetPos.x, Easing.EaseInOut(movePercent)), Mathf.Lerp(startPos.y + targetPos.y, targetPos.y, Easing.EaseInOut(movePercent))), t);
				break;
			}
			case MovementType.eased:
				tr.localPosition = new Vector2(Mathf.Lerp(startPos.x, targetPos.x, Easing.CubicInOut(movePercent)), Mathf.Lerp(startPos.y, targetPos.y, Easing.EaseInOut(movePercent)));
				break;
			case MovementType.easedsoft:
				tr.localPosition = new Vector2(Mathf.Lerp(startPos.x, targetPos.x, Easing.EaseInOut(movePercent)), Mathf.Lerp(startPos.y, targetPos.y, Easing.EaseInOut(movePercent)));
				break;
			}
			if (useRotations)
			{
				tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRot, targetRot, movePercent));
			}
		}
		else
		{
			tr.localPosition = targetPos;
			if (useRotations)
			{
				tr.localRotation = Quaternion.Euler(0f, 0f, targetRot);
			}
			ToggleTargetPosition();
		}
		tr.localPosition += new Vector3(0f, 0f, zPos);
		velocity = ((Vector2)tr.position - lastPos) / Time.fixedDeltaTime;
		lastPos = tr.position;
	}

	public void ToggleTargetPosition()
	{
		newMoveDur = UnityEngine.Random.Range(moveDurMin, moveDurMax);
		startPos = positionRotations[posIndex];
		if (useRotations)
		{
			startRot = positionRotations[posIndex].z;
			if (startRot == 0f && counterClockwise)
			{
				startRot = 360f;
			}
		}
		posIndex = (posIndex + 1) % positionRotations.Length;
		targetPos = positionRotations[posIndex];
		if (useRotations)
		{
			targetRot = positionRotations[posIndex].z;
			if (targetRot == 0f && base.transform.localRotation.eulerAngles.z > 180f && !counterClockwise)
			{
				targetRot = 360f;
			}
			movePercent = (base.transform.localRotation.eulerAngles.z - startRot % 360f) / (startRot - targetRot);
		}
		else
		{
			movePercent = ((Vector2)tr.localPosition - startPos).magnitude / (startPos - targetPos).magnitude;
		}
		isMoving = true;
	}

	protected static float BoundsVolume(Bounds b)
	{
		return Math.Abs(b.size.x * b.size.y * b.size.z);
	}

	private void OnDisable()
	{
		if (movingSound.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(movingSound);
		}
	}
}
