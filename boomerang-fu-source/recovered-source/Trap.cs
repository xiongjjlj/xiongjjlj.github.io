using System.Collections;
using UnityEngine;

public class Trap : MonoBehaviour
{
	private Animator animator;

	private bool isSet = true;

	private WaitForSeconds chargeDelay = new WaitForSeconds(0.75f);

	private WaitForSeconds attackDelay = new WaitForSeconds(0.25f);

	private WaitForSeconds cooldownDelay = new WaitForSeconds(0.5f);

	private void Start()
	{
		animator = GetComponent<Animator>();
	}

	private void OnTriggerStay2D()
	{
		if (isSet)
		{
			isSet = false;
			animator.SetTrigger("Charge");
			StartCoroutine(Attack());
		}
	}

	private IEnumerator Attack()
	{
		yield return chargeDelay;
		animator.SetTrigger("Attack");
		base.gameObject.layer = LayerMask.NameToLayer("Hazard");
		yield return attackDelay;
		base.gameObject.layer = LayerMask.NameToLayer("Trap");
		yield return cooldownDelay;
		isSet = true;
	}
}
