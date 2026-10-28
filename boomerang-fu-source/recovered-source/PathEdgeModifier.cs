using Pathfinding;

public class PathEdgeModifier : MonoModifier
{
	public override int Order => 49;

	public override void Apply(Path path)
	{
		if (!path.error && path.vectorPath != null && path.vectorPath.Count > 2 && !(path.path[path.path.Count - 1] as GridNode).HasConnectionsToAllEightNeighbours)
		{
			path.vectorPath.RemoveAt(path.vectorPath.Count - 1);
		}
	}
}
