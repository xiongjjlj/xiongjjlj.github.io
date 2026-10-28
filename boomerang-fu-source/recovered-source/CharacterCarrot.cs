using UnityEngine;

public class CharacterCarrot : CharacterPlayer
{
	public DeathSlice[] extraSlices;

	private Vector3[] extraSlicesInitPos;

	public GameObject[] hair;

	public override bool IsSliced
	{
		get
		{
			return base.IsSliced;
		}
		set
		{
			base.IsSliced = value;
			for (int i = 0; i < bodyNormal.Length; i++)
			{
				bodyNormal[i].SetActive(!value);
			}
			for (int j = 0; j < bodySliced.Length; j++)
			{
				bodySliced[j].SetActive(value);
			}
			if (!value && ShouldCurrentHatHideHair())
			{
				ShowCurrentHat();
			}
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
		base.Slice(sliceVector);
		SliceExtraSlices(sliceVector);
	}

	protected virtual void SliceExtraSlices(Vector3 sliceVector)
	{
		float num = (Chance.PercentageTrue(50f) ? 5f : (-5f));
		for (int i = 0; i < extraSlices.Length; i++)
		{
			sliceVector = Quaternion.AngleAxis(num * (float)(i + 1), Vector3.forward) * sliceVector;
			extraSlices[i].Slice(sliceVector);
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
}
