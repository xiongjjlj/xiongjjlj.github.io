using UnityEngine;

public class ABTester : MonoBehaviour
{
	public GameObject[] AObjects;

	public GameObject[] BObjects;

	private void Start()
	{
		bool flag = Chance.PercentageTrue(50f);
		if (AObjects != null)
		{
			GameObject[] aObjects = AObjects;
			for (int i = 0; i < aObjects.Length; i++)
			{
				aObjects[i].SetActive(flag);
			}
		}
		if (BObjects != null)
		{
			GameObject[] aObjects = BObjects;
			for (int i = 0; i < aObjects.Length; i++)
			{
				aObjects[i].SetActive(!flag);
			}
		}
	}
}
