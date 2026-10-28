using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>
{
	[Serializable]
	public class ParticleEffect
	{
		public PFXType type;

		public ParticleSystem prefab;

		public int quantity;

		public Vector3 offset = Vector3.zero;

		public bool recolorChildren = true;

		public PFXColor pfxColor;

		[EventRef]
		public string audioEvent;

		[HideInInspector]
		public Vector3 initialScale = Vector3.one;
	}

	[SerializeField]
	private List<ParticleSystem> activeParticles = new List<ParticleSystem>();

	[SerializeField]
	private List<ParticleSystem> particlesToRecycle = new List<ParticleSystem>();

	public ParticleEffect[] particleEffectList;

	public void Setup()
	{
		for (int i = 0; i < particleEffectList.Length; i++)
		{
			particleEffectList[i].initialScale = particleEffectList[i].prefab.transform.localScale;
			if (particleEffectList[i].prefab != null)
			{
				particleEffectList[i].prefab.CreatePool(particleEffectList[i].quantity);
			}
		}
		activeParticles.Clear();
	}

	private ParticleEffect GetEffectAsset(PFXType type)
	{
		for (int i = 0; i < particleEffectList.Length; i++)
		{
			if (type == particleEffectList[i].type)
			{
				return particleEffectList[i];
			}
		}
		Debug.Log("Error: particle effect not found");
		return null;
	}

	private ParticleSystem SpawnPFX(PFXType type, Vector3 position, Transform parent, Quaternion rotation)
	{
		ParticleEffect effectAsset = GetEffectAsset(type);
		if (effectAsset == null)
		{
			Debug.LogError("Could not find particle asset for " + type);
			return null;
		}
		ParticleSystem particleSystem = effectAsset.prefab.Spawn(position + effectAsset.offset, rotation);
		particleSystem.transform.localScale = effectAsset.initialScale;
		if (parent != null)
		{
			particleSystem.transform.SetParent(parent, worldPositionStays: true);
		}
		if (effectAsset.pfxColor != PFXColor.None && Singleton<LevelManager>.Instance.level != null)
		{
			Color color = default(Color);
			switch (effectAsset.pfxColor)
			{
			case PFXColor.Dust:
				color = Singleton<ColorManager>.Instance.GetDustColorAtPosition(position + effectAsset.offset);
				break;
			case PFXColor.Water:
				color = Singleton<LevelManager>.Instance.level.waterColor;
				break;
			}
			if (effectAsset.recolorChildren)
			{
				ParticleSystem[] componentsInChildren = particleSystem.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					ParticleSystem.MainModule main = componentsInChildren[i].main;
					main.startColor = color;
				}
			}
			else
			{
				ParticleSystem.MainModule main = particleSystem.main;
				main.startColor = color;
			}
		}
		activeParticles.Add(particleSystem);
		if (!particleSystem.main.loop)
		{
			StartCoroutine(RecycleWhenFinished(particleSystem));
		}
		if (!string.IsNullOrEmpty(effectAsset.audioEvent))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt(effectAsset.audioEvent, position);
		}
		return particleSystem;
	}

	private ParticleSystem SpawnPFXWithColor(PFXType type, Vector3 position, Color color, Transform parent, Quaternion rotation)
	{
		ParticleEffect effectAsset = GetEffectAsset(type);
		if (effectAsset == null)
		{
			return null;
		}
		ParticleSystem particleSystem = effectAsset.prefab.Spawn(position + effectAsset.offset, rotation);
		particleSystem.transform.localScale = effectAsset.initialScale;
		if (parent != null)
		{
			particleSystem.transform.SetParent(parent, worldPositionStays: true);
		}
		if (effectAsset.recolorChildren)
		{
			Color color2 = color;
			switch (effectAsset.pfxColor)
			{
			case PFXColor.Dust:
				color2 = Singleton<ColorManager>.Instance.GetDustColorAtPosition(position + effectAsset.offset);
				break;
			case PFXColor.Water:
				color2 = Singleton<LevelManager>.Instance.level.waterColor;
				break;
			}
			ParticleSystem[] componentsInChildren = particleSystem.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.MainModule main = componentsInChildren[i].main;
				main.startColor = color2;
			}
			if (effectAsset.pfxColor != PFXColor.None)
			{
				ParticleSystem.MainModule main = particleSystem.main;
				main.startColor = color;
			}
		}
		else
		{
			ParticleSystem.MainModule main = particleSystem.main;
			main.startColor = color;
		}
		activeParticles.Add(particleSystem);
		if (!particleSystem.main.loop)
		{
			StartCoroutine(RecycleWhenFinished(particleSystem));
		}
		if (!string.IsNullOrEmpty(effectAsset.audioEvent))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAt(effectAsset.audioEvent, position);
		}
		return particleSystem;
	}

	public ParticleSystem PlayPFX(PFXType type, Vector3 position, Color color, Transform parent, Quaternion rotation)
	{
		return SpawnPFXWithColor(type, position, color, parent, rotation);
	}

	public ParticleSystem PlayPFX(PFXType type, Vector3 position, Color color, Transform parent = null)
	{
		return SpawnPFXWithColor(type, position, color, parent, GetEffectAsset(type).prefab.transform.rotation);
	}

	public ParticleSystem PlayPFX(PFXType type, Vector3 position, Transform parent, Quaternion rotation)
	{
		return SpawnPFX(type, position, parent, rotation);
	}

	public ParticleSystem PlayPFX(PFXType type, Vector3 position, Transform parent = null)
	{
		return SpawnPFX(type, position, parent, GetEffectAsset(type).prefab.transform.rotation);
	}

	public void CleanUpParticles()
	{
		StopAllCoroutines();
		for (int i = 0; i < activeParticles.Count; i++)
		{
			if (!(activeParticles[i] == null))
			{
				activeParticles[i].gameObject.Recycle();
			}
		}
		activeParticles.Clear();
	}

	private IEnumerator RecycleWhenFinished(ParticleSystem pfx)
	{
		yield return new WaitForSeconds(pfx.main.duration);
		activeParticles.Remove(pfx);
		if (pfx != null && pfx.gameObject.activeSelf)
		{
			pfx.gameObject.Recycle();
		}
	}

	public void StopPFXAndRecycle(ParticleSystem pfx)
	{
		if (!activeParticles.Contains(pfx))
		{
			activeParticles.Add(pfx);
		}
		pfx.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		StartCoroutine(RecycleWhenFinished(pfx));
	}
}
