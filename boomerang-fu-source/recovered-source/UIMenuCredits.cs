public class UIMenuCredits : UIMenuBase
{
	private void Update()
	{
		if (Singleton<UIManager>.Instance.GetButtonDownFromEventSystem("UICancel"))
		{
			Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
			UIResponse_Close();
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/back");
		}
	}
}
