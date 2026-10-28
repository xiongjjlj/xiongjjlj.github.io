using System;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.Util;
using UnityEngine;

public class WaypointPath
{
	public delegate void OnWaypointPathComplete(WaypointPath p);

	private Vector3[] waypoints;

	private ABPath[] paths;

	private int completedPaths = -1;

	public List<Vector3> vectorPath;

	public List<GraphNode> nodePath;

	private OnWaypointPathComplete callback;

	public WaypointPath(Vector3[] waypoints, OnWaypointPathComplete callback)
	{
		this.waypoints = waypoints;
		this.callback = callback;
	}

	public bool HasError()
	{
		return completedPaths != paths.Length;
	}

	public void StartPath(Seeker seeker, OnPathDelegate config = null)
	{
		if (completedPaths != -1)
		{
			throw new Exception("Do not start the path more than once");
		}
		completedPaths = 0;
		paths = new ABPath[waypoints.Length - 1];
		for (int i = 0; i < paths.Length; i++)
		{
			paths[i] = ABPath.Construct(waypoints[i], waypoints[i + 1], OnPathComplete);
			config?.Invoke(paths[i]);
			seeker.StartPath(paths[i]);
		}
	}

	private void OnPathComplete(Path p)
	{
		if (p.error)
		{
			for (int i = 0; i < paths.Length; i++)
			{
				paths[i].Error();
			}
			Completed();
			return;
		}
		if (!(p is ABPath))
		{
			throw new Exception("Only ABPaths should be returned to this object");
		}
		completedPaths++;
		if (completedPaths == paths.Length)
		{
			Completed();
		}
	}

	private void Completed()
	{
		if (completedPaths == paths.Length)
		{
			vectorPath = ListPool<Vector3>.Claim();
			nodePath = ListPool<GraphNode>.Claim();
			for (int i = 0; i < paths.Length; i++)
			{
				List<Vector3> list = paths[i].vectorPath;
				List<GraphNode> path = paths[i].path;
				for (int j = 0; j < list.Count; j++)
				{
					if (vectorPath.Count == 0 || vectorPath[vectorPath.Count - 1] != list[j])
					{
						vectorPath.Add(list[j]);
					}
				}
				for (int k = 0; k < path.Count; k++)
				{
					if (nodePath.Count == 0 || nodePath[nodePath.Count - 1] != path[k])
					{
						nodePath.Add(path[k]);
					}
				}
			}
		}
		if (callback != null)
		{
			callback(this);
		}
	}
}
