using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

public class CellManager : Singleton<CellManager>
{
	public enum CellType
	{
		None,
		Fire,
		Ice
	}

	public class Cell
	{
		public CellType cellType;

		public float duration;

		public float timeStamp;

		public Actor owner;
	}

	public struct Coordinates
	{
		public int x;

		public int y;

		public Coordinates(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public ParticleSystem fireSystem;

	public ParticleSystem steamSystem;

	public ParticleSystem glowSystem;

	public ParticleSystem iceSparkleSystem;

	public ParticleSystem leafBurstSystem;

	public ParticleSystem leafBurstAutumnSystem;

	public static float cellSize = 1.2f;

	private static float fireCellRadius = 0.75f;

	private static float iceCellRadius = 0.5f;

	private bool emitCellPFX;

	private float cellPFXTimeStamp;

	private float timeBetweenCellPFX = 0.092f;

	private static float firePFXThreshold = 0.3f;

	private static float icePFXThreshold = 1f;

	private static int gridSize = 100;

	public Cell[][] cellGridStatic = new Cell[gridSize][];

	[SerializeField]
	private int numFireCells;

	private int numIceCells;

	private EventInstance fireSFXInstance;

	private Vector3 fireSFXPosition = Vector3.zero;

	[SerializeField]
	private List<Vector3> firePositions = new List<Vector3>(2048);

	private static float maxFireIntensity = 150f;

	private void Start()
	{
		for (int i = 0; i < cellGridStatic.Length; i++)
		{
			cellGridStatic[i] = new Cell[gridSize];
			for (int j = 0; j < cellGridStatic[i].Length; j++)
			{
				cellGridStatic[i][j] = new Cell();
			}
		}
	}

	public void RegisterCell(CellType cellType, Vector2 worldPosition, float duration, Actor owner, bool checkMovingFloors = true)
	{
		Cell[][] grid = cellGridStatic;
		Vector3 vector = worldPosition;
		if (checkMovingFloors)
		{
			MovingFloor movingFloor = MovingFloorAtPosition(worldPosition);
			if (movingFloor != null)
			{
				grid = GetCellGridOnMovingFloor(movingFloor);
				if (grid == null)
				{
					grid = cellGridStatic;
				}
				else
				{
					vector = movingFloor.transform.InverseTransformPoint(worldPosition);
				}
			}
		}
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(vector, ref grid);
		if (coordinatesOnGrid.y != -1 && coordinatesOnGrid.x != -1)
		{
			SetCellType(grid[coordinatesOnGrid.y][coordinatesOnGrid.x], cellType, duration, Singleton<TimeKeeper>.Instance.CurrentTime, owner, worldPosition);
		}
	}

	public void RegisterCellsWithinRadius(CellType cellType, Vector2 worldPosition, float duration, Actor owner, int radius, bool checkMovingFloors = true, bool checkForObstacles = false)
	{
		Cell[][] grid = cellGridStatic;
		Vector3 vector = worldPosition;
		Transform transform = null;
		if (checkMovingFloors)
		{
			MovingFloor movingFloor = MovingFloorAtPosition(worldPosition);
			if (movingFloor != null)
			{
				grid = GetCellGridOnMovingFloor(movingFloor);
				if (grid == null)
				{
					grid = cellGridStatic;
				}
				else
				{
					vector = movingFloor.transform.InverseTransformPoint(worldPosition);
					transform = movingFloor.transform;
				}
			}
		}
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(vector, ref grid);
		if (coordinatesOnGrid.y == -1 || coordinatesOnGrid.x == -1)
		{
			return;
		}
		float currentTime = Singleton<TimeKeeper>.Instance.CurrentTime;
		bool flag = (transform ? true : false);
		foreach (Coordinates item in GetCoordsWithinRadius(coordinatesOnGrid.x, coordinatesOnGrid.y, radius, grid))
		{
			Vector2 vector2 = ((!flag) ? GetWorldPositionOnStaticGrid(item.x, item.y, ref grid) : GetWorldPositionOnRelativeGrid(item.x, item.y, ref grid, transform));
			if (!checkForObstacles || !Physics2D.Raycast(worldPosition, (vector2 - worldPosition).normalized, (vector2 - worldPosition).magnitude, 1 << LayerMask.NameToLayer("Obstacle")))
			{
				if (flag)
				{
					SetCellType(grid[item.y][item.x], cellType, duration, currentTime, owner, vector2);
				}
				else
				{
					SetCellType(grid[item.y][item.x], cellType, duration, currentTime, owner, vector2);
				}
			}
		}
	}

	private void SetCellType(Cell cell, CellType cellType, float duration, float currentTime, Actor owner, Vector2 worldPosition)
	{
		switch (cellType)
		{
		case CellType.Fire:
			if (cell.cellType != CellType.Ice && Singleton<LevelManager>.Instance.CanFireSpawnAt(worldPosition))
			{
				if (numFireCells == 0)
				{
					Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/firecell_start", worldPosition);
					fireSFXInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/environment/firecell_loop", worldPosition);
					if (fireSFXInstance.isValid())
					{
						fireSFXInstance.setParameterByName("intensity", 0f);
						fireSFXInstance.start();
					}
				}
				if (cell.cellType != CellType.Fire)
				{
					numFireCells++;
				}
				cell.cellType = CellType.Fire;
				if (fireSystem != null)
				{
					EmitFirePFX(new ParticleSystem.EmitParams
					{
						position = worldPosition,
						applyShapeToPosition = true
					});
				}
			}
			else if (!Singleton<LevelManager>.Instance.IsPositionOffCliff(worldPosition) && Chance.PercentageTrue(85f))
			{
				PlaySteamPFXAt(worldPosition);
			}
			break;
		case CellType.Ice:
		{
			if (!Singleton<LevelManager>.Instance.CanIceSpawnAt(worldPosition))
			{
				break;
			}
			if (cell.cellType != CellType.Ice)
			{
				numIceCells++;
			}
			if (cell.cellType == CellType.Fire)
			{
				numFireCells--;
				fireSFXInstance.setParameterByName("plants", 0f);
				if (numFireCells == 0 && fireSFXInstance.isValid())
				{
					fireSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
					fireSFXInstance.release();
				}
			}
			cell.cellType = CellType.Ice;
			Transform parent = null;
			if (AreMovingFloorsInLevel())
			{
				MovingFloor movingFloor = MovingFloorAtPosition(worldPosition);
				if (movingFloor != null)
				{
					parent = movingFloor.transform;
				}
			}
			Singleton<DecalManager>.Instance.SpawnIceDecal(parent, worldPosition + Random.insideUnitCircle * 0.3f, Quaternion.Euler(0f, 0f, Random.Range(0f, 359f)));
			break;
		}
		}
		cell.timeStamp = Singleton<TimeKeeper>.Instance.CurrentTime;
		cell.duration = duration;
		cell.owner = owner;
	}

	private void DeregisterCell(Cell cell)
	{
		if (cell.cellType == CellType.Fire)
		{
			numFireCells--;
			fireSFXInstance.setParameterByName("plants", 0f);
			if (numFireCells == 0 && fireSFXInstance.isValid())
			{
				fireSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
				fireSFXInstance.release();
			}
		}
		else if (cell.cellType == CellType.Ice)
		{
			numIceCells--;
		}
		cell.cellType = CellType.None;
		cell.duration = 0f;
		cell.owner = null;
	}

	public void CleanUp()
	{
		CleanUpCellGrid(cellGridStatic);
		if (AreMovingFloorsInLevel())
		{
			for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; i++)
			{
				CleanUpCellGrid(Singleton<LevelManager>.Instance.level.allMovingFloors[i].cellGrid);
			}
		}
		numFireCells = 0;
		numIceCells = 0;
		if (fireSystem != null)
		{
			fireSystem.Clear();
			glowSystem.Clear();
		}
		if (steamSystem != null)
		{
			steamSystem.Clear();
		}
		if (fireSFXInstance.isValid())
		{
			fireSFXInstance.stop(STOP_MODE.ALLOWFADEOUT);
			fireSFXInstance.release();
		}
	}

	public void ExtinguishCellsAt(Vector2 worldPosition, CellType cellType, bool checkMovingFloors = true, int radius = 1)
	{
		Cell[][] grid = cellGridStatic;
		Vector2 position = worldPosition;
		if (checkMovingFloors)
		{
			MovingFloor movingFloor = MovingFloorAtPosition(worldPosition);
			if (movingFloor != null)
			{
				grid = GetCellGridOnMovingFloor(movingFloor);
				if (grid == null)
				{
					grid = cellGridStatic;
				}
				else
				{
					position = movingFloor.transform.InverseTransformPoint(worldPosition);
				}
			}
		}
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(position, ref grid, forceOnGrid: true);
		if (coordinatesOnGrid.y == -1 || coordinatesOnGrid.x == -1)
		{
			return;
		}
		if (radius > 1)
		{
			foreach (Coordinates item in GetCoordsWithinRadius(coordinatesOnGrid.x, coordinatesOnGrid.y, radius, grid))
			{
				if (grid[item.y][item.x].cellType == cellType)
				{
					DeregisterCell(grid[item.y][item.x]);
				}
			}
			return;
		}
		DeregisterCellsNear(coordinatesOnGrid, cellType, ref grid);
	}

	public void ExtinguishCellsTouchingCollider2D(Collider2D col, bool checkMovingFloors = false)
	{
		Cell[][] grid = cellGridStatic;
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(col.bounds.min, ref grid, forceOnGrid: true);
		Coordinates coordinatesOnGrid2 = GetCoordinatesOnGrid(col.bounds.max, ref grid, forceOnGrid: true);
		bool flag = false;
		for (int i = coordinatesOnGrid.y; i <= coordinatesOnGrid2.y; i++)
		{
			for (int j = coordinatesOnGrid.x; j <= coordinatesOnGrid2.x; j++)
			{
				if (grid[i][j].cellType != CellType.None && col.OverlapPoint(GetWorldPositionOnStaticGrid(j, i, ref grid)))
				{
					if (grid[i][j].cellType == CellType.Ice)
					{
						flag = true;
					}
					DeregisterCell(grid[i][j]);
				}
			}
		}
		if (!checkMovingFloors || !AreMovingFloorsInLevel())
		{
			return;
		}
		for (int k = 0; k < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; k++)
		{
			grid = Singleton<LevelManager>.Instance.level.allMovingFloors[k].cellGrid;
			coordinatesOnGrid = GetCoordinatesOnGrid(col.bounds.min, ref grid, forceOnGrid: true);
			coordinatesOnGrid2 = GetCoordinatesOnGrid(col.bounds.max, ref grid, forceOnGrid: true);
			for (int l = coordinatesOnGrid.y; l <= coordinatesOnGrid2.y; l++)
			{
				for (int m = coordinatesOnGrid.x; m <= coordinatesOnGrid2.x; m++)
				{
					if (grid[l][m].cellType != CellType.None && col.OverlapPoint(GetWorldPositionOnStaticGrid(m, l, ref grid)))
					{
						if (grid[l][m].cellType == CellType.Ice)
						{
							flag = true;
						}
						DeregisterCell(grid[l][m]);
					}
				}
			}
		}
	}

	public void PlayFirePFXAt(Vector3 worldPosition)
	{
		if (fireSystem != null)
		{
			EmitFirePFX(new ParticleSystem.EmitParams
			{
				position = worldPosition,
				applyShapeToPosition = true
			});
		}
	}

	public void PlaySteamPFXAt(Vector3 worldPosition)
	{
		if (steamSystem != null)
		{
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				position = worldPosition,
				applyShapeToPosition = true
			};
			steamSystem.Emit(emitParams, 1);
		}
	}

	public void SetPlantOnFire()
	{
		if (fireSFXInstance.isValid())
		{
			fireSFXInstance.setParameterByName("plants", 1f);
		}
	}

	public void PlayLeafBurstPFXAt(Vector3 worldPosition, bool autumnLeaves)
	{
		ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
		{
			position = worldPosition,
			applyShapeToPosition = true
		};
		if (autumnLeaves)
		{
			if (leafBurstAutumnSystem != null)
			{
				leafBurstAutumnSystem.Emit(emitParams, Random.Range(2, 5));
			}
		}
		else if (leafBurstSystem != null)
		{
			leafBurstSystem.Emit(emitParams, Random.Range(2, 5));
		}
	}

	private Coordinates GetCoordinatesOnGrid(Vector2 position, ref Cell[][] grid, bool forceOnGrid = false)
	{
		int num = Mathf.RoundToInt(position.y / cellSize) + grid.Length / 2;
		int num2 = Mathf.RoundToInt(position.x / cellSize) + grid[0].Length / 2;
		if (forceOnGrid)
		{
			num2 = Mathf.Max(0, num2);
			num2 = Mathf.Min(grid[0].Length - 1, num2);
			num = Mathf.Max(0, num);
			num = Mathf.Min(grid.Length - 1, num);
		}
		if (!IsIndexOnGrid(num2, num, ref grid))
		{
			num2 = -1;
			num = -1;
		}
		return new Coordinates(num2, num);
	}

	private Vector2 GetWorldPositionOnStaticGrid(int xPos, int yPos, ref Cell[][] grid)
	{
		float num = yPos - grid.Length / 2;
		return new Vector2((float)(xPos - grid[0].Length / 2) * cellSize, num * cellSize);
	}

	private Vector2 GetWorldPositionOnRelativeGrid(int xPos, int yPos, ref Cell[][] grid, Transform parent)
	{
		float num = yPos - grid.Length / 2;
		float num2 = xPos - grid[0].Length / 2;
		Vector2 vector = new Vector2(num2 * cellSize, num * cellSize);
		return parent.TransformPoint(vector);
	}

	private Cell[][] GetCellGridOnMovingFloor(MovingFloor mf)
	{
		if ((bool)mf && mf.cellGrid != null && mf.cellGrid.Length != 0)
		{
			return mf.cellGrid;
		}
		return null;
	}

	private bool AreMovingFloorsInLevel()
	{
		if (Singleton<LevelManager>.Instance.level != null)
		{
			return Singleton<LevelManager>.Instance.level.allMovingFloors.Length != 0;
		}
		return false;
	}

	private MovingFloor MovingFloorAtPosition(Vector2 worldPosition)
	{
		MovingFloor result = null;
		Collider2D collider2D = Physics2D.OverlapPoint(worldPosition, 1 << LayerMask.NameToLayer("MovingFloor"));
		if ((bool)collider2D)
		{
			result = collider2D.GetComponent<MovingFloor>();
		}
		return result;
	}

	private static IEnumerable<Coordinates> GetCoordsWithinRadius(int x, int y, int radius, Cell[][] grid)
	{
		int num = Mathf.Max(0, x - radius);
		int i2 = Mathf.Min(grid[0].Length - 1, x + radius);
		float radiusSquared = radius * radius;
		for (int j = num; j <= i2; j++)
		{
			int num2 = x - j;
			num2 *= num2;
			num2 = Mathf.RoundToInt(Mathf.Sqrt(radiusSquared - (float)num2));
			int num3 = Mathf.Max(0, y - num2);
			int j2 = Mathf.Min(grid.Length - 1, y + num2);
			for (int k = num3; k <= j2; k++)
			{
				yield return new Coordinates(j, k);
			}
		}
	}

	private void CleanUpCellGrid(Cell[][] grid)
	{
		for (int i = 0; i < grid.Length; i++)
		{
			for (int j = 0; j < grid[i].Length; j++)
			{
				grid[i][j].cellType = CellType.None;
			}
		}
	}

	private bool IsPositionTouchingCellInGrid(Coordinates coords, CellType cellType, ref Cell[][] grid)
	{
		if (coords.y == -1 || coords.x == -1)
		{
			return false;
		}
		if (grid[coords.y][coords.x].cellType == cellType)
		{
			return true;
		}
		if (grid[Mathf.Min(coords.y + 1, grid.Length - 1)][coords.x].cellType == cellType)
		{
			return true;
		}
		if (grid[Mathf.Max(coords.y - 1, 0)][coords.x].cellType == cellType)
		{
			return true;
		}
		if (grid[coords.y][Mathf.Min(coords.x + 1, grid[coords.y].Length - 1)].cellType == cellType)
		{
			return true;
		}
		if (grid[coords.y][Mathf.Max(coords.x - 1, 0)].cellType == cellType)
		{
			return true;
		}
		return false;
	}

	private bool IsIndexOnGrid(int x, int y, ref Cell[][] grid)
	{
		if (y < 0 || y >= grid.Length || x < 0 || x >= grid[0].Length)
		{
			return false;
		}
		return true;
	}

	private void DeregisterCellsNear(Coordinates coords, CellType cellType, ref Cell[][] grid)
	{
		if (coords.y != -1 && coords.x != -1)
		{
			if (grid[coords.y][coords.x].cellType == cellType)
			{
				DeregisterCell(grid[coords.y][coords.x]);
			}
			if (grid[Mathf.Min(coords.y + 1, grid.Length - 1)][coords.x].cellType == cellType)
			{
				DeregisterCell(grid[Mathf.Min(coords.y + 1, grid.Length - 1)][coords.x]);
			}
			if (grid[Mathf.Max(coords.y - 1, 0)][coords.x].cellType == cellType)
			{
				DeregisterCell(grid[Mathf.Max(coords.y - 1, 0)][coords.x]);
			}
			if (grid[coords.y][Mathf.Min(coords.x + 1, grid[coords.y].Length - 1)].cellType == cellType)
			{
				DeregisterCell(grid[coords.y][Mathf.Min(coords.x + 1, grid[coords.y].Length - 1)]);
			}
			if (grid[coords.y][Mathf.Max(coords.x - 1, 0)].cellType == cellType)
			{
				DeregisterCell(grid[coords.y][Mathf.Max(coords.x - 1, 0)]);
			}
		}
	}

	private Actor GetCellTypeOwnerInGrid(Coordinates coords, CellType cellType, ref Cell[][] grid)
	{
		if (coords.y == -1 || coords.x == -1)
		{
			return null;
		}
		int x = coords.x;
		int y = coords.y;
		if (grid[y][x].cellType == cellType)
		{
			return grid[y][x].owner;
		}
		y = Mathf.Min(coords.y + 1, grid.Length - 1);
		if (grid[y][x].cellType == cellType)
		{
			return grid[y][x].owner;
		}
		y = Mathf.Max(coords.y - 1, 0);
		if (grid[y][x].cellType == cellType)
		{
			return grid[y][x].owner;
		}
		x = Mathf.Min(coords.x + 1, grid[coords.y].Length - 1);
		y = coords.y;
		if (grid[y][x].cellType == cellType)
		{
			return grid[y][x].owner;
		}
		x = Mathf.Max(coords.x - 1, 0);
		if (grid[y][x].cellType == cellType)
		{
			return grid[y][x].owner;
		}
		return null;
	}

	public bool IsPositionTouchingCellType(Vector2 position, CellType cellType, bool checkMovingFloors = true)
	{
		Cell[][] grid = cellGridStatic;
		if (checkMovingFloors)
		{
			MovingFloor movingFloor = MovingFloorAtPosition(position);
			if (movingFloor != null)
			{
				grid = GetCellGridOnMovingFloor(movingFloor);
				if (grid == null)
				{
					grid = cellGridStatic;
				}
				else
				{
					position = movingFloor.transform.InverseTransformPoint(position);
				}
			}
		}
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(position, ref grid);
		return IsPositionTouchingCellInGrid(coordinatesOnGrid, cellType, ref grid);
	}

	public bool IsPositionTouchingCellType(Vector2 position, CellType cellType, out Actor owner, bool checkMovingFloors = true)
	{
		Cell[][] grid = cellGridStatic;
		if (checkMovingFloors)
		{
			MovingFloor movingFloor = MovingFloorAtPosition(position);
			if (movingFloor != null)
			{
				grid = GetCellGridOnMovingFloor(movingFloor);
				if (grid == null)
				{
					grid = cellGridStatic;
				}
				else
				{
					position = movingFloor.transform.InverseTransformPoint(position);
				}
			}
		}
		Coordinates coordinatesOnGrid = GetCoordinatesOnGrid(position, ref grid);
		owner = GetCellTypeOwnerInGrid(coordinatesOnGrid, cellType, ref grid);
		return IsPositionTouchingCellInGrid(coordinatesOnGrid, cellType, ref grid);
	}

	private void Update()
	{
		if (numFireCells == 0 && numIceCells == 0)
		{
			return;
		}
		float currentTime = Singleton<TimeKeeper>.Instance.CurrentTime;
		emitCellPFX = fireSystem != null && Singleton<TimeKeeper>.Instance.TimeSince(cellPFXTimeStamp) >= timeBetweenCellPFX;
		if (emitCellPFX)
		{
			cellPFXTimeStamp = currentTime;
			firePositions.Clear();
			UpdateFireSFXPosition();
		}
		if (numFireCells > 0)
		{
			UpdateFireSFXIntensity();
		}
		UpdateCellsOnGrid(ref cellGridStatic, currentTime);
		if (AreMovingFloorsInLevel())
		{
			for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; i++)
			{
				UpdateCellsOnGrid(ref Singleton<LevelManager>.Instance.level.allMovingFloors[i].cellGrid, currentTime, Singleton<LevelManager>.Instance.level.allMovingFloors[i].transform);
			}
		}
	}

	private void UpdateCellsOnGrid(ref Cell[][] grid, float currentTime, Transform parent = null)
	{
		if (grid == null || grid.Length == 0)
		{
			return;
		}
		bool flag = (parent ? true : false);
		int num = grid.Length;
		int num2 = grid[0].Length;
		Vector2 vector = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f));
		Cell cell = null;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				cell = grid[i][j];
				if (cell.cellType != CellType.None && currentTime - cell.timeStamp > cell.duration)
				{
					DeregisterCell(cell);
				}
				else
				{
					if (!emitCellPFX || grid[i][j].cellType == CellType.None)
					{
						continue;
					}
					float num3 = firePFXThreshold;
					if (grid[i][j].cellType == CellType.Ice)
					{
						num3 = icePFXThreshold;
					}
					if (currentTime + num3 - cell.timeStamp < cell.duration)
					{
						ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
						if (flag)
						{
							emitParams.position = GetWorldPositionOnRelativeGrid(j, i, ref grid, parent) + vector;
						}
						else
						{
							emitParams.position = GetWorldPositionOnStaticGrid(j, i, ref grid) + vector;
						}
						emitParams.applyShapeToPosition = true;
						if (grid[i][j].cellType == CellType.Fire)
						{
							EmitFirePFX(emitParams);
							firePositions.Add(emitParams.position);
						}
						else if (grid[i][j].cellType == CellType.Ice && Chance.PercentageTrue(8f))
						{
							iceSparkleSystem.Emit(emitParams, 1);
						}
					}
				}
			}
		}
	}

	private void EmitFirePFX(ParticleSystem.EmitParams emitParams)
	{
		fireSystem.Emit(emitParams, 1);
		if (glowSystem != null)
		{
			glowSystem.Emit(emitParams, 1);
		}
	}

	private void UpdateFireSFXPosition()
	{
		if (firePositions.Count < 1)
		{
			return;
		}
		Vector3 vector = firePositions[0];
		Vector3 vector2 = firePositions[0];
		for (int i = 1; i < firePositions.Count; i++)
		{
			Vector3 vector3 = firePositions[i];
			if (vector3.x < vector.x)
			{
				vector.x = vector3.x;
			}
			if (vector3.x > vector2.x)
			{
				vector2.x = vector3.x;
			}
			if (vector3.y < vector.y)
			{
				vector.y = vector3.y;
			}
			if (vector3.y > vector2.y)
			{
				vector2.y = vector3.y;
			}
			if (vector3.z < vector.z)
			{
				vector.z = vector3.z;
			}
			if (vector3.z > vector2.z)
			{
				vector2.z = vector3.z;
			}
		}
		fireSFXPosition = vector + 0.5f * (vector2 - vector);
	}

	private void UpdateFireSFXIntensity()
	{
		if (fireSFXInstance.isValid())
		{
			float value = (float)numFireCells / maxFireIntensity;
			fireSFXInstance.setParameterByName("intensity", value);
		}
	}
}
