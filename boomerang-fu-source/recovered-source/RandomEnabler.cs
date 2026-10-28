using UnityEngine;

public class RandomEnabler : MonoBehaviour
{
	public GameObject[] gameObjects;

	private void OnEnable()
	{
		int num = Random.Range(0, gameObjects.Length);
		for (int i = 0; i < gameObjects.Length; i++)
		{
			gameObjects[i].SetActive(i == num);
		}
	}
}
