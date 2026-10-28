using UnityEngine;

public class SetActiveTester : MonoBehaviour
{
	private void OnEnable()
	{
		Debug.Log("Tester ON");
	}

	private void OnDisable()
	{
		Debug.Log("Tester OFF");
	}
}
