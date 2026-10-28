using System;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
	private const string optionsFilename = "options.json";

	private const string modifiersFilename = "modifiers.json";

	private const string unlockedItemsFilename = "save.data";

	private PlatformData platformData;

	private void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public void LoadPlatformData()
	{
		platformData = new PlatformData();
	}

	public void SaveOptionsToDisk(GameOptions options, Action<bool> OnOptionsSaved)
	{
		if (options == null)
		{
			Debug.Log("GameOptions is empty, creating new instance");
			options = new GameOptions();
		}
		platformData.SaveJSON("options.json", options, OnOptionsSaved);
	}

	public void LoadOptionsFromDisk(Action<GameOptions> OnOptionsLoaded)
	{
		if (platformData.FileExists("options.json"))
		{
			platformData.LoadJSON("options.json", OnOptionsLoaded);
			return;
		}
		Debug.Log("Options file not found");
		OnOptionsLoaded(null);
	}

	public void DeleteOptionsFromDisk()
	{
		if (platformData == null)
		{
			LoadPlatformData();
		}
		platformData.DeleteFile("options.json");
	}

	public GameOptions RestoreDefaultOptions()
	{
		return new GameOptions();
	}

	internal void LoadOptionsFromDisk(object onOptionsLoaded)
	{
		throw new NotImplementedException();
	}

	public void SaveMatchSettingsToDisk(MatchSettings matchSettings, Action<bool> OnMatchSettingsLoaded)
	{
		if (matchSettings == null)
		{
			Debug.Log("MatchSettings is empty, creating new instance");
			matchSettings = new MatchSettings();
		}
		platformData.SaveJSON("modifiers.json", matchSettings, OnMatchSettingsLoaded);
	}

	public void LoadMatchSettingsFromDisk(Action<MatchSettings> OnMatchSettingsLoaded)
	{
		if (platformData.FileExists("modifiers.json"))
		{
			platformData.LoadJSON("modifiers.json", OnMatchSettingsLoaded);
			return;
		}
		Debug.Log("Custom match settings file not found");
		OnMatchSettingsLoaded(null);
	}

	public void DeleteMatchSettingsFromDisk()
	{
		platformData.DeleteFile("modifiers.json");
	}

	public void SaveUnlockedItemsToDisk(UnlockedItems unlockedItems, Action<bool> OnUnlockedItemsSaved)
	{
		if (unlockedItems == null)
		{
			Debug.Log("CRITICAL ERROR - Unlocked items data is empty, creating new instance");
			unlockedItems = new UnlockedItems();
		}
		platformData.SaveJSON("save.data", unlockedItems, OnUnlockedItemsSaved);
	}

	public void LoadUnlockedItemsFromDisk(Action<UnlockedItems> OnUnlockedItemsLoaded)
	{
		if (platformData.FileExists("save.data"))
		{
			platformData.LoadJSON("save.data", OnUnlockedItemsLoaded);
			return;
		}
		Debug.Log("WARNING - unlocked items file has NOT been found");
		OnUnlockedItemsLoaded(null);
	}

	public void DeleteUnlockedItemsFromDisk()
	{
		platformData.DeleteFile("save.data");
	}
}
