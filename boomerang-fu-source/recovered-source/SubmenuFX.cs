using System.Collections;
using UnityEngine;

public class SubmenuFX : MonoBehaviour
{
	private SpellerText[] spellerTexts;

	private WaitForSeconds spellerStringDelay = new WaitForSeconds(0.125f);

	private IEnumerator spellerRoutine;

	public void StartFX()
	{
		spellerRoutine = SpellText();
		spellerTexts = GetComponentsInChildren<SpellerText>();
		StartCoroutine(spellerRoutine);
	}

	private IEnumerator SpellText()
	{
		SpellerText[] array = spellerTexts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init();
		}
		SpellerText[] array2 = spellerTexts;
		foreach (SpellerText obj in array2)
		{
			obj.StartCoroutine(obj.TypeText());
			yield return spellerStringDelay;
		}
	}
}
