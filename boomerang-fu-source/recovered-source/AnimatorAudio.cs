using UnityEngine;

public class AnimatorAudio : MonoBehaviour
{
	public void PlaySound(string eventName)
	{
		Singleton<AudioManager>.Instance.PlayOneShot(eventName);
	}
}
