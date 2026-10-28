using System;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	public float radiusFinal = 5.5f;

	private float targetRadius;

	private int segments = 24;

	public float explosionDuration = 0.75f;

	private float explosionStartTime;

	private float radius;

	private bool isExploding = true;

	public float fadeDuration = 2f;

	private Vector3 position;

	private float degrees;

	public LayerMask killLayers;

	public LayerMask obstacleLayer;

	private Collider2D[] collidersInRange = new Collider2D[512];

	private List<Vector3> allCorners = new List<Vector3>(256);

	private static float timeBetweenUpdates = 0.033f;

	private float lastUpdateTimestamp;

	public Player owner;

	private List<Vector3> collisionPoints = new List<Vector3>(256);

	private List<vertex> vertexList = new List<vertex>(512);

	private List<Vector3> verticesTriangulated = new List<Vector3>(512);

	private List<Vector2> uvs = new List<Vector2>(512);

	private int[] triangles = new int[999];

	private Mesh mesh;

	public MeshFilter meshFilter;

	public Collider2D explosionCollider;

	public Renderer explosionRenderer;

	public AnimationCurve radiusCurve;

	public Gradient gradient;

	private MaterialPropertyBlock materialPropertyBlock;

	private int tintShaderPropertyID;

	private static float fireDelay = 0.25f;

	public bool isFireExplosion;

	private bool isMiniExplosion;

	[SerializeField]
	private bool hasSpawnedFireCells;

	public string explosionSoundBig;

	public string explosionSoundMini;

	private void Start()
	{
		materialPropertyBlock = new MaterialPropertyBlock();
		tintShaderPropertyID = Shader.PropertyToID("_TintColor");
	}

	public void Init(bool isMiniExplosion = false)
	{
		explosionStartTime = Singleton<TimeKeeper>.Instance.CurrentTime;
		this.isMiniExplosion = isMiniExplosion;
		hasSpawnedFireCells = false;
		targetRadius = radiusFinal;
		if (isMiniExplosion)
		{
			targetRadius *= 0.5f;
			segments = ((Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch) ? 16 : 32);
			Singleton<AudioManager>.Instance.PlayOneShotAt(explosionSoundMini, base.transform.position);
			Singleton<Camera3D>.Instance.Shake(1f, 0.75f);
		}
		else
		{
			segments = ((Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch) ? 24 : 48);
			Singleton<AudioManager>.Instance.PlayOneShotAt(explosionSoundBig, base.transform.position);
			Singleton<Camera3D>.Instance.Shake(2f, 0.75f);
		}
		if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() > 0)
		{
			Singleton<Camera3D>.Instance.AddCameraTarget(base.transform, 0.4f);
		}
		degrees = 360f / (float)segments;
		radius = 0.1f;
		lastUpdateTimestamp = 0f;
		UpdateExplosionMesh();
		WobbleInteractiveObstaclesInRange();
		isExploding = true;
	}

	private void Update()
	{
		RunTimers();
	}

	private void UpdateExplosionMesh()
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(lastUpdateTimestamp) < timeBetweenUpdates)
			{
				return;
			}
			lastUpdateTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
		if (Singleton<LevelManager>.Instance.level.allMovingFloors.Length != 0 || Singleton<LevelManager>.Instance.level.isFallingApart)
		{
			Singleton<LevelManager>.Instance.level.UpdateObstacles();
		}
		position = base.transform.position;
		if ((bool)Physics2D.OverlapPoint(position, 1 << LayerMask.NameToLayer("Obstacle")))
		{
			isExploding = false;
			return;
		}
		allCorners = Singleton<LevelManager>.Instance.level.allCorners;
		collisionPoints.Clear();
		RaycastAtCorners();
		RaycastInCircle();
		CompileVertices();
		GenerateMesh();
	}

	private void RaycastAtCorners()
	{
		foreach (Vector3 allCorner in allCorners)
		{
			Vector3 vector = allCorner;
			vector.z = base.transform.position.z;
			Vector3 normalized = (vector - base.transform.position).normalized;
			if ((vector - base.transform.position).magnitude > radius)
			{
				vector = base.transform.TransformPoint(normalized * radius);
			}
			RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, normalized, (vector - base.transform.position).magnitude, obstacleLayer);
			Vector3 item = vector;
			if ((bool)raycastHit2D)
			{
				item = raycastHit2D.point;
			}
			collisionPoints.Add(item);
		}
	}

	private void RaycastInCircle()
	{
		for (int i = 0; i < segments; i++)
		{
			Vector3 vector = VectorExt.VectorFromAngle(degrees * (float)i);
			RaycastHit2D raycastHit2D = Physics2D.Raycast(position, vector, radius, obstacleLayer);
			Vector3 item = position + vector * radius;
			item.z = position.z;
			if ((bool)raycastHit2D)
			{
				item = raycastHit2D.point;
			}
			collisionPoints.Add(item);
		}
	}

	private void CompileVertices()
	{
		vertex item = default(vertex);
		vertexList.Clear();
		for (int i = 0; i < collisionPoints.Count; i++)
		{
			item.point = new Vector3(collisionPoints[i].x, collisionPoints[i].y, position.z) - position;
			Vector2 vector = collisionPoints[i] - base.transform.position;
			item.angle = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
			vertexList.Add(item);
		}
		vertexList.Sort((vertex vertex3, vertex vertex2) => vertex2.angle.CompareTo(vertex3.angle));
	}

	private void GenerateMesh()
	{
		verticesTriangulated.Clear();
		verticesTriangulated.Add(base.transform.InverseTransformPoint(base.transform.position));
		for (int i = 0; i < vertexList.Count; i++)
		{
			verticesTriangulated.Add(vertexList[i].point);
		}
		int num = 0;
		int num2 = 0;
		if (triangles.Length < vertexList.Count * 3)
		{
			Array.Resize(ref triangles, vertexList.Count * 3);
		}
		for (int j = 0; j < vertexList.Count * 3; j += 3)
		{
			triangles[j] = 0;
			triangles[j + 1] = num + 1;
			if (j == vertexList.Count * 3 - 3)
			{
				triangles[j + 2] = 1;
			}
			else
			{
				triangles[j + 2] = num + 2;
			}
			num++;
			num2 = j;
		}
		for (int k = num2 + 1; k < triangles.Length; k++)
		{
			triangles[k] = 0;
		}
		if (mesh == null)
		{
			mesh = new Mesh();
			meshFilter.mesh = mesh;
		}
		mesh.Clear();
		mesh.SetVertices(verticesTriangulated);
		uvs.Clear();
		for (int l = 0; l < verticesTriangulated.Count; l++)
		{
			uvs.Add((Vector2)verticesTriangulated[l] / (radius * 2f));
		}
		mesh.SetUVs(0, uvs);
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private void KillPlayersInRange()
	{
		int num = Physics2D.OverlapCircleNonAlloc(base.transform.position, radius, collidersInRange, killLayers);
		for (int i = 0; i < num; i++)
		{
			if ((bool)Physics2D.Raycast(base.transform.position, (collidersInRange[i].transform.position - base.transform.position).normalized, (collidersInRange[i].transform.position - base.transform.position).magnitude, obstacleLayer))
			{
				continue;
			}
			if (collidersInRange[i].CompareTag("DamageCollider") || collidersInRange[i].CompareTag("ShieldCollider") || collidersInRange[i].CompareTag("Plant"))
			{
				CollisionBehaviour component = collidersInRange[i].GetComponent<CollisionBehaviour>();
				if (component != null)
				{
					component.OnTriggerStay2D(explosionCollider);
				}
			}
			else if (collidersInRange[i].CompareTag("Powerup"))
			{
				collidersInRange[i].GetComponent<Powerup>().OnTriggerEnter2D(explosionCollider);
			}
		}
	}

	private void WobbleInteractiveObstaclesInRange()
	{
		if (Singleton<LevelManager>.Instance.level == null)
		{
			return;
		}
		for (int i = 0; i < Singleton<LevelManager>.Instance.level.allInteractiveObstacles.Count; i++)
		{
			if (Singleton<LevelManager>.Instance.level.allInteractiveObstacles[i] is WobblyObstacle)
			{
				Vector2 vector = Singleton<LevelManager>.Instance.level.allInteractiveObstacles[i].transform.position - base.transform.position;
				if (vector.magnitude <= targetRadius)
				{
					Singleton<LevelManager>.Instance.level.allInteractiveObstacles[i].Interact(-vector.normalized * 50f);
				}
			}
		}
	}

	private void RunTimers()
	{
		materialPropertyBlock.Clear();
		materialPropertyBlock.SetColor(tintShaderPropertyID, gradient.Evaluate(Singleton<TimeKeeper>.Instance.TimeSince(explosionStartTime) / explosionDuration));
		explosionRenderer.SetPropertyBlock(materialPropertyBlock);
		if ((bool)Physics2D.OverlapPoint(base.transform.position, obstacleLayer))
		{
			isExploding = false;
		}
		if (isExploding && Singleton<TimeKeeper>.Instance.TimeSince(explosionStartTime) < explosionDuration)
		{
			radius = radiusCurve.Evaluate(Mathf.Clamp01(Singleton<TimeKeeper>.Instance.TimeSince(explosionStartTime) / explosionDuration)) * targetRadius;
			UpdateExplosionMesh();
			if (Singleton<TimeKeeper>.Instance.TimeSince(explosionStartTime) < explosionDuration - fadeDuration)
			{
				KillPlayersInRange();
			}
		}
		else
		{
			isExploding = false;
			Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
			Singleton<GameManager>.Instance.activeExplosives.Remove(base.gameObject);
			base.gameObject.Recycle();
		}
		if (isExploding && isFireExplosion && Singleton<TimeKeeper>.Instance.TimeSince(explosionStartTime) >= fireDelay && !hasSpawnedFireCells)
		{
			hasSpawnedFireCells = true;
			int num = (isMiniExplosion ? 40 : 80);
			float num2 = 360f / (float)num;
			float num3 = targetRadius - 1f;
			for (int i = 0; i < num; i++)
			{
				Vector2 normalized = VectorExt.VectorFromAngle(num2 * (float)i).normalized;
				RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, normalized, num3, 1 << LayerMask.NameToLayer("Obstacle"));
				Vector3 vector = ((!raycastHit2D) ? ((Vector3)((Vector2)base.transform.position + normalized * num3)) : ((Vector3)(raycastHit2D.point - normalized)));
				Singleton<CellManager>.Instance.RegisterCell(CellManager.CellType.Fire, vector, UnityEngine.Random.Range(1.6f, 1.9f), owner);
			}
		}
	}
}
