using System.Collections;
using UnityEngine;

public class MovingDoor : MonoBehaviour
{
	public Renderer doorRenderer;

	public Collider2D doorCollider;

	[SerializeField]
	private float movePercent;

	public float moveDur = 0.25f;

	public bool isMoving;

	public Player moveOwner;

	public bool isClosed;

	public bool canClose;

	public float closedZ;

	public float openZ;

	private Transform obstacleTransform;

	private Vector3 startPos;

	private Vector3 targetPos;

	private Bounds doorBounds;

	private float doorHeight;

	private Bounds newBounds;

	private Bounds prevBounds;

	private Color initColor;

	private Color activatorColor;

	private float flashDuration = 0.6f;

	private IEnumerator flashRoutine;

	private Collider2D touchingColliders;

	private void Start()
	{
		obstacleTransform = base.transform;
		startPos = obstacleTransform.localPosition;
		targetPos = obstacleTransform.localPosition;
		if (!doorCollider)
		{
			doorCollider = GetComponentInChildren<Collider2D>();
		}
		if (!doorRenderer)
		{
			doorRenderer = GetComponentInChildren<Renderer>();
		}
		initColor = doorRenderer.material.color;
		doorHeight = doorRenderer.bounds.extents.z;
		doorBounds = doorCollider.bounds;
	}

	private void FixedUpdate()
	{
		if (isMoving)
		{
			if (movePercent < 1f)
			{
				movePercent += Time.fixedDeltaTime / moveDur;
				obstacleTransform.localPosition = Vector3.Lerp(startPos, targetPos, Easing.EaseIn(movePercent));
			}
			else
			{
				obstacleTransform.localPosition = targetPos;
				isMoving = false;
			}
			if (!doorCollider.enabled && base.transform.position.z - doorHeight < 0f)
			{
				doorCollider.enabled = true;
				doorRenderer.enabled = true;
				Singleton<LevelManager>.Instance.level.UpdateObstacles();
				Singleton<LevelManager>.Instance.RescanGraphArea(doorBounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			}
			else if (doorCollider.enabled && base.transform.position.z - doorHeight > 0f)
			{
				doorCollider.enabled = false;
				doorRenderer.enabled = false;
				Singleton<LevelManager>.Instance.level.UpdateObstacles();
				Singleton<LevelManager>.Instance.RescanGraphArea(doorBounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			}
		}
	}

	public void ToggleTargetPosition(Actor activator = null)
	{
		if (isClosed)
		{
			startPos = new Vector3(startPos.x, startPos.y, closedZ);
			targetPos = new Vector3(startPos.x, startPos.y, openZ);
			isClosed = false;
		}
		else
		{
			if (!canClose)
			{
				return;
			}
			startPos = new Vector3(startPos.x, startPos.y, openZ);
			targetPos = new Vector3(startPos.x, startPos.y, closedZ);
			isClosed = true;
		}
		if (isMoving)
		{
			movePercent = 1f - movePercent;
		}
		else
		{
			movePercent = 0f;
		}
		isMoving = true;
		if (activator != null)
		{
			activatorColor = activator.character.Color;
			if (flashRoutine != null)
			{
				StopCoroutine(flashRoutine);
			}
			flashRoutine = FlashColor();
			StartCoroutine(flashRoutine);
		}
	}

	private IEnumerator FlashColor()
	{
		doorRenderer.material.SetColor("_Color", activatorColor);
		float t = 0f;
		while (t < flashDuration)
		{
			t += Time.deltaTime;
			doorRenderer.material.SetColor("_Color", Color.Lerp(activatorColor, initColor, Easing.EaseOut(t / flashDuration)));
			yield return null;
		}
		doorRenderer.material.SetColor("_Color", initColor);
	}

	private void KillEverythingTouching()
	{
	}

	private void OnDestroy()
	{
	}
}
