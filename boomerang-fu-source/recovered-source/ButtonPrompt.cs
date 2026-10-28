using System.Collections.Generic;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonPrompt : MonoBehaviour
{
	public string action;

	public Image glyph;

	public GameObject key;

	public TextMeshProUGUI keyText;

	public bool invertColours;

	public int axisContribution;

	private void Start()
	{
		if (invertColours)
		{
			Color color = keyText.color;
			keyText.color = key.GetComponent<Image>().color;
			key.GetComponent<Image>().color = color;
		}
	}

	public virtual void Init(Rewired.Player rewiredPlayer, string action = null)
	{
		if (action == null)
		{
			action = this.action;
		}
		if (Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(rewiredPlayer))
		{
			if (glyph.gameObject != base.gameObject)
			{
				glyph.gameObject.SetActive(value: true);
			}
			else
			{
				glyph.enabled = true;
			}
			if (key != null)
			{
				key.SetActive(value: false);
			}
			ActionElementMap firstElementMapWithAction = rewiredPlayer.controllers.maps.GetFirstElementMapWithAction(rewiredPlayer.controllers.Joysticks[0], action, skipDisabledMaps: true);
			if (firstElementMapWithAction != null)
			{
				Sprite sprite = null;
				if (ControllerGlyphs.ContainsJoystick(rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid))
				{
					sprite = ControllerGlyphs.GetGlyphForJoystick(rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid, firstElementMapWithAction.elementIdentifierId);
				}
				else
				{
					Debug.Log("Joystick hardware ID not found for " + rewiredPlayer.controllers.Joysticks[0].hardwareName + " with guid " + rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid.ToString());
					if (rewiredPlayer.controllers.Joysticks[0].templateCount != 0)
					{
						IControllerTemplate controllerTemplate = rewiredPlayer.controllers.Joysticks[0].Templates[0];
						List<ControllerTemplateElementTarget> list = new List<ControllerTemplateElementTarget>();
						if (controllerTemplate.GetElementTargets(firstElementMapWithAction, list) == 0)
						{
							return;
						}
						ControllerTemplateElementTarget controllerTemplateElementTarget = list[0];
						sprite = ControllerGlyphs.GetGlyphForTemplate(controllerTemplate.typeGuid, controllerTemplateElementTarget.element.id);
					}
				}
				if (sprite != null)
				{
					glyph.sprite = sprite;
				}
				else
				{
					Debug.LogError("Gylph not found for " + rewiredPlayer.controllers.Joysticks[0].hardwareName);
				}
			}
			else
			{
				Debug.LogError(rewiredPlayer.controllers.Joysticks[0].hardwareName + " not recognised");
			}
			return;
		}
		if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC)
		{
			Debug.LogWarning("Button prompt tried to display a keyboard glyph for a player with no controller on console platform!", base.gameObject);
			return;
		}
		if (glyph != null)
		{
			if (glyph.gameObject != base.gameObject)
			{
				glyph.gameObject.SetActive(value: false);
			}
			else
			{
				glyph.enabled = false;
			}
		}
		key.SetActive(value: true);
		string text = string.Empty;
		if (axisContribution == 0)
		{
			if (rewiredPlayer.controllers.maps.GetFirstElementMapWithAction(ControllerType.Keyboard, action, skipDisabledMaps: true) == null)
			{
				return;
			}
			text = ControllerGlyphs.GetKeyboardKeyName(rewiredPlayer.controllers.maps.GetFirstElementMapWithAction(ControllerType.Keyboard, action, skipDisabledMaps: true).keyCode).ToUpper();
		}
		else
		{
			foreach (ActionElementMap item in rewiredPlayer.controllers.maps.ButtonMapsWithAction(ControllerType.Keyboard, action, skipDisabledMaps: true))
			{
				if (axisContribution > 0 && item.axisContribution == Pole.Positive)
				{
					text = ControllerGlyphs.GetKeyboardKeyName(item.keyCode).ToUpper();
					break;
				}
				if (axisContribution < 0 && item.axisContribution == Pole.Negative)
				{
					text = ControllerGlyphs.GetKeyboardKeyName(item.keyCode).ToUpper();
					break;
				}
			}
		}
		keyText.GetComponent<RectTransform>();
		keyText.text = text;
	}
}
