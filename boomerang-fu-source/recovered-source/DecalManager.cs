using System.Collections.Generic;
using UnityEngine;

public class DecalManager : Singleton<DecalManager>
{
	public SplatDecal splatDecal;

	public Decal iceDecal;

	public List<GameObject> activeDecals = new List<GameObject>();

	public void Setup()
	{
		splatDecal.CreatePool(128);
		iceDecal.CreatePool(1024);
	}

	public Decal SpawnIceDecal(Transform parent, Vector3 position, Quaternion rotation)
	{
		if (parent != null)
		{
			position = parent.InverseTransformPoint(position);
		}
		Decal decal = iceDecal.Spawn(parent, position, rotation);
		activeDecals.Add(decal.gameObject);
		return decal;
	}

	public SplatDecal SpawnSplatDecal(Transform tf, Vector3 position, Quaternion rotation, Color color)
	{
		if (!Physics2D.OverlapPoint(position, Singleton<LevelManager>.Instance.floorLayers))
		{
			return null;
		}
		if (tf != null)
		{
			position = tf.InverseTransformPoint(position);
		}
		SplatDecal splatDecal = this.splatDecal.Spawn(tf, position, rotation);
		splatDecal.SetColor(color);
		activeDecals.Add(splatDecal.gameObject);
		return splatDecal;
	}

	public void CleanUpDecals()
	{
		for (int i = 0; i < activeDecals.Count; i++)
		{
			if (!(activeDecals[i] == null))
			{
				activeDecals[i].Recycle();
			}
		}
		activeDecals.Clear();
	}

	public void RemoveDecalsTouchingCollider(Collider2D col)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < activeDecals.Count; i++)
		{
			if (activeDecals[i] == null)
			{
				Debug.LogWarning("Tried to remove destroyed decal!");
			}
			else if (col.OverlapPoint(activeDecals[i].transform.position))
			{
				activeDecals[i].Recycle();
				list.Add(activeDecals[i]);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			activeDecals.Remove(list[j]);
		}
	}

	public void RecycleDecal(GameObject go)
	{
		if (go != null && activeDecals.Contains(go))
		{
			go.Recycle();
			activeDecals.Remove(go);
		}
	}
}
