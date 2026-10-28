using System.Collections.Generic;
using UnityEngine;

public class SplatDecalSpawner : MonoBehaviour
{
	public ParticleSystem ps;

	public List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

	private void OnParticleCollision(GameObject other)
	{
		int a = ps.GetCollisionEvents(other, collisionEvents);
		a = Mathf.Min(a, 4);
		ParticleSystem.MainModule main = ps.main;
		for (int i = 0; i < a; i++)
		{
			if (collisionEvents[i].velocity.magnitude > 5f)
			{
				Singleton<DecalManager>.Instance.SpawnSplatDecal(null, collisionEvents[i].intersection, Quaternion.identity, main.startColor.color);
			}
		}
	}
}
