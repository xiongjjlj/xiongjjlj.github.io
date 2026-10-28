using UnityEngine;

public class CharacterBanana : CharacterPlayer
{
	public DeathSlice[] extraSlices;

	private Vector3[] extraSlicesInitPos;

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
		if (!(actor != null))
		{
			return;
		}
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
		float num = (Chance.PercentageTrue(50f) ? 5f : (-5f));
		for (int i = 0; i < extraSlices.Length - ((!Chance.PercentageTrue(50f)) ? 1 : 0); i++)
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
}
