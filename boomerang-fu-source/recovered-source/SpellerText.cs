using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpellerText : MonoBehaviour
{
	private float letterTime = 0.25f;

	private string message;

	private Text text;

	private WaitForSeconds letterPause;

	public void Init()
	{
		text = GetComponent<Text>();
		letterPause = new WaitForSeconds(letterTime / (float)text.text.Length);
		message = text.text;
		text.text = "";
	}

	public IEnumerator TypeText()
	{
		float i = 0f;
		char[] array = message.ToCharArray();
		foreach (char c in array)
		{
			i++;
			text.text += c;
			yield return letterPause;
		}
	}
}
