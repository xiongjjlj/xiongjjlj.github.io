using System;
using Rewired.Data.Mapping;
using UnityEngine;

public class ControllerGlyphs : MonoBehaviour
{
	[Serializable]
	private class ControllerEntry
	{
		public string name;

		public HardwareJoystickMap joystick;

		public GlyphEntry[] glyphs;

		public Sprite GetGlyph(int elementIdentifierId)
		{
			if (glyphs == null)
			{
				return null;
			}
			for (int i = 0; i < glyphs.Length; i++)
			{
				if (glyphs[i] != null && glyphs[i].elementIdentifierId == elementIdentifierId)
				{
					return glyphs[i].GetGlyph();
				}
			}
			return null;
		}
	}

	[Serializable]
	private class ControllerTemplateEntry
	{
		public string name;

		public HardwareJoystickTemplateMap joystick;

		public GlyphEntry[] glyphs;

		public Sprite GetGlyph(int elementIdentifierId)
		{
			if (glyphs == null)
			{
				return null;
			}
			for (int i = 0; i < glyphs.Length; i++)
			{
				if (glyphs[i] != null && glyphs[i].elementIdentifierId == elementIdentifierId)
				{
					return glyphs[i].GetGlyph();
				}
			}
			return null;
		}
	}

	[Serializable]
	private class GlyphEntry
	{
		public int elementIdentifierId;

		public Sprite glyph;

		public Sprite GetGlyph()
		{
			return glyph;
		}
	}

	[Serializable]
	public class KeyboardKeyName
	{
		public KeyCode keyCode;

		public string name = string.Empty;
	}

	[SerializeField]
	private ControllerEntry[] controllers;

	[SerializeField]
	private ControllerTemplateEntry controllerTemplate;

	[SerializeField]
	private KeyboardKeyName[] keyboardKeyNames;

	private static ControllerGlyphs Instance;

	private void Awake()
	{
		Instance = this;
	}

	public static Sprite GetGlyphForJoystick(Guid joystickGuid, int elementIdentifierId)
	{
		if (Instance == null)
		{
			return null;
		}
		if (Instance.controllers == null)
		{
			return null;
		}
		for (int i = 0; i < Instance.controllers.Length; i++)
		{
			if (Instance.controllers[i] != null && !(Instance.controllers[i].joystick == null) && !(Instance.controllers[i].joystick.Guid != joystickGuid))
			{
				return Instance.controllers[i].GetGlyph(elementIdentifierId);
			}
		}
		return null;
	}

	public static Sprite GetGlyphForTemplate(Guid templateGuid, int elementIndentifierId)
	{
		if (Instance == null)
		{
			return null;
		}
		if (Instance.controllerTemplate == null)
		{
			return null;
		}
		return Instance.controllerTemplate.GetGlyph(elementIndentifierId);
	}

	public static bool ContainsJoystick(Guid joystickGuid)
	{
		for (int i = 0; i < Instance.controllers.Length; i++)
		{
			if (Instance.controllers[i] != null && !(Instance.controllers[i].joystick == null) && Instance.controllers[i].joystick.Guid == joystickGuid)
			{
				return true;
			}
		}
		return false;
	}

	public static string GetKeyboardKeyName(KeyCode keyCode)
	{
		if (Instance == null)
		{
			return keyCode.ToString();
		}
		for (int i = 0; i < Instance.keyboardKeyNames.Length; i++)
		{
			if (Instance.keyboardKeyNames[i].keyCode == keyCode)
			{
				return Instance.keyboardKeyNames[i].name;
			}
		}
		return keyCode.ToString();
	}
}
