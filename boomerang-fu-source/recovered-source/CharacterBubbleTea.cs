using System.Collections;
using UnityEngine;

public class CharacterBubbleTea : CharacterMilk
{
	public Color teaLiquidColor;

	protected float decalOffset = -0.024f;

	public GameObject[] hair;

	public DeathSlice[] extraSlices;

	private Vector3[] extraSlicesInitPos;

	private IEnumerator sliceCoroutine;

	public override Color Color
	{
		get
		{
			if (actor == null)
			{
				return colorDefault;
			}
			if (actor != null && actor.team == 1)
			{
				return colorTeamOne;
			}
			if (actor != null && actor.team == 2)
			{
				return colorTeamTwo;
			}
			return colorDefault;
		}
	}

	protected override void PrintSpillDecals()
	{
		if (actor != null && actor.actorState == Actor.ActorState.Alive)
		{
			return;
		}
		for (int i = 0; i < milkDecalPositions.Length; i++)
		{
			SplatDecal splatDecal = Singleton<DecalManager>.Instance.SpawnSplatDecal(null, new Vector3(milkDecalPositions[i].position.x, milkDecalPositions[i].position.y, decalOffset), milkDecalPositions[i].localRotation, Color);
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

	public override void AddHat(HatType targetHatType)
	{
		base.AddHat(targetHatType);
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(!ShouldCurrentHatHideHair());
		}
	}

	public override void RemoveCurrentHat()
	{
		base.RemoveCurrentHat();
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(value: true);
		}
	}

	public override void ShowCurrentHat()
	{
		base.ShowCurrentHat();
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(!ShouldCurrentHatHideHair());
		}
	}

	private bool ShouldCurrentHatHideHair()
	{
		if (_currentHat == null || hair == null)
		{
			return false;
		}
		if (_currentHat.hatType == HatType.Crown || _currentHat.hatType == HatType.None)
		{
			return false;
		}
		if (_currentHat.hatAttachment == Hat.HatAttachment.HatJoint && _currentHat.hideHair)
		{
			return true;
		}
		return false;
	}

	public override void Reset()
	{
		base.Reset();
		for (int i = 0; i < extraSlices.Length; i++)
		{
			extraSlices[i].Reset();
			extraSlices[i].transform.localPosition = extraSlicesInitPos[i];
		}
		if (sliceCoroutine != null)
		{
			StopCoroutine(sliceCoroutine);
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
		for (int j = 0; j < extraSlices.Length; j++)
		{
			extraSlices[j].InitDebris(Color);
			if (actor != null)
			{
				Physics2D.IgnoreCollision(extraSlices[j].GetComponent<Collider2D>(), actor.physicsCollider);
			}
		}
	}

	public override void Slice(Vector3 sliceVector)
	{
		HSBColor hsbColor = HSBColor.FromColor(Color);
		hsbColor.b -= 0.36f;
		Color color = HSBColor.ToColor(hsbColor);
		ParticleSystem component = milkSplatPFX.GetComponent<ParticleSystem>();
		if (component != null)
		{
			ParticleSystem.MainModule main = component.main;
			main.startColor = color;
		}
		ParticleSystem component2 = milkPourPFX.GetComponent<ParticleSystem>();
		if (component2 != null)
		{
			ParticleSystem.MainModule main = component2.main;
			main.startColor = color;
			ParticleSystem[] componentsInChildren = milkPourPFX.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				main = componentsInChildren[i].main;
				main.startColor = color;
			}
		}
		base.Slice(sliceVector);
		if (sliceCoroutine != null)
		{
			StopCoroutine(sliceCoroutine);
		}
		sliceCoroutine = SliceAfterDelay(0.8f);
		StartCoroutine(sliceCoroutine);
	}

	private IEnumerator SliceAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		float angle = 3f;
		Vector3 sliceVector = -actor.transform.up;
		sliceVector = Quaternion.AngleAxis((float)(extraSlices.Length + 2) * angle * -0.5f, Vector3.forward) * sliceVector;
		for (int i = 0; i < extraSlices.Length; i++)
		{
			sliceVector = Quaternion.AngleAxis(angle * (float)(i + 1), Vector3.forward) * sliceVector;
			extraSlices[i].Slice(sliceVector);
			yield return new WaitForSeconds(0.05f);
		}
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
}
