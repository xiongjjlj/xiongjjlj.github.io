using System;
using UnityEngine;

[Serializable]
public class Camera3DTarget
{
	public Transform Transform;

	public float Influence = 1f;

	public Vector3 Size;

	public Vector3 LastPosition;

	public Vector3 CameraPosition;

	public bool Transitioning;
}
