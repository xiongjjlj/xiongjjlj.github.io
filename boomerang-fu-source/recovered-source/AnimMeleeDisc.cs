using UnityEngine;

public class AnimMeleeDisc : MonoBehaviour
{
	public Transform parentHand;

	private void Update()
	{
		base.transform.position = parentHand.position;
		base.transform.rotation = Quaternion.Euler(base.transform.rotation.x, base.transform.rotation.y, parentHand.rotation.z);
	}
}
