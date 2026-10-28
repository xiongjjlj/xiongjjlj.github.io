using UnityEngine;

public class CharacterGrapes : CharacterCarrot
{
	protected override void SliceExtraSlices(Vector3 sliceVector)
	{
		float[] array = new float[3] { 0f, 120f, 240f };
		for (int i = 0; i < extraSlices.Length; i++)
		{
			sliceVector = Quaternion.AngleAxis(array[i], Vector3.forward) * -actor.transform.up;
			extraSlices[i].Slice(sliceVector);
		}
	}
}
