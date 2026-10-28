using System;
using UnityEngine;

[ExecuteInEditMode]
public class particleColorChangerMaster : MonoBehaviour
{
	[Serializable]
	public class colorChange
	{
		public string Name;

		public ParticleSystem[] colored_ParticleSystem;

		public Gradient Gradient_custom;
	}

	public float Speed_custom = 1f;

	public colorChange[] colorChangeList;

	public bool applyChanges;

	public bool Keep_applyChanges;

	private void Start()
	{
	}

	private void Update()
	{
		if (!applyChanges && !Keep_applyChanges)
		{
			return;
		}
		for (int i = 0; i < colorChangeList.Length; i++)
		{
			for (int j = 0; j < colorChangeList[i].colored_ParticleSystem.Length; j++)
			{
				ParticleSystem.ColorOverLifetimeModule colorOverLifetime = colorChangeList[i].colored_ParticleSystem[j].colorOverLifetime;
				colorOverLifetime.color = colorChangeList[i].Gradient_custom;
				colorChangeList[i].colored_ParticleSystem[j].playbackSpeed = Speed_custom;
			}
		}
		applyChanges = false;
	}
}
