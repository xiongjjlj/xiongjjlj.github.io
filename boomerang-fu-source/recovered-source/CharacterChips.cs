using System.Collections;
using UnityEngine;

public class CharacterChips : CharacterPlayer
{
	public GameObject chipsNormal;

	public DeathSlice[] extraSlices;

	private Vector3[] extraSlicesInitPos;

	public ParticleSystem slicedPFX;

	private IEnumerator sliceCoroutine;

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
		chipsNormal.SetActive(value: true);
		slicedPFX.Clear();
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
		if (actor != null && actor.actorState == Actor.ActorState.Alive)
		{
			anim.SetBool("IsSliced", value: true);
		}
		IsSliced = true;
		LoseCurrentHat(sliceVector);
		StartCoroutine(DelayFaceplant());
		if (sliceCoroutine != null)
		{
			StopCoroutine(sliceCoroutine);
		}
		sliceCoroutine = SliceAfterDelay(0.9f);
		StartCoroutine(sliceCoroutine);
		slicedPFX.Play();
	}

	private IEnumerator SliceAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		chipsNormal.SetActive(value: false);
		Vector3 sliceDirection = -actor.transform.up;
		for (int i = 0; i < extraSlices.Length; i++)
		{
			extraSlices[i].Slice(sliceDirection);
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
