using UnityEngine;

public class PowerupIcon : MonoBehaviour
{
	public PowerupType powerupType;

	public Animator anim;

	public void Show()
	{
		anim.SetTrigger("Show");
	}

	public void Hide()
	{
		anim.SetTrigger("Hide");
	}
}
