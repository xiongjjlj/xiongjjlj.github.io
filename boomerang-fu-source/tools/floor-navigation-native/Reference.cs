using UnityEngine;using Pathfinding;public partial class LevelManager {
	public void TagAreaAsOutOfBounds(Vector2 worldPosition, Vector2 extents, Quaternion rotation)
	{
		Vector3[] array = new Vector3[4]
		{
			new Vector2(0f - extents.x, 0f - extents.y),
			new Vector2(0f - extents.x, extents.y),
			new Vector2(extents.x, extents.y),
			new Vector2(extents.x, 0f - extents.y)
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (Vector2)VectorExt.RotatePointAroundPivot(array[i], Vector3.zero, rotation.eulerAngles) + worldPosition;
			array[i] = VectorExt.FlipYAndZ(array[i]);
		}
		GraphUpdateShape graphUpdateShape = new GraphUpdateShape(array, convex: true, base.transform.localToWorldMatrix, 1f);
		GraphUpdateObject graphUpdateObject = new GraphUpdateObject(graphUpdateShape.GetBounds());
		graphUpdateObject.shape = graphUpdateShape;
		graphUpdateObject.modifyTag = true;
		graphUpdateObject.setTag = 1;
		graphUpdateObject.updatePhysics = false;
		pathfinder.UpdateGraphs(graphUpdateObject);
	}

}
