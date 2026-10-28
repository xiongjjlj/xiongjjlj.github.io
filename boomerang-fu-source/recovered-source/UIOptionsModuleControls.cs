public class UIOptionsModuleControls : UIOptionsModule
{
	private UIButtonBinding[] bindingButtons;

	private void Start()
	{
		bindingButtons = GetComponentsInChildren<UIButtonBinding>();
	}

	public override void Enable()
	{
		base.Enable();
		RefreshAllBindingButtonLabels();
	}

	private void Update()
	{
		if (Singleton<UIManager>.Instance.GetButtonDownFromEventSystem("UICancel"))
		{
			Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.03f);
			UIMenuOptions uIMenuOptions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Options) as UIMenuOptions;
			if (uIMenuOptions != null)
			{
				uIMenuOptions.UIResponse_GoToOptionsModuleHome();
			}
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/back");
			Singleton<SettingsManager>.Instance.ApplyKeyBindings();
		}
	}

	public void RefreshAllBindingButtonLabels()
	{
		for (int i = 0; i < bindingButtons.Length; i++)
		{
			bindingButtons[i].SetTextToCurrentBinding();
			if (bindingButtons[i].buttonStyle != null)
			{
				bindingButtons[i].buttonStyle.UpdateButtonColors();
			}
		}
	}
}
