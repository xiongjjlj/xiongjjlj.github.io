using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FoliagePatch : MonoBehaviour
{
	[Serializable]
	public struct GridCell
	{
		public Vector3 center;

		public bool active;
	}

	[Serializable]
	public struct FoliageCell
	{
		public int cluster;

		public Vector3 center;

		public Vector3 worldPos;

		public Vector3 pushDirection;

		public Vector3 prevPushDirection;

		public Vector3 pushIntensity;

		public Vector3 displaceVector;

		public Color32 displaceVectorColor32;

		public float pushStrength;

		public float activation;

		public int[] vertIndices;

		public bool onFire;

		public float burnTimer;
	}

	public struct Influencer
	{
		public Transform transform;

		public Vector3 position;

		public Vector3 patchRelativePos;

		public Vector3 velocity;

		public float sqrSpeed;

		public float speedRatio;

		public Vector3 prevPos;
	}

	[Header("Grid Settings")]
	[SerializeField]
	private Mesh tileMeshPrefab;

	[SerializeField]
	private float meshScale = 0.5f;

	[SerializeField]
	private int randomSeed;

	public int xTileCount = 5;

	public int yTileCount = 5;

	public float xSpacing = 1f;

	public float ySpacing = 1f;

	public float yOffset = 0.5f;

	[Header("UV Settings")]
	[SerializeField]
	private float vOffset = 0.5f;

	[SerializeField]
	private float xTextureTiles = 4f;

	[SerializeField]
	private float yTextureTiles = 4f;

	[Header("Influencers")]
	[SerializeField]
	private float effectDistance = 1.5f;

	[SerializeField]
	private float influenceMinSpeed = 0.005f;

	[SerializeField]
	private float influenceMaxSpeed = 2f;

	[Header("Movement")]
	[SerializeField]
	private float activationIncreaseRate = 6f;

	[SerializeField]
	private float activationDecreaseRate = 1.25f;

	[SerializeField]
	private float cellBounciness = 14f;

	private int clusterSize = 6;

	private Vector3 vec3zero;

	private Vector3 vec3one;

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private Transform trans;

	[HideInInspector]
	[SerializeField]
	private bool[] activePoints;

	private int activePointCount;

	[HideInInspector]
	private FoliageCell[] cells;

	[HideInInspector]
	private Color32[] colors32;

	private int numActiveCells;

	private Influencer[] influencers;

	private float groundHeight;

	private bool rootMoved;

	private Vector3 rootPos;

	private Vector3 prevRootPos;

	private Bounds totalGridBounds;

	private const float totalBoundsPadding = 0.5f;

	private int clusterTotalX;

	private int clusterTotalY;

	private bool[] activatedClusters;

	private Bounds[] clusterBounds;

	private const float clusterBoundsPadding = 1f;

	private bool anyInfluencerInBounds;

	private float effectDistanceSqr;

	private float influenceMinSpeedSqr;

	private float influenceMaxSpeedSqr;

	private List<Transform> burnTargets = new List<Transform>();

	private List<Transform> cutTargets = new List<Transform>();

	private string text;

	private void Awake()
	{
		GeneratePatch();
		effectDistanceSqr = effectDistance * effectDistance;
		influenceMaxSpeedSqr = influenceMaxSpeed * influenceMaxSpeed;
		influenceMinSpeedSqr = influenceMinSpeed * influenceMinSpeed;
	}

	private void Update()
	{
		if (influencers != null && influencers.Length != 0)
		{
			rootPos = trans.position;
			rootPos.z = groundHeight;
			if (rootPos != prevRootPos)
			{
				rootMoved = true;
				prevRootPos = rootPos;
			}
			else
			{
				rootMoved = false;
			}
			for (int i = 0; i < activatedClusters.Length; i++)
			{
				activatedClusters[i] = false;
			}
			UpdateInfluencers();
			UpdateCells();
			meshFilter.mesh.colors32 = colors32;
		}
	}

	private void UpdateInfluencers()
	{
		anyInfluencerInBounds = false;
		for (int i = 0; i < influencers.Length; i++)
		{
			influencers[i].position = influencers[i].transform.position;
			influencers[i].position.z = rootPos.z;
			if (!totalGridBounds.Contains(influencers[i].position))
			{
				continue;
			}
			influencers[i].patchRelativePos = influencers[i].position - rootPos;
			influencers[i].velocity = influencers[i].position - influencers[i].prevPos;
			influencers[i].prevPos = influencers[i].position;
			influencers[i].sqrSpeed = Get2DSquareDistance(influencers[i].velocity.x, influencers[i].velocity.y);
			influencers[i].speedRatio = Mathf.Clamp01(influencers[i].sqrSpeed / influenceMaxSpeedSqr);
			for (int j = 0; j < clusterBounds.Length; j++)
			{
				if (clusterBounds[j].Contains(influencers[i].position))
				{
					activatedClusters[j] = true;
					break;
				}
			}
			anyInfluencerInBounds = true;
		}
	}

	private void UpdateCells()
	{
		if (!anyInfluencerInBounds && numActiveCells <= 0)
		{
			numActiveCells = 0;
			return;
		}
		for (int i = 0; i < cells.Length; i++)
		{
			if (!activatedClusters[cells[i].cluster])
			{
				continue;
			}
			if (activePoints[i])
			{
				if (rootMoved)
				{
					cells[i].worldPos = new Vector3(cells[i].center.x + rootPos.x, cells[i].center.y + rootPos.y, cells[i].center.z + rootPos.z);
				}
				cells[i].pushDirection = vec3zero;
				cells[i].pushStrength = 0f;
				for (int j = 0; j < influencers.Length; j++)
				{
					if (influencers[j].sqrSpeed >= influenceMinSpeedSqr)
					{
						Vector3 vector = new Vector3(cells[i].worldPos.x - influencers[j].position.x, cells[i].worldPos.y - influencers[j].position.y, cells[i].worldPos.z - influencers[j].position.z);
						float num = Get2DSquareDistance(vector.x, vector.y);
						if (!(num > effectDistanceSqr))
						{
							float num2 = 1f - Mathf.Clamp01(num / effectDistanceSqr);
							vector.Normalize();
							vector.x *= num2;
							vector.y *= num2;
							vector.z *= num2;
							cells[i].pushDirection = new Vector3(cells[i].pushDirection.x + vector.x, cells[i].pushDirection.y + vector.y, cells[i].pushDirection.z + vector.z);
							cells[i].pushStrength = cells[i].pushStrength + num2 * influencers[j].speedRatio;
						}
					}
				}
				cells[i].pushStrength = Mathf.Clamp01(cells[i].pushStrength);
				if (cells[i].pushStrength > 0.0125f)
				{
					if (cells[i].activation < 1f)
					{
						cells[i].prevPushDirection = cells[i].pushDirection;
						if (cells[i].activation <= 0f)
						{
							numActiveCells++;
						}
						cells[i].activation += Time.deltaTime * (1f - cells[i].pushStrength) * activationIncreaseRate;
						if (cells[i].activation >= 1f)
						{
							cells[i].activation = 1f;
						}
						UpdateVertexColor(i, _resting: false);
					}
				}
				else if (cells[i].activation > 0f)
				{
					cells[i].activation -= Time.deltaTime * activationDecreaseRate;
					if (cells[i].activation <= 0f)
					{
						cells[i].activation = 0f;
						numActiveCells--;
					}
					UpdateVertexColor(i, _resting: true);
				}
			}
			else if (cells[i].activation > 0f)
			{
				cells[i].activation -= Time.deltaTime * activationDecreaseRate;
				if (cells[i].activation <= 0f)
				{
					cells[i].activation = 0f;
					numActiveCells--;
				}
				UpdateVertexColor(i, _resting: true);
			}
		}
	}

	private void UpdateBurnTargets()
	{
	}

	private void UpdateCutTargets()
	{
	}

	public void SetInfluencers(Transform[] _influencerTransforms)
	{
		int num = _influencerTransforms.Length;
		influencers = new Influencer[num];
		for (int i = 0; i < num; i++)
		{
			influencers[i] = default(Influencer);
			influencers[i].transform = _influencerTransforms[i];
			influencers[i].velocity = Vector3.zero;
			influencers[i].sqrSpeed = 0f;
			influencers[i].prevPos = influencers[i].velocity;
		}
	}

	private void UpdateVertexColor(int _cellIndex, bool _resting)
	{
		if (_resting)
		{
			cells[_cellIndex].displaceVector = VectorToColorRange(cells[_cellIndex].prevPushDirection) * CalculateBounce(cells[_cellIndex].activation);
		}
		else
		{
			cells[_cellIndex].displaceVector = VectorToColorRange(cells[_cellIndex].pushDirection);
		}
		cells[_cellIndex].displaceVectorColor32 = ColorToColor32(cells[_cellIndex].displaceVector.x, cells[_cellIndex].displaceVector.y, cells[_cellIndex].displaceVector.z, cells[_cellIndex].activation);
		for (int i = 0; i < cells[_cellIndex].vertIndices.Length; i++)
		{
			colors32[cells[_cellIndex].vertIndices[i]] = cells[_cellIndex].displaceVectorColor32;
		}
	}

	private float CalculateBounce(float _activation)
	{
		return Mathf.Cos(_activation * cellBounciness) * 0.25f + 0.5f;
	}

	private Vector3 VectorToColorRange(Vector3 _vec)
	{
		return (_vec + vec3one) * 0.5f;
	}

	private Color32 ColorToColor32(float _r, float _g, float _b, float _a)
	{
		return new Color32((byte)(_r * 255f), (byte)(_g * 255f), (byte)(_b * 255f), (byte)(_a * 255f));
	}

	public void AddCutTarget(Transform _cutTarget)
	{
		bool flag = false;
		for (int i = 0; i < cutTargets.Count; i++)
		{
			if (cutTargets[i] == _cutTarget)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			cutTargets.Add(_cutTarget);
		}
	}

	public void ClearCutTarget(Transform _cutTarget)
	{
		for (int i = 0; i < cutTargets.Count; i++)
		{
			if (cutTargets[i] == _cutTarget)
			{
				cutTargets.Remove(_cutTarget);
				break;
			}
		}
	}

	public void AddBurnTarget(Transform _burnTarget)
	{
		bool flag = false;
		for (int i = 0; i < burnTargets.Count; i++)
		{
			if (burnTargets[i] == _burnTarget)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			burnTargets.Add(_burnTarget);
		}
	}

	public void ClearBurnTarget(Transform _burnTarget)
	{
		for (int i = 0; i < burnTargets.Count; i++)
		{
			if (burnTargets[i] == _burnTarget)
			{
				burnTargets.Remove(_burnTarget);
				break;
			}
		}
	}

	public void GeneratePatch(bool[] _activePoints = null)
	{
		if (_activePoints != null)
		{
			SetActivePoints(_activePoints);
		}
		vec3one = Vector3.one;
		vec3zero = Vector3.zero;
		numActiveCells = 0;
		if (trans == null)
		{
			trans = base.transform;
		}
		if (meshRenderer == null)
		{
			meshRenderer = GetComponent<MeshRenderer>();
		}
		if (meshFilter == null)
		{
			meshFilter = GetComponent<MeshFilter>();
		}
		rootPos = trans.position;
		rootPos.z = groundHeight;
		CalculateClusterBounds();
		activatedClusters = new bool[clusterTotalX * clusterTotalY];
		for (int i = 0; i < activatedClusters.Length; i++)
		{
			activatedClusters[i] = false;
		}
		Mesh mesh = new Mesh();
		int num = 0;
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		List<int> list3 = new List<int>();
		List<Color32> list4 = new List<Color32>();
		cells = new FoliageCell[xTileCount * yTileCount];
		int num2 = 0;
		System.Random random = new System.Random(randomSeed);
		for (int j = 0; j < xTileCount; j++)
		{
			for (int k = 0; k < yTileCount; k++)
			{
				if (!activePoints[num2])
				{
					num2++;
					continue;
				}
				int num3 = Mathf.FloorToInt((float)j / (float)xTileCount * (float)clusterTotalX);
				int num4 = num3 % clusterTotalX;
				cells[num2].cluster = num3 + num4;
				cells[num2].center = new Vector3((float)j * xSpacing, (float)k * ySpacing, 0f);
				if (k % 2 == 1)
				{
					cells[num2].center.x += yOffset;
				}
				cells[j].worldPos = rootPos + cells[j].center;
				Vector2 vector = new Vector2(((float)j + (float)random.Next(0, 8)) / xTextureTiles, ((float)k + (float)(random.Next(0, 8) * 2)) / yTextureTiles);
				cells[num2].vertIndices = new int[tileMeshPrefab.vertices.Length];
				for (int l = 0; l < cells[num2].vertIndices.Length; l++)
				{
					Vector3 item = tileMeshPrefab.vertices[l];
					item *= meshScale;
					item += cells[num2].center;
					list.Add(item);
					Vector2 item2 = tileMeshPrefab.uv[l];
					item2 += vector;
					if (k % 2 == 1)
					{
						item2.x += vOffset / yTextureTiles;
					}
					list2.Add(item2);
					list4.Add(new Color32(0, 0, 0, 0));
					cells[num2].vertIndices[l] = num;
					num++;
				}
				for (int m = 0; m < tileMeshPrefab.triangles.Length; m++)
				{
					list3.Add(tileMeshPrefab.triangles[m] + cells[num2].vertIndices[0]);
				}
				cells[j].activation = 0f;
				num2++;
			}
		}
		mesh.vertices = list.ToArray();
		mesh.uv = list2.ToArray();
		mesh.triangles = list3.ToArray();
		colors32 = list4.ToArray();
		mesh.colors32 = colors32;
		meshFilter.mesh = mesh;
		CalculateTotalBounds();
	}

	public bool[] GetCurrentActivePoints()
	{
		if (activePoints == null || activePoints.Length == 0)
		{
			return null;
		}
		return activePoints;
	}

	public int GetClusterSize()
	{
		return clusterSize;
	}

	public void ToggleActivePoint(int _pointIndex)
	{
		activePoints[_pointIndex] = !activePoints[_pointIndex];
		if (activePoints[_pointIndex])
		{
			activePointCount++;
		}
		else
		{
			activePointCount--;
		}
	}

	public void SetActivePoints(bool[] _activePoints)
	{
		activePoints = _activePoints;
		activePointCount = 0;
		for (int i = 0; i < activePoints.Length; i++)
		{
			if (activePoints[i])
			{
				activePointCount++;
			}
		}
	}

	public Bounds CalculateTotalBounds()
	{
		Vector3 min = new Vector3(base.transform.position.x - xSpacing * 0.5f - 0.5f, base.transform.position.y - ySpacing * 0.5f - 0.5f, rootPos.z);
		Vector3 worldPos = cells[cells.Length - 1].worldPos;
		Vector3 vector = worldPos;
		Debug.Log("Finalcellpos " + vector.ToString());
		Vector3 max = new Vector3(worldPos.x + xSpacing * 0.5f + 0.5f, worldPos.y + ySpacing * 0.5f + 0.5f, rootPos.z);
		totalGridBounds.SetMinMax(min, max);
		return totalGridBounds;
	}

	public Bounds[] CalculateClusterBounds()
	{
		float num = GetClusterSize();
		clusterTotalX = Mathf.CeilToInt((float)xTileCount / num);
		clusterTotalY = Mathf.CeilToInt((float)yTileCount / num);
		clusterBounds = new Bounds[clusterTotalX * clusterTotalY];
		Vector3 zero = Vector3.zero;
		Vector3 size = new Vector3(num * xSpacing + 1f, num * ySpacing + 1f, 0f);
		for (int i = 0; i < clusterBounds.Length; i++)
		{
			int num2 = i % clusterTotalX;
			int num3 = Mathf.FloorToInt((float)i / (float)clusterTotalX);
			zero.x = rootPos.x + (float)num2 * xSpacing * num + num * 0.5f - 0.5f;
			zero.y = rootPos.y + (float)num3 * ySpacing * num + num * 0.5f - 0.5f;
			zero.z = rootPos.z;
			clusterBounds[i] = new Bounds(zero, size);
		}
		return clusterBounds;
	}

	private float Get2DSquareDistance(float _x, float _y)
	{
		return _x * _x + _y * _y;
	}

	private void OnGUI()
	{
		GUI.color = Color.black;
		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i].cluster == 0 || cells[i].cluster == 9)
			{
				text = "CL: " + cells[i].cluster + "\nAC: " + cells[i].activation.ToString("0.0");
				Vector3 vector = Camera.main.WorldToScreenPoint(cells[i].worldPos);
				GUI.Label(new Rect(vector.x, (float)Screen.height - vector.y, 300f, 50f), text);
			}
		}
	}
}
