using System;
using UnityEngine;

[Serializable]
public class GameOptions
{
	public string language;

	public bool secondKeyboardAllowed;

	public bool recordReplays;

	public bool vibrationEnabled;

	public bool screenShakeEnabled;

	public bool freezeFramesEnabled;

	public bool conventionMode;

	public bool showButtonHints;

	public bool showRoundTips;

	public bool xinput;

	public bool enableSlowMo;

	public int graphicsQuality;

	public int graphicsVsync;

	public int graphicsResolutionWidth;

	public int graphicsResolutionHeight;

	public FullScreenMode graphicsFullscreen;

	public int sfxVolume;

	public int musicVolume;

	public KeyBindings keyboardOneBindings;

	public KeyBindings keyboardTwoBindings;

	public bool hideDLCOneAnnouncement;

	public bool botsWearHats;

	public bool hideDLCTwoAnnouncement;

	public GameOptions()
	{
		language = "";
		secondKeyboardAllowed = false;
		xinput = true;
		vibrationEnabled = true;
		screenShakeEnabled = true;
		freezeFramesEnabled = true;
		conventionMode = false;
		showButtonHints = true;
		showRoundTips = false;
		recordReplays = true;
		enableSlowMo = true;
		graphicsResolutionWidth = 0;
		graphicsResolutionHeight = 0;
		graphicsQuality = 1;
		graphicsVsync = 1;
		graphicsFullscreen = FullScreenMode.FullScreenWindow;
		sfxVolume = 10;
		musicVolume = 10;
		keyboardOneBindings = new KeyBindings();
		keyboardTwoBindings = new KeyBindings();
		hideDLCOneAnnouncement = false;
		hideDLCTwoAnnouncement = false;
		botsWearHats = false;
	}
}
