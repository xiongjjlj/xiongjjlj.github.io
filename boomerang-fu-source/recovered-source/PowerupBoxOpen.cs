using System.Collections;
using UnityEngine;

public class PowerupBoxOpen : MonoBehaviour
{
	private void OnEnable()
	{
		StartCoroutine(Hide());
	}

	private IEnumerator Hide()
	{
		yield return new WaitForSeconds(1f);
		base.gameObject.Recycle();
	}
}
