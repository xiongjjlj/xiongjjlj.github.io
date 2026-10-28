using FMOD.Studio;
using UnityEngine;

public class TrapFloor : MonoBehaviour
{
	public enum FallState
	{
		Idle,
		Warning,
		Falling,
		Respawning,
		Rising
	}

	public enum TileSize
	{
		Small,
		Medium,
		Large
	}

	public FallState fallState;

	private float fallTimer;

	private static float fallDuration = 1f;

	private static float fallDistance = 5f;

	private float respawnTimer;

	private static float respawnDuration = 4f;

	private float risingTimer;

	private static float risingDuration = 0.5f;

	private static float risingSolidThreshold = 0.2f;

	private bool risingSolidThresholdReached;

	private float warningTimer;

	private static float warningDuration = 2.5f;

	private float shakeTimer;

	private float shakeDuration = 0.01f;

	private float flashOpacity = 0.5f;

	public GameObject floor;

	public MeshRenderer floorRenderer;

	public TileSize tileSize;

	private Vector3 floorStartPos;

	private Vector3 floorTargetPos;

	private Color floorStartColor;

	private Color floorEndColor;

	private EventInstance floorCrackingLoop;

	private Collider2D floorCollider;

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
		floorCollider = GetComponent<Collider2D>();
	}

	private void Update()
	{
		if (fallState == FallState.Warning)
		{
			warningTimer += Time.deltaTime;
			if (shakeTimer < shakeDuration)
			{
				shakeTimer += Time.deltaTime;
			}
			else
			{
				shakeTimer = 0f;
				float num = 0.25f + 0.75f * (warningTimer / warningDuration);
				floorTargetPos = floorStartPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f) * num;
			}
			floor.transform.position = Vector3.Lerp(floor.transform.position, floorTargetPos, Time.deltaTime * 100f);
			floorRenderer.material.SetColor("_FlashColor", Color.Lerp(floorStartColor, floorEndColor, Easing.CubicIn(warningTimer / warningDuration)));
			if (!(warningTimer >= warningDuration))
			{
				return;
			}
			warningTimer = 0f;
			fallState = FallState.Falling;
			base.gameObject.layer = LayerMask.NameToLayer("Default");
			Singleton<LevelManager>.Instance.RescanGraphArea(floorCollider.bounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
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
			Collider2D[] array2 = Physics2D.OverlapBoxAll(base.transform.position, GetComponent<BoxCollider2D>().size, base.transform.rotation.eulerAngles.z, 1 << LayerMask.NameToLayer("Particle3D"));
			for (int j = 0; j < array2.Length; j++)
			{
				Debris3D component = array2[j].GetComponent<Debris3D>();
				if (component != null)
				{
					component.CheckForHazards();
				}
			}
			Singleton<CellManager>.Instance.ExtinguishCellsTouchingCollider2D(floorCollider);
			Singleton<DecalManager>.Instance.RemoveDecalsTouchingCollider(floorCollider);
		}
		else if (fallState == FallState.Falling)
		{
			fallTimer += Time.deltaTime;
			float z = floor.transform.position.z;
			float num2 = 1f;
			floor.transform.position = new Vector3(floorStartPos.x, floorStartPos.y, Mathf.Lerp(floorStartPos.z, floorStartPos.z + fallDistance, Easing.EaseOut(fallTimer / fallDuration)));
			if (floor.transform.position.z > num2 && z < num2)
			{
				switch (tileSize)
				{
				case TileSize.Small:
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.TileSplash, base.transform.position);
					break;
				case TileSize.Medium:
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.TileSplashMedium, base.transform.position);
					break;
				case TileSize.Large:
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.TileSplashLarge, base.transform.position);
					break;
				}
			}
			if (!(fallTimer >= fallDuration))
			{
				return;
			}
			floor.transform.position = floorStartPos + Vector3.forward * fallDistance;
			fallTimer = 0f;
			fallState = FallState.Respawning;
			Transform[] componentsInChildren = floor.transform.GetComponentsInChildren<Transform>();
			for (int k = 0; k < componentsInChildren.Length; k++)
			{
				Projectile component2 = componentsInChildren[k].GetComponent<Projectile>();
				if (component2 != null)
				{
					Transform parent = component2.transform.parent;
					component2.transform.SetParent(null, worldPositionStays: true);
					component2.interpolatedTransform.ReparentPreviousTransforms(parent, null);
				}
				DestructableObstacle[] componentsInChildren2 = floor.GetComponentsInChildren<DestructableObstacle>();
				for (int l = 0; l < componentsInChildren2.Length; l++)
				{
					componentsInChildren2[l].TakeSinkDamage();
				}
			}
		}
		else if (fallState == FallState.Respawning)
		{
			respawnTimer += Time.deltaTime;
			floorRenderer.material.SetColor("_FlashColor", Color.Lerp(floorEndColor, floorStartColor, respawnTimer / respawnDuration));
			if (respawnTimer >= respawnDuration)
			{
				respawnTimer = 0f;
				fallState = FallState.Rising;
				risingSolidThresholdReached = false;
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/floor_rise", base.transform.position);
			}
		}
		else if (fallState == FallState.Rising)
		{
			risingTimer += Time.deltaTime;
			floor.transform.position = new Vector3(floorStartPos.x, floorStartPos.y, Mathf.Lerp(floorStartPos.z + fallDistance, floorStartPos.z, Easing.EaseOut(risingTimer / risingDuration)));
			if (risingTimer >= risingDuration)
			{
				floor.transform.position = floorStartPos;
				risingTimer = 0f;
				fallState = FallState.Idle;
				Singleton<LevelManager>.Instance.RescanGraphArea(GetComponent<Collider2D>().bounds, forceAIsToRecreatePaths: true, reconnectTeleportals: false);
			}
			else if (risingTimer >= risingSolidThreshold && !risingSolidThresholdReached)
			{
				risingSolidThresholdReached = true;
				base.gameObject.layer = LayerMask.NameToLayer("Floor");
			}
		}
	}

	public void StartFalling()
	{
		if (fallState == FallState.Idle && Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			fallState = FallState.Warning;
			floorCrackingLoop = Singleton<AudioManager>.Instance.PlayLooping("event:/environment/floor_cracking", base.transform);
			Singleton<LevelManager>.Instance.TagAreaAsOutOfBounds(base.transform.position, GetComponent<BoxCollider2D>().size * 0.5f, base.transform.rotation);
		}
	}

	private void OnDisable()
	{
		if (Singleton<AudioManager>.InstanceExists && floorCrackingLoop.isValid())
		{
			Singleton<AudioManager>.Instance.StopLooping(floorCrackingLoop);
		}
	}
}
