using FMOD.Studio;
using UnityEngine;

public class FallingFloor : MonoBehaviour
{
	public enum FallState
	{
		Idle,
		Warning,
		Falling,
		Fallen
	}

	public FallState fallState;

	private float fallTimer;

	private static float fallDuration = 0.75f;

	private static float fallDistance = 6f;

	private float warningTimer;

	private static float warningDuration = 2f;

	private float shakeTimer;

	private float shakeDuration = 0.01f;

	private float flashOpacity = 0.5f;

	public GameObject floor;

	public MeshRenderer floorRenderer;

	private Vector3 floorStartPos;

	private Vector3 floorTargetPos;

	private Color floorStartColor;

	private Color floorEndColor;

	private EventInstance floorCrackingLoop;

	private void Start()
	{
		floorStartPos = floor.transform.position;
		floorTargetPos = floorStartPos;
		if (floorRenderer == null)
		{
			floorRenderer = floor.GetComponent<MeshRenderer>();
		}
		floorStartColor = floorRenderer.material.GetColor("_FlashColor");
		floorEndColor = floorStartColor;
		floorEndColor.a = flashOpacity;
	}

	private void Update()
	{
		if (fallState == FallState.Warning)
		{
			warningTimer += Time.deltaTime;
			if (!Singleton<LevelManager>.Instance.level.isFallingApart)
			{
				if (floorCrackingLoop.isValid())
				{
					Singleton<AudioManager>.Instance.StopLooping(floorCrackingLoop);
				}
				return;
			}
			if (shakeTimer < shakeDuration)
			{
				shakeTimer += Time.deltaTime;
			}
			else
			{
				shakeTimer = 0f;
				floorTargetPos = floorStartPos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
			}
			floor.transform.position = Vector3.Lerp(floor.transform.position, floorTargetPos, Time.deltaTime * 100f);
			floorRenderer.material.SetColor("_FlashColor", Color.Lerp(floorStartColor, floorEndColor, warningTimer / warningDuration));
			if (!(warningTimer >= warningDuration))
			{
				return;
			}
			fallState = FallState.Falling;
			base.gameObject.layer = LayerMask.NameToLayer("Default");
			Singleton<LevelManager>.Instance.RescanGraphArea(GetComponent<Collider2D>().bounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/floor_sink", base.transform.position);
			if (floorCrackingLoop.isValid())
			{
				Singleton<AudioManager>.Instance.StopLooping(floorCrackingLoop);
			}
			Collider2D[] array = Physics2D.OverlapBoxAll(base.transform.position, GetComponent<BoxCollider2D>().size, base.transform.rotation.eulerAngles.z, 1 << LayerMask.NameToLayer("Collectables"));
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].CompareTag("Powerup"))
				{
					array[i].GetComponent<Powerup>().FallInHazard();
				}
			}
			Collider2D component = GetComponent<Collider2D>();
			Singleton<CellManager>.Instance.ExtinguishCellsTouchingCollider2D(component);
			Singleton<DecalManager>.Instance.RemoveDecalsTouchingCollider(component);
		}
		else
		{
			if (fallState != FallState.Falling)
			{
				return;
			}
			fallTimer += Time.deltaTime;
			floor.transform.position = new Vector3(floorStartPos.x, floorStartPos.y, Mathf.Lerp(floorStartPos.z, floorStartPos.z + fallDistance, Easing.EaseOut(fallTimer / fallDuration)));
			if (!(fallTimer >= fallDuration))
			{
				return;
			}
			fallState = FallState.Fallen;
			floorRenderer.enabled = false;
			Transform[] componentsInChildren = floor.transform.GetComponentsInChildren<Transform>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				Projectile component2 = componentsInChildren[j].GetComponent<Projectile>();
				if (component2 != null)
				{
					Transform parent = component2.transform.parent;
					component2.transform.SetParent(null, worldPositionStays: true);
					component2.interpolatedTransform.ReparentPreviousTransforms(parent, null);
				}
				componentsInChildren[j].gameObject.SetActive(value: false);
			}
		}
	}

	public void StartFalling()
	{
		fallState = FallState.Warning;
		floorCrackingLoop = Singleton<AudioManager>.Instance.PlayLooping("event:/environment/floor_cracking", base.transform);
		Singleton<LevelManager>.Instance.TagAreaAsOutOfBounds(base.transform.position, GetComponent<BoxCollider2D>().size * 0.5f, base.transform.rotation);
	}

	private void OnDisable()
	{
		if (floorCrackingLoop.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(floorCrackingLoop);
		}
	}
}
