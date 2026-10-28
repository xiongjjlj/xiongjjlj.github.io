using System.Collections;
using UnityEngine;

public class CharacterMilk : CharacterPlayer
{
	[Header("Death Effects")]
	public GameObject milkPourPFX;

	public GameObject milkSplatPFX;

	public GameObject liquidSurface;

	public Transform[] milkDecalPositions;

	public override bool IsSquished
	{
		get
		{
			return base.IsSquished;
		}
		set
		{
			if (value)
			{
				PrintSpillDecals();
			}
			base.IsSquished = value;
		}
	}

	public override Color Color
	{
		get
		{
			if (actor != null && Singleton<GameManager>.Instance != null && actor.actorState == Actor.ActorState.Dead && (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionOut || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionIn))
			{
				return new Color(0.98f, 0.98f, 0.98f);
			}
			return base.Color;
		}
	}

	protected void Start()
	{
		milkPourPFX.SetActive(value: false);
		milkSplatPFX.SetActive(value: false);
		if (liquidSurface != null)
		{
			liquidSurface.SetActive(value: false);
		}
	}

	public override void Reset()
	{
		base.Reset();
		milkPourPFX.SetActive(value: false);
		milkSplatPFX.SetActive(value: false);
	}

	public override void Slice(Vector3 sliceVector)
	{
		base.Slice(sliceVector);
		if (liquidSurface != null)
		{
			liquidSurface.SetActive(value: true);
		}
		StartCoroutine(SpillMilk());
	}

	private IEnumerator SpillMilk()
	{
		milkSplatPFX.SetActive(value: true);
		milkPourPFX.SetActive(value: true);
		yield return new WaitForSeconds(0.45f);
		PrintSpillDecals();
		yield return new WaitForSeconds(0.1f);
		if (liquidSurface != null)
		{
			liquidSurface.SetActive(value: false);
		}
	}

	protected virtual void PrintSpillDecals()
	{
		if (actor != null && actor.actorState == Actor.ActorState.Alive)
		{
			return;
		}
		for (int i = 0; i < milkDecalPositions.Length; i++)
		{
			SplatDecal splatDecal = Singleton<DecalManager>.Instance.SpawnSplatDecal(null, new Vector3(milkDecalPositions[i].position.x, milkDecalPositions[i].position.y, -0.022f), milkDecalPositions[i].localRotation, Color.white);
			if (splatDecal != null)
			{
				splatDecal.SetLocalScale(milkDecalPositions[i].localScale);
				splatDecal.SetScaleInDuration(1f);
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && (!Singleton<SettingsManager>.Instance.teamMatch || !Singleton<SettingsManager>.Instance.MatchSettings.teamRevives))
				{
					splatDecal.lifetime = 0f;
				}
			}
		}
	}

	protected override void Faceplant()
	{
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/spill_liquid", base.transform.position);
	}
}
