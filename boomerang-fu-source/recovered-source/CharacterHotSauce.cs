using UnityEngine;

public class CharacterHotSauce : CharacterCoffee
{
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
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/sliced_hotsauce", base.transform.position);
	}

	protected override void PrintSpillDecals()
	{
		decalOffset = -0.26f;
		base.PrintSpillDecals();
	}
}
