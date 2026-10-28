using UnityEngine;

public class AnimatorFootsteps : MonoBehaviour
{
	private Character character;

	private void Start()
	{
		character = GetComponentInParent<Character>();
	}

	public void Footstep()
	{
		if (character != null && character.actor != null)
		{
			character.actor.EmitFootstep();
		}
	}
}
