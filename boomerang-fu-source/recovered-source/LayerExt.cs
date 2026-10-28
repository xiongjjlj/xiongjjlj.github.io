using System.Collections.Generic;
using UnityEngine;

public static class LayerExt
{
	public static void MoveToLayer(Transform root, int layer)
	{
		Stack<Transform> stack = new Stack<Transform>();
		stack.Push(root);
		while (stack.Count != 0)
		{
			Transform transform = stack.Pop();
			transform.gameObject.layer = layer;
			foreach (Transform item in transform)
			{
				stack.Push(item);
			}
		}
	}

	public static void MoveFromLayerToLayer(Transform root, int fromLayer, int targetLayer)
	{
		Stack<Transform> stack = new Stack<Transform>();
		stack.Push(root);
		while (stack.Count != 0)
		{
			Transform transform = stack.Pop();
			if (transform.gameObject.layer != fromLayer)
			{
				continue;
			}
			transform.gameObject.layer = targetLayer;
			foreach (Transform item in transform)
			{
				stack.Push(item);
			}
		}
	}
}
