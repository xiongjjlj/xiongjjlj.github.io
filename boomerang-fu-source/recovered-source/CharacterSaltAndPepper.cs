using System.Collections;
using UnityEngine;

public class CharacterSaltAndPepper : CharacterCoffee
{
	[Header("Salt and Pepper")]
	public Renderer faceplateTwo;

	protected MaterialPropertyBlock faceplatePropertyBlockTwo;

	public GameObject pepperPourPFX;

	public Transform[] pepperDecalPositions;

	public Color pepperLiquidColor;

	protected float pepperDecalOffset = -0.05f;

	protected override void Start()
	{
		base.Start();
		pepperPourPFX.SetActive(value: false);
	}

	public override void Reset()
	{
		base.Reset();
		pepperPourPFX.SetActive(value: false);
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
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/sliced_saltandpepper", base.transform.position);
	}

	protected override void PrintSpillDecals()
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
				splatDecal.SetScaleInDuration(0.6f);
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && (!Singleton<SettingsManager>.Instance.teamMatch || !Singleton<SettingsManager>.Instance.MatchSettings.teamRevives))
				{
					splatDecal.lifetime = 0f;
				}
			}
		}
		for (int j = 0; j < pepperDecalPositions.Length; j++)
		{
			SplatDecal splatDecal2 = Singleton<DecalManager>.Instance.SpawnSplatDecal(null, new Vector3(pepperDecalPositions[j].position.x, pepperDecalPositions[j].position.y, decalOffset + pepperDecalOffset), pepperDecalPositions[j].localRotation, pepperLiquidColor);
			if (splatDecal2 != null)
			{
				splatDecal2.SetLocalScale(pepperDecalPositions[j].localScale);
				splatDecal2.SetScaleInDuration(0.5f);
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && (!Singleton<SettingsManager>.Instance.teamMatch || !Singleton<SettingsManager>.Instance.MatchSettings.teamRevives))
				{
					splatDecal2.lifetime = 0f;
				}
			}
		}
	}

	protected override IEnumerator SpillCoffee()
	{
		pepperPourPFX.SetActive(value: true);
		milkPourPFX.SetActive(value: true);
		yield return new WaitForSeconds(0.05f);
		PrintSpillDecals();
		yield return new WaitForSeconds(0.3f);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/spill_saltandpepper", base.transform.position);
	}

	public override void SetFaceSheetOffset(FacialExpression expression)
	{
		base.SetFaceSheetOffset(expression);
		switch (expression)
		{
		case FacialExpression.content:
			expression = FacialExpression.sad;
			break;
		case FacialExpression.blinkcontent:
			expression = FacialExpression.blinksad;
			break;
		case FacialExpression.happy:
			expression = FacialExpression.blinksad;
			break;
		}
		float num = 4f;
		float num2 = (float)expression % num;
		float num3 = (int)Mathf.Floor((float)expression / num);
		Vector2 vector = new Vector2(num2 / num, (num - 1f) / num - num3 / num);
		if (faceplatePropertyBlockTwo == null)
		{
			faceplatePropertyBlockTwo = new MaterialPropertyBlock();
		}
		faceplatePropertyBlockTwo.SetVector("_MainTex_ST", new Vector4(0.25f, 0.25f, vector.x, vector.y));
		faceplateTwo.SetPropertyBlock(faceplatePropertyBlockTwo);
	}

	protected override void Faceplant()
	{
	}
}
