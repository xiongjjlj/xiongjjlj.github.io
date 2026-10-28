using System.Collections;
using UnityEngine;

public class CharacterCoffee : CharacterPlayer
{
	[Header("Death Effects")]
	public DeathSlice[] extraSlices;

	public Vector2[] sliceVectors;

	private Vector3[] extraSlicesInitPos;

	public GameObject milkPourPFX;

	public GameObject milkSplatPFX;

	public Transform[] milkDecalPositions;

	public Color coffeeLiquidColor;

	protected float decalOffset = -0.02f;

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
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/sliced_coffee", base.transform.position);
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
				return coffeeLiquidColor;
			}
			return base.Color;
		}
	}

	protected virtual void Start()
	{
		milkPourPFX.SetActive(value: false);
		if (milkSplatPFX != null)
		{
			milkSplatPFX.SetActive(value: false);
		}
	}

	public override void Reset()
	{
		base.Reset();
		for (int i = 0; i < extraSlices.Length; i++)
		{
			extraSlices[i].Reset();
			extraSlices[i].transform.localPosition = extraSlicesInitPos[i];
		}
		milkPourPFX.SetActive(value: false);
		if (milkSplatPFX != null)
		{
			milkSplatPFX.SetActive(value: false);
		}
	}

	public override void InitCharacter(CharacterMode mode)
	{
		extraSlicesInitPos = new Vector3[extraSlices.Length];
		for (int i = 0; i < extraSlices.Length; i++)
		{
			extraSlicesInitPos[i] = extraSlices[i].transform.localPosition;
		}
		base.InitCharacter(mode);
		if (actor != null)
		{
			for (int j = 0; j < extraSlices.Length; j++)
			{
				extraSlices[j].InitDebris(Color);
				Physics2D.IgnoreCollision(extraSlices[j].GetComponent<Collider2D>(), actor.physicsCollider);
			}
		}
	}

	public override void Slice(Vector3 sliceVector)
	{
		sliceVector = base.transform.TransformDirection(sliceVectors[0]);
		base.Slice(sliceVector);
		Chance.PercentageTrue(50f);
		for (int i = 0; i < extraSlices.Length; i++)
		{
			extraSlices[i].Slice(base.transform.TransformDirection(VectorExt.RotatePointAroundPivot(sliceVectors[i + 1], Vector3.zero, new Vector3(0f, 0f, Random.Range(-15f, 15f)))));
		}
		StartCoroutine(SpillCoffee());
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/sliced_coffee", base.transform.position);
	}

	public override void EvaporateSlice()
	{
		base.EvaporateSlice();
		for (int i = 0; i < extraSlices.Length; i++)
		{
			if (extraSlices[i].gameObject.activeSelf)
			{
				Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SpawnBurst, extraSlices[i].transform.position);
			}
		}
	}

	protected virtual IEnumerator SpillCoffee()
	{
		if (milkSplatPFX != null)
		{
			milkSplatPFX.SetActive(value: true);
		}
		milkPourPFX.SetActive(value: true);
		yield return new WaitForSeconds(0.05f);
		PrintSpillDecals();
	}

	protected override void Faceplant()
	{
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/spill_liquid", base.transform.position);
	}

	protected virtual void PrintSpillDecals()
	{
		if (actor != null && actor.actorState == Actor.ActorState.Alive)
		{
			return;
		}
		for (int i = 0; i < milkDecalPositions.Length; i++)
		{
			SplatDecal splatDecal = Singleton<DecalManager>.Instance.SpawnSplatDecal(null, new Vector3(milkDecalPositions[i].position.x, milkDecalPositions[i].position.y, decalOffset), milkDecalPositions[i].localRotation, coffeeLiquidColor);
			if (splatDecal != null)
			{
				splatDecal.SetLocalScale(milkDecalPositions[i].localScale);
				splatDecal.SetScaleInDuration(0.85f);
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && (!Singleton<SettingsManager>.Instance.teamMatch || !Singleton<SettingsManager>.Instance.MatchSettings.teamRevives))
				{
					splatDecal.lifetime = 0f;
				}
			}
		}
	}
}
