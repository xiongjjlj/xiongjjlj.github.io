using UnityEngine;

public class CharacterChocolate : CharacterCarrot
{
	protected override void SliceExtraSlices(Vector3 sliceVector)
	{
		for (int i = 0; i < extraSlices.Length; i++)
		{
			if (i == 1)
			{
				sliceVector = Quaternion.AngleAxis(-5f * (float)(i + 1), Vector3.forward) * sliceVector;
			}
			extraSlices[i].Slice(sliceVector);
		}
	}
}
