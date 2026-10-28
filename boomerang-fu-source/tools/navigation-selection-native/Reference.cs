using UnityEngine; using Pathfinding; public partial class LevelManager {
	public GraphMask GetBestGraphForTarget(Vector3 point, Vector3 targetPosition)
	{
		NNConstraint none = NNConstraint.None;
		none.constrainWalkability = true;
		none.walkable = true;
		none.constrainTags = false;
		float num = 7f;
		float num2 = float.MaxValue;
		GraphMask graphMask = 1;
		GraphMask result = 0;
		for (int i = 0; i < level.allMovingFloors.Length; i++)
		{
			graphMask |= level.allMovingFloors[i].graphMask;
		}
		for (int j = 0; j < AstarPath.active.data.graphs.Length; j++)
		{
			if ((int)(graphMask & GraphMask.FromGraph(AstarPath.active.data.graphs[j])) != (int)GraphMask.FromGraph(AstarPath.active.data.graphs[j]))
			{
				continue;
			}
			Vector3 vector = point;
			Vector3 vector2 = targetPosition;
			if (j > 0)
			{
				for (int k = 0; k < level.allMovingFloors.Length; k++)
				{
					if ((int)level.allMovingFloors[k].graphMask == (int)GraphMask.FromGraph(AstarPath.active.data.graphs[j]))
					{
						vector = level.allMovingFloors[k].GetInverseTransformPoint(vector);
						vector2 = level.allMovingFloors[k].GetInverseTransformPoint(targetPosition);
						break;
					}
				}
			}
			if (AstarPath.active.data.graphs[j].GetNearestForce(vector, none).node == null)
			{
				Debug.Log("This graph does not have any nearest nodes that meet the constraints");
				continue;
			}
			Vector3 vector3 = (Vector3)AstarPath.active.data.graphs[j].GetNearestForce(vector, none).node.position;
			if ((vector3 - vector).sqrMagnitude <= num)
			{
				float sqrMagnitude = (vector3 - vector2).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					result = GraphMask.FromGraph(AstarPath.active.data.graphs[j]);
				}
			}
		}
		return result;
	}
}
