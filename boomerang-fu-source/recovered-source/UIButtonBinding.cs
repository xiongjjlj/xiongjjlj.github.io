using System.Collections;
using I2.Loc;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UIButtonStyle))]
public class UIButtonBinding : UIButton
{
	public enum KeyboardMapType
	{
		KeyboardOne,
		KeyboardTwo
	}

	public enum BindingState
	{
		Assigned,
		Missing,
		Listening
	}

	public TextMeshProUGUI text;

	public string action;

	public int axisContribution;

	public bool doNotAllowSubmitKeybind;

	public KeyboardMapType keyboardMapType;

	private static float maxListenDuration = 5f;

	public BindingState bindingState;

	public UIButtonStyle buttonStyle;

	public bool applyBindingImmediately;

	protected override void Awake()
	{
		base.Awake();
		buttonStyle = GetComponent<UIButtonStyle>();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetTextToCurrentBinding();
	}

	protected override IEnumerator DelayedClickEvent(BaseEventData eventData)
	{
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		if (string.IsNullOrEmpty(action))
		{
			Debug.LogError(action + " input is not defined!");
			yield break;
		}
		text.text = LocalizationManager.GetTranslation("ui_binding_pressanykey");
		Canvas.ForceUpdateCanvases();
		Singleton<UIManager>.Instance.PlayButtonSubmitPFX(GetComponent<RectTransform>(), Singleton<ColorManager>.Instance.buttonDarkSelectedTextColor);
		bindingState = BindingState.Listening;
		if (buttonStyle != null)
		{
			buttonStyle.UpdateButtonColors();
			buttonStyle.Bounce();
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/on");
		}
		yield return StartCoroutine(ListenForKeyRoutine());
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
	}

	private IEnumerator ListenForKeyRoutine()
	{
		Debug.Log("Listening for key down...");
		yield return new WaitForSecondsRealtime(0.1f);
		float timer = maxListenDuration;
		while (timer > 0f)
		{
			timer -= Time.unscaledDeltaTime;
			if (ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Escape) || ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Backspace) || (doNotAllowSubmitKeybind && (ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Return) || ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Space))))
			{
				Debug.Log("User cancelled keyboard binding");
				yield return new WaitForSecondsRealtime(0.1f);
				OnStopped();
				yield break;
			}
			ControllerPollingInfo controllerPollingInfo = ReInput.controllers.Keyboard.PollForFirstKeyDown();
			if (controllerPollingInfo.success)
			{
				BindNewKey(controllerPollingInfo.keyboardKey);
				yield return new WaitForSecondsRealtime(0.1f);
				OnInputMapped();
				yield break;
			}
			yield return null;
		}
		Debug.Log("Keyboard mapping input listener timed out");
		OnStopped();
	}

	private void BindNewKey(KeyCode keyCode)
	{
		KeyBindings keyBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardOneBindings;
		if (keyboardMapType == KeyboardMapType.KeyboardTwo)
		{
			keyBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardTwoBindings;
		}
		RemoveKeyFromAllBindings(Singleton<SettingsManager>.Instance.GameOptions.keyboardOneBindings, keyCode);
		RemoveKeyFromAllBindings(Singleton<SettingsManager>.Instance.GameOptions.keyboardTwoBindings, keyCode);
		switch (action)
		{
		case "Dash":
			keyBindings.Dash = keyCode;
			keyBindings.UISubmit = keyCode;
			break;
		case "Swipe":
			keyBindings.Swipe = keyCode;
			keyBindings.UITabRight = keyCode;
			break;
		case "Throw":
			keyBindings.Throw = keyCode;
			keyBindings.UIRestore = keyCode;
			break;
		case "Back":
			keyBindings.Back = keyCode;
			keyBindings.UICancel = keyCode;
			break;
		case "Move Horizontal":
			if (axisContribution > 0)
			{
				keyBindings.MoveRight = keyCode;
			}
			else
			{
				keyBindings.MoveLeft = keyCode;
			}
			break;
		case "Move Vertical":
			if (axisContribution > 0)
			{
				keyBindings.MoveUp = keyCode;
			}
			else
			{
				keyBindings.MoveDown = keyCode;
			}
			break;
		}
	}

	private void RemoveKeyFromAllBindings(KeyBindings keyBindings, KeyCode keyCode)
	{
		if (keyBindings.Dash == keyCode)
		{
			keyBindings.Dash = KeyCode.None;
		}
		if (keyBindings.Swipe == keyCode)
		{
			keyBindings.Swipe = KeyCode.None;
		}
		if (keyBindings.Throw == keyCode)
		{
			keyBindings.Throw = KeyCode.None;
		}
		if (keyBindings.Back == keyCode)
		{
			keyBindings.Back = KeyCode.None;
		}
		if (keyBindings.MoveRight == keyCode)
		{
			keyBindings.MoveRight = KeyCode.None;
		}
		if (keyBindings.MoveLeft == keyCode)
		{
			keyBindings.MoveLeft = KeyCode.None;
		}
		if (keyBindings.MoveUp == keyCode)
		{
			keyBindings.MoveUp = KeyCode.None;
		}
		if (keyBindings.MoveDown == keyCode)
		{
			keyBindings.MoveDown = KeyCode.None;
		}
		if (keyBindings.UISubmit == keyCode)
		{
			keyBindings.UISubmit = KeyCode.None;
		}
		if (keyBindings.UICancel == keyCode)
		{
			keyBindings.UICancel = KeyCode.None;
		}
		if (keyBindings.UITabLeft == keyCode)
		{
			keyBindings.UITabLeft = KeyCode.None;
		}
		if (keyBindings.UITabRight == keyCode)
		{
			keyBindings.UITabRight = KeyCode.None;
		}
		if (keyBindings.UIRestore == keyCode)
		{
			keyBindings.UIRestore = KeyCode.None;
		}
	}

	public void SetTextToCurrentBinding()
	{
		KeyBindings keyBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardOneBindings;
		if (keyboardMapType == KeyboardMapType.KeyboardTwo)
		{
			keyBindings = Singleton<SettingsManager>.Instance.GameOptions.keyboardTwoBindings;
		}
		KeyCode keyCode = KeyCode.None;
		switch (action)
		{
		case "Dash":
			keyCode = keyBindings.Dash;
			break;
		case "Swipe":
			keyCode = keyBindings.Swipe;
			break;
		case "Throw":
			keyCode = keyBindings.Throw;
			break;
		case "Back":
			keyCode = keyBindings.Back;
			break;
		case "Move Horizontal":
			keyCode = ((axisContribution <= 0) ? keyBindings.MoveLeft : keyBindings.MoveRight);
			break;
		case "Move Vertical":
			keyCode = ((axisContribution <= 0) ? keyBindings.MoveDown : keyBindings.MoveUp);
			break;
		}
		if (keyCode == KeyCode.None)
		{
			text.text = LocalizationManager.GetTranslation("ui_binding_missing");
			bindingState = BindingState.Missing;
		}
		else
		{
			text.text = ControllerGlyphs.GetKeyboardKeyName(keyCode);
			bindingState = BindingState.Assigned;
		}
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		bindingState = BindingState.Listening;
		base.OnSubmit(eventData);
	}

	private void OnInputMapped()
	{
		if (applyBindingImmediately)
		{
			Singleton<SettingsManager>.Instance.ApplyKeyBindings();
		}
		UIMenuOptions uIMenuOptions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Options) as UIMenuOptions;
		if (uIMenuOptions != null)
		{
			uIMenuOptions.RefreshAllBindingButtonLabels();
		}
		if (buttonStyle != null)
		{
			buttonStyle.UpdateButtonColors();
			buttonStyle.Bounce();
			Canvas.ForceUpdateCanvases();
			Singleton<UIManager>.Instance.PlayButtonSubmitPFX(GetComponent<RectTransform>(), Singleton<ColorManager>.Instance.buttonSubmitColor);
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/on");
	}

	private void OnStopped()
	{
		SetTextToCurrentBinding();
		if (buttonStyle != null)
		{
			buttonStyle.UpdateButtonColors();
			buttonStyle.Bounce();
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/off");
	}
}
